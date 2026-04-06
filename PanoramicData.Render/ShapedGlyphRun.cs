namespace PanoramicData.Render;

/// <summary>
/// Represents the result of shaping a text run via HarfBuzz.
/// </summary>
internal sealed class ShapedGlyphRun
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ShapedGlyphRun"/> class.
	/// </summary>
	/// <param name="glyphs">The shaped glyphs.</param>
	/// <param name="totalWidth">The total width of the shaped text.</param>
	public ShapedGlyphRun(IReadOnlyList<ShapedGlyph> glyphs, float totalWidth)
	{
		Glyphs = glyphs;
		TotalWidth = totalWidth;
	}

	/// <summary>
	/// Gets the shaped glyphs.
	/// </summary>
	public IReadOnlyList<ShapedGlyph> Glyphs { get; }

	/// <summary>
	/// Gets the total width of the shaped text.
	/// </summary>
	public float TotalWidth { get; }
}
