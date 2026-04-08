namespace PanoramicData.Render;

/// <summary>
/// Represents a single table border definition with style, width, and color.
/// </summary>
/// <param name="Style">The visual border style.</param>
/// <param name="WidthEighthsOfPoint">The border width in eighths of a point.</param>
/// <param name="Color">The border color as hex RGB or "auto". Null means unspecified.</param>
internal readonly record struct TableBorderDefinition(
	BorderStyle Style = BorderStyle.None,
	int WidthEighthsOfPoint = 0,
	string? Color = null)
{
	/// <summary>
	/// A border with no visible style.
	/// </summary>
	public static readonly TableBorderDefinition None = new();

	/// <summary>
	/// Gets whether this border is visible.
	/// </summary>
	public bool IsVisible => Style != BorderStyle.None;

	/// <summary>
	/// Gets the border width in twips.
	/// </summary>
	public float GetWidthTwips() => WidthEighthsOfPoint * 2.5f;
}
