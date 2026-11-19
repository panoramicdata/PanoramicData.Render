using AwesomeAssertions;
using Xunit;

namespace PanoramicData.Render.Test;

public sealed class ExplicitPageBreakTests
{
	[Theory]
	[InlineData("h1000 h1000!b", "-|-")]
	[InlineData("h500 h500 h500!b h500", "-,-|-,-")]
	[InlineData("h6000 h6000 h6000!b", "-,-|-")]
	[InlineData("h10000 l5x1000!b", "-|5")]
	public void Paginate_ForcePageBreakBefore_PlacesBlocksAsExpected(string spec, string expectedShape)
	{
		var result = PaginationTestKit.Paginate(spec);

		PaginationTestKit.Shape(result).Should().Be(expectedShape);
	}

	[Fact]
	public void Paginate_ForcePageBreakBefore_FirstBlock_NoEmptyPage()
	{
		var result = PaginationTestKit.Paginate("h1000!b");

		result[0].PageNumber.Should().Be(1);
	}

	[Fact]
	public void Paginate_MultipleForceBreaks_CreatesMultiplePages()
	{
		var result = PaginationTestKit.Paginate("h100 h100!b h100!b h100!b");

		result.Select(page => page.PageNumber).Should().Equal(1, 2, 3, 4);
		PaginationTestKit.Shape(result).Should().Be("-|-|-|-");
	}

	[Fact]
	public void LayoutBlock_ForcePageBreakBefore_DefaultIsFalse() =>
		PaginationTestKit.Block("h100").ForcePageBreakBefore.Should().BeFalse();

	[Fact]
	public void LayoutBlock_ForcePageBreakBefore_CanBeSetTrue() =>
		PaginationTestKit.Block("h100!b").ForcePageBreakBefore.Should().BeTrue();
}
