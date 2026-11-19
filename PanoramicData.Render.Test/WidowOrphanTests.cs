using AwesomeAssertions;
using Xunit;

namespace PanoramicData.Render.Test;

public sealed class WidowOrphanTests
{
	[Theory]
	[InlineData("l3x5000", 5500f, "null")]
	[InlineData("l5x1000", 4500f, "3/2")]
	[InlineData("l4x1000", 2500f, "2/2")]
	[InlineData("l3x1000", 2500f, "null")]
	[InlineData("l3x5000!f", 5500f, "1/2")]
	public void TrySplitBlock_AppliesWidowOrphanConstraints(string block, float available, string expected)
	{
		var result = PageBuilder.TrySplitBlock(PaginationTestKit.Block(block), available);

		PaginationTestKit.DescribeSplit(result).Should().Be(expected);
	}

	[Theory]
	[InlineData("l4x1000", true)]
	[InlineData("l4x1000!f", false)]
	public void TrySplitBlock_WidowControl_PropagatedToSplitParts(string block, bool expected)
	{
		var result = PageBuilder.TrySplitBlock(PaginationTestKit.Block(block), 2500f);

		result.Should().NotBeNull();
		result!.Value.First.WidowOrphanControl.Should().Be(expected);
		result.Value.Second.WidowOrphanControl.Should().Be(expected);
	}

	[Theory]
	[InlineData("h12000 l3x500", "-|3")]
	[InlineData("h12000 l3x500!f", "-,1|2")]
	public void Paginate_WidowControl_PlacesLinesAsExpected(string spec, string expectedShape)
	{
		var result = PaginationTestKit.Paginate(spec);

		PaginationTestKit.Shape(result).Should().Be(expectedShape);
	}

	[Fact]
	public void LayoutBlock_WidowOrphanControl_DefaultIsTrue() =>
		PaginationTestKit.Block("h100").WidowOrphanControl.Should().BeTrue();

	[Fact]
	public void LayoutBlock_WidowOrphanControl_CanBeSetFalse() =>
		PaginationTestKit.Block("h100!f").WidowOrphanControl.Should().BeFalse();

	[Fact]
	public void DefaultWidowOrphanMinLines_IsTwo() =>
		PageBuilder.DefaultWidowOrphanMinLines.Should().Be(2);
}
