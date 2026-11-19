namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class TableLayoutEngine
{
	/// <summary>
	/// Computes resolved table border line segments for rendering.
	/// Segments include per-edge style, width, color, and dash pattern.
	/// </summary>
	/// <param name="layout">The computed table layout.</param>
	/// <returns>Border line segments in table-relative coordinates.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="layout"/> is <see langword="null"/>.</exception>
	internal static IReadOnlyList<TableBorderSegment> ComputeBorderSegments(TableLayoutResult layout)
	{
		ArgumentNullException.ThrowIfNull(layout);

		if (layout.Table.Rows.Count == 0 || layout.Table.GridColumns.Count == 0)
		{
			return [];
		}

		var positions = ComputeCellPositions(layout);
		if (positions.Count == 0)
		{
			return [];
		}

		var table = layout.Table;
		var grid = TableGridResolver.Resolve(table);
		var rowCount = grid.GetLength(0);
		var colCount = grid.GetLength(1);
		var segments = new List<TableBorderSegment>();

		foreach (var (position, rowSpan, columnSpan) in ComputeCellSpans(positions, grid, colCount))
		{
			var isFirstRow = position.RowIndex == 0;
			var isLastRow = position.RowIndex + rowSpan >= rowCount;
			var isFirstColumn = position.ColumnIndex == 0;
			var isLastColumn = position.ColumnIndex + columnSpan >= colCount;

			if (isFirstRow)
			{
				AddBorderSegment(segments, table, position, BorderEdge.Top, isFirstRow, isLastRow, isFirstColumn, isLastColumn);
			}

			if (isFirstColumn)
			{
				AddBorderSegment(segments, table, position, BorderEdge.Left, isFirstRow, isLastRow, isFirstColumn, isLastColumn);
			}

			AddBorderSegment(segments, table, position, BorderEdge.Bottom, isFirstRow, isLastRow, isFirstColumn, isLastColumn);
			AddBorderSegment(segments, table, position, BorderEdge.Right, isFirstRow, isLastRow, isFirstColumn, isLastColumn);
		}

		return segments;
	}

	/// <summary>
	/// Counts how many consecutive rows the cell at (<paramref name="ownerRow"/>, <paramref name="ownerCol"/>)
	/// spans by checking how many rows in the resolved grid reference the same owner.
	/// </summary>
	private static int CountVerticalSpan(ResolvedGridCell?[,] grid, int ownerRow, int ownerCol, int rowCount)
	{
		var span = 1;
		for (var r = ownerRow + 1; r < rowCount; r++)
		{
			if (grid[r, ownerCol] is { } cell
				&& cell.OwnerRowIndex == ownerRow
				&& cell.OwnerColumnIndex == ownerCol)
			{
				span++;
			}
			else
			{
				break;
			}
		}

		return span;
	}

	private static void AddBorderSegment(
		ICollection<TableBorderSegment> output,
		TableElement table,
		CellPosition position,
		BorderEdge edge,
		bool isFirstRow,
		bool isLastRow,
		bool isFirstColumn,
		bool isLastColumn)
	{
		var resolved = TableBorderResolver.ResolveCellEdge(
			table,
			table.Rows[position.RowIndex],
			position.Cell,
			edge,
			isFirstRow,
			isLastRow,
			isFirstColumn,
			isLastColumn);

		if (!resolved.HasValue || !resolved.Value.IsVisible)
		{
			return;
		}

		var border = resolved.Value;
		var strokeWidth = border.GetWidthTwips() > 0f ? border.GetWidthTwips() : 2.5f;
		var color = ResolveBorderColor(border.Color);
		var dashPattern = ResolveDashPattern(border.Style);

		output.Add(edge switch
		{
			BorderEdge.Top => new TableBorderSegment(position.X, position.Y, position.X + position.Width, position.Y, strokeWidth, color, border.Style, dashPattern),
			BorderEdge.Bottom => new TableBorderSegment(position.X, position.Y + position.Height, position.X + position.Width, position.Y + position.Height, strokeWidth, color, border.Style, dashPattern),
			BorderEdge.Left => new TableBorderSegment(position.X, position.Y, position.X, position.Y + position.Height, strokeWidth, color, border.Style, dashPattern),
			BorderEdge.Right => new TableBorderSegment(position.X + position.Width, position.Y, position.X + position.Width, position.Y + position.Height, strokeWidth, color, border.Style, dashPattern),
			_ => default,
		});
	}

	private static string ResolveBorderColor(string? color)
	{
		if (string.IsNullOrWhiteSpace(color)
			|| string.Equals(color, "auto", StringComparison.OrdinalIgnoreCase))
		{
			return "000000";
		}

		return color.ToUpperInvariant();
	}

	private static IReadOnlyList<float>? ResolveDashPattern(BorderStyle style)
		=> style switch
		{
			BorderStyle.Dotted => [2.5f, 2.5f],
			BorderStyle.Dashed => [7.5f, 5f],
			BorderStyle.DotDash => [7.5f, 5f, 2.5f, 5f],
			BorderStyle.DotDotDash => [7.5f, 5f, 2.5f, 5f, 2.5f, 5f],
			_ => null,
		};
}
