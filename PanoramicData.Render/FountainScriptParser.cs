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
			var line = lines[index];
			var value = line.Trim();
			if (value.Length == 0 || value.StartsWith('#') || value.StartsWith('=') && !PageBreak().IsMatch(value))
			{
				index++;
				continue;
			}

			if (TryReadDialogue(lines, ref index, out var dialogue))
			{
				blocks.Add(dialogue);
				continue;
			}

			var block = ParseLine(line, index == 0 || IsBoundary(lines[index - 1]), index == lines.Length - 1 || IsBoundary(lines[index + 1]));
			index++;
			if (block.Kind is FountainBlockKind.Action)
			{
				// An explicit source newline is meaningful within an action paragraph.
				var paragraph = new List<string> { block.Text };
				while (index < lines.Length && !IsBoundary(lines[index]))
				{
					paragraph.Add(lines[index++].TrimEnd());
				}

				block = block with { Text = string.Join('\n', paragraph) };
			}

			blocks.Add(block);
		}

		return new FountainScript(title, blocks);
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

			if (key != null && (line.StartsWith('\t') || line.StartsWith("   ", StringComparison.Ordinal)))
			{
				values[key] = values[key].Length == 0 ? line.Trim() : $"{values[key]}\n{line.Trim()}";
				index++;
				continue;
			}

			var match = TitleField().Match(line);
			// In a script with no title page, an initial transition is a body element.
			if (!match.Success || index == 0 && IsTransition(line.Trim()))
			{
				break;
			}

			key = match.Groups[1].Value.Trim();
			values[key] = MatchValue(match);
			index++;
		}

		return values;
	}

	private static string MatchValue(Match field) => field.Groups[2].Value.Trim();

	private static bool TryReadDialogue(string[] lines, ref int index, out FountainBlock dialogue)
	{
		dialogue = null!;
		var cue = lines[index].Trim();
		if (cue.StartsWith('!') || index + 1 >= lines.Length || IsBoundary(lines[index + 1]))
		{
			return false;
		}

		var forced = cue.StartsWith('@');
		var dual = cue.EndsWith('^');
		if (dual)
		{
			cue = cue[..^1].TrimEnd();
		}

		if (forced)
		{
			cue = cue[1..].Trim();
		}
		else if (index > 0 && !IsBoundary(lines[index - 1]) || !IsUpperCue(cue) || SceneHeading().IsMatch(cue) || IsTransition(cue) || cue.StartsWith('>'))
		{
			return false;
		}

		if (cue.Length == 0)
		{
			return false;
		}

		var speech = new List<FountainSpeechLine>();
		index++;
		while (index < lines.Length && !IsBoundary(lines[index]))
		{
			var line = lines[index++].Trim();
			speech.Add(new FountainSpeechLine(line, line.StartsWith('(') && line.EndsWith(')')));
		}

		dialogue = new FountainBlock(FountainBlockKind.Dialogue, cue, speech, dual);
		return true;
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

	[GeneratedRegex(@"(?:/\*).*?(?:\*/|\z)", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
	private static partial Regex Boneyard();

	[GeneratedRegex(@"\[\[.*?\]\]", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
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
