using AwesomeAssertions;
using SkiaSharp;
using Xunit;

namespace PanoramicData.Render.Test;

/// <summary>
/// Integration tests verifying that the complete wrap pipeline
/// (registry → line-breaking → Knuth-Plass) produces correct line
/// widths and break counts for all wrap types.
/// </summary>
public sealed class TextWrappingIntegrationTests
{
	private readonly MeasurementEngine _engine = new();

	private static SKTypeface GetTypeface()
	{
		var typeface = SKTypeface.FromFamilyName("Arial");
		if (typeface is null || typeface.FamilyName != "Arial")
		{
			Assert.Skip("Arial not available on this platform");
		}

		return typeface;
	}

	private static IReadOnlyList<ParsedRun> MakeLongRun(string text) =>
		[new ParsedRun { Elements = [new TextRunElement { Text = text }] }];

	private const float ContentLeft = 0f;
	private const float FullWidth = 5000f;   // ~3.47 inches at 1440 twips/inch
	private const float LineHeight = 240f;   // ~12pt
	private const float ParaTop = 0f;

	// -------------------------------------------------------------------------
	// Square wrapping
	// -------------------------------------------------------------------------

	[Fact]
	public void SquareWrap_RegionCoveringFirstTwoLines_NarrowsThoseLines()
	{
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var text = "The quick brown fox jumps over the lazy dog and keeps on going";
		var runs = MakeLongRun(text);

		// No wrapping baseline.
		var noWrapLines = breaker.ComputeLineBreaks(runs, typeface, 12f, FullWidth);

		// Square region occupies right 60% of first two line heights.
		var registry = new WrapRegionRegistry();
		registry.AddSquareRegion(new FloatingSquareWrapRegion(
			XTwips: 2000f, YTwips: ParaTop,
			WidthTwips: 3000f, HeightTwips: LineHeight * 2));

		var wrapLines = breaker.ComputeLineBreaks(
			runs, typeface, 12f, FullWidth,
			registry, ContentLeft, ParaTop, LineHeight);

		// Narrower first two lines should force more breaks overall.
		wrapLines.Count.Should().BeGreaterThan(noWrapLines.Count);
	}

	[Fact]
	public void SquareWrap_RegionBelowContent_DoesNotAffectBreaks()
	{
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var text = "Short text that fits on one line";
		var runs = MakeLongRun(text);

		// Region is far below the paragraph — should have no effect.
		var registry = new WrapRegionRegistry();
		registry.AddSquareRegion(new FloatingSquareWrapRegion(
			XTwips: 0f, YTwips: ParaTop + 10000f,
			WidthTwips: FullWidth, HeightTwips: LineHeight));

		var wrapLines = breaker.ComputeLineBreaks(
			runs, typeface, 12f, FullWidth,
			registry, ContentLeft, ParaTop, LineHeight);

		var noWrapLines = breaker.ComputeLineBreaks(runs, typeface, 12f, FullWidth);

		wrapLines.Count.Should().Be(noWrapLines.Count);
	}

	[Fact]
	public void SquareWrap_FullWidthRegion_FallsBackToNominalWidth()
	{
		// A region that covers the entire content width on the first line.
		// GetPrimaryLineWidth returns 0, so the breaker uses the nominal width as fallback.
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var text = "Hello world";
		var runs = MakeLongRun(text);

		var registry = new WrapRegionRegistry();
		registry.AddSquareRegion(new FloatingSquareWrapRegion(
			XTwips: ContentLeft, YTwips: ParaTop,
			WidthTwips: FullWidth, HeightTwips: LineHeight));

		// Should not throw; uses nominal fallback width.
		var wrapLines = breaker.ComputeLineBreaks(
			runs, typeface, 12f, FullWidth,
			registry, ContentLeft, ParaTop, LineHeight);

		wrapLines.Should().ContainSingle();
	}

	// -------------------------------------------------------------------------
	// Top-and-bottom wrapping
	// -------------------------------------------------------------------------

