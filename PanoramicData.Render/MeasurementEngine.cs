using SkiaSharp;

namespace PanoramicData.Render;

/// <summary>
/// Measures text using SkiaSharp font metrics.
/// </summary>
internal sealed class MeasurementEngine
{
	/// <summary>
	/// Measures per-character advance widths for the given text.
	/// </summary>
	/// <param name="typeface">The typeface to measure with.</param>
	/// <param name="fontSize">The font size in SkiaSharp text units.</param>
	/// <param name="text">The text to measure.</param>
	/// <returns>A list of advance widths, one per character in <paramref name="text"/>.</returns>
	public IReadOnlyList<float> MeasureGlyphAdvances(SKTypeface typeface, float fontSize, string text)
	{
		ArgumentNullException.ThrowIfNull(typeface);
		ArgumentNullException.ThrowIfNull(text);

		if (fontSize <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(fontSize));
		}

		if (text.Length == 0)
		{
			return [];
		}

		using var font = new SKFont(typeface, fontSize);
		var advances = new float[text.Length];
		for (var index = 0; index < text.Length; index++)
		{
			advances[index] = font.MeasureText(text[index].ToString());
		}

		return advances;
	}
}