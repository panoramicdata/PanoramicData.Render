using AwesomeAssertions;
using Xunit;

namespace PanoramicData.Render.Test;

public sealed class ExplicitColumnBreakTests
{
	[Theory]
	[InlineData("h1000 h1000!c h1000", 2, "-@0,-@1,-@1")]
	[InlineData("h7000 h7000 h1000!c", 2, "-@0,-@1|-@0")]
	[InlineData("h1000 h1000!c", 1, "-|-")]
	public void Paginate_ForceColumnBreakBefore_PlacesBlocksAsExpected(string spec, int columns, string expectedShape)
	{
		var result = PaginationTestKit.Paginate(spec, columns);

		PaginationTestKit.Shape(result, withColumns: true).Should().Be(expectedShape);
	}

	[Fact]
	public void LayoutBlock_ForceColumnBreakBefore_DefaultIsFalse() =>
		PaginationTestKit.Block("h100").ForceColumnBreakBefore.Should().BeFalse();

	[Fact]
	public void LayoutBlock_ForceColumnBreakBefore_CanBeSetTrue() =>
		PaginationTestKit.Block("h100!c").ForceColumnBreakBefore.Should().BeTrue();
}
