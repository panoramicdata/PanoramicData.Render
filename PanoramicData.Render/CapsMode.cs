namespace PanoramicData.Render;

/// <summary>
/// Specifies the caps transformation mode for a text run.
/// Determined from the resolved <see cref="ToggleState.Caps"/> and
/// <see cref="ToggleState.SmallCaps"/> toggle properties.
/// </summary>
internal enum CapsMode
{
	/// <summary>No caps transformation; text is rendered as authored.</summary>
	None,

	/// <summary>All characters are rendered as uppercase glyphs at the original font size.</summary>
	AllCaps,

	/// <summary>
	/// Lowercase characters are rendered as uppercase glyphs at a reduced font size
	/// (typically ~80% of the parent size); characters that are already uppercase
	/// retain their original size.
	/// </summary>
	SmallCaps
}
