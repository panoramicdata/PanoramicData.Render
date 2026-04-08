namespace PanoramicData.Render;

/// <summary>
/// Represents a merged table cell region spanning one or more rows and columns.
/// Coordinates are relative to the table top-left corner.
/// </summary>
/// <param name="StartRowIndex">The zero-based starting row index.</param>
/// <param name="StartColumnIndex">The zero-based starting column index.</param>
/// <param name="RowSpan">The number of rows spanned by this region.</param>
/// <param name="ColumnSpan">The number of columns spanned by this region.</param>
/// <param name="X">The x-coordinate of the region's left edge in twips.</param>
/// <param name="Y">The y-coordinate of the region's top edge in twips.</param>
/// <param name="Width">The total region width in twips.</param>
/// <param name="Height">The total region height in twips.</param>
/// <param name="Cell">The source cell element for the region.</param>
internal readonly record struct MergedCellRegion(int StartRowIndex, int StartColumnIndex, int RowSpan, int ColumnSpan, float X, float Y, float Width, float Height, TableCellElement Cell);
