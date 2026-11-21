namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using static PanoramicData.Render.Test.TableLayoutBuilders;

internal static class MergeLayoutBuilders
{
	internal static TableLayoutResult MixedMergeLayout(float rowHeight) => LayoutGrid(9600f, [1000f, 1000f, 1000f],
		RowAt(rowHeight, TableLayoutEngineTests.MakeCell(gridSpan: 2), TableLayoutEngineTests.MakeCell()),
		RowAt(rowHeight, TableLayoutEngineTests.MakeCell(verticalMerge: VerticalMergeState.Restart), TableLayoutEngineTests.MakeCell(), TableLayoutEngineTests.MakeCell()),
		RowAt(rowHeight, TableLayoutEngineTests.MakeCell(verticalMerge: VerticalMergeState.Continue), TableLayoutEngineTests.MakeCell(), TableLayoutEngineTests.MakeCell()));

	internal static void AssertRegion(MergedCellRegion region, int rowSpan, int columnSpan, float width, float height)
	{
		region.RowSpan.Should().Be(rowSpan);
		region.ColumnSpan.Should().Be(columnSpan);
		region.Width.Should().Be(width);
		region.Height.Should().Be(height);
	}

	internal static MergedCellRegion AssertSingleRegion(TableLayoutResult layout, int rowSpan, int columnSpan, float width, float height)
	{
		var regions = TableLayoutEngine.ComputeMergedCellRegions(layout);

		regions.Should().HaveCount(1);
		AssertRegion(regions[0], rowSpan, columnSpan, width, height);
		return regions[0];
	}

	internal static MergedCellContentLayout SingleContentLayout(TableLayoutResult layout)
	{
		var contentLayouts = TableLayoutEngine.ComputeMergedCellContentLayouts(layout);

		contentLayouts.Should().HaveCount(1);
		return contentLayouts[0];
	}

	internal static void AssertRejectsNullLayout(Func<TableLayoutResult, object?> call)
	{
		var act = () => call(null!);

		act.Should().Throw<ArgumentNullException>().WithParameterName("layout");
	}
}