namespace PanoramicData.Render;

/// <summary>
/// Specifies the text flow direction in a table cell.
/// </summary>
internal enum CellTextDirection
{
	/// <summary>
	/// Left-to-right, top-to-bottom (default horizontal text).
	/// </summary>
	LeftToRightTopToBottom = 0,

	/// <summary>
	/// Top-to-bottom, right-to-left (vertical text rotated 90° clockwise).
	/// </summary>
	TopToBottomRightToLeft = 1,

	/// <summary>
	/// Bottom-to-top, left-to-right (vertical text rotated 90° counter-clockwise).
	/// </summary>
	BottomToTopLeftToRight = 2,
}
