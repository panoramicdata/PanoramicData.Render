namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using SkiaSharp;
using SkiaSharp.HarfBuzz;
using Xunit;

public class MeasurementEngineTests
{
	[Fact]
	public void MeasureGlyphAdvances_WithNullTypeface_ThrowsArgumentNullException()
	{
		var engine = new MeasurementEngine();

		var act = () => engine.MeasureGlyphAdvances(null!, 12, "Hello");

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void MeasureGlyphAdvances_WithNullText_ThrowsArgumentNullException()
	{
		using var typeface = CreateTypefaceForTests();
		var engine = new MeasurementEngine();

		var act = () => engine.MeasureGlyphAdvances(typeface, 12, null!);

		act.Should().Throw<ArgumentNullException>();
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void MeasureGlyphAdvances_WithNonPositiveFontSize_ThrowsArgumentOutOfRangeException(float fontSize)
	{
		using var typeface = CreateTypefaceForTests();
		var engine = new MeasurementEngine();

		var act = () => engine.MeasureGlyphAdvances(typeface, fontSize, "Hello");

		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void MeasureGlyphAdvances_WithEmptyText_ReturnsEmpty()
	{
		using var typeface = CreateTypefaceForTests();
		var engine = new MeasurementEngine();

		var result = engine.MeasureGlyphAdvances(typeface, 12, string.Empty);

		result.Should().BeEmpty();
	}

	[Fact]
	public void MeasureGlyphAdvances_WithAsciiText_ReturnsPerCharacterAdvances()
	{
		using var typeface = CreateTypefaceForTests();
		using var font = new SKFont(typeface, 12);
		var engine = new MeasurementEngine();
		const string text = "Hello";

		var result = engine.MeasureGlyphAdvances(typeface, 12, text);

		result.Should().HaveCount(text.Length);
		for (var index = 0; index < text.Length; index++)
		{
			result[index].Should().BeApproximately(font.MeasureText(text[index].ToString()), 0.001f);
		}
	}

	[Fact]
	public void MeasureGlyphAdvances_WithWhitespace_PreservesCharacterPositions()
	{
		using var typeface = CreateTypefaceForTests();
		using var font = new SKFont(typeface, 16);
		var engine = new MeasurementEngine();
		const string text = "A B";

		var result = engine.MeasureGlyphAdvances(typeface, 16, text);

		result.Should().HaveCount(3);
		result[1].Should().BeApproximately(font.MeasureText(" "), 0.001f);
	}

	[Fact]
	public void ShapeText_WithNullTypeface_ThrowsArgumentNullException()
	{
		var engine = new MeasurementEngine();

		var act = () => engine.ShapeText(null!, 12, "Hello");

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void ShapeText_WithNullText_ThrowsArgumentNullException()
	{
		using var typeface = CreateTypefaceForTests();
		var engine = new MeasurementEngine();

		var act = () => engine.ShapeText(typeface, 12, null!);

		act.Should().Throw<ArgumentNullException>();
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void ShapeText_WithNonPositiveFontSize_ThrowsArgumentOutOfRangeException(float fontSize)
	{
		using var typeface = CreateTypefaceForTests();
		var engine = new MeasurementEngine();

		var act = () => engine.ShapeText(typeface, fontSize, "Hello");

		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void ShapeText_WithEmptyText_ReturnsEmptyRun()
	{
		using var typeface = CreateTypefaceForTests();
		var engine = new MeasurementEngine();

		var result = engine.ShapeText(typeface, 12, string.Empty);

		result.Glyphs.Should().BeEmpty();
		result.TotalWidth.Should().Be(0);
	}

	[Fact]
	public void ShapeText_WithAsciiText_ReturnsGlyphsWithPositiveAdvances()
	{
		using var typeface = CreateTypefaceForTests();
		var engine = new MeasurementEngine();
		const string text = "Hello";

		var result = engine.ShapeText(typeface, 12, text);

		result.Glyphs.Should().NotBeEmpty();
		result.TotalWidth.Should().BeGreaterThan(0);
		foreach (var glyph in result.Glyphs)
		{
			glyph.AdvanceWidth.Should().BeGreaterThan(0);
			glyph.OffsetX.Should().BeGreaterThanOrEqualTo(0);
			_ = glyph.OffsetY; // Ensure OffsetY is accessible
		}
	}

	[Fact]
	public void ShapeText_TotalWidth_MatchesSKShaperResult()
	{
		using var typeface = CreateTypefaceForTests();
		using var shaper = new SKShaper(typeface);
		using var font = new SKFont(typeface, 14);
		var engine = new MeasurementEngine();
		const string text = "Testing width";

		var result = engine.ShapeText(typeface, 14, text);
		var expected = shaper.Shape(text, font);

		result.TotalWidth.Should().BeApproximately(expected.Width, 0.001f);
	}

	[Fact]
	public void ShapeText_GlyphAdvanceWidths_SumToTotalWidth()
	{
		using var typeface = CreateTypefaceForTests();
		var engine = new MeasurementEngine();
		const string text = "Sum test";

		var result = engine.ShapeText(typeface, 12, text);

		var sumOfAdvances = result.Glyphs.Sum(g => g.AdvanceWidth);
		sumOfAdvances.Should().BeApproximately(result.TotalWidth, 0.01f);
	}

	[Fact]
	public void ShapeText_ClustersMapBackToSourceText()
	{
		using var typeface = CreateTypefaceForTests();
		var engine = new MeasurementEngine();
		const string text = "ABC";

		var result = engine.ShapeText(typeface, 12, text);

		// Cluster values should be within the source text range
		foreach (var glyph in result.Glyphs)
		{
			glyph.Cluster.Should().BeLessThan((uint)text.Length);
		}
	}

	[Fact]
	public void ShapeText_WithWhitespace_ProducesGlyphsForAllCharacters()
	{
		using var typeface = CreateTypefaceForTests();
		var engine = new MeasurementEngine();
		const string text = "A B";

		var result = engine.ShapeText(typeface, 16, text);

		// Simple Latin text: one glyph per character
		result.Glyphs.Should().HaveCount(3);
	}

	[Fact]
	public void ShapeText_CodepointsAreNonZero()
	{
		using var typeface = CreateTypefaceForTests();
		var engine = new MeasurementEngine();
		const string text = "Hi";

		var result = engine.ShapeText(typeface, 12, text);

		foreach (var glyph in result.Glyphs)
		{
			glyph.Codepoint.Should().BeGreaterThan(0u);
		}
	}

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

	private static SKTypeface CreateTypefaceForTests()
	{
		var fontPath = FindInstalledFontFile();
		fontPath.Should().NotBeNullOrWhiteSpace();
		return SKTypeface.FromFile(fontPath!);
	}

	private static string? FindInstalledFontFile()
	{
		var candidates = new[]
		{
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts"),
			"/usr/share/fonts",
			"/usr/local/share/fonts",
			"/Library/Fonts"
		};

		foreach (var directory in candidates)
		{
			if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
			{
				continue;
			}

			var file = Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories)
				.FirstOrDefault(path =>
					path.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) ||
					path.EndsWith(".otf", StringComparison.OrdinalIgnoreCase));

			if (!string.IsNullOrWhiteSpace(file))
			{
				return file;
			}
		}

		return null;
	}
}