namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;
using static PanoramicData.Render.Test.NestedTableBuilders;
using static PanoramicData.Render.Test.TableLayoutBuilders;

public sealed partial class TableLayoutEngineTests
{
	[Fact]
	public void ComputeCellBackgrounds_MergedCell_UsesMergedCellGeometry()
	{
		var layout = LayoutGrid(2000f, [1000f, 1000f],
			RowOf(ShadedCell(new ParagraphShading(ShadingPattern.Clear, FillColor: "ABCDEF"), 2)),
			RowOf(MakeCell(), MakeCell()));

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
		var backgrounds = HeaderShadeBackgrounds(RowOf(MakeCell()), RowOf(MakeCell()));

		backgrounds.Should().ContainSingle();
		backgrounds[0].RowIndex.Should().Be(0);
		backgrounds[0].Shading.FillColor.Should().Be("DDEEAA");
	}

	[Fact]
	public void ComputeCellBackgrounds_DirectCellShadingOverridesConditionalStyleShading()
	{
		var backgrounds = HeaderShadeBackgrounds(RowOf(ShadedCell(new ParagraphShading(ShadingPattern.Clear, FillColor: "ABCDEF"))));

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
		AssertLayout(MakeCell(), 0, 0f);
	}

	[Fact]
	public void LayoutCellContent_SingleParagraph_ReturnsOneBlock()
	{
		AssertLayout(MakeCellWithParagraphs(1), 1, TableLayoutEngine.DefaultRowHeightTwips);

		TableLayoutEngine.LayoutCellContent(MakeCellWithParagraphs(1)).Blocks[0].Block.Should().BeOfType<ParagraphBlock>();
	}

	[Fact]
	public void LayoutCellContent_ThreeParagraphs_HeightIsSumOfEstimates()
	{
		AssertLayout(MakeCellWithParagraphs(3), 3, TableLayoutEngine.DefaultRowHeightTwips * 3f);
	}

	[Fact]
	public void LayoutCellContent_TablePlaceholderBlock_UsesDefaultHeight()
	{
		var cell = new TableCellElement
		{
			Blocks = [new TablePlaceholderBlock { TableElement = new DocumentFormat.OpenXml.Wordprocessing.Table() }],
		};

		AssertLayout(cell, 1, TableLayoutEngine.DefaultRowHeightTwips);
	}

	[Fact]
	public void MeasureCellContentHeight_EmptyCell_ReturnsZero()
	{
		TableLayoutEngine.MeasureCellContentHeight(MakeCell()).Should().Be(0f);
	}

	[Fact]
	public void MeasureCellContentHeight_TwoParagraphs_ReturnsSumOfEstimates()
	{
		TableLayoutEngine.MeasureCellContentHeight(MakeCellWithParagraphs(2)).Should().Be(TableLayoutEngine.DefaultRowHeightTwips * 2f);
	}

	[Fact]
	public void ComputeRowHeights_ExactHeightRule_IgnoresContent()
	{
		var row = new TableRowElement { Cells = [MakeCellWithParagraphs(5)], HeightTwips = 300f, HeightRule = RowHeightRule.Exact };

		TableLayoutEngine.ComputeRowHeights(SingleColumnTable(row))[0].Should().Be(300f); // Exact ignores content
	}

	[Fact]
	public void ComputeRowHeights_AtLeastHeightRule_UsesMaxOfSpecifiedAndContent()
	{
		var row = new TableRowElement { Cells = [MakeCellWithParagraphs(5)], HeightTwips = 300f, HeightRule = RowHeightRule.AtLeast };

		TableLayoutEngine.ComputeRowHeights(SingleColumnTable(row))[0].Should().Be(1200f); // 5 x 240 > 300
	}

	[Fact]
	public void ComputeRowHeights_AutoWithContent_UsesContentHeight()
	{
		TableLayoutEngine.ComputeRowHeights(SingleColumnTable(RowAt(200f, MakeCellWithParagraphs(3))))[0].Should().Be(720f); // 3 x 240 > 200
	}

