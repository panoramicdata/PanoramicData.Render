using SkiaSharp;
using SkiaSharp.HarfBuzz;

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

	/// <summary>
	/// Measures per-character advance widths in twips for the given text.
	/// </summary>
	/// <param name="typeface">The typeface to measure with.</param>
	/// <param name="fontSizePoints">The font size in typographic points.</param>
	/// <param name="text">The text to measure.</param>
	/// <returns>A list of advance widths in twips, one per character in <paramref name="text"/>.</returns>
	public IReadOnlyList<float> MeasureGlyphAdvancesInTwips(SKTypeface typeface, float fontSizePoints, string text)
	{
		var advances = MeasureGlyphAdvances(typeface, fontSizePoints, text);
		var twipAdvances = new float[advances.Count];
		for (var i = 0; i < advances.Count; i++)
		{
			twipAdvances[i] = TwipConverter.PointsToTwips(advances[i]);
		}

		return twipAdvances;
	}

	/// <summary>
	/// Shapes text using HarfBuzz, producing a glyph run with correct advance widths,
	/// kerning, and ligature substitution applied.
	/// </summary>
	/// <param name="typeface">The typeface to shape with.</param>
	/// <param name="fontSize">The font size in SkiaSharp text units.</param>
	/// <param name="text">The text to shape.</param>
	/// <returns>A <see cref="ShapedGlyphRun"/> containing the shaped glyphs.</returns>
	public ShapedGlyphRun ShapeText(SKTypeface typeface, float fontSize, string text)
	{
		ArgumentNullException.ThrowIfNull(typeface);
		ArgumentNullException.ThrowIfNull(text);

		if (fontSize <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(fontSize));
		}

		if (text.Length == 0)
		{
			return new ShapedGlyphRun([], 0);
		}

		using var shaper = new SKShaper(typeface);
		using var font = new SKFont(typeface, fontSize);
		var result = shaper.Shape(text, font);

		var points = result.Points;
		var codepoints = result.Codepoints;
		var clusters = result.Clusters;
		var glyphCount = points.Length;

		var glyphs = new ShapedGlyph[glyphCount];
		for (var i = 0; i < glyphCount; i++)
		{
			var advanceWidth = i < glyphCount - 1
				? points[i + 1].X - points[i].X
				: result.Width - points[i].X;

			glyphs[i] = new ShapedGlyph(
				Codepoint: codepoints[i],
				AdvanceWidth: advanceWidth,
				OffsetX: points[i].X,
				OffsetY: points[i].Y,
				Cluster: clusters[i]);
		}

		return new ShapedGlyphRun(glyphs, result.Width);
	}

	/// <summary>
	/// Shapes text using HarfBuzz and returns results in twips.
	/// </summary>
	/// <param name="typeface">The typeface to shape with.</param>
	/// <param name="fontSizePoints">The font size in typographic points.</param>
	/// <param name="text">The text to shape.</param>
	/// <returns>A <see cref="ShapedGlyphRun"/> with all measurements in twips.</returns>
	public ShapedGlyphRun ShapeTextInTwips(SKTypeface typeface, float fontSizePoints, string text)
	{
		var run = ShapeText(typeface, fontSizePoints, text);
		var twipGlyphs = new ShapedGlyph[run.Glyphs.Count];
		for (var i = 0; i < run.Glyphs.Count; i++)
		{
			var g = run.Glyphs[i];
			twipGlyphs[i] = new ShapedGlyph(
				Codepoint: g.Codepoint,
				AdvanceWidth: TwipConverter.PointsToTwips(g.AdvanceWidth),
				OffsetX: TwipConverter.PointsToTwips(g.OffsetX),
				OffsetY: TwipConverter.PointsToTwips(g.OffsetY),
				Cluster: g.Cluster);
		}

		return new ShapedGlyphRun(twipGlyphs, TwipConverter.PointsToTwips(run.TotalWidth));
	}
}