namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using SkiaSharp;
using SkiaSharp.HarfBuzz;
using Xunit;

public partial class MeasurementEngineTests
{

	[Fact]
	public void MeasureGlyphAdvancesInTwips_ReturnsValuesScaledBy20()
	{
		using var typeface = CreateTypefaceForTests();
		var engine = new MeasurementEngine();
		const string text = "Test";

		var pointAdvances = engine.MeasureGlyphAdvances(typeface, 12, text);
		var twipAdvances = engine.MeasureGlyphAdvancesInTwips(typeface, 12, text);

		twipAdvances.Should().HaveCount(pointAdvances.Count);
		for (var i = 0; i < pointAdvances.Count; i++)
		{
			twipAdvances[i].Should().BeApproximately(pointAdvances[i] * 20f, 0.001f);
		}
	}

	[Fact]
	public void MeasureGlyphAdvancesInTwips_WithEmptyText_ReturnsEmpty()
	{
		using var typeface = CreateTypefaceForTests();
		var engine = new MeasurementEngine();

		var result = engine.MeasureGlyphAdvancesInTwips(typeface, 12, string.Empty);

		result.Should().BeEmpty();
	}

	[Fact]
	public void ShapeTextInTwips_TotalWidth_IsScaledBy20()
	{
		using var typeface = CreateTypefaceForTests();
		var engine = new MeasurementEngine();
		const string text = "Twip test";

		var pointRun = engine.ShapeText(typeface, 12, text);
		var twipRun = engine.ShapeTextInTwips(typeface, 12, text);

		twipRun.TotalWidth.Should().BeApproximately(pointRun.TotalWidth * 20f, 0.01f);
	}

	[Fact]
	public void ShapeTextInTwips_GlyphAdvances_AreScaledBy20()
	{
		using var typeface = CreateTypefaceForTests();
		var engine = new MeasurementEngine();
		const string text = "ABC";

		var pointRun = engine.ShapeText(typeface, 12, text);
		var twipRun = engine.ShapeTextInTwips(typeface, 12, text);

		twipRun.Glyphs.Should().HaveCount(pointRun.Glyphs.Count);
		for (var i = 0; i < pointRun.Glyphs.Count; i++)
		{
			twipRun.Glyphs[i].AdvanceWidth.Should().BeApproximately(pointRun.Glyphs[i].AdvanceWidth * 20f, 0.01f);
			twipRun.Glyphs[i].OffsetX.Should().BeApproximately(pointRun.Glyphs[i].OffsetX * 20f, 0.01f);
			twipRun.Glyphs[i].OffsetY.Should().BeApproximately(pointRun.Glyphs[i].OffsetY * 20f, 0.01f);
			twipRun.Glyphs[i].Codepoint.Should().Be(pointRun.Glyphs[i].Codepoint);
			twipRun.Glyphs[i].Cluster.Should().Be(pointRun.Glyphs[i].Cluster);
		}
	}

	[Fact]
	public void ShapeTextInTwips_WithEmptyText_ReturnsEmptyRun()
	{
		using var typeface = CreateTypefaceForTests();
		var engine = new MeasurementEngine();

		var result = engine.ShapeTextInTwips(typeface, 12, string.Empty);

		result.Glyphs.Should().BeEmpty();
		result.TotalWidth.Should().Be(0);
	}

