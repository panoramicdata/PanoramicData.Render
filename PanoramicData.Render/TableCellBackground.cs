namespace PanoramicData.Render;

/// <summary>
/// Represents a table cell background fill region to be painted before cell content.
/// </summary>
/// <param name="RowIndex">The zero-based row index of the owning cell.</param>
/// <param name="ColumnIndex">The zero-based starting column index of the owning cell.</param>
/// <param name="X">The background rectangle x-coordinate in twips.</param>
/// <param name="Y">The background rectangle y-coordinate in twips.</param>
/// <param name="Width">The background rectangle width in twips.</param>
/// <param name="Height">The background rectangle height in twips.</param>
/// <param name="Shading">The resolved shading definition for the cell background.</param>
/// <param name="Cell">The source table cell.</param>
internal readonly record struct TableCellBackground(
	int RowIndex,
	int ColumnIndex,
	float X,
	float Y,
	float Width,
	float Height,
	ParagraphShading Shading,
	TableCellElement Cell);