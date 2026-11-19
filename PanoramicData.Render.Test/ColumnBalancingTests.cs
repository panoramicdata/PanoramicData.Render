using AwesomeAssertions;
using Xunit;

namespace PanoramicData.Render.Test;

public sealed class ColumnBalancingTests
{
	[Theory]
	[InlineData("l4x1000!f", "2@0,2@1")]
	[InlineData("l30x1000!f", "12@0,12@1|3@0,3@1")]
	[InlineData("l4x1000!f h1000!c", "4@0,-@1")]
	public void Paginate_TwoColumnSection_BalancesAsExpected(string spec, string expectedShape)
	{
		var result = PaginationTestKit.Paginate(spec, columns: 2);

		PaginationTestKit.Shape(result, withColumns: true).Should().Be(expectedShape);
	}
}
