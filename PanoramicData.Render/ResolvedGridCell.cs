namespace PanoramicData.Render;

/// <summary>
/// Represents one position in the resolved table grid, linking back to
/// the owning <see cref="TableCellElement"/> and its origin row/column.
/// </summary>
/// <param name="OwnerRowIndex">The zero-based row index where the owning cell starts.</param>
/// <param name="OwnerColumnIndex">The zero-based column index where the owning cell starts.</param>
/// <param name="Cell">The <see cref="TableCellElement"/> that occupies this grid position.</param>
internal readonly record struct ResolvedGridCell(
	int OwnerRowIndex,
	int OwnerColumnIndex,
	TableCellElement Cell);
