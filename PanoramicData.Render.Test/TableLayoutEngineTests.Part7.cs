namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed partial class TableLayoutEngineTests
{
	[Fact]
	public void ComputeCellBackgrounds_MergedCell_UsesMergedCellGeometry()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement
				{
					Cells =
					[
						new TableCellElement
						{
							Blocks = [],
							GridSpan = 2,
							Shading = new ParagraphShading(ShadingPattern.Clear, FillColor: "ABCDEF"),
						},
					],
				},
				new TableRowElement { Cells = [MakeCell(), MakeCell()] },
			],
		};

		var layout = TableLayoutEngine.Layout(table, 2000f);

		var backgrounds = TableLayoutEngine.ComputeCellBackgrounds(layout);

		backgrounds.Should().ContainSingle();
		backgrounds[0].X.Should().Be(0f);
		backgrounds[0].Y.Should().Be(0f);
		backgrounds[0].Width.Should().Be(2000f);
		backgrounds[0].Height.Should().Be(240f);
	}

	[Fact]
	public void ComputeCellBackgrounds_UsesConditionalStyleShadingWhenCellHasNoDirectShading()
	{
		var firstRowShading = new Shading { Fill = "ddeeaa" };
		firstRowShading.SetAttribute(new OpenXmlAttribute("w", "val", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "clear"));

		var style = new Style(
			new TableStyleProperties(new TableCellProperties(firstRowShading))
			{ Type = TableStyleOverrideValues.FirstRow })
		{
			Type = StyleValues.Table,
			StyleId = "HeaderShade"
		};

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(style));
		using var doc = DocxDocument.Load(stream);

		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement { Cells = [MakeCell()] },
				new TableRowElement { Cells = [MakeCell()] },
			],
			StyleId = "HeaderShade",
			Look = new TableLookOptions(ApplyFirstRow: true),
		};

		var layout = TableLayoutEngine.Layout(table, 1000f);

		var backgrounds = TableLayoutEngine.ComputeCellBackgrounds(layout, doc.StylesPart?.Styles);

		backgrounds.Should().ContainSingle();
		backgrounds[0].RowIndex.Should().Be(0);
		backgrounds[0].Shading.FillColor.Should().Be("DDEEAA");
	}

	[Fact]
	public void ComputeCellBackgrounds_DirectCellShadingOverridesConditionalStyleShading()
	{
		var firstRowShading = new Shading { Fill = "ddeeaa" };
		firstRowShading.SetAttribute(new OpenXmlAttribute("w", "val", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "clear"));

		var style = new Style(
			new TableStyleProperties(new TableCellProperties(firstRowShading))
			{ Type = TableStyleOverrideValues.FirstRow })
		{
			Type = StyleValues.Table,
			StyleId = "HeaderShade"
		};

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(style));
		using var doc = DocxDocument.Load(stream);

		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement
				{
					Cells =
					[
						new TableCellElement
						{
							Blocks = [],
							Shading = new ParagraphShading(ShadingPattern.Clear, FillColor: "ABCDEF"),
						},
					],
				},
			],
			StyleId = "HeaderShade",
			Look = new TableLookOptions(ApplyFirstRow: true),
		};

		var layout = TableLayoutEngine.Layout(table, 1000f);

		var backgrounds = TableLayoutEngine.ComputeCellBackgrounds(layout, doc.StylesPart?.Styles);

		backgrounds.Should().ContainSingle();
		backgrounds[0].Shading.FillColor.Should().Be("ABCDEF");
	}

	// ---- Cell content layout (4.2.3) ----

	[Fact]
	public void LayoutCellContent_NullCell_ThrowsArgumentNullException()
	{
		var act = () => TableLayoutEngine.LayoutCellContent(null!);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("cell");
	}

	[Fact]
	public void LayoutCellContent_EmptyBlocks_ReturnsEmpty()
	{
		var cell = MakeCell();

		var (blocks, totalHeight) = TableLayoutEngine.LayoutCellContent(cell);

		blocks.Should().BeEmpty();
		totalHeight.Should().Be(0f);
	}

	[Fact]
	public void LayoutCellContent_SingleParagraph_ReturnsOneBlock()
	{
		var cell = MakeCellWithParagraphs(1);

		var (blocks, totalHeight) = TableLayoutEngine.LayoutCellContent(cell);

		blocks.Should().HaveCount(1);
		blocks[0].Block.Should().BeOfType<ParagraphBlock>();
		totalHeight.Should().Be(TableLayoutEngine.DefaultRowHeightTwips);
	}

	[Fact]
	public void LayoutCellContent_ThreeParagraphs_HeightIsSumOfEstimates()
	{
		var cell = MakeCellWithParagraphs(3);

		var (blocks, totalHeight) = TableLayoutEngine.LayoutCellContent(cell);

		blocks.Should().HaveCount(3);
		totalHeight.Should().Be(TableLayoutEngine.DefaultRowHeightTwips * 3f);
	}

	[Fact]
	public void LayoutCellContent_TablePlaceholderBlock_UsesDefaultHeight()
	{
		var cell = new TableCellElement
		{
			Blocks = [new TablePlaceholderBlock { TableElement = new DocumentFormat.OpenXml.Wordprocessing.Table() }],
		};

		var (blocks, totalHeight) = TableLayoutEngine.LayoutCellContent(cell);

		blocks.Should().HaveCount(1);
		totalHeight.Should().Be(TableLayoutEngine.DefaultRowHeightTwips);
	}

	[Fact]
	public void MeasureCellContentHeight_EmptyCell_ReturnsZero()
	{
		var cell = MakeCell();

		var height = TableLayoutEngine.MeasureCellContentHeight(cell);

		height.Should().Be(0f);
	}

	[Fact]
	public void MeasureCellContentHeight_TwoParagraphs_ReturnsSumOfEstimates()
	{
		var cell = MakeCellWithParagraphs(2);

		var height = TableLayoutEngine.MeasureCellContentHeight(cell);

		height.Should().Be(TableLayoutEngine.DefaultRowHeightTwips * 2f);
	}

	[Fact]
	public void ComputeRowHeights_ExactHeightRule_IgnoresContent()
	{
		var cell = MakeCellWithParagraphs(5);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(4800f)],
			Rows = [new TableRowElement
			{
				Cells = [cell],
				HeightTwips = 300f,
				HeightRule = RowHeightRule.Exact,
			}],
		};

		var heights = TableLayoutEngine.ComputeRowHeights(table);

		heights[0].Should().Be(300f); // Exact ignores content
	}

	[Fact]
	public void ComputeRowHeights_AtLeastHeightRule_UsesMaxOfSpecifiedAndContent()
	{
		var cell = MakeCellWithParagraphs(5); // 5 × 240 = 1200
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(4800f)],
			Rows = [new TableRowElement
			{
				Cells = [cell],
				HeightTwips = 300f,
				HeightRule = RowHeightRule.AtLeast,
			}],
		};

		var heights = TableLayoutEngine.ComputeRowHeights(table);

		heights[0].Should().Be(1200f); // Content (1200) > specified (300)
	}

	[Fact]
	public void ComputeRowHeights_AutoWithContent_UsesContentHeight()
	{
		var cell = MakeCellWithParagraphs(3); // 3 × 240 = 720
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(4800f)],
			Rows = [new TableRowElement
			{
				Cells = [cell],
				HeightTwips = 200f,
			}],
		};

		var heights = TableLayoutEngine.ComputeRowHeights(table);

		heights[0].Should().Be(720f); // Content (720) > specified (200)
	}

	[Fact]
	public void ComputeRowHeights_ContinueCell_IgnoredForContentHeight()
	{
		// A continue cell's content shouldn't affect the row height
		var contCell = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() },
					  new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() },
					  new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() },
					  new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() },
					  new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() }],
			VerticalMerge = VerticalMergeState.Continue,
		};
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(4800f)],
			Rows = [new TableRowElement { Cells = [contCell] }],
		};

		var heights = TableLayoutEngine.ComputeRowHeights(table);

		// Continue cell content ignored, no other cells → DefaultRowHeightTwips
		heights[0].Should().Be(TableLayoutEngine.DefaultRowHeightTwips);
	}

	private static TableCellElement MakeCellWithParagraphs(int count, CellMargins? margins = null)
	{
		var blocks = new List<DocumentBlock>();
		for (var i = 0; i < count; i++)
		{
			blocks.Add(new ParagraphBlock
			{
				SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph()
			});
		}

		return new TableCellElement { Blocks = blocks, Margins = margins ?? CellMargins.None };
	}

	private static TableCellElement MakeCell(
		int gridSpan = 1,
		VerticalMergeState verticalMerge = VerticalMergeState.None) => new()
	{
		Blocks = [],
		GridSpan = gridSpan,
		VerticalMerge = verticalMerge,
	};

	// ---- Cell margins (4.2.4) ----

	[Fact]
	public void ComputeContentWidth_NoMargins_ReturnsCellWidth()
	{
		var width = TableLayoutEngine.ComputeContentWidth(4800f, CellMargins.None);

		width.Should().Be(4800f);
	}

	[Fact]
	public void ComputeContentWidth_WithMargins_SubtractsLeftRight()
	{
		var margins = new CellMargins(0f, 100f, 0f, 150f);

		var width = TableLayoutEngine.ComputeContentWidth(4800f, margins);

		width.Should().Be(4550f);
	}

	[Fact]
	public void ComputeContentWidth_WithBorderSpacing_SubtractsBothSides()
	{
		var width = TableLayoutEngine.ComputeContentWidth(4800f, CellMargins.None, 50f);

		width.Should().Be(4700f);
	}

	[Fact]
	public void ComputeContentWidth_MarginsExceedWidth_ReturnsZero()
	{
		var margins = new CellMargins(0f, 3000f, 0f, 2000f);

		var width = TableLayoutEngine.ComputeContentWidth(4800f, margins);

		width.Should().Be(0f);
	}

	[Fact]
	public void MeasureCellContentHeight_WithMargins_IncludesTopBottom()
	{
		var margins = new CellMargins(50f, 0f, 75f, 0f);
		var cell = MakeCellWithParagraphs(2, margins);

		var height = TableLayoutEngine.MeasureCellContentHeight(cell);

		// 2 × 240 + 50 + 75 = 605
		height.Should().Be(605f);
	}

	[Fact]
	public void MeasureCellContentHeight_EmptyCellWithMargins_ReturnsMarginSum()
	{
		var cell = new TableCellElement
		{
			Blocks = [],
			Margins = new CellMargins(100f, 0f, 100f, 0f),
		};

		var height = TableLayoutEngine.MeasureCellContentHeight(cell);

		height.Should().Be(200f);
	}

	[Fact]
	public void LayoutCellContent_EmptyCellWithMargins_ReturnsMarginHeight()
	{
		var cell = new TableCellElement
		{
			Blocks = [],
			Margins = new CellMargins(60f, 0f, 40f, 0f),
		};

		var (blocks, totalHeight) = TableLayoutEngine.LayoutCellContent(cell);

		blocks.Should().BeEmpty();
		totalHeight.Should().Be(100f);
	}

	[Fact]
	public void LayoutCellContent_WithMargins_HeightIncludesMargins()
	{
		var margins = new CellMargins(30f, 0f, 20f, 0f);
		var cell = MakeCellWithParagraphs(1, margins);

		var (blocks, totalHeight) = TableLayoutEngine.LayoutCellContent(cell);

		blocks.Should().HaveCount(1);
		// 30 (top) + 240 (content) + 20 (bottom) = 290
		totalHeight.Should().Be(290f);
	}

	[Fact]
	public void LayoutCellContent_WithBorderSpacing_HeightIncludesSpacing()
	{
		var cell = MakeCellWithParagraphs(1);

		var (blocks, totalHeight) = TableLayoutEngine.LayoutCellContent(cell, 15f);

		blocks.Should().HaveCount(1);
		// 15 (top spacing) + 240 (content) + 15 (bottom spacing) = 270
		totalHeight.Should().Be(270f);
	}
}
