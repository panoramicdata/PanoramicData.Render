using System.Text.RegularExpressions;

namespace PanoramicData.Render;

internal static partial class FountainScriptParser
{
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

	[GeneratedRegex(@"^(?:INT[./]|EXT[./]|EST\.|I/E\.)(?:\s|/|[A-Z])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex SceneHeading();

	[GeneratedRegex(@"\s+#[^#]+#\s*$", RegexOptions.CultureInvariant)]
	private static partial Regex SceneNumber();
}
