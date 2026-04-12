namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;
using OoxmlSectionProperties = DocumentFormat.OpenXml.Wordprocessing.SectionProperties;

/// <summary>
/// Parses the top-level elements of a document body into <see cref="DocumentBlock"/> instances.
/// </summary>
internal static class DocumentBlockParser
{
	/// <summary>
	/// Parses all top-level elements in the document body into an ordered list of blocks.
	/// Paragraphs become <see cref="ParagraphBlock"/>, tables become <see cref="TablePlaceholderBlock"/>,
	/// structured document tags (<c>w:sdt</c>) are unwrapped to expose their inner content,
	/// and section breaks (in paragraph properties) become <see cref="SectionBreakBlock"/>.
	/// </summary>
	/// <param name="body">The document body to parse.</param>
	/// <returns>An ordered list of <see cref="DocumentBlock"/> instances.</returns>
	public static IReadOnlyList<DocumentBlock> Parse(Body body)
	{
		ArgumentNullException.ThrowIfNull(body);

		var blocks = new List<DocumentBlock>();
		ParseElements(body.ChildElements, blocks);
		return blocks;
	}

	private static void ParseElements(IEnumerable<DocumentFormat.OpenXml.OpenXmlElement> elements, List<DocumentBlock> blocks)
	{
		foreach (var element in elements)
		{
			switch (element)
			{
				case Paragraph paragraph:
					// Check if paragraph contains page/column breaks in runs
					var paragraphSegments = SplitParagraphAtRunBreaks(paragraph);
					blocks.AddRange(paragraphSegments);

					// Check for section break in paragraph properties
					var sectPr = paragraph.ParagraphProperties?.GetFirstChild<OoxmlSectionProperties>();
					if (sectPr is not null)
					{
						blocks.Add(new SectionBreakBlock
						{
							SectionInfo = SectionInfoParser.Parse(sectPr)
						});
					}

					break;

				case Table table:
					blocks.Add(new TablePlaceholderBlock { TableElement = table });
					break;

				case SdtBlock sdtBlock:
					// Unwrap block-level content controls: parse their inner content elements
					var sdtContent = sdtBlock.SdtContentBlock;
					if (sdtContent is not null)
					{
						ParseElements(sdtContent.ChildElements, blocks);
					}

					break;
			}
		}
	}

	internal static ParagraphBlock CreateParagraphBlock(Paragraph paragraph)
	{
		var pPr = paragraph.ParagraphProperties;
		var numPr = pPr?.NumberingProperties;

		var bookmarkStarts = paragraph.Elements<BookmarkStart>()
			.Where(bs => bs.Id?.Value is not null && bs.Name?.Value is not null)
			.Select(bs => new BookmarkStartInfo(int.Parse(bs.Id!.Value!, System.Globalization.CultureInfo.InvariantCulture), bs.Name!.Value!))
			.ToArray();

		var bookmarkEnds = paragraph.Elements<BookmarkEnd>()
			.Where(be => be.Id?.Value is not null)
			.Select(be => new BookmarkEndInfo(int.Parse(be.Id!.Value!, System.Globalization.CultureInfo.InvariantCulture)))
			.ToArray();

		return new ParagraphBlock
		{
			SourceElement = paragraph,
			StyleId = pPr?.ParagraphStyleId?.Val?.Value,
			NumberingId = numPr?.NumberingId?.Val?.Value,
			NumberingLevel = numPr?.NumberingLevelReference?.Val?.Value,
			PageBreakBefore = pPr?.PageBreakBefore is { } pbb
				&& (pbb.Val is null || pbb.Val.Value),
			BookmarkStarts = bookmarkStarts,
			BookmarkEnds = bookmarkEnds,
			IsBiDi = pPr?.BiDi is { } bidi
				&& (bidi.Val is null || bidi.Val.Value),
			Alignment = MapJustification(pPr?.Justification)
		};
	}

	/// <summary>
	/// Maps an OpenXML <see cref="Justification"/> element to a <see cref="ParagraphAlignment"/> value.
	/// </summary>
	private static ParagraphAlignment? MapJustification(Justification? jc)
	{
		if (jc?.Val?.Value is not { } value)
		{
			return null;
		}

		if (value == JustificationValues.Left || value == JustificationValues.Start)
		{
			return ParagraphAlignment.Left;
		}

		if (value == JustificationValues.Center)
		{
			return ParagraphAlignment.Center;
		}

		if (value == JustificationValues.Right || value == JustificationValues.End)
		{
			return ParagraphAlignment.Right;
		}

		if (value == JustificationValues.Both || value == JustificationValues.Distribute)
		{
			return ParagraphAlignment.Justified;
		}

		return null;
	}

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
				Alignment = para.Alignment
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

