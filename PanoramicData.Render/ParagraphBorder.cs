namespace PanoramicData.Render;

/// <summary>
/// Represents a single border definition with style, width, spacing, and color.
/// </summary>
/// <remarks>
/// Corresponds to individual OOXML border elements (w:top, w:bottom, etc.) within w:pBdr.
/// Width is specified in eighths of a point (w:sz attribute).
/// Spacing is in points (w:space attribute, 0–31).
/// Color is a hex RGB string (e.g., "FF0000") or "auto".
/// </remarks>
/// <param name="Style">The visual style of the border line.</param>
/// <param name="WidthEighthsOfPoint">The border width in eighths of a point (w:sz). 1 point = 8 units.</param>
/// <param name="SpacingPoints">The spacing between the border and paragraph content, in points (w:space).</param>
/// <param name="Color">The border color as hex RGB (e.g., "FF0000") or "auto" for automatic color. Null means unspecified.</param>
internal readonly record struct ParagraphBorder(
	BorderStyle Style = BorderStyle.None,
	int WidthEighthsOfPoint = 0,
	float SpacingPoints = 0f,
	string? Color = null)
{
	/// <summary>
	/// A border with no style (invisible).
	/// </summary>
	public static readonly ParagraphBorder None = new();

	/// <summary>
	/// Gets the border width in twips.
	/// Conversion: eighths-of-a-point × 2.5 = twips (since 1 point = 20 twips, 1/8 point = 2.5 twips).
	/// </summary>
	public float GetWidthTwips() => WidthEighthsOfPoint * 2.5f;

	/// <summary>
	/// Gets the spacing between the border and content in twips.
	/// Conversion: points × 20 = twips.
	/// </summary>
	public float GetSpacingTwips() => SpacingPoints * 20f;

	/// <summary>
	/// Gets whether this border has a visible style (anything other than None).
	/// </summary>
	public bool IsVisible => Style != BorderStyle.None;
}
