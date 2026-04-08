namespace PanoramicData.Render;

/// <summary>
/// Represents a vertically merged table cell region (a cell spanning multiple rows via <c>w:vMerge</c>).
/// Coordinates are relative to the table top-left corner.
/// </summary>
/// <param name="StartRowIndex">The zero-based starting row index.</param>
/// <param name="ColumnIndex">The zero-based starting column index.</param>
/// <param name="RowSpan">The number of rows spanned by this region.</param>
/// <param name="X">The x-coordinate of the region's left edge in twips.</param>
/// <param name="Y">The y-coordinate of the region's top edge in twips.</param>
/// <param name="Width">The total region width in twips.</param>
/// <param name="Height">The region height in twips.</param>
/// <param name="Cell">The source cell element for the region.</param>
internal readonly record struct VerticalMergeRegion(
	int StartRowIndex,
	int ColumnIndex,
	int RowSpan,
	float X,
	float Y,
	float Width,
	float Height,
	TableCellElement Cell);
