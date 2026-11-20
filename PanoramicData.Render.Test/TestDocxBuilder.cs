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

	public static MemoryStream CreateDocxWithTable() => BuildBodyDocx(
		OneCellTable("Cell 1"),
		TextParagraph("After table"));

	public static MemoryStream CreateDocxWithParagraphs(int count) => BuildBodyDocx(
		[.. Enumerable.Range(1, count).Select(i => TextParagraph($"Paragraph {i}"))]);

	public static MemoryStream CreateDocxWithMixedContent() => BuildBodyDocx(
		TextParagraph("Before"),
		OneCellTable("Cell"),
		TextParagraph("After"),
		TextParagraph("Last"));
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

	public static MemoryStream CreateDocxWithDefaultHeader(string headerText = "Header Text") => BuildHeaderDocx(
		new Header(TextParagraph(headerText)));

	public static MemoryStream CreateDocxWithDefaultFooter(string footerText = "Footer Text") => BuildFooterDocx(
		new Footer(TextParagraph(footerText)));

	public static MemoryStream CreateDocxWithMultipleHeaders()
	{
		return BuildDocx(doc =>
		{
			var mainPart = doc.AddMainDocumentPart();

			var defaultRelId = AddHeader(mainPart, new Header(TextParagraph("Default Header")));
			var firstRelId = AddHeader(mainPart, new Header(TextParagraph("First Page Header")));

			var sectPr = new SectionProperties(
				new HeaderReference { Type = HeaderFooterValues.Default, Id = defaultRelId },
				new HeaderReference { Type = HeaderFooterValues.First, Id = firstRelId });

			mainPart.Document = new Document(new Body(TextParagraph("Body text"), sectPr));
		});
	}

	public static MemoryStream CreateDocxWithHeaderContainingTable() => BuildHeaderDocx(
		new Header(TextParagraph("Header Text"), OneCellTable("Header Cell")));

	public static MemoryStream CreateDocxWithFooterContainingMixedContent() => BuildFooterDocx(
		new Footer(TextParagraph("Before table"), OneCellTable("Footer Cell"), TextParagraph("After table")));

	public static MemoryStream CreateDocxWithSingleFootnote(int footnoteId = 1, string text = "Footnote text")
	{
		var separator = new Footnote { Type = FootnoteEndnoteValues.Separator, Id = -1 };
		separator.Append(new Paragraph(new Run(new SeparatorMark())));

		var note = new Footnote { Id = footnoteId };
		note.Append(TextParagraph(text));

		return BuildFootnotesDocx(new Footnotes(separator, note));
	}

	public static MemoryStream CreateDocxWithFootnoteContainingTable(int footnoteId = 2)
	{
		var note = new Footnote { Id = footnoteId };
		note.Append(TextParagraph("Before table"), OneCellTable("Cell"), TextParagraph("After table"));

		return BuildFootnotesDocx(new Footnotes(note));
	}

	private static MemoryStream BuildWithBodyText(Action<MainDocumentPart> addParts) => BuildDocx(doc =>
	{
		var mainPart = doc.AddMainDocumentPart();
		mainPart.Document = new Document(new Body(TextParagraph("Body text")));
		addParts(mainPart);
	});

	private static Paragraph TextParagraph(string text) => new(new Run(new Text(text)));

	private static Table OneCellTable(string text) => new(new TableRow(new TableCell(TextParagraph(text))));

	private static MemoryStream BuildBodyDocx(params OpenXmlElement[] bodyChildren) => BuildDocx(doc =>
	{
		var mainPart = doc.AddMainDocumentPart();
		mainPart.Document = new Document(new Body(bodyChildren));
	});

	private static string AddHeader(MainDocumentPart mainPart, Header header)
	{
		var headerPart = mainPart.AddNewPart<HeaderPart>();
		headerPart.Header = header;
		return mainPart.GetIdOfPart(headerPart);
	}

	private static MemoryStream BuildHeaderDocx(Header header) => BuildDocx(doc =>
	{
		var mainPart = doc.AddMainDocumentPart();
		var relId = AddHeader(mainPart, header);
		var sectPr = new SectionProperties(new HeaderReference { Type = HeaderFooterValues.Default, Id = relId });
		mainPart.Document = new Document(new Body(TextParagraph("Body text"), sectPr));
	});

	private static MemoryStream BuildFooterDocx(Footer footer) => BuildDocx(doc =>
	{
		var mainPart = doc.AddMainDocumentPart();
		var footerPart = mainPart.AddNewPart<FooterPart>();
		footerPart.Footer = footer;
		var relId = mainPart.GetIdOfPart(footerPart);
		var sectPr = new SectionProperties(new FooterReference { Type = HeaderFooterValues.Default, Id = relId });
		mainPart.Document = new Document(new Body(TextParagraph("Body text"), sectPr));
	});

	private static MemoryStream BuildFootnotesDocx(Footnotes footnotes) => BuildDocx(doc =>
	{
		var mainPart = doc.AddMainDocumentPart();
		mainPart.Document = new Document(new Body(TextParagraph("Body text")));
		mainPart.AddNewPart<FootnotesPart>().Footnotes = footnotes;
	});
}