	[Fact]
	public void MeasureCharacter_WithNullTypeface_ThrowsArgumentNullException()
	{
		var engine = new MeasurementEngine();

		var act = () => engine.MeasureCharacter(null!, 12, 'A');

		act.Should().Throw<ArgumentNullException>();
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void MeasureCharacter_WithNonPositiveFontSize_ThrowsArgumentOutOfRangeException(float fontSize)
	{
		using var typeface = CreateTypefaceForTests();
		var engine = new MeasurementEngine();

		var act = () => engine.MeasureCharacter(typeface, fontSize, 'A');

		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void MeasureCharacter_ReturnsPositiveAdvanceWidth()
	{
		using var typeface = CreateTypefaceForTests();
		var engine = new MeasurementEngine();

		var metrics = engine.MeasureCharacter(typeface, 12, 'A');

		metrics.AdvanceWidth.Should().BeGreaterThan(0);
	}

	[Fact]
	public void MeasureCharacter_ReturnsPositiveAscent()
	{
		using var typeface = CreateTypefaceForTests();
		var engine = new MeasurementEngine();

		var metrics = engine.MeasureCharacter(typeface, 12, 'A');

		metrics.Ascent.Should().BeGreaterThan(0);
	}

	[Fact]
	public void MeasureCharacter_ReturnsPositiveDescent()
	{
		using var typeface = CreateTypefaceForTests();
		var engine = new MeasurementEngine();

		var metrics = engine.MeasureCharacter(typeface, 12, 'A');

		metrics.Descent.Should().BeGreaterThan(0);
	}

	[Fact]
	public void MeasureCharacter_ReturnsNonNegativeLeading()
	{
		using var typeface = CreateTypefaceForTests();
		var engine = new MeasurementEngine();

		var metrics = engine.MeasureCharacter(typeface, 12, 'A');

		metrics.Leading.Should().BeGreaterThanOrEqualTo(0);
	}

	[Fact]
	public void MeasureCharacter_LineHeight_EqualsAscentPlusDescentPlusLeading()
	{
		using var typeface = CreateTypefaceForTests();
		var engine = new MeasurementEngine();

		var metrics = engine.MeasureCharacter(typeface, 12, 'A');

		metrics.LineHeight.Should().BeApproximately(
			metrics.Ascent + metrics.Descent + metrics.Leading, 0.001f);
	}

	[Fact]
	public void MeasureCharacter_AdvanceWidth_MatchesMeasureGlyphAdvances()
	{
		using var typeface = CreateTypefaceForTests();
		var engine = new MeasurementEngine();

		var charMetrics = engine.MeasureCharacter(typeface, 12, 'W');
		var advances = engine.MeasureGlyphAdvances(typeface, 12, "W");

		charMetrics.AdvanceWidth.Should().BeApproximately(advances[0], 0.001f);
	}

	[Fact]
	public void MeasureCharacter_LargerFontSize_ProducesLargerMetrics()
	{
		using var typeface = CreateTypefaceForTests();
		var engine = new MeasurementEngine();

		var small = engine.MeasureCharacter(typeface, 10, 'A');
		var large = engine.MeasureCharacter(typeface, 20, 'A');

		large.AdvanceWidth.Should().BeGreaterThan(small.AdvanceWidth);
		large.Ascent.Should().BeGreaterThan(small.Ascent);
		large.Descent.Should().BeGreaterThan(small.Descent);
		large.LineHeight.Should().BeGreaterThan(small.LineHeight);
	}

	[Fact]
	public void MeasureCharacter_Space_HasZeroOrPositiveAdvanceWidth()
	{
		using var typeface = CreateTypefaceForTests();
		var engine = new MeasurementEngine();

		var metrics = engine.MeasureCharacter(typeface, 12, ' ');

		metrics.AdvanceWidth.Should().BeGreaterThan(0);
	}

	[Fact]
	public void MeasureCharacterInTwips_ScalesAllValuesByTwipsPerPoint()
	{
		using var typeface = CreateTypefaceForTests();
		var engine = new MeasurementEngine();

		var points = engine.MeasureCharacter(typeface, 12, 'A');
		var twips = engine.MeasureCharacterInTwips(typeface, 12, 'A');

		twips.AdvanceWidth.Should().BeApproximately(points.AdvanceWidth * 20f, 0.01f);
		twips.Ascent.Should().BeApproximately(points.Ascent * 20f, 0.01f);
		twips.Descent.Should().BeApproximately(points.Descent * 20f, 0.01f);
		twips.Leading.Should().BeApproximately(points.Leading * 20f, 0.01f);
		twips.LineHeight.Should().BeApproximately(points.LineHeight * 20f, 0.01f);
	}

	[Fact]
	public void ShapeText_WithIsRtlFalse_ProducesPositiveWidth()
	{
		using var typeface = CreateTypefaceForTests();
		var engine = new MeasurementEngine();

		var result = engine.ShapeText(typeface, 12, "Hello", isRtl: false);

		result.TotalWidth.Should().BeGreaterThan(0);
		result.Glyphs.Count.Should().BeGreaterThan(0);
	}

	[Fact]
	public void ShapeText_WithIsRtlTrue_ProducesPositiveWidth()
	{
		using var typeface = CreateTypefaceForTests();
		var engine = new MeasurementEngine();

		// The isRtl parameter is currently informational; HarfBuzz auto-detects direction
		var result = engine.ShapeText(typeface, 12, "Hello", isRtl: true);

		result.TotalWidth.Should().BeGreaterThan(0);
		result.Glyphs.Count.Should().BeGreaterThan(0);
	}
}
