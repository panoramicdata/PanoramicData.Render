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