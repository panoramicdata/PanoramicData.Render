namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class TopBottomWrapLayoutEngineTests
{
	private static IReadOnlyList<WrapLineSegment> Compute(float lineTop, float lineHeight, FloatingTopBottomWrapRegion region)
		=> TopBottomWrapLayoutEngine.ComputeAvailableSegments(100f, 500f, lineTop, lineHeight, [region]);

	[Fact]
	public void ComputeAvailableSegments_NoRegions_ReturnsFullSegment()
	{
		var segments = TopBottomWrapLayoutEngine.ComputeAvailableSegments(100f, 500f, 1000f, 100f, []);

		WrapAssert.Segments(segments, (100f, 500f));
	}

	[Fact]
	public void ComputeAvailableSegments_LineInsideExclusionBand_ReturnsEmpty()
	{
		WrapAssert.Segments(Compute(1000f, 100f, new(YTwips: 900f, HeightTwips: 300f)));
	}

	[Fact]
	public void ComputeAvailableSegments_LineAboveExclusionBand_ReturnsFullSegment()
	{
		var segments = Compute(1000f, 100f, new(YTwips: 1200f, HeightTwips: 300f));

		segments.Should().ContainSingle();
		segments[0].WidthTwips.Should().Be(500f);
	}

	[Fact]
	public void ComputeAvailableSegments_DistanceExpandsBlockedBand()
	{
		var region = new FloatingTopBottomWrapRegion(YTwips: 1100f, HeightTwips: 100f, DistanceTopTwips: 80f, DistanceBottomTwips: 120f);

		Compute(1000f, 50f, region).Should().BeEmpty();
		Compute(800f, 50f, region).Should().ContainSingle();
	}

	[Theory]
	[InlineData("regions")]
	[InlineData("width")]
	[InlineData("height")]
	public void ComputeAvailableSegments_InvalidArgument_Throws(string invalidArgument)
		=> WrapAssert.Guard<FloatingTopBottomWrapRegion>(invalidArgument, TopBottomWrapLayoutEngine.ComputeAvailableSegments);
}
