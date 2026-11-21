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

		var contentLayouts = TableLayoutEngine.ComputeMergedCellContentLayouts(layout);

		contentLayouts.Should().HaveCount(1);
		contentLayouts[0].CellWidth.Should().Be(2500f);
		contentLayouts[0].ContentX.Should().Be(40f); // left margin
		contentLayouts[0].ContentWidth.Should().Be(2410f); // 2500 - 40 - 50
		contentLayouts[0].ContentHeight.Should().Be(240f); // one paragraph block
		contentLayouts[0].Blocks.Should().HaveCount(1);
	}

	[Fact]
	public void ComputeMergedCellContentLayouts_VerticalMerge_CenterAlignmentAdjustsY()
	{
		var restartCell = ParagraphCell(1, verticalMerge: VerticalMergeState.Restart, verticalAlignment: CellVerticalAlignment.Center);
		var layout = LayoutGrid(9600f, [1200f],
			RowAt(300f, restartCell),
			RowAt(500f, MakeCell(verticalMerge: VerticalMergeState.Continue)));

		var contentLayouts = TableLayoutEngine.ComputeMergedCellContentLayouts(layout);

		contentLayouts.Should().HaveCount(1);
		contentLayouts[0].CellHeight.Should().Be(800f);
		contentLayouts[0].ContentY.Should().Be(280f); // (800 - 240) / 2
	}

	[Fact]
	public void ComputeMergedCellContentLayouts_CombinedMerge_UsesRectangularRegionGeometry()
	{
		var restart = ParagraphCell(2, new CellMargins(10f, 20f, 10f, 20f), gridSpan: 2, verticalMerge: VerticalMergeState.Restart);
		var layout = LayoutGrid(9600f, [1000f, 1500f, 1000f],
			RowAt(400f, restart, MakeCell()),
			RowAt(600f, MakeCell(gridSpan: 2, verticalMerge: VerticalMergeState.Continue), MakeCell()));

		var contentLayouts = TableLayoutEngine.ComputeMergedCellContentLayouts(layout);

		contentLayouts.Should().HaveCount(1);
		contentLayouts[0].RowSpan.Should().Be(2);
		contentLayouts[0].ColumnSpan.Should().Be(2);
		contentLayouts[0].CellWidth.Should().Be(2500f);
		contentLayouts[0].CellHeight.Should().Be(1100f);
		contentLayouts[0].ContentWidth.Should().Be(2460f);
		contentLayouts[0].ContentHeight.Should().Be(480f); // 2 paragraph blocks
		contentLayouts[0].Blocks.Should().HaveCount(2);
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

		backgrounds[0].RowIndex.Should().Be(0);
		backgrounds[0].ColumnIndex.Should().Be(0);
		backgrounds[0].X.Should().Be(0f);
		backgrounds[0].Y.Should().Be(0f);
		backgrounds[0].Width.Should().Be(1000f);
		backgrounds[0].Height.Should().Be(240f);
		backgrounds[0].Shading.FillColor.Should().Be("FFFF00");

		backgrounds[1].RowIndex.Should().Be(0);
		backgrounds[1].ColumnIndex.Should().Be(1);
		backgrounds[1].X.Should().Be(1000f);
		backgrounds[1].Width.Should().Be(1500f);
		backgrounds[1].Shading.Pattern.Should().Be(ShadingPattern.Solid);
		backgrounds[1].Shading.PatternColor.Should().Be("00FF00");
	}
}
