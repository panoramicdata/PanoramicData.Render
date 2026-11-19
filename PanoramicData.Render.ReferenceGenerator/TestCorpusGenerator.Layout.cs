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
		=> BuildDocument(path, mainPart =>
		{
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

			return TitledBody(
				"Multi-Level List:",
				MakeListParagraph("First item", 1, 0),
				MakeListParagraph("Sub-item A", 1, 1),
				MakeListParagraph("Sub-item B", 1, 1),
				MakeListParagraph("Sub-sub-item i", 1, 2),
				MakeListParagraph("Second item", 1, 0),
				MakeListParagraph("Sub-item A", 1, 1),
				MakeListParagraph("Third item", 1, 0));
		});

	private static void GenerateAutoFitTable(string path)
		=> BuildDocument(path, _ => TitledBody(
			"Auto-fit Table",
			new Table(
				new TableProperties(
					new TableLayout { Type = TableLayoutValues.Autofit },
					new TableWidth { Width = "0", Type = TableWidthUnitValues.Auto },
					CreateSingleBorders()),
				MakeRow(true, "ID", "Description", "Notes"),
				MakeRow(false, "1", "Short", "Auto-fit should keep this narrow."),
				MakeRow(false, "2", "A significantly longer description to force the middle column wider.", "Tests proportional auto-fit behavior."))));

	private static void GenerateTableStyleFirstLast(string path)
		=> BuildDocument(path, mainPart =>
		{
			AddTableStyleDefinitions(mainPart,
				CreateTableStyle(
					"CorpusFirstLastStyle",
					(TableStyleOverrideValues.FirstRow, CreateShading("D9E1F2")),
					(TableStyleOverrideValues.LastRow, CreateShading("FCE4D6")),
					(TableStyleOverrideValues.FirstColumn, CreateShading("E2F0D9")),
					(TableStyleOverrideValues.LastColumn, CreateShading("FFF2CC"))));

			return TitledBody(
				"Table Style: First/Last Row + First/Last Column",
				new Table(
					new TableProperties(
						new TableStyle { Val = "CorpusFirstLastStyle" },
						CreateTableLook(applyFirstRow: true, applyLastRow: true, applyFirstColumn: true, applyLastColumn: true),
						CreateSingleBorders()),
					MakeRow(true, "Region", "Q1", "Q2", "Total"),
					MakeRow(false, "North", "120", "140", "260"),
					MakeRow(false, "South", "95", "110", "205"),
					MakeRow(true, "Grand Total", "215", "250", "465")));
		});

	private static void GenerateTableStyleBanding(string path)
		=> BuildDocument(path, mainPart =>
		{
			AddTableStyleDefinitions(mainPart,
				CreateTableStyle(
					"CorpusBandingStyle",
					(TableStyleOverrideValues.Band1Horizontal, CreateShading("F2F2F2")),
					(TableStyleOverrideValues.Band2Horizontal, CreateShading("E6EEF8")),
					(TableStyleOverrideValues.Band1Vertical, CreateShading("FFF2CC")),
					(TableStyleOverrideValues.Band2Vertical, CreateShading("E2F0D9"))));

			return TitledBody(
				"Table Style: Odd/Even Row + Column Banding",
				new Table(
					new TableProperties(
						new TableStyle { Val = "CorpusBandingStyle" },
						CreateTableLook(applyBandedRows: true, applyBandedColumns: true),
						CreateSingleBorders()),
					MakeRow(true, "C1", "C2", "C3", "C4"),
					MakeRow(false, "R2C1", "R2C2", "R2C3", "R2C4"),
					MakeRow(false, "R3C1", "R3C2", "R3C3", "R3C4"),
					MakeRow(false, "R4C1", "R4C2", "R4C3", "R4C4"),
					MakeRow(false, "R5C1", "R5C2", "R5C3", "R5C4")));
		});

	private static string AddPngImage(MainDocumentPart mainPart)
	{
		var imagePart = mainPart.AddImagePart(ImagePartType.Png);
		using (var imageStream = new MemoryStream(CreateMinimalPngBytes()))
		{
			imagePart.FeedData(imageStream);
		}

		return mainPart.GetIdOfPart(imagePart);
	}

	private static void GenerateInlineImages(string path)
		=> BuildDocument(path, mainPart =>
		{
			var inlineDrawing = CreateInlineImageDrawing(AddPngImage(mainPart), 914400L, 914400L, "InlineImage");

			return TitledBody(
				"Inline image demonstration:",
				new Paragraph(new Run(new Text("Before ")), new Run(inlineDrawing), new Run(new Text(" After"))));
		});

	private static void GenerateFloatingImages(string path)
		=> BuildDocument(path, mainPart =>
		{
			var anchorDrawing = CreateAnchorImageDrawing(AddPngImage(mainPart), 1200000L, 800000L, "FloatingImage");

			return TitledBody(
				"Floating image with wrapping demonstration.",
				new Paragraph(
					new Run(anchorDrawing),
					new Run(new Text(
						" This paragraph contains additional text that should wrap around a floating image in Word reference output."))),
				TextParagraph("Second paragraph to exercise layout after anchored object."));
		});

	private static void GenerateFootnotes(string path)
		=> BuildDocument(path, mainPart =>
		{
			var footnotesPart = mainPart.AddNewPart<FootnotesPart>();
			var separator = new Footnote { Type = FootnoteEndnoteValues.Separator, Id = -1 };
			separator.Append(new Paragraph(new Run(new SeparatorMark())));

			var note = new Footnote { Id = 1 };
			note.Append(TextParagraph("This is a sample footnote for visual regression."));
			footnotesPart.Footnotes = new Footnotes(separator, note);

			return
			[
				new Paragraph(
					new Run(new Text("Footnote example")),
					new Run(new FootnoteReference { Id = 1 })),
				TextParagraph("Additional body content after the footnote reference."),
				DefaultSectionProperties()
			];
		});

	private static void GenerateColumns(string path)
		=> BuildDocument(path, _ =>
		[
			TextParagraph("Two-column layout test."),
			TextParagraph("Column text paragraph 1. " +
				"This document verifies that section column metadata is preserved in the reference output."),
			TextParagraph("Column text paragraph 2. " +
				"Additional content helps Word flow text across both columns."),
			TextParagraph("Column text paragraph 3. " +
				"Further lines provide density for visual comparison."),
			new SectionProperties(
				new PageSize { Width = 12240, Height = 15840 },
				new PageMargin { Top = 1440, Right = 1440, Bottom = 1440, Left = 1440 },
				new Columns { ColumnCount = 2, Space = "720" })
		]);

	private static void GenerateWatermark(string path)
		=> BuildDocument(path, mainPart =>
		{
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

			return
			[
				TextParagraph("Watermark test content in body area."),
				TextParagraph("Word reference should include a centered DRAFT watermark."),
				new SectionProperties(
					new PageSize { Width = 12240, Height = 15840 },
					new PageMargin { Top = 1440, Right = 1440, Bottom = 1440, Left = 1440, Header = 720, Footer = 720 },
					new HeaderReference { Type = HeaderFooterValues.Default, Id = headerRelId })
			];
		});
}