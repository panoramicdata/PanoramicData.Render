namespace PanoramicData.Render;

/// <summary>
/// Computes table layout geometry for fixed-width tables.
/// Column widths are taken directly from the <c>w:tblGrid</c> grid column definitions.
/// </summary>
internal static class TableLayoutEngine
{
	/// <summary>
	/// The default row height estimate in twips when no explicit height is specified
	/// and cell content is not yet measured. Corresponds to approximately 12pt single-spaced text.
	/// </summary>
	internal const float DefaultRowHeightTwips = 240f;

	/// <summary>
	/// Computes the fixed-width layout of a table.
	/// </summary>
	/// <param name="table">The parsed table element.</param>
	/// <param name="availableWidthTwips">The available width for the table in twips (page content width minus indentation).</param>
	/// <returns>A <see cref="TableLayoutResult"/> with computed column offsets, widths, and row heights.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="table"/> is <see langword="null"/>.</exception>
	internal static TableLayoutResult Layout(TableElement table, float availableWidthTwips)
	{
		ArgumentNullException.ThrowIfNull(table);

		var columnWidths = ComputeFixedColumnWidths(table, availableWidthTwips);
		var columnOffsets = ComputeColumnOffsets(columnWidths);
		var tableWidth = columnWidths.Count > 0 ? columnOffsets[^1] + columnWidths[^1] : 0f;
		var tableXOffset = ComputeTableXOffset(table, tableWidth, availableWidthTwips);
		var rowHeights = ComputeRowHeights(table);
		var totalHeight = 0f;
		foreach (var rh in rowHeights)
		{
			totalHeight += rh;
		}

		return new TableLayoutResult
		{
			TableXOffset = tableXOffset,
			TableWidthTwips = tableWidth,
			ColumnOffsets = columnOffsets,
			ColumnWidths = columnWidths,
			RowHeights = rowHeights,
			TotalHeightTwips = totalHeight,
			Table = table,
		};
	}

	/// <summary>
	/// Computes column widths for a fixed-width table layout.
	/// Uses the grid column widths directly from <c>w:tblGrid</c>.
	/// </summary>
	internal static IReadOnlyList<float> ComputeFixedColumnWidths(TableElement table, float availableWidthTwips)
	{
		if (table.GridColumns.Count == 0)
		{
			return [];
		}

		var widths = new float[table.GridColumns.Count];
		var hasAnyExplicit = false;

		for (var i = 0; i < table.GridColumns.Count; i++)
		{
			var w = table.GridColumns[i].WidthTwips;
			if (w > 0f)
			{
				widths[i] = w;
				hasAnyExplicit = true;
			}
		}

		if (!hasAnyExplicit)
		{
			// No explicit widths: distribute available width equally
			var equalWidth = availableWidthTwips / table.GridColumns.Count;
			for (var i = 0; i < widths.Length; i++)
			{
				widths[i] = equalWidth;
			}
		}
		else
		{
			// Fill any zero-width columns with equal share of remaining space
			var totalExplicit = 0f;
			var zeroCount = 0;
			foreach (var w in widths)
			{
				if (w > 0f)
				{
					totalExplicit += w;
				}
				else
				{
					zeroCount++;
				}
			}

			if (zeroCount > 0)
			{
				var remaining = Math.Max(0f, availableWidthTwips - totalExplicit);
				var share = remaining / zeroCount;
				for (var i = 0; i < widths.Length; i++)
				{
					if (widths[i] == 0f)
					{
						widths[i] = share;
					}
				}
			}
		}

		return widths;
	}

	/// <summary>
	/// Computes the cumulative x-offset of each column (left edge position relative to the table's left edge).
	/// </summary>
	internal static IReadOnlyList<float> ComputeColumnOffsets(IReadOnlyList<float> columnWidths)
	{
		if (columnWidths.Count == 0)
		{
			return [];
		}

		var offsets = new float[columnWidths.Count];
		offsets[0] = 0f;
		for (var i = 1; i < columnWidths.Count; i++)
		{
			offsets[i] = offsets[i - 1] + columnWidths[i - 1];
		}

		return offsets;
	}

	/// <summary>
	/// Computes the table's x-offset from the left content edge based on alignment and indentation.
	/// </summary>
	internal static float ComputeTableXOffset(TableElement table, float tableWidth, float availableWidthTwips)
	{
		return table.Alignment switch
		{
			TableAlignment.Center => Math.Max(0f, (availableWidthTwips - tableWidth) / 2f),
			TableAlignment.Right => Math.Max(0f, availableWidthTwips - tableWidth),
			_ => table.IndentationTwips,
		};
	}

	/// <summary>
	/// Computes the height of each row. Uses the explicit row height when specified;
	/// otherwise uses a default estimate.
	/// </summary>
	internal static IReadOnlyList<float> ComputeRowHeights(TableElement table)
	{
		var heights = new float[table.Rows.Count];
		for (var i = 0; i < table.Rows.Count; i++)
		{
			var row = table.Rows[i];
			heights[i] = row.HeightTwips > 0f
				? row.HeightTwips
				: DefaultRowHeightTwips;
		}

		return heights;
	}
}
