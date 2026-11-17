namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;
using OoxmlSectionProperties = DocumentFormat.OpenXml.Wordprocessing.SectionProperties;
using OoxmlPBdr = DocumentFormat.OpenXml.Wordprocessing.ParagraphBorders;
using OoxmlBorderType = DocumentFormat.OpenXml.Wordprocessing.BorderType;

/// <summary>
/// Parses the top-level elements of a document body into <see cref="DocumentBlock"/> instances.
/// </summary>
internal static partial class DocumentBlockParser
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
			if (element is Paragraph paragraph)
			{
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
			}
			else if (element is Table table)
			{
				blocks.Add(new TablePlaceholderBlock { TableElement = table });
			}
			else if (element is SdtBlock sdtBlock)
			{
				// Unwrap block-level content controls: parse their inner content elements
				var sdtContent = sdtBlock.SdtContentBlock;
				if (sdtContent is not null)
				{
					ParseElements(sdtContent.ChildElements, blocks);
				}
			}
		}
	}

	internal static ParagraphBlock CreateParagraphBlock(Paragraph paragraph)
	{
		var pPr = paragraph.ParagraphProperties;
		var (numberingId, numberingLevel) = ResolveNumbering(pPr?.NumberingProperties);

		return new ParagraphBlock
		{
			SourceElement = paragraph,
			StyleId = GetStyleId(pPr),
			NumberingId = numberingId,
			NumberingLevel = numberingLevel,
			PageBreakBefore = IsEnabled(pPr?.PageBreakBefore),
			BookmarkStarts = ParseBookmarkStarts(paragraph),
			BookmarkEnds = ParseBookmarkEnds(paragraph),
			IsBiDi = IsEnabled(pPr?.BiDi),
			Alignment = MapJustification(pPr?.Justification),
			Indentation = ParseIndentation(pPr?.Indentation),
			Borders = ParseParagraphBorders(pPr?.ParagraphBorders)
		};
	}

	private static string? GetStyleId(ParagraphProperties? pPr) => pPr?.ParagraphStyleId?.Val?.Value;

	private static (int? NumberingId, int? NumberingLevel) ResolveNumbering(NumberingProperties? numPr)
	{
		var numberingId = GetNumberingId(numPr);
		return numberingId is null ? (null, null) : (numberingId, GetNumberingLevel(numPr));
	}

	// Per OOXML spec, numId=0 means "suppress inherited list numbering": treat as no numbering.
}

