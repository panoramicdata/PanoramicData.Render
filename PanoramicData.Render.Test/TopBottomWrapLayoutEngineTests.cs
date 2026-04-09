namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class TopBottomWrapLayoutEngineTests
{
	[Fact]
	public void ComputeAvailableSegments_NoRegions_ReturnsFullSegment()
	{
		var segments = TopBottomWrapLayoutEngine.ComputeAvailableSegments(100f, 500f, 1000f, 100f, []);

		segments.Should().ContainSingle();
		segments[0].XTwips.Should().Be(100f);
		segments[0].WidthTwips.Should().Be(500f);
	}

	[Fact]
	public void ComputeAvailableSegments_LineInsideExclusionBand_ReturnsEmpty()
	{
		var regions = new[]
		{
			new FloatingTopBottomWrapRegion(YTwips: 900f, HeightTwips: 300f)
		};

		var segments = TopBottomWrapLayoutEngine.ComputeAvailableSegments(100f, 500f, 1000f, 100f, regions);

		segments.Should().BeEmpty();
	}

	[Fact]
	public void ComputeAvailableSegments_LineAboveExclusionBand_ReturnsFullSegment()
	{
		var regions = new[]
		{
			new FloatingTopBottomWrapRegion(YTwips: 1200f, HeightTwips: 300f)
		};

		var segments = TopBottomWrapLayoutEngine.ComputeAvailableSegments(100f, 500f, 1000f, 100f, regions);

		segments.Should().ContainSingle();
		segments[0].WidthTwips.Should().Be(500f);
	}

	[Fact]
	public void ComputeAvailableSegments_DistanceExpandsBlockedBand()
	{
		var regions = new[]
		{
			new FloatingTopBottomWrapRegion(YTwips: 1100f, HeightTwips: 100f, DistanceTopTwips: 80f, DistanceBottomTwips: 120f)
		};

		var segmentsAt1000 = TopBottomWrapLayoutEngine.ComputeAvailableSegments(100f, 500f, 1000f, 50f, regions);
		segmentsAt1000.Should().BeEmpty();

		var segmentsAt800 = TopBottomWrapLayoutEngine.ComputeAvailableSegments(100f, 500f, 800f, 50f, regions);
		segmentsAt800.Should().ContainSingle();
	}

	[Fact]
	public void ComputeAvailableSegments_NullRegions_ThrowsArgumentNullException()
	{
		var act = () => TopBottomWrapLayoutEngine.ComputeAvailableSegments(100f, 500f, 1000f, 100f, null!);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void ComputeAvailableSegments_NonPositiveContentWidth_ThrowsArgumentOutOfRangeException()
	{
		var act = () => TopBottomWrapLayoutEngine.ComputeAvailableSegments(100f, 0f, 1000f, 100f, []);

		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void ComputeAvailableSegments_NonPositiveLineHeight_ThrowsArgumentOutOfRangeException()
	{
		var act = () => TopBottomWrapLayoutEngine.ComputeAvailableSegments(100f, 500f, 1000f, 0f, []);

		act.Should().Throw<ArgumentOutOfRangeException>();
	}
}
