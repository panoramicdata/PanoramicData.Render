namespace PanoramicData.Render;

/// <summary>
/// Indicates the vertical merge state of a table cell.
/// </summary>
internal enum VerticalMergeState
{
	/// <summary>
	/// The cell is not part of a vertical merge.
	/// </summary>
	None = 0,

	/// <summary>
	/// The cell starts a new vertical merge group (<c>w:vMerge val="restart"</c>).
	/// </summary>
	Restart = 1,

	/// <summary>
	/// The cell continues a vertical merge group (<c>w:vMerge</c> with no val or val="continue").
	/// </summary>
	Continue = 2,
}
