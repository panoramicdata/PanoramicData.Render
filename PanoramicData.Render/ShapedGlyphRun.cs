namespace PanoramicData.Render;

/// <summary>
/// Represents the result of shaping a text run via HarfBuzz.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ShapedGlyphRun"/> class.
/// </remarks>
/// <param name="glyphs">The shaped glyphs.</param>
/// <param name="totalWidth">The total width of the shaped text.</param>
internal sealed class ShapedGlyphRun(IReadOnlyList<ShapedGlyph> glyphs, float totalWidth)
{

	/// <summary>
	/// Gets the shaped glyphs.
	/// </summary>
	public IReadOnlyList<ShapedGlyph> Glyphs { get; } = glyphs;

	/// <summary>
	/// Gets the total width of the shaped text.
	/// </summary>
	public float TotalWidth { get; } = totalWidth;
}
