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

	internal static void AssertRejectsNullLayout(Func<TableLayoutResult, object?> call)
	{
		var act = () => call(null!);

		act.Should().Throw<ArgumentNullException>().WithParameterName("layout");
	}
}