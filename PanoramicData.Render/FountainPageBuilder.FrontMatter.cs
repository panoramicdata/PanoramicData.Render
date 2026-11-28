using System.Text.RegularExpressions;

namespace PanoramicData.Render;

internal sealed partial class FountainPageBuilder
{
	private int AddStageFrontMatter(IReadOnlyList<FountainBlock> blocks)
	{
		var index = 0;
		var priorHeading = string.Empty;
		while (ReadFrontMatterName(blocks.ElementAtOrDefault(index)) is { } name)
		{
			var cast = name is "CAST" or "CHARACTERS";
			if (priorHeading.Length == 0 || layout.IsUsStage && priorHeading is "CAST" or "CHARACTERS")
			{
				NewPage(numbered: false);
			}

			AddCentered(cast && layout.IsUsStage ? "CHARACTERS" : name, after: 2);
			index++;
			AddFrontMatterParagraphs(blocks, ref index, cast);

			priorHeading = name;
		}

		return index;
	}

	private static string? ReadFrontMatterName(FountainBlock? block)
	{
		if (block is not { Kind: FountainBlockKind.Centered })
		{
			return null;
		}

		var name = FountainLineFormatter.PlainText(block.Text).ToUpperInvariant();
		return FrontMatter().IsMatch(name) ? name : null;
	}

	private void AddFrontMatterParagraphs(IReadOnlyList<FountainBlock> blocks, ref int index, bool cast)
	{
		while (index < blocks.Count && blocks[index] is { Kind: FountainBlockKind.Action } action)
		{
			foreach (var line in action.Text.Split('\n'))
			{
				AddFrontMatterLine(line, cast);
			}
			_baseline += _lineHeight;
			index++;
		}
	}

	private void AddFrontMatterLine(string text, bool cast)
	{
		var separator = cast ? text.IndexOf(" - ", StringComparison.Ordinal) : -1;
		if (separator < 0 || !layout.IsUsStage || formatter.Measure(text[..separator]) > 170)
		{
			AddLines(formatter.Wrap(text, layout.PageWidth - 144), 72);
			return;
		}

		EnsureRoom(1);
		AddAtCurrentBaseline(FountainLineFormatter.PlainText(text[..separator]).ToUpperInvariant(), 72);
		AddLines(formatter.Wrap(text[(separator + 3)..], layout.PageWidth - 324), 252);
	}

	[GeneratedRegex(@"^(?:CAST|CHARACTERS|SETTING|TIME)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex FrontMatter();
}
