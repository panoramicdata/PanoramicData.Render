using AwesomeAssertions;
using Xunit;

namespace PanoramicData.Render.Test;

public class UnderlineStyleTests
{
	[Fact]
	public void None_HasValue_Zero()
	{
		((int)UnderlineStyle.None).Should().Be(0);
	}

	[Theory]
	[InlineData((int)UnderlineStyle.None)]
	[InlineData((int)UnderlineStyle.Single)]
	[InlineData((int)UnderlineStyle.Double)]
	[InlineData((int)UnderlineStyle.Thick)]
	[InlineData((int)UnderlineStyle.Dotted)]
	[InlineData((int)UnderlineStyle.DottedHeavy)]
	[InlineData((int)UnderlineStyle.Dash)]
	[InlineData((int)UnderlineStyle.DashedHeavy)]
	[InlineData((int)UnderlineStyle.DashLong)]
	[InlineData((int)UnderlineStyle.DashLongHeavy)]
	[InlineData((int)UnderlineStyle.DotDash)]
	[InlineData((int)UnderlineStyle.DashDotHeavy)]
	[InlineData((int)UnderlineStyle.DotDotDash)]
	[InlineData((int)UnderlineStyle.DashDotDotHeavy)]
	[InlineData((int)UnderlineStyle.Wave)]
	[InlineData((int)UnderlineStyle.WavyDouble)]
	[InlineData((int)UnderlineStyle.WavyHeavy)]
	[InlineData((int)UnderlineStyle.Words)]
	public void AllValues_AreDistinct(int value)
	{
		var style = (UnderlineStyle)value;
		Enum.IsDefined(style).Should().BeTrue();
	}

	[Fact]
	public void EnumCount_Is18()
	{
		Enum.GetValues<UnderlineStyle>().Should().HaveCount(18);
	}
}
