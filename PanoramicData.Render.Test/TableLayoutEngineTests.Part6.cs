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
	public void ComputeVerticalMergeRegions_MultipleRegions_ReturnsInOrder()
	{
		var layout = LayoutGrid(9600f, [1000f, 1000f],
			RowAt(300f, MakeCell(verticalMerge: VerticalMergeState.Restart), MakeCell(verticalMerge: VerticalMergeState.Restart)),
			RowAt(300f, MakeCell(verticalMerge: VerticalMergeState.Continue), MakeCell(verticalMerge: VerticalMergeState.Continue)));

		var regions = TableLayoutEngine.ComputeVerticalMergeRegions(layout);

		regions.Select(region => (region.StartRowIndex, region.ColumnIndex)).Should().Equal([(0, 0), (0, 1)]);
	}

	[Fact]
	public void ComputeVerticalMergeRegions_ContinueWithoutRestart_IsIgnored()
	{
		var layout = LayoutGrid(9600f, [1000f], RowAt(300f, MakeCell(verticalMerge: VerticalMergeState.Continue)));

		TableLayoutEngine.ComputeVerticalMergeRegions(layout).Should().BeEmpty();
	}

	[Fact]
	public void ComputeVerticalMergeRegions_WithHorizontalAndVerticalMerge_UsesOwnerGeometry()
	{
		var restart = MakeCell(gridSpan: 2, verticalMerge: VerticalMergeState.Restart);
		var layout = LayoutGrid(9600f, [1000f, 1500f, 2000f],
			RowAt(400f, restart, MakeCell()),
			RowAt(600f, MakeCell(gridSpan: 2, verticalMerge: VerticalMergeState.Continue), MakeCell()));

		var regions = TableLayoutEngine.ComputeVerticalMergeRegions(layout);

		regions.Should().HaveCount(1);
		regions[0].ColumnIndex.Should().Be(0);
		regions[0].RowSpan.Should().Be(2);
		regions[0].Width.Should().Be(2500f);
		regions[0].Height.Should().Be(1000f);
		regions[0].Cell.Should().BeSameAs(restart);
	}

	// ---- Combined merge regions (4.4.3) ----

	[Fact]
	public void ComputeMergedCellRegions_NullLayout_ThrowsArgumentNullException()
	{
		AssertRejectsNullLayout(TableLayoutEngine.ComputeMergedCellRegions);
	}

	[Fact]
	public void ComputeMergedCellRegions_EmptyTable_ReturnsEmpty()
	{
		var layout = TableLayoutEngine.Layout(new TableElement { GridColumns = [], Rows = [] }, 9600f);

		TableLayoutEngine.ComputeMergedCellRegions(layout).Should().BeEmpty();
	}

	[Fact]
	public void ComputeMergedCellRegions_NoMerges_ReturnsEmpty()
	{
		var layout = LayoutGrid(9600f, [1000f, 1000f],
			RowOf(MakeCell(), MakeCell()),
			RowOf(MakeCell(), MakeCell()));

		TableLayoutEngine.ComputeMergedCellRegions(layout).Should().BeEmpty();
	}

	[Fact]
	public void ComputeMergedCellRegions_HorizontalOnly_IncludesRegion()
	{
		var layout = LayoutGrid(9600f, [1200f, 800f, 1000f], RowAt(300f, MakeCell(gridSpan: 2), MakeCell()));

		var regions = TableLayoutEngine.ComputeMergedCellRegions(layout);

		regions.Should().HaveCount(1);
		AssertRegion(regions[0], 1, 2, 2000f, 300f);
	}

	[Fact]
	public void ComputeMergedCellRegions_VerticalOnly_IncludesRegion()
	{
		var layout = LayoutGrid(9600f, [1000f],
			RowAt(250f, MakeCell(verticalMerge: VerticalMergeState.Restart)),
			RowAt(350f, MakeCell(verticalMerge: VerticalMergeState.Continue)));

		var regions = TableLayoutEngine.ComputeMergedCellRegions(layout);

		regions.Should().HaveCount(1);
		AssertRegion(regions[0], 2, 1, 1000f, 600f);
	}

	[Fact]
	public void ComputeMergedCellRegions_CombinedHorizontalAndVertical_IncludesRectangularRegion()
	{
		var restart = MakeCell(gridSpan: 2, verticalMerge: VerticalMergeState.Restart);
		var layout = LayoutGrid(9600f, [1000f, 1500f, 500f],
			RowAt(400f, restart, MakeCell()),
			RowAt(600f, MakeCell(gridSpan: 2, verticalMerge: VerticalMergeState.Continue), MakeCell()));

		var regions = TableLayoutEngine.ComputeMergedCellRegions(layout);

		regions.Should().HaveCount(1);
		regions[0].StartRowIndex.Should().Be(0);
		regions[0].StartColumnIndex.Should().Be(0);
		AssertRegion(regions[0], 2, 2, 2500f, 1000f);
		regions[0].Cell.Should().BeSameAs(restart);
	}

	[Fact]
	public void ComputeMergedCellRegions_MixedMergedCells_ReturnsInReadingOrder()
	{
		var layout = MixedMergeLayout(300f);

		var regions = TableLayoutEngine.ComputeMergedCellRegions(layout);

		regions.Select(region => (region.StartRowIndex, region.StartColumnIndex)).Should().Equal([(0, 0), (1, 0)]);
	}
}
