namespace PanoramicData.Render.ReferenceGenerator;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using V = DocumentFormat.OpenXml.Vml;

/// <summary>
/// Generates a corpus of test DOCX documents using the OpenXML SDK.
/// Each document exercises a specific rendering feature for visual regression testing.
/// </summary>
internal static class TestCorpusGenerator
{
	/// <summary>
	/// Generates all test DOCX documents in the specified directory.
	/// Returns the number of documents created.
	/// </summary>
	public static int GenerateAll(string outputDir)
	{
		var generators = new (string Name, Action<string> Generate)[]
		{
			("basic-text", GenerateBasicText),
			("paragraph-alignment", GenerateParagraphAlignment),
			("paragraph-indentation", GenerateParagraphIndentation),
			("character-formatting", GenerateCharacterFormatting),
			("simple-table", GenerateSimpleTable),
			("merged-cells-table", GenerateMergedCellsTable),
			("table-style-first-last", GenerateTableStyleFirstLast),
			("table-style-banding", GenerateTableStyleBanding),
			("auto-fit-table", GenerateAutoFitTable),
			("multi-level-list", GenerateMultiLevelList),
			("inline-images", GenerateInlineImages),
			("floating-images", GenerateFloatingImages),
			("headers-and-footers", GenerateHeadersAndFooters),
			("multi-section", GenerateMultiSection),
			("footnotes", GenerateFootnotes),
			("columns", GenerateColumns),
			("tab-stops", GenerateTabStops),
			("watermark", GenerateWatermark),
			("rtl-text", GenerateRtlText),
			("page-break", GeneratePageBreak),
		};

		foreach (var (name, generate) in generators)
		{
			var path = Path.Combine(outputDir, $"{name}.docx");
			Console.WriteLine($"  Creating {name}.docx");
			generate(path);
		}

		return generators.Length;
	}

	private static void GenerateBasicText(string path)
	{
		using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		var mainPart = doc.AddMainDocumentPart();
		var body = new Body(
			new Paragraph(
				new Run(new Text("The quick brown fox jumps over the lazy dog. ") { Space = SpaceProcessingModeValues.Preserve }),
				new Run(new Text("This is a basic text document used for visual regression testing. ") { Space = SpaceProcessingModeValues.Preserve }),
				new Run(new Text("It contains a single paragraph of plain text with no special formatting."))),
			DefaultSectionProperties());

		mainPart.Document = new Document(body);
	}

	private static void GenerateParagraphAlignment(string path)
	{
		using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		var mainPart = doc.AddMainDocumentPart();
		var body = new Body(
			MakeParagraph("This paragraph is left-aligned (the default).", JustificationValues.Left),
			MakeParagraph("This paragraph is center-aligned.", JustificationValues.Center),
			MakeParagraph("This paragraph is right-aligned.", JustificationValues.Right),
			MakeParagraph(
				"This paragraph is justified. It contains enough text to demonstrate how Word distributes space between words when justifying a line of text across the full width of the page margin.",
				JustificationValues.Both),
			DefaultSectionProperties());

		mainPart.Document = new Document(body);
	}

	private static void GenerateParagraphIndentation(string path)
	{
		using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		var mainPart = doc.AddMainDocumentPart();
		var body = new Body(
			new Paragraph(new Run(new Text("No indentation (default)."))),
			MakeIndentedParagraph("Left indent of 720 twips (0.5 inch).", left: "720"),
			MakeIndentedParagraph("Left indent of 1440 twips (1 inch).", left: "1440"),
			MakeIndentedParagraph("Right indent of 720 twips (0.5 inch).", right: "720"),
			MakeIndentedParagraph("First line indent of 720 twips (0.5 inch).", firstLine: "720"),
			MakeIndentedParagraph("Hanging indent: first line at 0, rest at 720 twips.", left: "720", hanging: "720"),
			DefaultSectionProperties());

		mainPart.Document = new Document(body);
	}

