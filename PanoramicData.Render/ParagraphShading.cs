namespace PanoramicData.Render;

/// <summary>
/// Represents the shading (background) properties of a paragraph or table cell.
/// </summary>
/// <remarks>
/// Corresponds to the OOXML w:shd element with w:val (pattern), w:color (pattern color), and w:fill (background color).
/// For a simple solid background, use <see cref="ShadingPattern.Clear"/> with a <see cref="FillColor"/>,
/// or <see cref="ShadingPattern.Solid"/> with a <see cref="PatternColor"/>.
/// </remarks>
/// <param name="Pattern">The shading pattern type.</param>
/// <param name="PatternColor">
/// The pattern foreground color as hex RGB (e.g., "FF0000"). Null means auto/unspecified.
/// Used when <paramref name="Pattern"/> is not <see cref="ShadingPattern.Clear"/>.
/// </param>
/// <param name="FillColor">
/// The background fill color as hex RGB (e.g., "FFFF00"). Null means no fill (transparent).
/// This is the primary background color for simple paragraph shading.
/// </param>
internal readonly record struct ParagraphShading(
	ShadingPattern Pattern = ShadingPattern.Clear,
	string? PatternColor = null,
	string? FillColor = null)
{
	/// <summary>
	/// A shading instance with no pattern and no fill.
	/// </summary>
	public static readonly ParagraphShading None = new();

	/// <summary>
	/// Gets whether this shading has any visible effect (non-clear pattern or a fill color).
	/// </summary>
	public bool HasVisibleShading =>
		Pattern != ShadingPattern.Clear || FillColor is not null;

	/// <summary>
	/// Gets the effective background color, considering the pattern.
	/// For <see cref="ShadingPattern.Solid"/>, returns <see cref="PatternColor"/> (which covers the fill).
	/// For <see cref="ShadingPattern.Clear"/>, returns <see cref="FillColor"/>.
	/// For other patterns, returns <see cref="FillColor"/> (the base layer).
	/// </summary>
	public string? GetEffectiveBackgroundColor() => Pattern switch
	{
		ShadingPattern.Solid => PatternColor ?? FillColor,
		_ => FillColor
	};
}
