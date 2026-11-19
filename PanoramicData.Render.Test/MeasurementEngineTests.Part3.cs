namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using SkiaSharp;
using SkiaSharp.HarfBuzz;
using Xunit;

public partial class MeasurementEngineTests
{

	[Fact]
	public void ShapeTextInTwips_WithIsRtlFlag_ProducesPositiveWidth()
	{
		using var typeface = CreateTypefaceForTests();
		var engine = new MeasurementEngine();

		var result = engine.ShapeTextInTwips(typeface, 12, "Test", isRtl: true);

		result.TotalWidth.Should().BeGreaterThan(0);
	}

	[Fact]
	public void MeasureGlyphAdvances_AreLinearDesignWidths_NotHintedToWholePoints()
	{
		// Liberation Sans 'A' is 1366/2048 em. FreeType on Linux hints the default SKFont's advances to whole
		// points (5 at 8pt), which laid documents out differently there than on Windows.
		using var typeface = CreateTypefaceForTests();
		var engine = new MeasurementEngine();

		var advances = engine.MeasureGlyphAdvances(typeface, 8, "A");

		advances[0].Should().BeApproximately(8f * 1366f / 2048f, 0.001f);
	}

	private static SKTypeface CreateTypefaceForTests() => TestFonts.CreateSans();
}
