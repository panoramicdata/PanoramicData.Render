using AwesomeAssertions;
using SkiaSharp;
using Xunit;

namespace PanoramicData.Render.Test;

public sealed class WrapRegionRegistryTests
{
	// --- IsEmpty ---

	[Fact]
	public void IsEmpty_NoRegionsAdded_ReturnsTrue()
	{
		var registry = new WrapRegionRegistry();

		registry.IsEmpty.Should().BeTrue();
	}

	[Fact]
	public void IsEmpty_SquareRegionAdded_ReturnsFalse()
	{
		var registry = new WrapRegionRegistry();
		registry.AddSquareRegion(new FloatingSquareWrapRegion(100f, 100f, 200f, 50f, 0f, 0f, 0f, 0f));

		registry.IsEmpty.Should().BeFalse();
	}

	[Fact]
	public void IsEmpty_TightRegionAdded_ReturnsFalse()
	{
		var registry = new WrapRegionRegistry();
		var points = new[] { new TightWrapPoint(0f, 0f), new TightWrapPoint(100f, 0f), new TightWrapPoint(50f, 100f) };
		registry.AddTightRegion(new FloatingTightWrapRegion(points, 0f, 0f, 0f, 0f));

		registry.IsEmpty.Should().BeFalse();
	}

	[Fact]
	public void IsEmpty_TopBottomRegionAdded_ReturnsFalse()
	{
		var registry = new WrapRegionRegistry();
		registry.AddTopBottomRegion(new FloatingTopBottomWrapRegion(YTwips: 100f, HeightTwips: 50f));

		registry.IsEmpty.Should().BeFalse();
	}

	// --- GetAvailableSegments: no regions ---

	[Fact]
	public void GetAvailableSegments_EmptyRegistry_ReturnsFullContentBand()
	{
		var registry = new WrapRegionRegistry();

		var segments = registry.GetAvailableSegments(0f, 5000f, 0f, 240f);

		segments.Should().ContainSingle();
		segments[0].XTwips.Should().Be(0f);
		segments[0].WidthTwips.Should().Be(5000f);
	}

	// --- GetAvailableSegments: square region ---

	[Fact]
	public void GetAvailableSegments_SquareRegionOnRight_ReducesWidthOnOverlappingLine()
	{
		// Content: X=0, W=5000. Image: X=3000, Y=0, W=1000, H=240.
		var registry = new WrapRegionRegistry();
		registry.AddSquareRegion(new FloatingSquareWrapRegion(3000f, 0f, 1000f, 240f, 0f, 0f, 0f, 0f));

		// Line at Y=0, H=240 overlaps the image.
		var segments = registry.GetAvailableSegments(0f, 5000f, 0f, 240f);

		// Two segments: [0, 3000) and [4000, 5000)
		segments.Should().HaveCount(2);
		segments[0].XTwips.Should().Be(0f);
		segments[0].WidthTwips.Should().Be(3000f);
		segments[1].XTwips.Should().Be(4000f);
		segments[1].WidthTwips.Should().Be(1000f);
	}

	[Fact]
	public void GetAvailableSegments_SquareRegionOnRight_NoReductionOnNonOverlappingLine()
	{
		// Content: X=0, W=5000. Image: X=3000, Y=0, W=1000, H=240.
		var registry = new WrapRegionRegistry();
		registry.AddSquareRegion(new FloatingSquareWrapRegion(3000f, 0f, 1000f, 240f, 0f, 0f, 0f, 0f));

		// Line at Y=300 is entirely below the image — full width available.
		var segments = registry.GetAvailableSegments(0f, 5000f, 300f, 240f);

		segments.Should().ContainSingle();
		segments[0].XTwips.Should().Be(0f);
		segments[0].WidthTwips.Should().Be(5000f);
	}

	// --- GetAvailableSegments: top-bottom region ---

	[Fact]
	public void GetAvailableSegments_TopBottomRegion_ReturnsEmptyOnBlockedLine()
	{
		// Content: X=0, W=5000. Top-bottom image: Y=100, H=200.
		var registry = new WrapRegionRegistry();
		registry.AddTopBottomRegion(new FloatingTopBottomWrapRegion(YTwips: 100f, HeightTwips: 200f));

		// Line overlapping the top-bottom region → no segments.
		var segments = registry.GetAvailableSegments(0f, 5000f, 150f, 240f);

		segments.Should().BeEmpty();
	}

	// --- GetPrimaryLineWidth ---

	[Fact]
	public void GetPrimaryLineWidth_EmptyRegistry_ReturnsFullWidth()
	{
		var registry = new WrapRegionRegistry();

		var width = registry.GetPrimaryLineWidth(0f, 5000f, 0f, 240f);

		width.Should().Be(5000f);
	}

	[Fact]
	public void GetPrimaryLineWidth_SquareRegionOnRight_ReturnsWidestSegmentWidth()
	{
		// Content: X=0, W=5000. Image at X=3500 width=1000.
		// Segments on overlap line: [0, 3500) = 3500, [4500, 5000) = 500.
		var registry = new WrapRegionRegistry();
		registry.AddSquareRegion(new FloatingSquareWrapRegion(3500f, 0f, 1000f, 240f, 0f, 0f, 0f, 0f));

		var width = registry.GetPrimaryLineWidth(0f, 5000f, 0f, 240f);

		// Widest segment is the left one: 3500.
		width.Should().Be(3500f);
	}

	[Fact]
	public void GetPrimaryLineWidth_TopBottomBlockingLine_ReturnsNominalWidthFallback()
	{
		// A top-bottom region returns empty segments; GetPrimaryLineWidth should return 0.
		var registry = new WrapRegionRegistry();
		registry.AddTopBottomRegion(new FloatingTopBottomWrapRegion(YTwips: 0f, HeightTwips: 240f));

		var width = registry.GetPrimaryLineWidth(0f, 5000f, 0f, 240f);

		width.Should().Be(0f);
	}

