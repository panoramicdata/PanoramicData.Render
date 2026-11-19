using AwesomeAssertions;
using SkiaSharp;
using Xunit;

namespace PanoramicData.Render.Test;

public sealed class WrapRegionRegistryTests
{
	private static WrapRegionRegistry Registry(params FloatingSquareWrapRegion[] squares)
	{
		var registry = new WrapRegionRegistry();
		foreach (var square in squares)
		{
			registry.AddSquareRegion(square);
		}

		return registry;
	}

	private static WrapRegionRegistry RegistryWithTopBottom(float y, float height, params FloatingSquareWrapRegion[] squares)
	{
		var registry = Registry(squares);
		registry.AddTopBottomRegion(new FloatingTopBottomWrapRegion(YTwips: y, HeightTwips: height));
		return registry;
	}

	private static FloatingSquareWrapRegion Square(float x, float width, float height = 240f)
		=> new(x, 0f, width, height);

	private static IReadOnlyList<WrapLineSegment> Segments(WrapRegionRegistry registry, float lineTop = 0f)
		=> registry.GetAvailableSegments(0f, 5000f, lineTop, 240f);

	[Theory]
	[InlineData("none", true)]
	[InlineData("square", false)]
	[InlineData("tight", false)]
	[InlineData("topBottom", false)]
	public void IsEmpty_ReflectsRegionsAdded(string regionKind, bool expectedEmpty)
	{
		var registry = new WrapRegionRegistry();
		if (regionKind == "square")
		{
			registry.AddSquareRegion(new FloatingSquareWrapRegion(100f, 100f, 200f, 50f, 0f, 0f, 0f, 0f));
		}
		else if (regionKind == "tight")
		{
			var points = new[] { new TightWrapPoint(0f, 0f), new TightWrapPoint(100f, 0f), new TightWrapPoint(50f, 100f) };
			registry.AddTightRegion(new FloatingTightWrapRegion(points, 0f, 0f, 0f, 0f));
		}
		else if (regionKind == "topBottom")
		{
			registry.AddTopBottomRegion(new FloatingTopBottomWrapRegion(YTwips: 100f, HeightTwips: 50f));
		}

		registry.IsEmpty.Should().Be(expectedEmpty);
	}

	[Fact]
	public void GetAvailableSegments_EmptyRegistry_ReturnsFullContentBand()
	{
		WrapAssert.Segments(Segments(new WrapRegionRegistry()), (0f, 5000f));
	}

	[Fact]
	public void GetAvailableSegments_SquareRegionOnRight_ReducesWidthOnOverlappingLine()
	{
		// Image: X=3000, W=1000 overlaps the line at Y=0: [0, 3000) and [4000, 5000) remain.
		WrapAssert.Segments(Segments(Registry(Square(3000f, 1000f))), (0f, 3000f), (4000f, 1000f));
	}

	[Fact]
	public void GetAvailableSegments_SquareRegionOnRight_NoReductionOnNonOverlappingLine()
	{
		// Line at Y=300 is entirely below the image: full width available.
		WrapAssert.Segments(Segments(Registry(Square(3000f, 1000f)), lineTop: 300f), (0f, 5000f));
	}

	[Fact]
	public void GetAvailableSegments_TopBottomRegion_ReturnsEmptyOnBlockedLine()
	{
		var registry = RegistryWithTopBottom(100f, 200f);

		WrapAssert.Segments(Segments(registry, lineTop: 150f));
	}

	[Fact]
	public void GetPrimaryLineWidth_EmptyRegistry_ReturnsFullWidth()
	{
		new WrapRegionRegistry().GetPrimaryLineWidth(0f, 5000f, 0f, 240f).Should().Be(5000f);
	}

	[Fact]
	public void GetPrimaryLineWidth_SquareRegionOnRight_ReturnsWidestSegmentWidth()
	{
		// Segments on the overlapping line: [0, 3500) = 3500 and [4500, 5000) = 500.
		var registry = Registry(Square(3500f, 1000f));

		registry.GetPrimaryLineWidth(0f, 5000f, 0f, 240f).Should().Be(3500f);
	}

	[Fact]
	public void GetPrimaryLineWidth_TopBottomBlockingLine_ReturnsNominalWidthFallback()
	{
		// A top-bottom region returns empty segments; GetPrimaryLineWidth should return 0.
		var registry = RegistryWithTopBottom(0f, 240f);

		registry.GetPrimaryLineWidth(0f, 5000f, 0f, 240f).Should().Be(0f);
	}

	// --- Integration: ParagraphLineBreaker with WrapRegionRegistry ---

	// The shipped test font, not a system font, so the results are the same on every machine.
	private static SKTypeface GetTypeface() => TestFonts.Sans;

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
		var registry = Registry(new FloatingSquareWrapRegion(
			XTwips: 2000f,
			YTwips: paragraphTopTwips,
			WidthTwips: 3000f,
			HeightTwips: lineHeightTwips * 2));

		// With wrapping: first two lines should be narrower, so more lines overall.
		var linesWith = breaker.ComputeLineBreaks(
			runs, typeface, 12f, lineWidthTwips,
			registry, contentLeftTwips: 0f,
			paragraphTopTwips: paragraphTopTwips,
			estimatedLineHeightTwips: lineHeightTwips);

		linesWith.Count.Should().BeGreaterThan(linesWithout.Count);
	}

	[Fact]
	public void GetAvailableSegments_TwoSquareRegions_BothExclusionsApplied()
	{
		// Images at [500, 1000) and [3000, 4000).
		WrapAssert.Segments(
			Segments(Registry(Square(500f, 500f), Square(3000f, 1000f))),
			(0f, 500f),
			(1000f, 2000f),
			(4000f, 1000f));
	}

	[Fact]
	public void GetAvailableSegments_SquareAndTopBottom_TopBottomSupersedesSquare()
	{
		// The square region alone would leave two segments; the top-bottom region blocks the line.
		var registry = RegistryWithTopBottom(0f, 240f, Square(2000f, 1000f));

		WrapAssert.Segments(Segments(registry));
	}

	[Fact]
	public void GetAvailableSegments_TwoOverlappingSquareRegions_MergesExclusions()
	{
		// Images [1000, 2500) and [2000, 3500) merge to [1000, 3500).
		WrapAssert.Segments(
			Segments(Registry(Square(1000f, 1500f), Square(2000f, 1500f))),
			(0f, 1000f),
			(3500f, 1500f));
	}

	[Fact]
	public void GetPrimaryLineWidth_TwoSquareRegions_ReturnsWidestAvailableSegment()
	{
		// Segments: [0, 500)=500, [1000, 3000)=2000, [4000, 5000)=1000.
		var registry = Registry(Square(500f, 500f), Square(3000f, 1000f));

		registry.GetPrimaryLineWidth(0f, 5000f, 0f, 240f).Should().Be(2000f);
	}
}
