namespace PanoramicData.Render.Test;

using Xunit;

public sealed class TightWrapLayoutEngineTests
{
	private static IReadOnlyList<WrapLineSegment> Compute(float contentWidth, FloatingTightWrapRegion region, params TightWrapPoint[] unused)
		=> TightWrapLayoutEngine.ComputeAvailableSegments(100f, contentWidth, 1000f, 100f, [region]);

	private static FloatingTightWrapRegion Region(params (float X, float Y)[] points)
		=> new(points.Select(p => new TightWrapPoint(p.X, p.Y)).ToArray());

	[Fact]
	public void ComputeAvailableSegments_NoRegions_ReturnsFullSegment()
	{
		var segments = TightWrapLayoutEngine.ComputeAvailableSegments(100f, 500f, 1000f, 100f, []);

		WrapAssert.Segments(segments, (100f, 500f));
	}

	[Fact]
	public void ComputeAvailableSegments_PolygonCrossingLine_SubtractsInteriorRange()
	{
		var region = Region((250f, 900f), (450f, 900f), (450f, 1200f), (250f, 1200f));

		WrapAssert.Segments(Compute(600f, region), (100f, 150f), (450f, 250f));
	}

	[Fact]
	public void ComputeAvailableSegments_TrianglePolygon_ComputesScanlineIntersections()
	{
		var region = Region((200f, 900f), (500f, 900f), (350f, 1200f));

		WrapAssert.Segments(Compute(700f, region), 0.001f, (100f, 175f), (425f, 375f));
	}

	[Fact]
	public void ComputeAvailableSegments_WrapDistancesExpandSubtractedRange()
	{
		var polygon = Region((300f, 900f), (400f, 900f), (400f, 1100f), (300f, 1100f)).Polygon;
		var region = new FloatingTightWrapRegion(polygon, DistanceTopTwips: 50f, DistanceBottomTwips: 50f, DistanceLeftTwips: 20f, DistanceRightTwips: 30f);

		// 100 -> 280, then 400 + 30 = 430
		var segments = Compute(600f, region);

		segments.Should().HaveCount(2);
		segments[0].WidthTwips.Should().Be(180f);
		segments[1].XTwips.Should().Be(430f);
	}

	[Theory]
	[InlineData("regions")]
	[InlineData("width")]
	[InlineData("height")]
	public void ComputeAvailableSegments_InvalidArgument_Throws(string invalidArgument)
		=> WrapAssert.Guard<FloatingTightWrapRegion>(invalidArgument, TightWrapLayoutEngine.ComputeAvailableSegments);
}
