using AwesomeAssertions;
using Xunit;

namespace PanoramicData.Render.Test;

public sealed class KeepWithNextTests
{
	[Theory]
	[InlineData("h5000!n h5000", "-,-")]
	[InlineData("h6000 h6000!n h4000", "-|-,-")]
	[InlineData("h4000 h4000!n h4000!n h4000", "-|-,-,-")]
	[InlineData("h7000!n h7000", "-|-")]
	[InlineData("h5000!n h5000!b", "-|-")]
	public void Paginate_KeepWithNext_PlacesBlocksAsExpected(string spec, string expectedShape)
	{
		var result = PaginationTestKit.Paginate(spec);

		PaginationTestKit.Shape(result).Should().Be(expectedShape);
	}

	[Fact]
	public void Paginate_KeepWithNext_PageNumbersCorrect()
	{
		var result = PaginationTestKit.Paginate("h6000 h6000!n h4000");

		result.Select(page => page.PageNumber).Should().Equal(1, 2);
	}

	[Fact]
	public void LayoutBlock_KeepWithNext_DefaultIsFalse() =>
		PaginationTestKit.Block("h100").KeepWithNext.Should().BeFalse();

	[Fact]
	public void LayoutBlock_KeepWithNext_CanBeSetTrue() =>
		PaginationTestKit.Block("h100!n").KeepWithNext.Should().BeTrue();
}
