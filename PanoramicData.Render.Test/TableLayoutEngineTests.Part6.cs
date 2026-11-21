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
		var layout = CombinedMergeLayout(restart, 2000f);

		var regions = TableLayoutEngine.ComputeVerticalMergeRegions(layout);

		regions.Should().ContainSingle().Which.Should().BeEquivalentTo(new { ColumnIndex = 0, RowSpan = 2, Width = 2500f, Height = 1000f });
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

		AssertSingleRegion(layout, 1, 2, 2000f, 300f);
	}

	[Fact]
	public void ComputeMergedCellRegions_VerticalOnly_IncludesRegion()
	{
		var layout = SingleColumnMergeLayout(1000f, 250f, 350f, MakeCell(verticalMerge: VerticalMergeState.Restart));

		AssertSingleRegion(layout, 2, 1, 1000f, 600f);
	}

	[Fact]
	public void ComputeMergedCellRegions_CombinedHorizontalAndVertical_IncludesRectangularRegion()
	{
		var restart = MakeCell(gridSpan: 2, verticalMerge: VerticalMergeState.Restart);
		var layout = CombinedMergeLayout(restart, 500f);

		var region = AssertSingleRegion(layout, 2, 2, 2500f, 1000f);

		region.StartRowIndex.Should().Be(0);
		region.StartColumnIndex.Should().Be(0);
		region.Cell.Should().BeSameAs(restart);
	}

	[Fact]
	public void ComputeMergedCellRegions_MixedMergedCells_ReturnsInReadingOrder()
	{
		var layout = MixedMergeLayout(300f);

		var regions = TableLayoutEngine.ComputeMergedCellRegions(layout);

		regions.Select(region => (region.StartRowIndex, region.StartColumnIndex)).Should().Equal([(0, 0), (1, 0)]);
	}
}
