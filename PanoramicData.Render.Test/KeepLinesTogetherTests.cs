using AwesomeAssertions;
using Xunit;

namespace PanoramicData.Render.Test;

public sealed class KeepLinesTogetherTests
{
	[Theory]
	[InlineData("l3x1000!kf", 2500f, "null")]
	[InlineData("l3x1000!f", 2500f, "2/1")]
	public void TrySplitBlock_RespectsKeepLines(string block, float available, string expected)
	{
		var result = PageBuilder.TrySplitBlock(PaginationTestKit.Block(block), available);

		PaginationTestKit.DescribeSplit(result).Should().Be(expected);
	}

	[Theory]
	[InlineData("h10000 l3x1500!kf", "-|3")]
	[InlineData("h5000 l1000,1000!kf", "-,2")]
	[InlineData("l3x5000!kf", "3")]
	public void Paginate_KeepLinesTogether_PlacesBlocksAsExpected(string spec, string expectedShape)
	{
		var result = PaginationTestKit.Paginate(spec);

		PaginationTestKit.Shape(result).Should().Be(expectedShape);
	}

	[Fact]
	public void LayoutBlock_KeepLinesTogether_DefaultIsFalse() =>
		PaginationTestKit.Block("h100").KeepLinesTogether.Should().BeFalse();

	[Fact]
	public void LayoutBlock_KeepLinesTogether_CanBeSetTrue() =>
		PaginationTestKit.Block("h100!k").KeepLinesTogether.Should().BeTrue();
}