	private static void GenerateCharacterFormatting(string path)
	{
		using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		var mainPart = doc.AddMainDocumentPart();
		var body = new Body(
			new Paragraph(
				new Run(new Text("Normal text. ") { Space = SpaceProcessingModeValues.Preserve }),
				new Run(
					new RunProperties(new Bold()),
					new Text("Bold text. ") { Space = SpaceProcessingModeValues.Preserve }),
				new Run(
					new RunProperties(new Italic()),
					new Text("Italic text. ") { Space = SpaceProcessingModeValues.Preserve }),
				new Run(
					new RunProperties(new Bold(), new Italic()),
					new Text("Bold italic text. ") { Space = SpaceProcessingModeValues.Preserve }),
				new Run(
					new RunProperties(new Underline { Val = UnderlineValues.Single }),
					new Text("Underlined text. ") { Space = SpaceProcessingModeValues.Preserve }),
				new Run(
					new RunProperties(new Strike()),
					new Text("Strikethrough text. ") { Space = SpaceProcessingModeValues.Preserve }),
				new Run(
					new RunProperties(new FontSize { Val = "32" }),
					new Text("16pt text. ") { Space = SpaceProcessingModeValues.Preserve }),
				new Run(
					new RunProperties(new FontSize { Val = "48" }),
					new Text("24pt text.") { Space = SpaceProcessingModeValues.Preserve })),
			new Paragraph(
				new Run(new Text("Text with ") { Space = SpaceProcessingModeValues.Preserve }),
				new Run(
					new RunProperties(new VerticalTextAlignment { Val = VerticalPositionValues.Superscript }),
					new Text("superscript")),
				new Run(new Text(" and ") { Space = SpaceProcessingModeValues.Preserve }),
				new Run(
					new RunProperties(new VerticalTextAlignment { Val = VerticalPositionValues.Subscript }),
					new Text("subscript")),
				new Run(new Text("."))),
			new Paragraph(
				new Run(
					new RunProperties(new Color { Val = "FF0000" }),
					new Text("Red text. ") { Space = SpaceProcessingModeValues.Preserve }),
				new Run(
					new RunProperties(new Color { Val = "0000FF" }),
					new Text("Blue text. ") { Space = SpaceProcessingModeValues.Preserve }),
				new Run(
					new RunProperties(new Color { Val = "008000" }),
					new Text("Green text."))),
			DefaultSectionProperties());

		mainPart.Document = new Document(body);
	}

