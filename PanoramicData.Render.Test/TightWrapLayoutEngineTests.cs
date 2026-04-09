namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class TightWrapLayoutEngineTests
{
	[Fact]
	public void ComputeAvailableSegments_NoRegions_ReturnsFullSegment()
	{
		var segments = TightWrapLayoutEngine.ComputeAvailableSegments(100f, 500f, 1000f, 100f, []);

		segments.Should().ContainSingle();
		segments[0].XTwips.Should().Be(100f);
		segments[0].WidthTwips.Should().Be(500f);
	}

	[Fact]
	public void ComputeAvailableSegments_PolygonCrossingLine_SubtractsInteriorRange()
	{
		var polygon = new TightWrapPoint[]
		{
			new(250f, 900f),
			new(450f, 900f),
			new(450f, 1200f),
			new(250f, 1200f)
		};
		var regions = new[] { new FloatingTightWrapRegion(polygon) };

		var segments = TightWrapLayoutEngine.ComputeAvailableSegments(100f, 600f, 1000f, 100f, regions);

		segments.Should().HaveCount(2);
		segments[0].XTwips.Should().Be(100f);
		segments[0].WidthTwips.Should().Be(150f);
		segments[1].XTwips.Should().Be(450f);
		segments[1].WidthTwips.Should().Be(250f);
	}

	[Fact]
	public void ComputeAvailableSegments_TrianglePolygon_ComputesScanlineIntersections()
	{
		var polygon = new TightWrapPoint[]
		{
			new(200f, 900f),
			new(500f, 900f),
			new(350f, 1200f)
		};
		var regions = new[] { new FloatingTightWrapRegion(polygon) };

		var segments = TightWrapLayoutEngine.ComputeAvailableSegments(100f, 700f, 1000f, 100f, regions);

		segments.Should().HaveCount(2);
		segments[0].XTwips.Should().Be(100f);
		segments[0].WidthTwips.Should().BeApproximately(175f, 0.001f);
		segments[1].XTwips.Should().BeApproximately(425f, 0.001f);
		segments[1].WidthTwips.Should().BeApproximately(375f, 0.001f);
	}

	[Fact]
	public void ComputeAvailableSegments_WrapDistancesExpandSubtractedRange()
	{
		var polygon = new TightWrapPoint[]
		{
			new(300f, 900f),
			new(400f, 900f),
			new(400f, 1100f),
			new(300f, 1100f)
		};
		var regions = new[]
		{
			new FloatingTightWrapRegion(polygon, DistanceTopTwips: 50f, DistanceBottomTwips: 50f, DistanceLeftTwips: 20f, DistanceRightTwips: 30f)
		};

		var segments = TightWrapLayoutEngine.ComputeAvailableSegments(100f, 600f, 1000f, 100f, regions);

		segments.Should().HaveCount(2);
		segments[0].WidthTwips.Should().Be(180f); // 100 -> 280
		segments[1].XTwips.Should().Be(430f); // 400 + 30
	}

	[Fact]
	public void ComputeAvailableSegments_NullRegions_ThrowsArgumentNullException()
	{
		var act = () => TightWrapLayoutEngine.ComputeAvailableSegments(100f, 500f, 1000f, 100f, null!);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void ComputeAvailableSegments_NonPositiveContentWidth_ThrowsArgumentOutOfRangeException()
	{
		var act = () => TightWrapLayoutEngine.ComputeAvailableSegments(100f, 0f, 1000f, 100f, []);

		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void ComputeAvailableSegments_NonPositiveLineHeight_ThrowsArgumentOutOfRangeException()
	{
		var act = () => TightWrapLayoutEngine.ComputeAvailableSegments(100f, 500f, 1000f, 0f, []);

		act.Should().Throw<ArgumentOutOfRangeException>();
	}
}
