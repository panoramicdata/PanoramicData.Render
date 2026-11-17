namespace PanoramicData.Render.ReferenceGenerator;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using V = DocumentFormat.OpenXml.Vml;

internal static partial class TestCorpusGenerator
{
	private static void GenerateMultiLevelList(string path)
	{
		using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		var mainPart = doc.AddMainDocumentPart();

		// Create numbering part with a multi-level list definition
		var numberingPart = mainPart.AddNewPart<NumberingDefinitionsPart>();

		var abstractNum = new AbstractNum(
			MakeLevel(0, "decimal", "%1."),
			MakeLevel(1, "lowerLetter", "%2)"),
			MakeLevel(2, "lowerRoman", "%3."))
		{ AbstractNumberId = 1 };

		var numInstance = new NumberingInstance(
			new AbstractNumId { Val = 1 })
		{ NumberID = 1 };

		numberingPart.Numbering = new Numbering(abstractNum, numInstance);

		var body = new Body(
			new Paragraph(new Run(new Text("Multi-Level List:"))),
			MakeListParagraph("First item", 1, 0),
			MakeListParagraph("Sub-item A", 1, 1),
			MakeListParagraph("Sub-item B", 1, 1),
			MakeListParagraph("Sub-sub-item i", 1, 2),
			MakeListParagraph("Second item", 1, 0),
			MakeListParagraph("Sub-item A", 1, 1),
			MakeListParagraph("Third item", 1, 0),
			DefaultSectionProperties());

		mainPart.Document = new Document(body);
	}

	private static void GenerateAutoFitTable(string path)
	{
		using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		var mainPart = doc.AddMainDocumentPart();

		var table = new Table(
			new TableProperties(
				new TableLayout { Type = TableLayoutValues.Autofit },
				new TableWidth { Width = "0", Type = TableWidthUnitValues.Auto },
				new TableBorders(
					new TopBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
					new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
					new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
					new RightBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
					new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
					new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "000000" })),
			new TableRow(
				MakeTableCell("ID", bold: true),
				MakeTableCell("Description", bold: true),
				MakeTableCell("Notes", bold: true)),
			new TableRow(
				MakeTableCell("1"),
				MakeTableCell("Short"),
				MakeTableCell("Auto-fit should keep this narrow.")),
			new TableRow(
				MakeTableCell("2"),
				MakeTableCell("A significantly longer description to force the middle column wider."),
				MakeTableCell("Tests proportional auto-fit behavior.")));

