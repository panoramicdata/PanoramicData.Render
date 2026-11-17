namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;
using OoxmlSectionProperties = DocumentFormat.OpenXml.Wordprocessing.SectionProperties;
using OoxmlPBdr = DocumentFormat.OpenXml.Wordprocessing.ParagraphBorders;
using OoxmlBorderType = DocumentFormat.OpenXml.Wordprocessing.BorderType;

internal static partial class DocumentBlockParser
{
	/// <summary>
	/// Splits a paragraph into multiple blocks if it contains explicit page or column breaks in run elements.
	/// Returns a list of blocks: paragraph segments separated by break markers with ForcePageBreakBefore set.
	/// </summary>
	private static IReadOnlyList<DocumentBlock> SplitParagraphAtRunBreaks(Paragraph paragraph)
	{
		var blocks = new List<DocumentBlock>();
		var runBreaks = FindRunBreaks(paragraph);

		if (runBreaks.Count == 0)
		{
			// No run breaks found, return single paragraph
			blocks.Add(CreateParagraphBlock(paragraph));
			return blocks;
		}

		// There are page/column breaks. We need to split the paragraph and create marker blocks for page breaks.
		// For now, create a paragraph block that marks all subsequent content with ForcePageBreakBefore.
		// A complete implementation would split the paragraph and apply formatting per-segment.
		var para = CreateParagraphBlock(paragraph);

		// If any page break exists in this paragraph, force a page break before it in layout.
		if (runBreaks.Any(b => b.BreakType == RunBreakType.Page))
		{
			blocks.Add(new ParagraphBlock
			{
				SourceElement = para.SourceElement,
				StyleId = para.StyleId,
				NumberingId = para.NumberingId,
				NumberingLevel = para.NumberingLevel,
				PageBreakBefore = true,
				BookmarkStarts = para.BookmarkStarts,
				BookmarkEnds = para.BookmarkEnds,
				IsBiDi = para.IsBiDi,
				Alignment = para.Alignment,
				Indentation = para.Indentation,
				Borders = para.Borders
			});
		}
		else
		{
			blocks.Add(para);
		}

		return blocks;
	}

	/// <summary>
	/// Finds all page/column break elements within the runs of a paragraph.
	/// Returns a list of (RunIndex, BreakType) tuples indicating where breaks occur.
	/// </summary>
	private static IReadOnlyList<(int RunIndex, RunBreakType BreakType)> FindRunBreaks(Paragraph paragraph)
	{
		var breaks = new List<(int, RunBreakType)>();
		var runIndex = 0;

		foreach (var element in paragraph.ChildElements)
		{
			if (element is not Run run)
			{
				continue;
			}

			foreach (var runChild in run.ChildElements)
			{
				if (runChild is Break brk)
				{
					var breakType = ParseBreakType(brk);
					if (breakType == RunBreakType.Page || breakType == RunBreakType.Column)
					{
						breaks.Add((runIndex, breakType));
					}
				}
			}

			runIndex++;
		}

		return breaks;
	}

	/// <summary>
	/// Parses an OpenXML <see cref="Indentation"/> element into a <see cref="ParagraphIndentation"/> value.
	/// </summary>
	private static ParagraphIndentation ParseIndentation(Indentation? ind)
	{
		if (ind is null)
		{
			return ParagraphIndentation.None;
		}

		return new ParagraphIndentation(
			Left: ParseTwips(ind.Left?.Value),
			Right: ParseTwips(ind.Right?.Value),
			FirstLine: ParseTwips(ind.FirstLine?.Value),
			Hanging: ParseTwips(ind.Hanging?.Value));
	}

	/// <summary>
	/// Parses a twip string value into a float, returning 0 if null or invalid.
	/// </summary>
	private static float ParseTwips(string? value)
		=> float.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : 0f;

	/// <summary>
	/// Determines the break type from an OpenXML Break element.
	/// </summary>
	private static RunBreakType ParseBreakType(Break brk)
	{
		if (brk.Type?.Value == BreakValues.Column)
		{
			return RunBreakType.Column;
		}

		if (brk.Type?.Value == BreakValues.Page)
		{
			return RunBreakType.Page;
		}

		return RunBreakType.Line;
	}
}
