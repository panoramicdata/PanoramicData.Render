namespace PanoramicData.Render;

/// <summary>
/// Specifies the alignment of content at a tab stop.
/// Corresponds to the OOXML w:tab/@w:val attribute (TabStopValues).
/// </summary>
internal enum TabStopType
{
	/// <summary>
	/// Content flows forward from the tab stop position (left-aligned).
	/// </summary>
	Left,

	/// <summary>
	/// Content is centered on the tab stop position.
	/// </summary>
	Center,

	/// <summary>
	/// Content ends at the tab stop position (right-aligned).
	/// </summary>
	Right,

	/// <summary>
	/// The decimal point (or rightmost digit if no decimal) aligns at the tab stop position.
	/// </summary>
	Decimal,

	/// <summary>
	/// A vertical bar is drawn at the tab stop position. No text alignment is performed.
	/// </summary>
	Bar
}
