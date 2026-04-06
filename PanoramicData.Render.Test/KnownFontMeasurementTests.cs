namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using SkiaSharp;
using SkiaSharp.HarfBuzz;
using Xunit;

/// <summary>
/// Verification tests that validate the measurement pipeline produces correct
/// twip values for known fonts. Uses Arial (available on Windows) and validates
/// against direct SkiaSharp measurements within ±1 twip tolerance.
/// </summary>
public class KnownFontMeasurementTests
{
	private const float OneTwip = 1f;
	private const float TwipsPerPoint = 20f;

	[Fact]
	public void Arial12pt_PerCharacterAdvances_MatchDirectSkiaMeasurement()
	{
		using var typeface = TryLoadArial();
		const float fontSize = 12f;
		const string text = "Hello World";

		using var font = new SKFont(typeface, fontSize);
		var engine = new MeasurementEngine();

		var twipAdvances = engine.MeasureGlyphAdvancesInTwips(typeface, fontSize, text);

		twipAdvances.Should().HaveCount(text.Length);
		for (var i = 0; i < text.Length; i++)
		{
			var expected = font.MeasureText(text[i].ToString()) * TwipsPerPoint;
			twipAdvances[i].Should().BeApproximately(expected, OneTwip,
				$"character '{text[i]}' at index {i}");
		}
	}

	[Fact]
	public void Arial12pt_ShapedTotalWidth_MatchesDirectHarfBuzz()
	{
		using var typeface = TryLoadArial();
		const float fontSize = 12f;
		const string text = "Hello World";

		using var shaper = new SKShaper(typeface);
		using var font = new SKFont(typeface, fontSize);
		var directResult = shaper.Shape(text, font);
		var expectedTwips = directResult.Width * TwipsPerPoint;

		var engine = new MeasurementEngine();
		var run = engine.ShapeTextInTwips(typeface, fontSize, text);

		run.TotalWidth.Should().BeApproximately(expectedTwips, OneTwip);
	}

	[Fact]
	public void Arial12pt_ShapedGlyphAdvances_MatchDirectHarfBuzz()
	{
		using var typeface = TryLoadArial();
		const float fontSize = 12f;
		const string text = "Hello World";

		using var shaper = new SKShaper(typeface);
		using var font = new SKFont(typeface, fontSize);
		var directResult = shaper.Shape(text, font);

		var engine = new MeasurementEngine();
		var run = engine.ShapeTextInTwips(typeface, fontSize, text);

		run.Glyphs.Should().HaveCount(directResult.Points.Length);
		for (var i = 0; i < directResult.Points.Length; i++)
		{
			var expectedAdvance = i < directResult.Points.Length - 1
				? (directResult.Points[i + 1].X - directResult.Points[i].X) * TwipsPerPoint
				: (directResult.Width - directResult.Points[i].X) * TwipsPerPoint;

			run.Glyphs[i].AdvanceWidth.Should().BeApproximately(expectedAdvance, OneTwip,
				$"glyph at index {i}");
		}
	}

	[Fact]
	public void Arial12pt_CharacterMetrics_MatchDirectSkiaFontMetrics()
	{
		using var typeface = TryLoadArial();
		const float fontSize = 12f;

		using var font = new SKFont(typeface, fontSize);
		var skiaMetrics = font.Metrics;

		var engine = new MeasurementEngine();
		var metrics = engine.MeasureCharacterInTwips(typeface, fontSize, 'A');

		metrics.Ascent.Should().BeApproximately(-skiaMetrics.Ascent * TwipsPerPoint, OneTwip);
		metrics.Descent.Should().BeApproximately(skiaMetrics.Descent * TwipsPerPoint, OneTwip);
		metrics.Leading.Should().BeApproximately(skiaMetrics.Leading * TwipsPerPoint, OneTwip);
	}

	[Theory]
	[InlineData(8f)]
	[InlineData(10f)]
	[InlineData(11f)]
	[InlineData(12f)]
	[InlineData(14f)]
	[InlineData(16f)]
	[InlineData(18f)]
	[InlineData(24f)]
	[InlineData(36f)]
	[InlineData(48f)]
	public void Arial_VariousSizes_AdvancesConsistentWithDirectMeasurement(float fontSize)
	{
		using var typeface = TryLoadArial();
		const string text = "The quick brown fox jumps over the lazy dog";

		using var font = new SKFont(typeface, fontSize);
		var engine = new MeasurementEngine();
		var twipAdvances = engine.MeasureGlyphAdvancesInTwips(typeface, fontSize, text);

		for (var i = 0; i < text.Length; i++)
		{
			var expected = font.MeasureText(text[i].ToString()) * TwipsPerPoint;
			twipAdvances[i].Should().BeApproximately(expected, OneTwip,
				$"font size {fontSize}pt, character '{text[i]}' at index {i}");
		}
	}

