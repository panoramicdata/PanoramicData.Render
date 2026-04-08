namespace PanoramicData.Render;

/// <summary>
/// Specifies how a row height value should be interpreted.
/// </summary>
internal enum RowHeightRule
{
	/// <summary>
	/// Row height is determined automatically from content.
	/// </summary>
	Auto = 0,

	/// <summary>
	/// Row height is at least the specified value, but may grow.
	/// </summary>
	AtLeast = 1,

	/// <summary>
	/// Row height is exactly the specified value (content may be clipped).
	/// </summary>
	Exact = 2,
}
