namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using static PanoramicData.Render.Test.MergeLayoutBuilders;
using static PanoramicData.Render.Test.NestedTableBuilders;
using static PanoramicData.Render.Test.TableLayoutBuilders;

public sealed partial class TableLayoutEngineTests
{
	[Fact]
	public void ComputeMergedCellContentLayouts_NullLayout_ThrowsArgumentNullException()
	{
		AssertRejectsNullLayout(TableLayoutEngine.ComputeMergedCellContentLayouts);
	}

	[Fact]
	public void ComputeMergedCellContentLayouts_NoMergedCells_ReturnsEmpty()
	{
		var layout = LayoutGrid(9600f, [1000f], RowOf(MakeCell()));

		TableLayoutEngine.ComputeMergedCellContentLayouts(layout).Should().BeEmpty();
	}

	[Fact]
	public void ComputeMergedCellContentLayouts_HorizontalMerge_AdjustsContentAreaForMargins()
	{
		var mergedCell = ParagraphCell(1, new CellMargins(20f, 50f, 30f, 40f), gridSpan: 2);
		var layout = LayoutGrid(9600f, [1000f, 1500f, 500f], RowAt(400f, mergedCell, MakeCell()));

		var content = SingleContentLayout(layout);

		content.CellWidth.Should().Be(2500f);
		content.ContentX.Should().Be(40f); // left margin
		content.ContentWidth.Should().Be(2410f); // 2500 - 40 - 50
		content.ContentHeight.Should().Be(240f); // one paragraph block
		content.Blocks.Should().HaveCount(1);
	}

	[Fact]
	public void ComputeMergedCellContentLayouts_VerticalMerge_CenterAlignmentAdjustsY()
	{
		var restartCell = ParagraphCell(1, verticalMerge: VerticalMergeState.Restart, verticalAlignment: CellVerticalAlignment.Center);
		var layout = SingleColumnMergeLayout(1200f, 300f, 500f, restartCell);

		var content = SingleContentLayout(layout);

		content.CellHeight.Should().Be(800f);
		content.ContentY.Should().Be(280f); // (800 - 240) / 2
	}

	[Fact]
	public void ComputeMergedCellContentLayouts_CombinedMerge_UsesRectangularRegionGeometry()
	{
		var restart = ParagraphCell(2, new CellMargins(10f, 20f, 10f, 20f), gridSpan: 2, verticalMerge: VerticalMergeState.Restart);
		var layout = CombinedMergeLayout(restart, 1000f);

		var content = SingleContentLayout(layout);

		content.RowSpan.Should().Be(2);
		content.ColumnSpan.Should().Be(2);
		content.CellWidth.Should().Be(2500f);
		content.CellHeight.Should().Be(1100f);
		content.ContentWidth.Should().Be(2460f);
		content.ContentHeight.Should().Be(480f); // 2 paragraph blocks
		content.Blocks.Should().HaveCount(2);
	}

	[Fact]
	public void ComputeMergedCellContentLayouts_MultipleMergedCells_ReturnsInReadingOrder()
	{
		var layout = MixedMergeLayout(0f);

		var contentLayouts = TableLayoutEngine.ComputeMergedCellContentLayouts(layout);

		contentLayouts.Select(content => (content.StartRowIndex, content.StartColumnIndex)).Should().Equal([(0, 0), (1, 0)]);
	}

	// ---- Table cell backgrounds (4.8.2) ----

	[Fact]
	public void ComputeCellBackgrounds_NullLayout_ThrowsArgumentNullException()
	{
		AssertRejectsNullLayout(TableLayoutEngine.ComputeCellBackgrounds);
	}

	[Fact]
	public void ComputeCellBackgrounds_NoVisibleShading_ReturnsEmpty()
	{
		var layout = LayoutGrid(1200f, [1200f], RowOf(MakeCell()));

		TableLayoutEngine.ComputeCellBackgrounds(layout).Should().BeEmpty();
	}

	[Fact]
	public void ComputeCellBackgrounds_ShadedCells_ReturnsReadingOrderRectangles()
	{
		var layout = LayoutGrid(2500f, [1000f, 1500f],
			RowOf(
				ShadedCell(new ParagraphShading(ShadingPattern.Clear, FillColor: "FFFF00")),
				ShadedCell(new ParagraphShading(ShadingPattern.Solid, PatternColor: "00FF00"))));

		var backgrounds = TableLayoutEngine.ComputeCellBackgrounds(layout);

		backgrounds.Should().HaveCount(2);

		backgrounds[0].Should().BeEquivalentTo(new { RowIndex = 0, ColumnIndex = 0, X = 0f, Y = 0f, Width = 1000f, Height = 240f });
		backgrounds[0].Shading.FillColor.Should().Be("FFFF00");

		backgrounds[1].Should().BeEquivalentTo(new { RowIndex = 0, ColumnIndex = 1, X = 1000f, Width = 1500f });
		backgrounds[1].Shading.Pattern.Should().Be(ShadingPattern.Solid);
		backgrounds[1].Shading.PatternColor.Should().Be("00FF00");
	}
}