	[Fact]
	public void Arial12pt_ShapedAndUnshaped_ProduceSimilarTotalWidth()
	{
		using var typeface = TryLoadArial();
		const float fontSize = 12f;
		const string text = "Hello World";

		var engine = new MeasurementEngine();
		var advances = engine.MeasureGlyphAdvancesInTwips(typeface, fontSize, text);
		var unshaped = advances.Sum();
		var shaped = engine.ShapeTextInTwips(typeface, fontSize, text);

		// For simple Latin text, shaped and unshaped widths should be close.
		// HarfBuzz may apply kerning, so allow a wider tolerance (5% of total).
		var tolerance = unshaped * 0.05f;
		shaped.TotalWidth.Should().BeApproximately(unshaped, tolerance);
	}

	[Fact]
	public void Arial12pt_LineHeightInTwips_IsConsistentAcrossCharacters()
	{
		using var typeface = TryLoadArial();
		const float fontSize = 12f;
		var engine = new MeasurementEngine();

		// Font metrics (ascent/descent/leading) are font-wide, not character-specific.
		// All characters at the same size should report the same line height.
		var metricsA = engine.MeasureCharacterInTwips(typeface, fontSize, 'A');
		var metricsZ = engine.MeasureCharacterInTwips(typeface, fontSize, 'z');
		var metricsSpace = engine.MeasureCharacterInTwips(typeface, fontSize, ' ');

		metricsA.Ascent.Should().Be(metricsZ.Ascent);
		metricsA.Descent.Should().Be(metricsZ.Descent);
		metricsA.Leading.Should().Be(metricsZ.Leading);
		metricsA.LineHeight.Should().Be(metricsZ.LineHeight);

		metricsA.Ascent.Should().Be(metricsSpace.Ascent);
		metricsA.LineHeight.Should().Be(metricsSpace.LineHeight);
	}

	[Fact]
	public void Arial12pt_SuperscriptSimulation_ProducesSmallerMetrics()
	{
		using var typeface = TryLoadArial();
		const float normalSize = 12f;
		const float superscriptSize = normalSize * (2f / 3f); // Word's typical ratio

		var engine = new MeasurementEngine();
		var normal = engine.MeasureCharacterInTwips(typeface, normalSize, 'A');
		var super = engine.MeasureCharacterInTwips(typeface, superscriptSize, 'A');

		// Superscript should be proportionally smaller
		super.AdvanceWidth.Should().BeLessThan(normal.AdvanceWidth);
		super.Ascent.Should().BeLessThan(normal.Ascent);
		super.LineHeight.Should().BeLessThan(normal.LineHeight);

		// The ratio should be approximately 2/3
		var widthRatio = super.AdvanceWidth / normal.AdvanceWidth;
		widthRatio.Should().BeApproximately(2f / 3f, 0.01f);
	}

	[Fact]
	public void Arial12pt_MeasureCharacter_AdvanceWidthMatchesMeasureGlyphAdvances()
	{
		using var typeface = TryLoadArial();
		const float fontSize = 12f;
		var engine = new MeasurementEngine();

		foreach (var ch in "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 ")
		{
			var charMetrics = engine.MeasureCharacterInTwips(typeface, fontSize, ch);
			var advances = engine.MeasureGlyphAdvancesInTwips(typeface, fontSize, ch.ToString());

			charMetrics.AdvanceWidth.Should().BeApproximately(advances[0], OneTwip,
				$"character '{ch}'");
		}
	}

	private static SKTypeface TryLoadArial()
	{
		var arialPath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.Windows),
			"Fonts", "arial.ttf");

		if (!File.Exists(arialPath))
		{
			Assert.Skip("Arial font not available on this platform");
		}

		return SKTypeface.FromFile(arialPath)
			?? throw new InvalidOperationException("Failed to load Arial typeface");
	}
}
