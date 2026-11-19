namespace PanoramicData.Render.Test;

using Xunit;

public sealed class SquareWrapLayoutEngineTests
{
	private static IReadOnlyList<WrapLineSegment> Compute(float lineHeight, params FloatingSquareWrapRegion[] regions)
		=> SquareWrapLayoutEngine.ComputeAvailableSegments(100f, 600f, 1000f, lineHeight, regions);

	[Fact]
	public void ComputeAvailableSegments_NoRegions_ReturnsFullContentWidth()
	{
		var segments = SquareWrapLayoutEngine.ComputeAvailableSegments(
			contentLeftTwips: 100f,
			contentWidthTwips: 500f,
			lineTopTwips: 1000f,
			lineHeightTwips: 200f,
			regions: []);

		WrapAssert.Segments(segments, (100f, 500f));
	}

	[Fact]
	public void ComputeAvailableSegments_RegionNotOverlappingLine_DoesNotAffectSegments()
	{
		var segments = SquareWrapLayoutEngine.ComputeAvailableSegments(100f, 500f, 1000f, 200f, [new(200f, 2000f, 100f, 100f)]);

		WrapAssert.Segments(segments, (100f, 500f));
	}

	[Fact]
	public void ComputeAvailableSegments_OverlappingRegionSplitsLineIntoTwoSegments()
	{
		var segments = Compute(200f, new(250f, 900f, 200f, 400f));

		WrapAssert.Segments(segments, (100f, 150f), (450f, 250f));
	}

	[Fact]
	public void ComputeAvailableSegments_FullWidthOverlap_ReturnsEmpty()
	{
		var segments = Compute(200f, new(100f, 900f, 600f, 400f));

		WrapAssert.Segments(segments);
	}

	[Fact]
	public void ComputeAvailableSegments_WrapDistancesExpandExcludedArea()
	{
		var segments = Compute(
			100f,
			new FloatingSquareWrapRegion(
				XTwips: 300f,
				YTwips: 900f,
				WidthTwips: 100f,
				HeightTwips: 100f,
				DistanceTopTwips: 150f,
				DistanceBottomTwips: 50f,
				DistanceLeftTwips: 40f,
				DistanceRightTwips: 60f));

		// 100 -> 260, then 300+100+60 = 460 -> 700
		WrapAssert.Segments(segments, (100f, 160f), (460f, 240f));
	}

	[Fact]
	public void ComputeAvailableSegments_MultipleRegions_SubtractsAllExclusions()
	{
		var segments = Compute(100f, new(180f, 900f, 100f, 200f), new(420f, 900f, 120f, 200f));

		WrapAssert.Segments(segments, (100f, 80f), (280f, 140f), (540f, 160f));
	}

	[Fact]
	public void ComputeAvailableSegments_OverlappingRegions_MergesExclusionIntervals()
	{
		// Content: [100, 700]. Exclusions [200, 350) and [300, 500) merge to [200, 500).
		var segments = Compute(100f, new(200f, 900f, 150f, 200f), new(300f, 900f, 200f, 200f));

		WrapAssert.Segments(segments, (100f, 100f), (500f, 200f));
	}

	[Theory]
	[InlineData("regions")]
	[InlineData("width")]
	[InlineData("height")]
	public void ComputeAvailableSegments_InvalidArgument_Throws(string invalidArgument)
		=> WrapAssert.Guard<FloatingSquareWrapRegion>(invalidArgument, SquareWrapLayoutEngine.ComputeAvailableSegments);
}
