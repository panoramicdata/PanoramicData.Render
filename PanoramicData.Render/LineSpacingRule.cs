namespace PanoramicData.Render;

/// <summary>
/// Specifies how the line spacing value is interpreted.
/// Corresponds to the OOXML w:lineRule attribute on w:spacing.
/// </summary>
internal enum LineSpacingRule
{
	/// <summary>
	/// Proportional spacing: the line spacing value is a multiple of the natural line height
	/// (240 twips = single spacing, 360 = 1.5×, 480 = double, etc.).
	/// </summary>
	Auto,

	/// <summary>
	/// Fixed spacing: the line height is exactly the specified value in twips,
	/// regardless of content height (content may be clipped).
	/// </summary>
	Exact,

	/// <summary>
	/// Minimum spacing: the line height is the greater of the natural line height
	/// and the specified value in twips.
	/// </summary>
	AtLeast
}