	private static void GenerateSimpleTable(string path)
	{
		using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		var mainPart = doc.AddMainDocumentPart();

		var tblPr = new TableProperties(
			new TableBorders(
				new TopBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
				new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
				new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
				new RightBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
				new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
				new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "000000" }),
			new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct });

		var table = new Table(tblPr);

		// Header row with bold text
		var headerRow = new TableRow(
			MakeTableCell("Name", bold: true),
			MakeTableCell("Value", bold: true),
			MakeTableCell("Description", bold: true));
		table.Append(headerRow);

		// Data rows
		table.Append(new TableRow(
			MakeTableCell("Alpha"),
			MakeTableCell("100"),
			MakeTableCell("First item")));
		table.Append(new TableRow(
			MakeTableCell("Beta"),
			MakeTableCell("200"),
			MakeTableCell("Second item")));
		table.Append(new TableRow(
			MakeTableCell("Gamma"),
			MakeTableCell("300"),
			MakeTableCell("Third item")));

		var body = new Body(
			new Paragraph(new Run(new Text("Simple Table"))),
			table,
			DefaultSectionProperties());

		mainPart.Document = new Document(body);
	}

	private static void GenerateMergedCellsTable(string path)
	{
		using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		var mainPart = doc.AddMainDocumentPart();

		var tblPr = new TableProperties(
			new TableBorders(
				new TopBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
				new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
				new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
				new RightBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
				new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
				new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "000000" }),
			new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct });

		var table = new Table(tblPr);

		// Row 1: horizontally merged cell spanning 3 columns
		table.Append(new TableRow(
			new TableCell(
				new TableCellProperties(new HorizontalMerge { Val = MergedCellValues.Restart }),
				new Paragraph(new Run(new Text("Merged across 3 columns")))),
			new TableCell(
				new TableCellProperties(new HorizontalMerge { Val = MergedCellValues.Continue }),
				new Paragraph()),
			new TableCell(
				new TableCellProperties(new HorizontalMerge { Val = MergedCellValues.Continue }),
				new Paragraph())));

		// Row 2: vertically merged start + normal cells
		table.Append(new TableRow(
			new TableCell(
				new TableCellProperties(new VerticalMerge { Val = MergedCellValues.Restart }),
				new Paragraph(new Run(new Text("Vertically merged")))),
			MakeTableCell("B2"),
			MakeTableCell("C2")));

		// Row 3: vertically merged continue + normal cells
		table.Append(new TableRow(
			new TableCell(
				new TableCellProperties(new VerticalMerge()),
				new Paragraph()),
			MakeTableCell("B3"),
			MakeTableCell("C3")));

		var body = new Body(
			new Paragraph(new Run(new Text("Table with Merged Cells"))),
			table,
			DefaultSectionProperties());

		mainPart.Document = new Document(body);
	}

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

	private static void GenerateRtlText(string path)
	{
		using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		var mainPart = doc.AddMainDocumentPart();

		mainPart.Document = new Document(new Body(
			new Paragraph(
				new ParagraphProperties(new BiDi()),
				new Run(
					new RunProperties(new RightToLeftText()),
					new Text("مرحبا بالعالم هذا اختبار نص عربي من اليمين إلى اليسار"))),
			new Paragraph(
				new Run(new Text("Mixed LTR/RTL: English then العربية ثم English again."))),
			DefaultSectionProperties()));
	}

	private static void GenerateHeadersAndFooters(string path)
	{
		using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		var mainPart = doc.AddMainDocumentPart();

		// Create header
		var headerPart = mainPart.AddNewPart<HeaderPart>();
		var headerRelId = mainPart.GetIdOfPart(headerPart);
		headerPart.Header = new Header(
			new Paragraph(
				new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
				new Run(
					new RunProperties(new Bold(), new FontSize { Val = "20" }),
					new Text("Document Header — Visual Regression Test"))));

		// Create footer
		var footerPart = mainPart.AddNewPart<FooterPart>();
		var footerRelId = mainPart.GetIdOfPart(footerPart);
		footerPart.Footer = new Footer(
			new Paragraph(
				new ParagraphProperties(new Justification { Val = JustificationValues.Right }),
				new Run(
					new RunProperties(new FontSize { Val = "16" }),
					new Text("Page Footer — PanoramicData.Render"))));

		var sectPr = new SectionProperties(
			new PageSize { Width = 12240, Height = 15840 },
			new PageMargin { Top = 1440, Right = 1440, Bottom = 1440, Left = 1440, Header = 720, Footer = 720 },
			new HeaderReference { Type = HeaderFooterValues.Default, Id = headerRelId },
			new FooterReference { Type = HeaderFooterValues.Default, Id = footerRelId });

		var body = new Body(
			new Paragraph(new Run(new Text(
				"This document has a header and a footer. The header is centered with bold text. The footer is right-aligned with smaller text."))),
			new Paragraph(new Run(new Text(
				"This second paragraph provides additional body content to verify that the header and footer appear correctly in relation to the body text."))),
			sectPr);

		mainPart.Document = new Document(body);
	}

	private static void GenerateMultiSection(string path)
	{
		using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		var mainPart = doc.AddMainDocumentPart();

		// Section 1: US Letter portrait
		var sect1 = new SectionProperties(
			new PageSize { Width = 12240, Height = 15840 },
			new PageMargin { Top = 1440, Right = 1440, Bottom = 1440, Left = 1440 },
			new SectionType { Val = SectionMarkValues.NextPage });

		// Section 2 (final): US Letter landscape
		var sect2 = new SectionProperties(
			new PageSize { Width = 15840, Height = 12240, Orient = PageOrientationValues.Landscape },
			new PageMargin { Top = 1440, Right = 1440, Bottom = 1440, Left = 1440 });

		var body = new Body(
			new Paragraph(new Run(new Text("Section 1: US Letter Portrait"))),
			new Paragraph(new Run(new Text("This is the first section of the document, using standard US Letter portrait orientation."))),
			new Paragraph(new ParagraphProperties((SectionProperties)sect1.CloneNode(true))),
			new Paragraph(new Run(new Text("Section 2: US Letter Landscape"))),
			new Paragraph(new Run(new Text("This is the second section, using landscape orientation. The page dimensions are swapped."))),
			sect2);

		mainPart.Document = new Document(body);
	}

	private static void GenerateTabStops(string path)
	{
		using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		var mainPart = doc.AddMainDocumentPart();

		// Left tab at 2 inches, center tab at center, right tab at right margin
		var tabs = new Tabs(
			new TabStop { Val = TabStopValues.Left, Position = 2880 },
			new TabStop { Val = TabStopValues.Center, Position = 4680 },
			new TabStop { Val = TabStopValues.Right, Position = 9360 });

		var body = new Body(
			new Paragraph(new Run(new Text("Tab Stops Demo:"))),
			new Paragraph(
				new ParagraphProperties((Tabs)tabs.CloneNode(true)),
				new Run(
					new Text("Left") { Space = SpaceProcessingModeValues.Preserve },
					new TabChar(),
					new Text("At 2in") { Space = SpaceProcessingModeValues.Preserve },
					new TabChar(),
					new Text("Center") { Space = SpaceProcessingModeValues.Preserve },
					new TabChar(),
					new Text("Right"))),
			new Paragraph(
				new ParagraphProperties(
					new Tabs(
						new TabStop { Val = TabStopValues.Right, Leader = TabStopLeaderCharValues.Dot, Position = 9360 })),
				new Run(
					new Text("Chapter 1") { Space = SpaceProcessingModeValues.Preserve },
					new TabChar(),
					new Text("1"))),
			new Paragraph(
				new ParagraphProperties(
					new Tabs(
						new TabStop { Val = TabStopValues.Right, Leader = TabStopLeaderCharValues.Dot, Position = 9360 })),
				new Run(
					new Text("Chapter 2") { Space = SpaceProcessingModeValues.Preserve },
					new TabChar(),
					new Text("15"))),
			DefaultSectionProperties());

		mainPart.Document = new Document(body);
	}

	private static void GeneratePageBreak(string path)
	{
		using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		var mainPart = doc.AddMainDocumentPart();

		var body = new Body(
			new Paragraph(new Run(new Text("Page 1: This is the content on the first page."))),
			new Paragraph(new Run(new Break { Type = BreakValues.Page })),
			new Paragraph(new Run(new Text("Page 2: This content appears after an explicit page break."))),
			new Paragraph(new Run(new Break { Type = BreakValues.Page })),
			new Paragraph(new Run(new Text("Page 3: This is the third and final page."))),
			DefaultSectionProperties());

		mainPart.Document = new Document(body);
	}

	// --- Helper methods ---

	private static SectionProperties DefaultSectionProperties() =>
		new(
			new PageSize { Width = 12240, Height = 15840 },
			new PageMargin { Top = 1440, Right = 1440, Bottom = 1440, Left = 1440 });

	private static Paragraph MakeParagraph(string text, JustificationValues alignment) =>
		new(
			new ParagraphProperties(new Justification { Val = alignment }),
			new Run(new Text(text)));

	private static Paragraph MakeIndentedParagraph(string text,
		string? left = null, string? right = null,
		string? firstLine = null, string? hanging = null)
	{
		var ind = new Indentation();
		if (left is not null) ind.Left = left;
		if (right is not null) ind.Right = right;
		if (firstLine is not null) ind.FirstLine = firstLine;
		if (hanging is not null) ind.Hanging = hanging;

		return new Paragraph(
			new ParagraphProperties(ind),
			new Run(new Text(text)));
	}

	private static TableCell MakeTableCell(string text, bool bold = false)
	{
		var run = bold
			? new Run(new RunProperties(new Bold()), new Text(text))
			: new Run(new Text(text));

		return new TableCell(new Paragraph(run));
	}

	private static void AddTableStyleDefinitions(MainDocumentPart mainPart, params Style[] styles)
	{
		var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
		stylesPart.Styles = new Styles(styles);
	}

	private static Style CreateTableStyle(string styleId, params (TableStyleOverrideValues Type, Shading Shading)[] overrides)
	{
		var styleChildren = new List<OpenXmlElement>();
		foreach (var styleOverride in overrides)
		{
			styleChildren.Add(new TableStyleProperties(new TableCellProperties((Shading)styleOverride.Shading.CloneNode(true)))
			{
				Type = styleOverride.Type
			});
		}

		var style = new Style(styleChildren)
		{
			Type = StyleValues.Table,
			StyleId = styleId,
			CustomStyle = true
		};

		style.Append(new StyleName { Val = styleId });
		return style;
	}

	private static Shading CreateShading(string fillHex)
	{
		var shading = new Shading { Fill = fillHex };
		shading.SetAttribute(new OpenXmlAttribute("w", "val", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "clear"));
		return shading;
	}

	private static TableLook CreateTableLook(
		bool applyFirstRow = false,
		bool applyLastRow = false,
		bool applyFirstColumn = false,
		bool applyLastColumn = false,
		bool applyBandedRows = false,
		bool applyBandedColumns = false)
	{
		var look = new TableLook();
		look.SetAttribute(new OpenXmlAttribute("w", "firstRow", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", applyFirstRow ? "1" : "0"));
		look.SetAttribute(new OpenXmlAttribute("w", "lastRow", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", applyLastRow ? "1" : "0"));
		look.SetAttribute(new OpenXmlAttribute("w", "firstColumn", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", applyFirstColumn ? "1" : "0"));
		look.SetAttribute(new OpenXmlAttribute("w", "lastColumn", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", applyLastColumn ? "1" : "0"));
		look.SetAttribute(new OpenXmlAttribute("w", "noHBand", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", applyBandedRows ? "0" : "1"));
		look.SetAttribute(new OpenXmlAttribute("w", "noVBand", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", applyBandedColumns ? "0" : "1"));
		return look;
	}

	private static Level MakeLevel(int levelIndex, string numFmt, string lvlText)
	{
		var level = new Level(
			new StartNumberingValue { Val = 1 },
			new NumberingFormat { Val = numFmt switch
			{
				"decimal" => NumberFormatValues.Decimal,
				"lowerLetter" => NumberFormatValues.LowerLetter,
				"lowerRoman" => NumberFormatValues.LowerRoman,
				_ => NumberFormatValues.Decimal
			} },
			new LevelText { Val = lvlText },
			new ParagraphProperties(
				new Indentation
				{
					Left = ((levelIndex + 1) * 720).ToString(),
					Hanging = "360"
				}))
		{ LevelIndex = levelIndex };

		return level;
	}

	private static Paragraph MakeListParagraph(string text, int numId, int ilvl) =>
		new(
			new ParagraphProperties(
				new NumberingProperties(
					new NumberingLevelReference { Val = ilvl },
					new NumberingId { Val = numId })),
			new Run(new Text(text)));

	private static Drawing CreateInlineImageDrawing(string relationshipId, long widthEmu, long heightEmu, string name)
	{
		var pic = CreatePictureElement(relationshipId, name, widthEmu, heightEmu);
		var graphicData = new A.GraphicData(pic)
		{
			Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture"
		};

		var inline = new DW.Inline(
			new DW.Extent { Cx = widthEmu, Cy = heightEmu },
			new DW.EffectExtent(),
			new DW.DocProperties { Id = 1U, Name = name },
			new DW.NonVisualGraphicFrameDrawingProperties(),
			new A.Graphic(graphicData))
		{
			DistanceFromTop = 0U,
			DistanceFromBottom = 0U,
			DistanceFromLeft = 0U,
			DistanceFromRight = 0U
		};

		return new Drawing(inline);
	}

	private static Drawing CreateAnchorImageDrawing(string relationshipId, long widthEmu, long heightEmu, string name)
	{
		var pic = CreatePictureElement(relationshipId, name, widthEmu, heightEmu);
		var graphicData = new A.GraphicData(pic)
		{
			Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture"
		};

		var anchor = new DW.Anchor(
			new DW.SimplePosition { X = 0, Y = 0 },
			new DW.HorizontalPosition(new DW.PositionOffset("914400"))
			{
				RelativeFrom = DW.HorizontalRelativePositionValues.Margin
			},
			new DW.VerticalPosition(new DW.PositionOffset("914400"))
			{
				RelativeFrom = DW.VerticalRelativePositionValues.Paragraph
			},
			new DW.Extent { Cx = widthEmu, Cy = heightEmu },
			new DW.EffectExtent(),
			new DW.WrapSquare { WrapText = DW.WrapTextValues.BothSides },
			new DW.DocProperties { Id = 2U, Name = name },
			new DW.NonVisualGraphicFrameDrawingProperties(),
			new A.Graphic(graphicData))
		{
			DistanceFromTop = 0U,
			DistanceFromBottom = 0U,
			DistanceFromLeft = 114300U,
			DistanceFromRight = 114300U,
			SimplePos = false,
			RelativeHeight = 0U,
			BehindDoc = false,
			Locked = false,
			LayoutInCell = true,
			AllowOverlap = true
		};

		return new Drawing(anchor);
	}

	private static PIC.Picture CreatePictureElement(string relationshipId, string name, long widthEmu, long heightEmu)
	{
		return new PIC.Picture(
			new PIC.NonVisualPictureProperties(
				new PIC.NonVisualDrawingProperties { Id = 1U, Name = name },
				new PIC.NonVisualPictureDrawingProperties()),
			new PIC.BlipFill(
				new A.Blip { Embed = relationshipId },
				new A.Stretch(new A.FillRectangle())),
			new PIC.ShapeProperties(
				new A.Transform2D(
					new A.Offset { X = 0L, Y = 0L },
					new A.Extents { Cx = widthEmu, Cy = heightEmu }),
				new A.PresetGeometry(new A.AdjustValueList())
				{
					Preset = A.ShapeTypeValues.Rectangle
				}));
	}

	private static byte[] CreateMinimalPngBytes()
	{
		const string base64Png = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAdSURBVDhPY/jPwPCfEsyALkAqHjVg1IBRAwaLAQAwxP4Q7zYsrwAAAABJRU5ErkJggg==";
		return Convert.FromBase64String(base64Png);
	}
}
