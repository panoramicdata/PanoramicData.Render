namespace PanoramicData.Render;

/// <summary>
/// Represents the computed position and dimensions of a single table cell
/// within the table layout, in twips relative to the table's top-left corner.
/// </summary>
/// <param name="RowIndex">The zero-based row index of the cell.</param>
/// <param name="ColumnIndex">The zero-based starting column index of the cell.</param>
/// <param name="X">The x-coordinate of the cell's left edge relative to the table's left edge, in twips.</param>
/// <param name="Y">The y-coordinate of the cell's top edge relative to the table's top edge, in twips.</param>
/// <param name="Width">The cell width in twips (accounts for grid span).</param>
/// <param name="Height">The cell height in twips (accounts for vertical merge).</param>
/// <param name="Cell">The source <see cref="TableCellElement"/>.</param>
internal readonly record struct CellPosition(
	int RowIndex,
	int ColumnIndex,
	float X,
	float Y,
	float Width,
	float Height,
	TableCellElement Cell);
