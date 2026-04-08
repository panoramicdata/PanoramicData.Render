namespace PanoramicData.Render;

/// <summary>
/// Resolves effective table borders using OOXML precedence:
/// cell-level > row-level > table-level.
/// </summary>
internal static class TableBorderResolver
{
	/// <summary>
	/// Resolves the effective border for a specific cell edge.
	/// </summary>
	/// <param name="table">The parent table element.</param>
	/// <param name="row">The parent row element.</param>
	/// <param name="cell">The target cell element.</param>
	/// <param name="edge">The edge to resolve.</param>
	/// <returns>The resolved border definition, or null if no border is defined.</returns>
	/// <exception cref="ArgumentNullException">Any input element is null.</exception>
	internal static TableBorderDefinition? ResolveCellEdge(
		TableElement table,
		TableRowElement row,
		TableCellElement cell,
		BorderEdge edge)
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

		return GetBorder(table.Borders, edge);
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
}
