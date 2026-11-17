namespace PanoramicData.Render.Test;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Drawing = DocumentFormat.OpenXml.Drawing;

internal static partial class TestDocxBuilder
{
	private static MemoryStream BuildDocx(Action<WordprocessingDocument> build)
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			build(doc);
		}

		stream.Position = 0;
		return stream;
	}

	public static MemoryStream CreateMinimalDocx()
	{
		return BuildDocx(doc =>
		{
			var mainPart = doc.AddMainDocumentPart();
			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Test")))));
		});
	}

	public static MemoryStream CreateFullDocx()
	{
		return BuildDocx(doc =>
		{
			var mainPart = doc.AddMainDocumentPart();
			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Test")))));

			var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
			stylesPart.Styles = new Styles();

			var themePart = mainPart.AddNewPart<ThemePart>();
			themePart.Theme = new Drawing.Theme { Name = "Test" };

			var numberingPart = mainPart.AddNewPart<NumberingDefinitionsPart>();
			numberingPart.Numbering = new Numbering();

			var settingsPart = mainPart.AddNewPart<DocumentSettingsPart>();
			settingsPart.Settings = new Settings();
		});
	}

	public static MemoryStream CreateDocxWithoutMainPart()
	{
		return BuildDocx(_ =>
		{
			// Intentionally don't add MainDocumentPart
		});
	}

	public static MemoryStream CreateDocxWithoutBody()
	{
		return BuildDocx(doc =>
		{
			var mainPart = doc.AddMainDocumentPart();
			mainPart.Document = new Document(); // No Body
		});
	}

	public static MemoryStream CreateDocxWithSectionProperties(SectionProperties sectPr)
	{
		return BuildDocx(doc =>
		{
			var mainPart = doc.AddMainDocumentPart();
			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Test"))),
				sectPr));
		});
	}

	public static MemoryStream CreateDocxWithMultipleSections()
	{
		return BuildDocx(doc =>
		{
			var mainPart = doc.AddMainDocumentPart();

			// First section: landscape A4 (section break in paragraph properties)
			var firstSectPr = new SectionProperties(
				new PageSize
				{
					Width = 16838,
					Height = 11906,
					Orient = PageOrientationValues.Landscape
				},
				new SectionType { Val = SectionMarkValues.NextPage });

			var para1 = new Paragraph(
				new ParagraphProperties(firstSectPr),
				new Run(new Text("Section 1")));

			// Final section: portrait US Letter (body-level section properties)
			var finalSectPr = new SectionProperties(
				new PageSize { Width = 12240, Height = 15840 });

			mainPart.Document = new Document(new Body(
				para1,
				new Paragraph(new Run(new Text("Section 2"))),
				finalSectPr));
		});
	}

	public static MemoryStream CreateDocxWithStyledParagraph(string styleId)
	{
		return BuildDocx(doc =>
		{
			var mainPart = doc.AddMainDocumentPart();
			var pPr = new ParagraphProperties(
				new ParagraphStyleId { Val = styleId });
			mainPart.Document = new Document(new Body(
				new Paragraph(pPr, new Run(new Text("Styled")))));
		});
	}

	public static MemoryStream CreateDocxWithTable()
	{
		return BuildDocx(doc =>
		{
			var mainPart = doc.AddMainDocumentPart();
			var table = new Table(
				new TableRow(
					new TableCell(
						new Paragraph(new Run(new Text("Cell 1"))))));
			mainPart.Document = new Document(new Body(
				table,
				new Paragraph(new Run(new Text("After table")))));
		});
	}

	public static MemoryStream CreateDocxWithParagraphs(int count)
	{
		return BuildDocx(doc =>
		{
			var mainPart = doc.AddMainDocumentPart();
			var body = new Body();
			for (int i = 0; i < count; i++)
			{
				body.Append(new Paragraph(new Run(new Text($"Paragraph {i + 1}"))));
			}

			mainPart.Document = new Document(body);
		});
	}

	public static MemoryStream CreateDocxWithMixedContent()
	{
		return BuildDocx(doc =>
		{
			var mainPart = doc.AddMainDocumentPart();
			var table = new Table(
				new TableRow(
					new TableCell(
						new Paragraph(new Run(new Text("Cell"))))));
			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Before"))),
				table,
				new Paragraph(new Run(new Text("After"))),
				new Paragraph(new Run(new Text("Last")))));
		});
	}

	public static MemoryStream CreateDocxWithEmptyBody()
	{
		return BuildDocx(doc =>
		{
			var mainPart = doc.AddMainDocumentPart();
			mainPart.Document = new Document(new Body());
		});
	}

	public static MemoryStream CreateDocxWithNumberedParagraph(int numId, int ilvl)
	{
		return BuildDocx(doc =>
		{
			var mainPart = doc.AddMainDocumentPart();
			var numPr = new NumberingProperties(
				new NumberingLevelReference { Val = ilvl },
				new NumberingId { Val = numId });
			var pPr = new ParagraphProperties(numPr);
			mainPart.Document = new Document(new Body(
				new Paragraph(pPr, new Run(new Text("List item")))));
		});
	}

	public static MemoryStream CreateDocxWithDefaultHeader(string headerText = "Header Text")
	{
		return BuildDocx(doc =>
		{
			var mainPart = doc.AddMainDocumentPart();

			var headerPart = mainPart.AddNewPart<HeaderPart>();
			var relId = mainPart.GetIdOfPart(headerPart);
			headerPart.Header = new Header(
				new Paragraph(new Run(new Text(headerText))));

			var sectPr = new SectionProperties(
				new HeaderReference { Type = HeaderFooterValues.Default, Id = relId });

			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Body text"))),
				sectPr));
		});
	}

	public static MemoryStream CreateDocxWithDefaultFooter(string footerText = "Footer Text")
	{
		return BuildDocx(doc =>
		{
			var mainPart = doc.AddMainDocumentPart();

			var footerPart = mainPart.AddNewPart<FooterPart>();
			var relId = mainPart.GetIdOfPart(footerPart);
			footerPart.Footer = new Footer(
				new Paragraph(new Run(new Text(footerText))));

			var sectPr = new SectionProperties(
				new FooterReference { Type = HeaderFooterValues.Default, Id = relId });

			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Body text"))),
				sectPr));
		});
	}

	public static MemoryStream CreateDocxWithMultipleHeaders()
	{
		return BuildDocx(doc =>
		{
			var mainPart = doc.AddMainDocumentPart();

			var defaultHeaderPart = mainPart.AddNewPart<HeaderPart>();
			var defaultRelId = mainPart.GetIdOfPart(defaultHeaderPart);
			defaultHeaderPart.Header = new Header(
				new Paragraph(new Run(new Text("Default Header"))));

			var firstHeaderPart = mainPart.AddNewPart<HeaderPart>();
			var firstRelId = mainPart.GetIdOfPart(firstHeaderPart);
			firstHeaderPart.Header = new Header(
				new Paragraph(new Run(new Text("First Page Header"))));

			var sectPr = new SectionProperties(
				new HeaderReference { Type = HeaderFooterValues.Default, Id = defaultRelId },
				new HeaderReference { Type = HeaderFooterValues.First, Id = firstRelId });

			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Body text"))),
				sectPr));
		});
	}

	public static MemoryStream CreateDocxWithHeaderContainingTable()
	{
		return BuildDocx(doc =>
		{
			var mainPart = doc.AddMainDocumentPart();

			var headerPart = mainPart.AddNewPart<HeaderPart>();
			var relId = mainPart.GetIdOfPart(headerPart);
			headerPart.Header = new Header(
				new Paragraph(new Run(new Text("Header Text"))),
				new Table(
					new TableRow(
						new TableCell(
							new Paragraph(new Run(new Text("Header Cell")))))));

			var sectPr = new SectionProperties(
				new HeaderReference { Type = HeaderFooterValues.Default, Id = relId });

			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Body text"))),
				sectPr));
		});
	}

	public static MemoryStream CreateDocxWithFooterContainingMixedContent()
	{
		return BuildDocx(doc =>
		{
			var mainPart = doc.AddMainDocumentPart();

			var footerPart = mainPart.AddNewPart<FooterPart>();
			var relId = mainPart.GetIdOfPart(footerPart);
			footerPart.Footer = new Footer(
				new Paragraph(new Run(new Text("Before table"))),
				new Table(
					new TableRow(
						new TableCell(
							new Paragraph(new Run(new Text("Footer Cell")))))),
				new Paragraph(new Run(new Text("After table"))));

			var sectPr = new SectionProperties(
				new FooterReference { Type = HeaderFooterValues.Default, Id = relId });

			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Body text"))),
				sectPr));
		});
	}

	public static MemoryStream CreateDocxWithSingleFootnote(int footnoteId = 1, string text = "Footnote text")
	{
		return BuildDocx(doc =>
		{
			var mainPart = doc.AddMainDocumentPart();
			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Body text")))));

			var footnotesPart = mainPart.AddNewPart<FootnotesPart>();
			var separator = new Footnote { Type = FootnoteEndnoteValues.Separator, Id = -1 };
			separator.Append(new Paragraph(new Run(new SeparatorMark())));

			var note = new Footnote { Id = footnoteId };
			note.Append(new Paragraph(new Run(new Text(text))));

			footnotesPart.Footnotes = new Footnotes(separator, note);
		});
	}

	public static MemoryStream CreateDocxWithFootnoteContainingTable(int footnoteId = 2)
	{
		return BuildDocx(doc =>
		{
			var mainPart = doc.AddMainDocumentPart();
			mainPart.Document = new Document(new Body(
				new Paragraph(new Run(new Text("Body text")))));

			var footnotesPart = mainPart.AddNewPart<FootnotesPart>();
			var note = new Footnote { Id = footnoteId };
			note.Append(
				new Paragraph(new Run(new Text("Before table"))),
				new Table(
					new TableRow(
						new TableCell(
							new Paragraph(new Run(new Text("Cell")))))),
				new Paragraph(new Run(new Text("After table"))));

			footnotesPart.Footnotes = new Footnotes(note);
		});
	}
}
