using System.Text.RegularExpressions;

namespace PanoramicData.Render;

internal static partial class FountainScriptParser
{
	public static FountainScript Parse(string source, CancellationToken cancellationToken)
	{
		var text = source.TrimStart('\uFEFF').ReplaceLineEndings("\n");
		text = Boneyard().Replace(text, string.Empty);
		text = Notes().Replace(text, string.Empty);
		var lines = text.Split('\n');
		var title = ReadTitle(lines, out var index);
		var blocks = new List<FountainBlock>();

		while (index < lines.Length)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (IsOmitted(lines[index].Trim()))
			{
				index++;
				continue;
			}

			if (TryReadDialogue(lines, ref index, out var dialogue))
			{
				blocks.Add(dialogue);
				continue;
			}

			blocks.Add(ReadBodyBlock(lines, ref index));
		}

		return new FountainScript(title, blocks);
	}

	private static bool IsOmitted(string value)
	{
		return value.Length == 0 || value.StartsWith('#') || value.StartsWith('=') && !PageBreak().IsMatch(value);
	}

	private static FountainBlock ReadBodyBlock(string[] lines, ref int index)
	{
		var block = ParseLine(lines[index], index == 0 || IsBoundary(lines[index - 1]), index == lines.Length - 1 || IsBoundary(lines[index + 1]));
		index++;
		if (block.Kind != FountainBlockKind.Action)
		{
			return block;
		}

		// An explicit source newline is meaningful within an action paragraph.
		var paragraph = new List<string> { block.Text };
		while (index < lines.Length && !IsBoundary(lines[index]))
		{
			paragraph.Add(lines[index++].TrimEnd());
		}

		return block with { Text = string.Join('\n', paragraph) };
	}

	private static Dictionary<string, string> ReadTitle(string[] lines, out int index)
	{
		var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		string? key = null;
		index = 0;
		while (index < lines.Length)
		{
			var line = lines[index];
			if (line.Length == 0)
			{
				break;
			}

			if (key != null && IsTitleContinuation(line))
			{
				values[key] = values[key].Length == 0 ? line.Trim() : $"{values[key]}\n{line.Trim()}";
				index++;
				continue;
			}

			var match = ReadTitleField(line, index);
			if (match is null)
			{
				break;
			}

			key = match.Groups[1].Value.Trim();
			values[key] = match.Groups[2].Value.Trim();
			index++;
		}

		return values;
	}

	private static bool IsTitleContinuation(string line)
	{
		return line.StartsWith('\t') || line.StartsWith("   ", StringComparison.Ordinal);
	}

	private static Match? ReadTitleField(string line, int index)
	{
		// In a script with no title page, an initial transition is a body element.
		var match = TitleField().Match(line);
		return match.Success && (index != 0 || !IsTransition(line.Trim())) ? match : null;
	}

	private static bool TryReadDialogue(string[] lines, ref int index, out FountainBlock dialogue)
	{
		dialogue = null!;
		var cue = lines[index].Trim();
		if (cue.StartsWith('!') || index + 1 >= lines.Length || IsBoundary(lines[index + 1]))
		{
			return false;
		}

		var dual = cue.EndsWith('^');
		if (dual)
		{
			cue = cue[..^1].TrimEnd();
		}

		cue = ReadCharacterCue(lines, index, cue);
		if (cue.Length == 0)
		{
			return false;
		}

		dialogue = new FountainBlock(FountainBlockKind.Dialogue, cue, ReadSpeech(lines, ref index), dual);
		return true;
	}

	private static List<FountainSpeechLine> ReadSpeech(string[] lines, ref int index)
	{
		var speech = new List<FountainSpeechLine>();
		index++;
		while (index < lines.Length && !IsBoundary(lines[index]))
		{
			var line = lines[index++].Trim();
			speech.Add(new FountainSpeechLine(line, line.StartsWith('(') && line.EndsWith(')')));
		}

		return speech;
	}

	private static string ReadCharacterCue(string[] lines, int index, string cue)
	{
		if (cue.StartsWith('@'))
		{
			return cue[1..].Trim();
		}

		var beginsBlock = index == 0 || IsBoundary(lines[index - 1]);
		return beginsBlock && IsUpperCue(cue) && !SceneHeading().IsMatch(cue) && !IsTransition(cue) && !cue.StartsWith('>')
			? cue
			: string.Empty;
	}

	private static bool IsUpperCue(string cue)
	{
		var extension = cue.IndexOf('(');
		var name = extension < 0 ? cue : cue[..extension];
		return name.Any(char.IsUpper) && !name.Any(char.IsLower)
			&& char.IsLetterOrDigit(name[0]);
	}

	private static FountainBlock ParseLine(string line, bool startsBlock, bool endsBlock)
	{
		var value = line.Trim();
		return value switch
		{
			_ when PageBreak().IsMatch(value) => new(FountainBlockKind.PageBreak, string.Empty),
			_ when value.StartsWith('!') => new(FountainBlockKind.Action, value[1..]),
			_ when value.StartsWith('>') && value.EndsWith('<') => new(FountainBlockKind.Centered, value[1..^1].Trim()),
			_ when value.StartsWith('>') => new(FountainBlockKind.Transition, value[1..].Trim()),
			_ when value.StartsWith('~') => new(FountainBlockKind.Lyric, value[1..]),
			_ when startsBlock && endsBlock && (SceneHeading().IsMatch(value) || value.StartsWith('.') && !value.StartsWith("..", StringComparison.Ordinal)) =>
				new(FountainBlockKind.Scene, SceneNumber().Replace(value.TrimStart('.'), string.Empty).TrimEnd()),
			_ when startsBlock && endsBlock && IsTransition(value) => new(FountainBlockKind.Transition, value),
			_ => new(FountainBlockKind.Action, line.TrimEnd())
		};
	}

	private static bool IsBoundary(string line) => (line != "  " && string.IsNullOrWhiteSpace(line))
		|| line.TrimStart() is ['#' or '=' or '~' or '>', ..];

	private static bool IsTransition(string line) =>
		!line.Any(char.IsLower) && (line.EndsWith("TO:", StringComparison.Ordinal)
			|| line is "FADE IN:" or "FADE OUT:" or "FADE OUT." or "CUT TO BLACK.");

	[GeneratedRegex(@"(?:/\*).*?(?:\*/|\z)", RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
	private static partial Regex Boneyard();

	[GeneratedRegex(@"\[\[.*?\]\]", RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
	private static partial Regex Notes();

	[GeneratedRegex(@"^([A-Za-z][\w /'-]*):\s*(.*)$", RegexOptions.CultureInvariant)]
	private static partial Regex TitleField();

	[GeneratedRegex(@"^={3,}$", RegexOptions.CultureInvariant)]
	private static partial Regex PageBreak();

	[GeneratedRegex(@"^(?:INT[./]|EXT[./]|EST\.|I/E\.)(?:\s|/|[A-Z])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex SceneHeading();

	[GeneratedRegex(@"\s+#[^#]+#\s*$", RegexOptions.CultureInvariant)]
	private static partial Regex SceneNumber();
}
