namespace PanoramicData.Render.ReferenceGenerator;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class FieldUpdateCorpusGenerator
{
	// --- Staleness Injectors (OpenXML SDK) ---

	private static void ModifyBody(string docxPath, Action<Body> modify)
	{
		using var doc = WordprocessingDocument.Open(docxPath, isEditable: true);
		modify(doc.MainDocumentPart!.Document!.Body!);
	}

	private static Paragraph TextParagraph(string text, OpenXmlElement? properties = null)
		=> properties is null
			? new Paragraph(new Run(new Text(text)))
			: new Paragraph(properties, new Run(new Text(text)));

	private static Paragraph PageBreakParagraph(string text)
		=> TextParagraph(text, new ParagraphProperties(new PageBreakBefore()));

	private static Paragraph StyledParagraph(string styleId, string text)
		=> TextParagraph(text, new ParagraphProperties(new ParagraphStyleId { Val = styleId }));

	private static void InjectTocStaleness(string docxPath)
		=> ModifyBody(docxPath, body =>
		{
			// Add 10+ headings across multiple pages to make the TOC very stale
			for (var i = 2; i <= 12; i++)
			{
				body.AppendChild(new Paragraph(
					new ParagraphProperties(
						new ParagraphStyleId { Val = "Heading1" },
						new PageBreakBefore()),
					new Run(new Text($"Chapter {i}: Generated Heading"))));
				body.AppendChild(StyledParagraph("Heading2", $"{i}.1 Sub-section"));
				body.AppendChild(TextParagraph($"Body text for chapter {i}. This provides content to ensure the chapter occupies space on the page and affects page numbering."));
				body.AppendChild(StyledParagraph("Heading3", $"{i}.1.1 Detail"));
				body.AppendChild(TextParagraph("Detailed content under the tertiary heading."));
			}
		});

	private static void InjectTofStaleness(string docxPath)
		=> ModifyBody(docxPath, body =>
		{
			// Word's TOF requires SEQ fields in caption paragraphs, not just Caption-styled text.
			for (var i = 2; i <= 7; i++)
			{
				body.AppendChild(PageBreakParagraph($"Content before figure {i}."));
				body.AppendChild(new Paragraph(
					new ParagraphProperties(
						new ParagraphStyleId { Val = "Caption" }),
					new Run(new Text("Figure ") { Space = SpaceProcessingModeValues.Preserve }),
					new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
					new Run(new FieldCode { Space = SpaceProcessingModeValues.Preserve, Text = " SEQ Figure \\* ARABIC " }),
					new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
					new Run(new Text(i.ToString())),
					new Run(new FieldChar { FieldCharType = FieldCharValues.End }),
					new Run(new Text($". Generated Diagram {i}"))));
				body.AppendChild(TextParagraph($"Description of generated diagram {i}."));
			}
		});

	private static void InjectPageOfStaleness(string docxPath)
		=> ModifyBody(docxPath, body =>
		{
			// Enough content to span 5+ pages while the footer still says "Page 1 of 1"
			for (var i = 1; i <= 6; i++)
			{
				body.AppendChild(PageBreakParagraph($"Page {i + 1} content. This is additional material that pushes the document well beyond the original single page."));
				for (var j = 1; j <= 5; j++)
				{
					body.AppendChild(TextParagraph($"Additional paragraph {j} on page {i + 1}. The quick brown fox jumps over the lazy dog. Lorem ipsum dolor sit amet, consectetur adipiscing elit."));
				}
			}
		});

	private static void InjectCrossRefsStaleness(string docxPath)
		=> ModifyBody(docxPath, body =>
		{
			var bookmarkPara = body.Descendants<BookmarkStart>()
				.FirstOrDefault(b => b.Name?.Value == "_RefTarget")?.Parent;

			if (bookmarkPara is null)
			{
				return;
			}

			// Filler before the bookmarked paragraph pushes it to page 5+, making the PAGEREF value stale
			for (var i = 1; i <= 5; i++)
			{
				body.InsertBefore(PageBreakParagraph($"Filler content page {i}. This text is injected to push the bookmarked heading to a later page, making the PAGEREF field stale."), bookmarkPara);
				for (var j = 1; j <= 4; j++)
				{
					body.InsertBefore(TextParagraph($"Extra paragraph {j} on filler page {i}. The PAGEREF should resolve to the actual page number of the bookmark target."), bookmarkPara);
				}
			}
		});
}
