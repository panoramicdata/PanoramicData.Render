namespace PanoramicData.Render.ReferenceGenerator;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

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
			("multi-level-list", GenerateMultiLevelList),
			("headers-and-footers", GenerateHeadersAndFooters),
			("multi-section", GenerateMultiSection),
			("tab-stops", GenerateTabStops),
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
}