	// --- Integration: ParagraphLineBreaker with WrapRegionRegistry ---

	private static SKTypeface GetTypeface()
	{
		var typeface = SKTypeface.FromFamilyName("Arial");
		if (typeface is null || typeface.FamilyName != "Arial")
		{
			Assert.Skip("Arial not available on this platform");
		}

		return typeface;
	}

	[Fact]
	public void ComputeLineBreaks_WithSquareWrapRegistry_ProducesMoreLinesThanWithout()
	{
		// Arrange: a paragraph with a long sentence and a wrap region taking half the line width.
		var typeface = GetTypeface();
		var engine = new MeasurementEngine();
		var breaker = new ParagraphLineBreaker(engine);

		var runs = new[]
		{
			new ParsedRun
			{
				Elements = [new TextRunElement { Text = "The quick brown fox jumps over the lazy dog" }]
			}
		};

		const float lineWidthTwips = 5000f;
		const float lineHeightTwips = 240f;
		const float paragraphTopTwips = 0f;

		// Without wrapping: break normally at full width.
		var linesWithout = breaker.ComputeLineBreaks(runs, typeface, 12f, lineWidthTwips);

		// Register a square wrap region that covers the right half of lines 0 and 1.
		// Image: starts at x=2000 (roughly the middle), full width 5000, height = 2 line heights.
		var registry = new WrapRegionRegistry();
		registry.AddSquareRegion(new FloatingSquareWrapRegion(
			XTwips: 2000f,
			YTwips: paragraphTopTwips,
			WidthTwips: 3000f,  // occupies from 2000 to 5000
			HeightTwips: lineHeightTwips * 2));

		// With wrapping: first two lines should be narrower → more lines overall.
		var linesWith = breaker.ComputeLineBreaks(
			runs, typeface, 12f, lineWidthTwips,
			registry, contentLeftTwips: 0f,
			paragraphTopTwips: paragraphTopTwips,
			estimatedLineHeightTwips: lineHeightTwips);

		linesWith.Count.Should().BeGreaterThan(linesWithout.Count);
	}

	// --- Multiple floating objects (step 5.3.6) ---

	[Fact]
	public void GetAvailableSegments_TwoSquareRegions_BothExclusionsApplied()
	{
		// Content: [0, 5000]. Two images: one at [500, 1000) and one at [3000, 4000).
		var registry = new WrapRegionRegistry();
		registry.AddSquareRegion(new FloatingSquareWrapRegion(500f, 0f, 500f, 240f));
		registry.AddSquareRegion(new FloatingSquareWrapRegion(3000f, 0f, 1000f, 240f));

		var segments = registry.GetAvailableSegments(0f, 5000f, 0f, 240f);

		// Three available segments: [0, 500), [1000, 3000), [4000, 5000)
		segments.Should().HaveCount(3);
		segments[0].XTwips.Should().Be(0f);
		segments[0].WidthTwips.Should().Be(500f);
		segments[1].XTwips.Should().Be(1000f);
		segments[1].WidthTwips.Should().Be(2000f);
		segments[2].XTwips.Should().Be(4000f);
		segments[2].WidthTwips.Should().Be(1000f);
	}

	[Fact]
	public void GetAvailableSegments_SquareAndTopBottom_TopBottomSupersedesSquare()
	{
		// Square wrap region would leave two segments; top-bottom on same line → fully blocked.
		var registry = new WrapRegionRegistry();
		registry.AddSquareRegion(new FloatingSquareWrapRegion(2000f, 0f, 1000f, 240f));
		registry.AddTopBottomRegion(new FloatingTopBottomWrapRegion(YTwips: 0f, HeightTwips: 240f));

		var segments = registry.GetAvailableSegments(0f, 5000f, 0f, 240f);

		// Top-bottom region removes all text, so no segments available.
		segments.Should().BeEmpty();
	}

	[Fact]
	public void GetAvailableSegments_TwoOverlappingSquareRegions_MergesExclusions()
	{
		// Content: [0, 5000]. Overlapping images: [1000, 2500) and [2000, 3500).
		// Merged exclusion: [1000, 3500). Available: [0, 1000) and [3500, 5000).
		var registry = new WrapRegionRegistry();
		registry.AddSquareRegion(new FloatingSquareWrapRegion(1000f, 0f, 1500f, 240f));
		registry.AddSquareRegion(new FloatingSquareWrapRegion(2000f, 0f, 1500f, 240f));

		var segments = registry.GetAvailableSegments(0f, 5000f, 0f, 240f);

		segments.Should().HaveCount(2);
		segments[0].XTwips.Should().Be(0f);
		segments[0].WidthTwips.Should().Be(1000f);
		segments[1].XTwips.Should().Be(3500f);
		segments[1].WidthTwips.Should().Be(1500f);
	}

	[Fact]
	public void GetPrimaryLineWidth_TwoSquareRegions_ReturnsWidestAvailableSegment()
	{
		// Content [0, 5000]. Two images at [500, 1000) and [3000, 4000).
		// Segments: [0, 500)=500, [1000, 3000)=2000, [4000, 5000)=1000.
		// Widest = 2000.
		var registry = new WrapRegionRegistry();
		registry.AddSquareRegion(new FloatingSquareWrapRegion(500f, 0f, 500f, 240f));
		registry.AddSquareRegion(new FloatingSquareWrapRegion(3000f, 0f, 1000f, 240f));

		var width = registry.GetPrimaryLineWidth(0f, 5000f, 0f, 240f);

		width.Should().Be(2000f);
	}
}