	[Fact]
	public void ComputeRowHeights_ContinueCell_IgnoredForContentHeight()
	{
		// A continue cell's content shouldn't affect the row height
		var contCell = ParagraphCell(5, verticalMerge: VerticalMergeState.Continue);

		// Continue cell content ignored, no other cells, so DefaultRowHeightTwips
		TableLayoutEngine.ComputeRowHeights(SingleColumnTable(RowOf(contCell)))[0].Should().Be(TableLayoutEngine.DefaultRowHeightTwips);
	}

	private static TableCellElement MakeCellWithParagraphs(int count, CellMargins? margins = null)
		=> ParagraphCell(count, margins);

	internal static TableCellElement MakeCell(
		int gridSpan = 1,
		VerticalMergeState verticalMerge = VerticalMergeState.None) => new()
	{
		Blocks = [],
		GridSpan = gridSpan,
		VerticalMerge = verticalMerge,
	};

	private static IReadOnlyList<TableCellBackground> HeaderShadeBackgrounds(params TableRowElement[] rows)
	{
		using var stream = TestDocxBuilder.CreateDocxWithStyles(HeaderShadeStyles());
		using var doc = DocxDocument.Load(stream);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f)],
			Rows = rows,
			StyleId = "HeaderShade",
			Look = new TableLookOptions(ApplyFirstRow: true),
		};

		return TableLayoutEngine.ComputeCellBackgrounds(TableLayoutEngine.Layout(table, 1000f), doc.StylesPart?.Styles);
	}

	private static void AssertLayout(TableCellElement cell, int expectedBlocks, float expectedHeight)
	{
		var (blocks, totalHeight) = TableLayoutEngine.LayoutCellContent(cell);
		blocks.Should().HaveCount(expectedBlocks);
		totalHeight.Should().Be(expectedHeight);
	}

	// ---- Cell margins (4.2.4) ----

	[Fact]
	public void ComputeContentWidth_NoMargins_ReturnsCellWidth()
	{
		TableLayoutEngine.ComputeContentWidth(4800f, CellMargins.None).Should().Be(4800f);
	}

	[Fact]
	public void ComputeContentWidth_WithMargins_SubtractsLeftRight()
	{
		TableLayoutEngine.ComputeContentWidth(4800f, new CellMargins(0f, 100f, 0f, 150f)).Should().Be(4550f);
	}

	[Fact]
	public void ComputeContentWidth_WithBorderSpacing_SubtractsBothSides()
	{
		TableLayoutEngine.ComputeContentWidth(4800f, CellMargins.None, 50f).Should().Be(4700f);
	}

	[Fact]
	public void ComputeContentWidth_MarginsExceedWidth_ReturnsZero()
	{
		TableLayoutEngine.ComputeContentWidth(4800f, new CellMargins(0f, 3000f, 0f, 2000f)).Should().Be(0f);
	}

	[Fact]
	public void MeasureCellContentHeight_WithMargins_IncludesTopBottom()
	{
		var cell = MakeCellWithParagraphs(2, new CellMargins(50f, 0f, 75f, 0f));

		// 2 x 240 + 50 + 75 = 605
		TableLayoutEngine.MeasureCellContentHeight(cell).Should().Be(605f);
	}

	[Fact]
	public void MeasureCellContentHeight_EmptyCellWithMargins_ReturnsMarginSum()
	{
		TableLayoutEngine.MeasureCellContentHeight(MakeCellWithParagraphs(0, new CellMargins(100f, 0f, 100f, 0f))).Should().Be(200f);
	}

	[Fact]
	public void LayoutCellContent_EmptyCellWithMargins_ReturnsMarginHeight()
	{
		AssertLayout(MakeCellWithParagraphs(0, new CellMargins(60f, 0f, 40f, 0f)), 0, 100f);
	}

	[Fact]
	public void LayoutCellContent_WithMargins_HeightIncludesMargins()
	{
		// 30 (top) + 240 (content) + 20 (bottom) = 290
		AssertLayout(MakeCellWithParagraphs(1, new CellMargins(30f, 0f, 20f, 0f)), 1, 290f);
	}

	[Fact]
	public void LayoutCellContent_WithBorderSpacing_HeightIncludesSpacing()
	{
		var (blocks, totalHeight) = TableLayoutEngine.LayoutCellContent(MakeCellWithParagraphs(1), 15f);

		blocks.Should().HaveCount(1);
		// 15 (top spacing) + 240 (content) + 15 (bottom spacing) = 270
		totalHeight.Should().Be(270f);
	}
}
