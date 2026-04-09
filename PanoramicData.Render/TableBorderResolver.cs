namespace PanoramicData.Render;

/// <summary>
/// Resolves effective table borders using OOXML precedence:
/// cell-level > row-level > table-level (including insideH/insideV for inner edges).
/// </summary>
internal static class TableBorderResolver
{
	/// <summary>
	/// Resolves the effective border for a specific cell edge.
	/// Position flags indicate whether the cell is at the outer boundary of the table.
	/// When a cell is not at a boundary on a given edge, the table's insideH or insideV
	/// border is used as the table-level fallback instead of the outer border.
	/// </summary>
	/// <param name="table">The parent table element.</param>
	/// <param name="row">The parent row element.</param>
	/// <param name="cell">The target cell element.</param>
	/// <param name="edge">The edge to resolve.</param>
	/// <param name="isFirstRow">True if this cell is in the first row of the table.</param>
	/// <param name="isLastRow">True if this cell is in the last row of the table.</param>
	/// <param name="isFirstColumn">True if this cell is in the first column of the table.</param>
	/// <param name="isLastColumn">True if this cell is in the last column of the table.</param>
	/// <returns>The resolved border definition, or null if no border is defined.</returns>
	/// <exception cref="ArgumentNullException">Any input element is null.</exception>
	internal static TableBorderDefinition? ResolveCellEdge(
		TableElement table,
		TableRowElement row,
		TableCellElement cell,
		BorderEdge edge,
		bool isFirstRow = true,
		bool isLastRow = true,
		bool isFirstColumn = true,
		bool isLastColumn = true)
	{
		ArgumentNullException.ThrowIfNull(table);
		ArgumentNullException.ThrowIfNull(row);
		ArgumentNullException.ThrowIfNull(cell);

		var fromCell = GetBorder(cell.Borders, edge);
		if (fromCell.HasValue)
		{
			return fromCell.Value;
		}

		var fromRow = GetBorder(row.Borders, edge);
		if (fromRow.HasValue)
		{
			return fromRow.Value;
		}

		return GetTableBorder(table.Borders, edge, isFirstRow, isLastRow, isFirstColumn, isLastColumn);
	}

	private static TableBorderDefinition? GetBorder(TableBorderSet borders, BorderEdge edge)
		=> edge switch
		{
			BorderEdge.Top => borders.Top,
			BorderEdge.Bottom => borders.Bottom,
			BorderEdge.Left => borders.Left,
			BorderEdge.Right => borders.Right,
			_ => null,
		};

	private static TableBorderDefinition? GetTableBorder(
		TableBorderSet borders,
		BorderEdge edge,
		bool isFirstRow,
		bool isLastRow,
		bool isFirstColumn,
		bool isLastColumn)
		=> edge switch
		{
			BorderEdge.Top when !isFirstRow => borders.InsideHorizontal,
			BorderEdge.Bottom when !isLastRow => borders.InsideHorizontal,
			BorderEdge.Left when !isFirstColumn => borders.InsideVertical,
			BorderEdge.Right when !isLastColumn => borders.InsideVertical,
			BorderEdge.Top => borders.Top,
			BorderEdge.Bottom => borders.Bottom,
			BorderEdge.Left => borders.Left,
			BorderEdge.Right => borders.Right,
			_ => null,
		};
}
