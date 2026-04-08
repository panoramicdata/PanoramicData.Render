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
			var row = table.Rows[rowIndex];
			var colIndex = 0;

			foreach (var cell in row.Cells)
			{
				if (colIndex >= columnCount)
				{
					break;
				}

				var span = cell.GridSpan;

				if (cell.VerticalMerge == VerticalMergeState.Continue)
				{
					// Find the restart cell above in the same column
					var ownerRowIndex = FindVerticalMergeOwner(grid, rowIndex, colIndex);
					var ownerCell = ownerRowIndex >= 0
						? grid[ownerRowIndex, colIndex]!.Value.Cell
						: cell;
					var ownerCol = ownerRowIndex >= 0
						? grid[ownerRowIndex, colIndex]!.Value.OwnerColumnIndex
						: colIndex;
					var ownerRow = ownerRowIndex >= 0
						? ownerRowIndex
						: rowIndex;

					for (var s = 0; s < span && colIndex + s < columnCount; s++)
					{
						grid[rowIndex, colIndex + s] = new ResolvedGridCell(ownerRow, ownerCol, ownerCell);
					}
				}
				else
				{
					// None or Restart: this cell owns its grid positions
					for (var s = 0; s < span && colIndex + s < columnCount; s++)
					{
						grid[rowIndex, colIndex + s] = new ResolvedGridCell(rowIndex, colIndex, cell);
					}
				}

				colIndex += span;
			}
		}

		return grid;
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
