namespace PanoramicData.Render;

/// <summary>
/// Resolves a <see cref="TableElement"/> into a two-dimensional grid that accounts
/// for horizontal (<c>w:gridSpan</c>) and vertical (<c>w:vMerge</c>) merging.
/// Each position in the grid references the owning cell and its origin coordinates.
/// </summary>
internal static class TableGridResolver
{
	/// <summary>
	/// Resolves the table grid, producing a 2D array [row, column] of <see cref="ResolvedGridCell"/>.
	/// Cells that are null indicate positions outside the grid definition (e.g. extra columns).
	/// </summary>
	/// <param name="table">The parsed table element.</param>
	/// <returns>A 2D array of resolved grid cells.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="table"/> is <see langword="null"/>.</exception>
	internal static ResolvedGridCell?[,] Resolve(TableElement table)
	{
		ArgumentNullException.ThrowIfNull(table);

		var rowCount = table.Rows.Count;
		var columnCount = table.GridColumns.Count;

		if (rowCount == 0 || columnCount == 0)
		{
			return new ResolvedGridCell?[0, 0];
		}

		var grid = new ResolvedGridCell?[rowCount, columnCount];

		for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
		{
			var colIndex = 0;
			foreach (var cell in table.Rows[rowIndex].Cells)
			{
				if (colIndex >= columnCount)
				{
					break;
				}

				PlaceCell(grid, rowIndex, colIndex, cell);
				colIndex += cell.GridSpan;
			}
		}

		return grid;
	}

	private static void PlaceCell(ResolvedGridCell?[,] grid, int rowIndex, int colIndex, TableCellElement cell)
	{
		var columnCount = grid.GetLength(1);
		var (ownerRow, ownerCol, ownerCell) = cell.VerticalMerge == VerticalMergeState.Continue
			? ResolveMergeOwner(grid, rowIndex, colIndex, cell)
			: (rowIndex, colIndex, cell);

		for (var s = 0; s < cell.GridSpan && colIndex + s < columnCount; s++)
		{
			grid[rowIndex, colIndex + s] = new ResolvedGridCell(ownerRow, ownerCol, ownerCell);
		}
	}

	private static (int OwnerRow, int OwnerColumn, TableCellElement OwnerCell) ResolveMergeOwner(ResolvedGridCell?[,] grid, int rowIndex, int colIndex, TableCellElement cell)
	{
		var ownerRowIndex = FindVerticalMergeOwner(grid, rowIndex, colIndex);
		if (ownerRowIndex < 0)
		{
			return (rowIndex, colIndex, cell);
		}

		var owner = grid[ownerRowIndex, colIndex]!.Value;
		return (ownerRowIndex, owner.OwnerColumnIndex, owner.Cell);
	}
	/// <summary>
	/// Walks upward from the given row to find the row index of the cell
	/// that started the vertical merge in the given column.
	/// </summary>
	private static int FindVerticalMergeOwner(ResolvedGridCell?[,] grid, int currentRow, int column)
	{
		for (var r = currentRow - 1; r >= 0; r--)
		{
			if (grid[r, column] is { } cell)
			{
				return cell.OwnerRowIndex;
			}
		}

		return -1;
	}
}
