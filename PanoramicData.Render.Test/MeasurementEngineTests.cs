namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using SkiaSharp;
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