namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public class TwipConverterTests
{
	[Fact]
	public void PointsToTwips_OnePoint_Returns20Twips()
	{
		TwipConverter.PointsToTwips(1f).Should().Be(20f);
	}

	[Fact]
	public void PointsToTwips_Zero_ReturnsZero()
	{
		TwipConverter.PointsToTwips(0f).Should().Be(0f);
	}

	[Fact]
	public void PointsToTwips_HalfPoint_Returns10Twips()
	{
		TwipConverter.PointsToTwips(0.5f).Should().Be(10f);
	}

	[Fact]
	public void PointsToTwips_NegativeValue_ReturnsNegativeTwips()
	{
		TwipConverter.PointsToTwips(-1f).Should().Be(-20f);
	}

	[Fact]
	public void TwipsToPoints_TwentyTwips_Returns1Point()
	{
		TwipConverter.TwipsToPoints(20f).Should().Be(1f);
	}

	[Fact]
	public void TwipsToPoints_Zero_ReturnsZero()
	{
		TwipConverter.TwipsToPoints(0f).Should().Be(0f);
	}

	[Fact]
	public void TwipsToPoints_1440Twips_Returns72Points()
	{
		TwipConverter.TwipsToPoints(1440f).Should().BeApproximately(72f, 0.001f);
	}

	[Fact]
	public void PointsToTwips_RoundTrip_PreservesValue()
	{
		const float original = 12.5f;
		var twips = TwipConverter.PointsToTwips(original);
		var result = TwipConverter.TwipsToPoints(twips);

		result.Should().BeApproximately(original, 0.001f);
	}

	[Fact]
	public void InchesToTwips_OneInch_Returns1440()
	{
		TwipConverter.InchesToTwips(1.0).Should().Be(1440);
	}

	[Fact]
	public void InchesToTwips_Zero_ReturnsZero()
	{
		TwipConverter.InchesToTwips(0.0).Should().Be(0);
	}

	[Fact]
	public void InchesToTwips_HalfInch_Returns720()
	{
		TwipConverter.InchesToTwips(0.5).Should().Be(720);
	}

	[Fact]
	public void TwipsToInches_1440Twips_Returns1Inch()
	{
		TwipConverter.TwipsToInches(1440).Should().BeApproximately(1.0, 0.0001);
	}

	[Fact]
	public void TwipsToInches_Zero_ReturnsZero()
	{
		TwipConverter.TwipsToInches(0).Should().Be(0.0);
	}

	[Fact]
	public void TwipsToPixels_At96Dpi_1440TwipsReturns96Pixels()
	{
		TwipConverter.TwipsToPixels(1440, 96.0).Should().BeApproximately(96.0, 0.001);
	}

	[Fact]
	public void TwipsToPixels_At72Dpi_1440TwipsReturns72Pixels()
	{
		TwipConverter.TwipsToPixels(1440, 72.0).Should().BeApproximately(72.0, 0.001);
	}

	[Fact]
	public void TwipsToPixels_Zero_ReturnsZero()
	{
		TwipConverter.TwipsToPixels(0, 96.0).Should().Be(0.0);
	}
}