		mainPart.Document = new Document(new Body(
			new Paragraph(new Run(new Text("Auto-fit Table"))),
			table,
			DefaultSectionProperties()));
	}

	private static void GenerateTableStyleFirstLast(string path)
	{
		using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		var mainPart = doc.AddMainDocumentPart();

		AddTableStyleDefinitions(mainPart,
			CreateTableStyle(
				"CorpusFirstLastStyle",
				(TableStyleOverrideValues.FirstRow, CreateShading("D9E1F2")),
				(TableStyleOverrideValues.LastRow, CreateShading("FCE4D6")),
				(TableStyleOverrideValues.FirstColumn, CreateShading("E2F0D9")),
				(TableStyleOverrideValues.LastColumn, CreateShading("FFF2CC"))));

		var table = new Table(
			new TableProperties(
				new TableStyle { Val = "CorpusFirstLastStyle" },
				CreateTableLook(applyFirstRow: true, applyLastRow: true, applyFirstColumn: true, applyLastColumn: true),
				new TableBorders(
					new TopBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
					new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
					new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
					new RightBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
					new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
					new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "000000" })),
			new TableRow(MakeTableCell("Region", true), MakeTableCell("Q1", true), MakeTableCell("Q2", true), MakeTableCell("Total", true)),
			new TableRow(MakeTableCell("North"), MakeTableCell("120"), MakeTableCell("140"), MakeTableCell("260")),
			new TableRow(MakeTableCell("South"), MakeTableCell("95"), MakeTableCell("110"), MakeTableCell("205")),
			new TableRow(MakeTableCell("Grand Total", true), MakeTableCell("215", true), MakeTableCell("250", true), MakeTableCell("465", true)));

		mainPart.Document = new Document(new Body(
			new Paragraph(new Run(new Text("Table Style: First/Last Row + First/Last Column"))),
			table,
			DefaultSectionProperties()));
	}

	private static void GenerateTableStyleBanding(string path)
	{
		using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		var mainPart = doc.AddMainDocumentPart();

		AddTableStyleDefinitions(mainPart,
			CreateTableStyle(
				"CorpusBandingStyle",
				(TableStyleOverrideValues.Band1Horizontal, CreateShading("F2F2F2")),
				(TableStyleOverrideValues.Band2Horizontal, CreateShading("E6EEF8")),
				(TableStyleOverrideValues.Band1Vertical, CreateShading("FFF2CC")),
				(TableStyleOverrideValues.Band2Vertical, CreateShading("E2F0D9"))));

		var table = new Table(
			new TableProperties(
				new TableStyle { Val = "CorpusBandingStyle" },
				CreateTableLook(applyBandedRows: true, applyBandedColumns: true),
				new TableBorders(
					new TopBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
					new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
					new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
					new RightBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
					new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
					new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "000000" })),
			new TableRow(MakeTableCell("C1", true), MakeTableCell("C2", true), MakeTableCell("C3", true), MakeTableCell("C4", true)),
			new TableRow(MakeTableCell("R2C1"), MakeTableCell("R2C2"), MakeTableCell("R2C3"), MakeTableCell("R2C4")),
			new TableRow(MakeTableCell("R3C1"), MakeTableCell("R3C2"), MakeTableCell("R3C3"), MakeTableCell("R3C4")),
			new TableRow(MakeTableCell("R4C1"), MakeTableCell("R4C2"), MakeTableCell("R4C3"), MakeTableCell("R4C4")),
			new TableRow(MakeTableCell("R5C1"), MakeTableCell("R5C2"), MakeTableCell("R5C3"), MakeTableCell("R5C4")));

		mainPart.Document = new Document(new Body(
			new Paragraph(new Run(new Text("Table Style: Odd/Even Row + Column Banding"))),
			table,
			DefaultSectionProperties()));
	}

	private static void GenerateInlineImages(string path)
	{
		using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		var mainPart = doc.AddMainDocumentPart();

		var imagePart = mainPart.AddImagePart(ImagePartType.Png);
		using (var imageStream = new MemoryStream(CreateMinimalPngBytes()))
		{
			imagePart.FeedData(imageStream);
		}

		var relId = mainPart.GetIdOfPart(imagePart);
		var inlineDrawing = CreateInlineImageDrawing(relId, 914400L, 914400L, "InlineImage");

		mainPart.Document = new Document(new Body(
			new Paragraph(new Run(new Text("Inline image demonstration:"))),
			new Paragraph(new Run(new Text("Before ")), new Run(inlineDrawing), new Run(new Text(" After"))),
			DefaultSectionProperties()));
	}

	private static void GenerateFloatingImages(string path)
	{
		using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		var mainPart = doc.AddMainDocumentPart();

		var imagePart = mainPart.AddImagePart(ImagePartType.Png);
		using (var imageStream = new MemoryStream(CreateMinimalPngBytes()))
		{
			imagePart.FeedData(imageStream);
		}

		var relId = mainPart.GetIdOfPart(imagePart);
		var anchorDrawing = CreateAnchorImageDrawing(relId, 1200000L, 800000L, "FloatingImage");

		mainPart.Document = new Document(new Body(
			new Paragraph(new Run(new Text("Floating image with wrapping demonstration."))),
			new Paragraph(
				new Run(anchorDrawing),
				new Run(new Text(
					" This paragraph contains additional text that should wrap around a floating image in Word reference output."))),
			new Paragraph(new Run(new Text("Second paragraph to exercise layout after anchored object."))),
			DefaultSectionProperties()));
	}

	private static void GenerateFootnotes(string path)
	{
		using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		var mainPart = doc.AddMainDocumentPart();

		var footnotesPart = mainPart.AddNewPart<FootnotesPart>();
		var separator = new Footnote { Type = FootnoteEndnoteValues.Separator, Id = -1 };
		separator.Append(new Paragraph(new Run(new SeparatorMark())));

		var note = new Footnote { Id = 1 };
		note.Append(new Paragraph(new Run(new Text("This is a sample footnote for visual regression."))));
		footnotesPart.Footnotes = new Footnotes(separator, note);

		mainPart.Document = new Document(new Body(
			new Paragraph(
				new Run(new Text("Footnote example")),
				new Run(new FootnoteReference { Id = 1 })),
			new Paragraph(new Run(new Text("Additional body content after the footnote reference."))),
			DefaultSectionProperties()));
	}

	private static void GenerateColumns(string path)
	{
		using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		var mainPart = doc.AddMainDocumentPart();

		var section = new SectionProperties(
			new PageSize { Width = 12240, Height = 15840 },
			new PageMargin { Top = 1440, Right = 1440, Bottom = 1440, Left = 1440 },
			new Columns { ColumnCount = 2, Space = "720" });

		mainPart.Document = new Document(new Body(
			new Paragraph(new Run(new Text("Two-column layout test."))),
			new Paragraph(new Run(new Text("Column text paragraph 1. " +
				"This document verifies that section column metadata is preserved in the reference output."))),
			new Paragraph(new Run(new Text("Column text paragraph 2. " +
				"Additional content helps Word flow text across both columns."))),
			new Paragraph(new Run(new Text("Column text paragraph 3. " +
				"Further lines provide density for visual comparison."))),
			section));
	}

	private static void GenerateWatermark(string path)
	{
		using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		var mainPart = doc.AddMainDocumentPart();

		var headerPart = mainPart.AddNewPart<HeaderPart>();
		var headerRelId = mainPart.GetIdOfPart(headerPart);

		var watermarkShape = new V.Shape
		{
			Id = "PowerPlusWaterMarkObject1001",
			Type = "#_x0000_t136",
			Style = "position:absolute;width:527.85pt;height:131.95pt;rotation:315;z-index:-251658752;mso-position-horizontal:center;mso-position-vertical:center;mso-position-horizontal-relative:margin;mso-position-vertical-relative:margin",
			FillColor = "silver"
		};
		watermarkShape.Append(new V.Fill { Opacity = ".5" });
		watermarkShape.Append(new V.TextPath { Style = "font-family:\"Calibri\";font-size:1pt", String = "DRAFT" });

		headerPart.Header = new Header(
			new Paragraph(
				new Run(new DocumentFormat.OpenXml.Wordprocessing.Picture(watermarkShape))));

		var section = new SectionProperties(
			new PageSize { Width = 12240, Height = 15840 },
			new PageMargin { Top = 1440, Right = 1440, Bottom = 1440, Left = 1440, Header = 720, Footer = 720 },
			new HeaderReference { Type = HeaderFooterValues.Default, Id = headerRelId });

		mainPart.Document = new Document(new Body(
			new Paragraph(new Run(new Text("Watermark test content in body area."))),
			new Paragraph(new Run(new Text("Word reference should include a centered DRAFT watermark."))),
			section));
	}
}