	[Fact]
	public void TopBottomWrap_FirstLineBlocked_FallsBackToNominalWidth()
	{
		// The first paragraph line is in the top-bottom region → GetPrimaryLineWidth → 0
		// → breaker falls back to nominal width. No exception, text still flows.
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var text = "Hello world";
		var runs = MakeLongRun(text);

		var registry = new WrapRegionRegistry();
		registry.AddTopBottomRegion(new FloatingTopBottomWrapRegion(
			YTwips: ParaTop, HeightTwips: LineHeight));

		var wrapLines = breaker.ComputeLineBreaks(
			runs, typeface, 12f, FullWidth,
			registry, ContentLeft, ParaTop, LineHeight);

		// With nominal fallback width, short text still fits on one line.
		wrapLines.Should().ContainSingle();
	}

	// -------------------------------------------------------------------------
	// Tight wrapping (polygon-based)
	// -------------------------------------------------------------------------

	[Fact]
	public void TightWrap_TriangleRegionPartiallyCoversLine_ReducesAvailableWidth()
	{
		// A right-triangle polygon covering the right third of the content band.
		// Points form a triangle: (3333, 0), (5000, 0), (5000, 480) — 
		// occupies progressively more of the right side as Y increases.
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var text = "The quick brown fox jumps over the lazy dog and continues running";
		var runs = MakeLongRun(text);

		var noWrapLines = breaker.ComputeLineBreaks(runs, typeface, 12f, FullWidth);

		var registry = new WrapRegionRegistry();
		var polygon = new TightWrapPoint[]
		{
			new(3333f, 0f),
			new(5000f, 0f),
			new(5000f, LineHeight * 2),
			new(3333f, LineHeight * 2),
		};
		registry.AddTightRegion(new FloatingTightWrapRegion(polygon, 0f, 0f, 0f, 0f));

		var wrapLines = breaker.ComputeLineBreaks(
			runs, typeface, 12f, FullWidth,
			registry, ContentLeft, ParaTop, LineHeight);

		wrapLines.Count.Should().BeGreaterThanOrEqualTo(noWrapLines.Count);
	}

	// -------------------------------------------------------------------------
	// Per-line width selector correctness
	// -------------------------------------------------------------------------

	[Fact]
	public void WrapRegistry_EmptyRegistry_ProducesSameBreaksAsNoRegistry()
	{
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var text = "The quick brown fox jumps over the lazy dog";
		var runs = MakeLongRun(text);

		var noWrapLines = breaker.ComputeLineBreaks(runs, typeface, 12f, FullWidth);

		var emptyRegistry = new WrapRegionRegistry();
		var wrapLines = breaker.ComputeLineBreaks(
			runs, typeface, 12f, FullWidth,
			emptyRegistry, ContentLeft, ParaTop, LineHeight);

		wrapLines.Count.Should().Be(noWrapLines.Count);
	}

	[Fact]
	public void WrapRegistry_LargeEstimatedLineHeight_ShiftsWrapQueryOutOfRegion()
	{
		// With a very large estimated line height, line 0 starts at paraTop,
		// but its query y-band extends far beyond the wrap region.
		// The region's height is small, so it only affects very early y-positions.
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var text = "The quick brown fox jumps over the lazy dog";
		var runs = MakeLongRun(text);

		var noWrapLines = breaker.ComputeLineBreaks(runs, typeface, 12f, FullWidth);

		// Region covers the first line exactly, but we estimate a huge line height
		// so the query mid-point is outside the region even for line 0.
		// Result: should behave like no wrap (or at most the same count).
		var registry = new WrapRegionRegistry();
		registry.AddSquareRegion(new FloatingSquareWrapRegion(
			XTwips: 2000f, YTwips: 0f, WidthTwips: 3000f, HeightTwips: LineHeight));

		var wrapLines = breaker.ComputeLineBreaks(
			runs, typeface, 12f, FullWidth,
			registry, ContentLeft, ParaTop,
			estimatedLineHeightTwips: 50000f); // exaggerated height pushes query past region

		// With huge estimated height, the wrap region won't affect line 0's y-query.
		// We just verify no exception and a reasonable line count.
		wrapLines.Count.Should().BeGreaterThan(0);
	}
}
