using AwesomeAssertions;
using Xunit;

namespace PanoramicData.Render.Test;

/// <summary>
/// Tests verifying column flow mechanics, Y-position accuracy, break position handling and column geometry.
/// Placements read column:x:y:width. Default section: available height 12960, 2 columns at X 1440 and 6480
/// (width 4320), 3 columns at X 1440, 4800 and 8160 (width 2640).
/// </summary>
public sealed class ColumnFlowTests
{
	private const string C0 = "@0:1440:1440:4320";
	private const string C1 = "@1:6480:1440:4320";
	private const string T0 = "@0:1440:1440:2640";
	private const string T1 = "@1:4800:1440:2640";
	private const string T2 = "@2:8160:1440:2640";

	[Theory]
	[InlineData("h2000 h3000 h1000!c h1500", 2, "-@0:1440:1440:4320,-@0:1440:3440:4320,-@1:6480:1440:4320,-@1:6480:2440:4320")]
	[InlineData("l9x1000!f", 3, $"3{T0},3{T1},3{T2}")]
	[InlineData("h1000 h1000!b", 2, $"-{C0}|-{C0}")]
	[InlineData("h1000!c", 2, $"-{C0}")]
	[InlineData("h14000", 2, $"-{C0}")]
	[InlineData("h7000 h7000 h7000", 2, $"-{C0},-{C1}|-{C0}")]
	[InlineData("l20x1000!f h1000!c", 2, $"12{C0},8{C1}|-{C0}")]
	[InlineData("h7000 h7000 h7000", 3, $"-{T0},-{T1},-{T2}")]
	[InlineData("l30x1000!f", 2, $"12{C0},12{C1}|3{C0},3{C1}")]
	public void Paginate_FlowsColumnsAsExpected(string spec, int columns, string expectedGeometry)
	{
		var result = PaginationTestKit.Paginate(spec, columns);

		PaginationTestKit.GeometryShape(result).Should().Be(expectedGeometry);
		result.Select(page => page.PageNumber).Should().Equal(Enumerable.Range(1, result.Count));
	}

	[Fact]
	public void Paginate_WithHeaderHeight_ReducesAvailableColumnSpace()
	{
		const string Spec = "l24x1000!f";

		var withHeader = PaginationTestKit.Paginate(Spec, 2, headerHeight: 2000f);
		var withoutHeader = PaginationTestKit.Paginate(Spec, 2);

		withoutHeader.Should().ContainSingle("24 lines fit on 1 page without header");
		withHeader.Should().HaveCount(2, "header reduces available height, requiring 2 pages");
		withHeader[0].ContentTopTwips.Should().Be(2720f);
	}

	[Fact]
	public void ComputeColumnRegions_ThreeEqualColumns_CorrectXPositionsAndWidths()
	{
		var regions = PageBuilder.ComputeColumnRegions(PaginationTestKit.Section(3));

		regions.Select(r => (r.XTwips, r.WidthTwips)).Should().Equal(
			(1440f, 2640f), (4800f, 2640f), (8160f, 2640f));
	}

	[Fact]
	public void PaginateDocument_MultiColumnSection_ProducesColumnPlacements()
	{
		var twoColSection = PaginationTestKit.Section(2);
		var blocks = new[]
		{
			PaginationTestKit.Block("l4x1000!f"),
			new LayoutBlock(new SectionBreakBlock { SectionInfo = twoColSection }, 0f),
			PaginationTestKit.Block("h1000"),
		};

		var result = PageBuilder.PaginateDocument(blocks, new SectionInfo());

		result.Should().HaveCount(2);
		result[0].Section.ColumnCount.Should().Be(2);
		PaginationTestKit.Shape([result[0]], withColumns: true).Should().Be("2@0,2@1");
		result[1].Section.ColumnCount.Should().Be(1);
		result[1].Blocks.Should().ContainSingle();
	}
}
