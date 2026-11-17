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
internal static partial class TestCorpusGenerator
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
			("with-toc", GenerateWithToc),
			("with-tof", GenerateWithTof),
			("with-cross-refs", GenerateWithCrossRefs),
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
			CreateRunStyleParagraph(),
			CreateScriptParagraph(),
			CreateColorParagraph(),
			DefaultSectionProperties());

		mainPart.Document = new Document(body);
	}

	private static Paragraph CreateRunStyleParagraph()
	{
		return new Paragraph(
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
				new Text("24pt text.") { Space = SpaceProcessingModeValues.Preserve }));
	}

	private static Paragraph CreateScriptParagraph()
	{
		return new Paragraph(
			new Run(new Text("Text with ") { Space = SpaceProcessingModeValues.Preserve }),
			new Run(
				new RunProperties(new VerticalTextAlignment { Val = VerticalPositionValues.Superscript }),
				new Text("superscript")),
			new Run(new Text(" and ") { Space = SpaceProcessingModeValues.Preserve }),
			new Run(
				new RunProperties(new VerticalTextAlignment { Val = VerticalPositionValues.Subscript }),
				new Text("subscript")),
			new Run(new Text(".")));
	}

	private static Paragraph CreateColorParagraph()
	{
		return new Paragraph(
			new Run(
				new RunProperties(new Color { Val = "FF0000" }),
				new Text("Red text. ") { Space = SpaceProcessingModeValues.Preserve }),
			new Run(
				new RunProperties(new Color { Val = "0000FF" }),
				new Text("Blue text. ") { Space = SpaceProcessingModeValues.Preserve }),
			new Run(
				new RunProperties(new Color { Val = "008000" }),
				new Text("Green text.")));
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
}
