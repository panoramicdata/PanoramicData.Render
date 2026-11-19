namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class SquareWrapLayoutEngineTests
{
	[Fact]
	public void ComputeAvailableSegments_NoRegions_ReturnsFullContentWidth()
	{
		var segments = SquareWrapLayoutEngine.ComputeAvailableSegments(
			contentLeftTwips: 100f,
			contentWidthTwips: 500f,
			lineTopTwips: 1000f,
			lineHeightTwips: 200f,
			regions: []);

		segments.Should().ContainSingle();
		segments[0].XTwips.Should().Be(100f);
		segments[0].WidthTwips.Should().Be(500f);
	}

	[Fact]
	public void ComputeAvailableSegments_RegionNotOverlappingLine_DoesNotAffectSegments()
	{
		var regions = new[]
		{
			new FloatingSquareWrapRegion(200f, 2000f, 100f, 100f)
		};

		var segments = SquareWrapLayoutEngine.ComputeAvailableSegments(100f, 500f, 1000f, 200f, regions);

		segments.Should().ContainSingle();
		segments[0].XTwips.Should().Be(100f);
		segments[0].WidthTwips.Should().Be(500f);
	}

	[Fact]
	public void ComputeAvailableSegments_OverlappingRegionSplitsLineIntoTwoSegments()
	{
		var regions = new[]
		{
			new FloatingSquareWrapRegion(250f, 900f, 200f, 400f)
		};

		var segments = SquareWrapLayoutEngine.ComputeAvailableSegments(100f, 600f, 1000f, 200f, regions);

		segments.Should().HaveCount(2);
		segments[0].XTwips.Should().Be(100f);
		segments[0].WidthTwips.Should().Be(150f);
		segments[1].XTwips.Should().Be(450f);
		segments[1].WidthTwips.Should().Be(250f);
	}

	[Fact]
	public void ComputeAvailableSegments_FullWidthOverlap_ReturnsEmpty()
	{
		var regions = new[]
		{
			new FloatingSquareWrapRegion(100f, 900f, 600f, 400f)
		};

		var segments = SquareWrapLayoutEngine.ComputeAvailableSegments(100f, 600f, 1000f, 200f, regions);

		segments.Should().BeEmpty();
	}

	[Fact]
	public void ComputeAvailableSegments_WrapDistancesExpandExcludedArea()
	{
		var regions = new[]
		{
			new FloatingSquareWrapRegion(
				XTwips: 300f,
				YTwips: 900f,
				WidthTwips: 100f,
				HeightTwips: 100f,
				DistanceTopTwips: 150f,
				DistanceBottomTwips: 50f,
				DistanceLeftTwips: 40f,
				DistanceRightTwips: 60f)
		};

		var segments = SquareWrapLayoutEngine.ComputeAvailableSegments(100f, 600f, 1000f, 100f, regions);

		segments.Should().HaveCount(2);
		segments[0].XTwips.Should().Be(100f);
		segments[0].WidthTwips.Should().Be(160f); // 100 -> 260
		segments[1].XTwips.Should().Be(460f); // 300+100+60
		segments[1].WidthTwips.Should().Be(240f); // 460 -> 700
	}

	[Fact]
	public void ComputeAvailableSegments_MultipleRegions_SubtractsAllExclusions()
	{
		var regions = new[]
		{
			new FloatingSquareWrapRegion(180f, 900f, 100f, 200f),
			new FloatingSquareWrapRegion(420f, 900f, 120f, 200f)
		};

		var segments = SquareWrapLayoutEngine.ComputeAvailableSegments(100f, 600f, 1000f, 100f, regions);

		segments.Should().HaveCount(3);
		segments[0].XTwips.Should().Be(100f);
		segments[0].WidthTwips.Should().Be(80f);
		segments[1].XTwips.Should().Be(280f);
		segments[1].WidthTwips.Should().Be(140f);
		segments[2].XTwips.Should().Be(540f);
		segments[2].WidthTwips.Should().Be(160f);
	}

	[Fact]
	public void ComputeAvailableSegments_OverlappingRegions_MergesExclusionIntervals()
	{
		// Content: [100, 700]. Region A excludes [200, 350], Region B excludes [300, 500].
		// Merged exclusion: [200, 500]. Available: [100, 200) and [500, 700).
		var regions = new[]
		{
			new FloatingSquareWrapRegion(200f, 900f, 150f, 200f),  // X=200, W=150 → [200, 350)
			new FloatingSquareWrapRegion(300f, 900f, 200f, 200f)   // X=300, W=200 → [300, 500)
		};

		var segments = SquareWrapLayoutEngine.ComputeAvailableSegments(100f, 600f, 1000f, 100f, regions);

		// After both SubtractRange calls the exclusion [200, 500) is fully removed.
		segments.Should().HaveCount(2);
		segments[0].XTwips.Should().Be(100f);
		segments[0].WidthTwips.Should().Be(100f); // [100, 200)
		segments[1].XTwips.Should().Be(500f);
		segments[1].WidthTwips.Should().Be(200f); // [500, 700)
	}

	[Fact]
	public void ComputeAvailableSegments_NullRegions_ThrowsArgumentNullException()
	{
		var act = () => SquareWrapLayoutEngine.ComputeAvailableSegments(100f, 500f, 1000f, 200f, null!);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void ComputeAvailableSegments_NonPositiveContentWidth_ThrowsArgumentOutOfRangeException()
	{
		var act = () => SquareWrapLayoutEngine.ComputeAvailableSegments(100f, 0f, 1000f, 200f, []);

		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void ComputeAvailableSegments_NonPositiveLineHeight_ThrowsArgumentOutOfRangeException()
	{
		var act = () => SquareWrapLayoutEngine.ComputeAvailableSegments(100f, 500f, 1000f, 0f, []);

		act.Should().Throw<ArgumentOutOfRangeException>();
	}
}
