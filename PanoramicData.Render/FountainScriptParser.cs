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

	private static bool IsBoundary(string line) => (line != "  " && string.IsNullOrWhiteSpace(line))
		|| line.TrimStart() is ['#' or '=' or '~' or '>', ..];

	private static bool IsTransition(string line) =>
		!line.Any(char.IsLower) && (line.EndsWith("TO:", StringComparison.Ordinal)
			|| line is "FADE IN:" or "FADE OUT:" or "FADE OUT." or "CUT TO BLACK.");

	[GeneratedRegex(@"(?:/\*).*?(?:\*/|\z)", RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
	private static partial Regex Boneyard();

	[GeneratedRegex(@"\[\[.*?\]\]", RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
	private static partial Regex Notes();

	[GeneratedRegex(@"^={3,}$", RegexOptions.CultureInvariant)]
	private static partial Regex PageBreak();
}
