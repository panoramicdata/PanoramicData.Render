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
		=> BuildDocument(path, _ =>
		[
			new Paragraph(
				new Run(new Text("The quick brown fox jumps over the lazy dog. ") { Space = SpaceProcessingModeValues.Preserve }),
				new Run(new Text("This is a basic text document used for visual regression testing. ") { Space = SpaceProcessingModeValues.Preserve }),
				new Run(new Text("It contains a single paragraph of plain text with no special formatting."))),
			DefaultSectionProperties()
		]);

	private static void GenerateParagraphAlignment(string path)
		=> BuildDocument(path, _ =>
		[
			MakeParagraph("This paragraph is left-aligned (the default).", JustificationValues.Left),
			MakeParagraph("This paragraph is center-aligned.", JustificationValues.Center),
			MakeParagraph("This paragraph is right-aligned.", JustificationValues.Right),
			MakeParagraph(
				"This paragraph is justified. It contains enough text to demonstrate how Word distributes space between words when justifying a line of text across the full width of the page margin.",
				JustificationValues.Both),
			DefaultSectionProperties()
		]);

	private static void GenerateParagraphIndentation(string path)
		=> BuildDocument(path, _ =>
		[
			TextParagraph("No indentation (default)."),
			MakeIndentedParagraph("Left indent of 720 twips (0.5 inch).", left: "720"),
			MakeIndentedParagraph("Left indent of 1440 twips (1 inch).", left: "1440"),
			MakeIndentedParagraph("Right indent of 720 twips (0.5 inch).", right: "720"),
			MakeIndentedParagraph("First line indent of 720 twips (0.5 inch).", firstLine: "720"),
			MakeIndentedParagraph("Hanging indent: first line at 0, rest at 720 twips.", left: "720", hanging: "720"),
			DefaultSectionProperties()
		]);

	private static void GenerateCharacterFormatting(string path)
		=> BuildDocument(path, _ =>
		[
			CreateRunStyleParagraph(),
			CreateScriptParagraph(),
			CreateColorParagraph(),
			DefaultSectionProperties()
		]);

	private static Paragraph CreateRunStyleParagraph()
	{
		return new Paragraph(
			new Run(new Text("Normal text. ") { Space = SpaceProcessingModeValues.Preserve }),
			StyledRun(new Bold(), "Bold text. "),
			StyledRun(new Italic(), "Italic text. "),
			new Run(
				new RunProperties(new Bold(), new Italic()),
				new Text("Bold italic text. ") { Space = SpaceProcessingModeValues.Preserve }),
			StyledRun(new Underline { Val = UnderlineValues.Single }, "Underlined text. "),
			StyledRun(new Strike(), "Strikethrough text. "),
			StyledRun(new FontSize { Val = "32" }, "16pt text. "),
			StyledRun(new FontSize { Val = "48" }, "24pt text."));
	}

	private static Run StyledRun(OpenXmlElement property, string text, bool preserveSpace = true)
		=> new(
			new RunProperties(property),
			preserveSpace ? new Text(text) { Space = SpaceProcessingModeValues.Preserve } : new Text(text));

	private static Paragraph CreateScriptParagraph()
	{
		return new Paragraph(
			new Run(new Text("Text with ") { Space = SpaceProcessingModeValues.Preserve }),
			StyledRun(new VerticalTextAlignment { Val = VerticalPositionValues.Superscript }, "superscript", preserveSpace: false),
			new Run(new Text(" and ") { Space = SpaceProcessingModeValues.Preserve }),
			StyledRun(new VerticalTextAlignment { Val = VerticalPositionValues.Subscript }, "subscript", preserveSpace: false),
			new Run(new Text(".")));
	}

	private static Paragraph CreateColorParagraph()
	{
		return new Paragraph(
			StyledRun(new Color { Val = "FF0000" }, "Red text. "),
			StyledRun(new Color { Val = "0000FF" }, "Blue text. "),
			StyledRun(new Color { Val = "008000" }, "Green text.", preserveSpace: false));
	}

	private static void GenerateSimpleTable(string path)
		=> BuildDocument(path, _ => TitledBody(
			"Simple Table",
			new Table(
				new TableProperties(
					CreateSingleBorders(),
					new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct }),
				MakeRow(true, "Name", "Value", "Description"),
				MakeRow(false, "Alpha", "100", "First item"),
				MakeRow(false, "Beta", "200", "Second item"),
				MakeRow(false, "Gamma", "300", "Third item"))));

	private static void GenerateMergedCellsTable(string path)
		=> BuildDocument(path, _ => TitledBody(
			"Table with Merged Cells",
			new Table(
				new TableProperties(
					CreateSingleBorders(),
					new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct }),
				new TableRow(
					MergedCell(new HorizontalMerge { Val = MergedCellValues.Restart }, "Merged across 3 columns"),
					MergedCell(new HorizontalMerge { Val = MergedCellValues.Continue }),
					MergedCell(new HorizontalMerge { Val = MergedCellValues.Continue })),
				new TableRow(
					MergedCell(new VerticalMerge { Val = MergedCellValues.Restart }, "Vertically merged"),
					MakeTableCell("B2"),
					MakeTableCell("C2")),
				new TableRow(
					MergedCell(new VerticalMerge()),
					MakeTableCell("B3"),
					MakeTableCell("C3")))));

	private static TableCell MergedCell(OpenXmlElement merge, string? text = null)
		=> new(
			new TableCellProperties(merge),
			text is null ? new Paragraph() : TextParagraph(text));
}