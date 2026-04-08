namespace PanoramicData.Render;

/// <summary>
/// Represents a horizontally merged table cell region (a cell with <c>GridSpan &gt; 1</c>).
/// Coordinates are relative to the table top-left corner.
/// </summary>
/// <param name="RowIndex">The zero-based row index.</param>
/// <param name="StartColumnIndex">The zero-based starting column index.</param>
/// <param name="ColumnSpan">The number of columns spanned by this region.</param>
/// <param name="X">The x-coordinate of the region's left edge in twips.</param>
/// <param name="Y">The y-coordinate of the region's top edge in twips.</param>
/// <param name="Width">The total region width in twips.</param>
/// <param name="Height">The region height in twips.</param>
/// <param name="Cell">The source cell element for the region.</param>
internal readonly record struct HorizontalMergeRegion(
	int RowIndex,
	int StartColumnIndex,
	int ColumnSpan,
	float X,
	float Y,
	float Width,
	float Height,
	TableCellElement Cell);
