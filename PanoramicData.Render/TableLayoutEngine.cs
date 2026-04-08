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
	/// Computes the height of each row. For rows with <see cref="RowHeightRule.Exact"/>,
	/// uses the specified height. For <see cref="RowHeightRule.AtLeast"/> or <see cref="RowHeightRule.Auto"/>,
	/// uses the maximum of the specified height and the tallest cell content in the row.
	/// </summary>
	internal static IReadOnlyList<float> ComputeRowHeights(TableElement table)
	{
		var heights = new float[table.Rows.Count];
		for (var i = 0; i < table.Rows.Count; i++)
		{
			var row = table.Rows[i];
			var specifiedHeight = row.HeightTwips > 0f ? row.HeightTwips : 0f;

			if (row.HeightRule == RowHeightRule.Exact && specifiedHeight > 0f)
			{
				heights[i] = specifiedHeight;
				continue;
			}

			// Measure content height of each cell in this row
			var maxContentHeight = 0f;
			foreach (var cell in row.Cells)
			{
				if (cell.VerticalMerge == VerticalMergeState.Continue)
				{
					continue;
				}

				var contentHeight = MeasureCellContentHeight(cell);
				if (contentHeight > maxContentHeight)
				{
					maxContentHeight = contentHeight;
				}
			}

			var contentBasedHeight = maxContentHeight > 0f ? maxContentHeight : DefaultRowHeightTwips;
			heights[i] = Math.Max(specifiedHeight, contentBasedHeight);
		}

		return heights;
	}

	/// <summary>
	/// Measures the total height of a cell's content by laying out its blocks,
	/// including top and bottom cell margins.
	/// </summary>
	internal static float MeasureCellContentHeight(TableCellElement cell)
	{
		var totalHeight = 0f;
		foreach (var block in cell.Blocks)
		{
			totalHeight += EstimateBlockHeight(block);
		}

		totalHeight += cell.Margins.Top + cell.Margins.Bottom;

		return totalHeight;
	}

	/// <summary>
	/// Lays out the content of a cell into <see cref="LayoutBlock"/> instances.
	/// Returns the blocks and total content height including top and bottom margins.
	/// </summary>
	internal static (IReadOnlyList<LayoutBlock> Blocks, float TotalHeight) LayoutCellContent(TableCellElement cell)
	{
		ArgumentNullException.ThrowIfNull(cell);

		if (cell.Blocks.Count == 0)
		{
			var marginHeight = cell.Margins.Top + cell.Margins.Bottom;
			return ([], marginHeight);
		}

		var layoutBlocks = new List<LayoutBlock>();
		var totalHeight = cell.Margins.Top;

		foreach (var block in cell.Blocks)
		{
			var height = EstimateBlockHeight(block);
			layoutBlocks.Add(new LayoutBlock(block, height));
			totalHeight += height;
		}

		totalHeight += cell.Margins.Bottom;

		return (layoutBlocks, totalHeight);
	}

	/// <summary>
	/// Computes the effective content width of a cell, accounting for left and right margins.
	/// </summary>
	/// <param name="cellWidth">The total cell width in twips.</param>
	/// <param name="margins">The cell margins.</param>
	/// <returns>The content area width in twips (never less than zero).</returns>
	internal static float ComputeContentWidth(float cellWidth, CellMargins margins)
		=> Math.Max(0f, cellWidth - margins.Left - margins.Right);

	/// <summary>
	/// Computes the vertical offset for cell content based on vertical alignment.
	/// The offset is relative to the cell's top edge.
	/// </summary>
	/// <param name="cellHeight">The total cell height in twips.</param>
	/// <param name="contentHeight">The total content height in twips (including margins).</param>
	/// <param name="alignment">The vertical alignment of the cell.</param>
	/// <returns>The vertical offset for positioning content within the cell.</returns>
	internal static float ComputeVerticalContentOffset(float cellHeight, float contentHeight, CellVerticalAlignment alignment)
		=> alignment switch
		{
			CellVerticalAlignment.Center => Math.Max(0f, (cellHeight - contentHeight) / 2f),
			CellVerticalAlignment.Bottom => Math.Max(0f, cellHeight - contentHeight),
			_ => 0f, // Top (default)
		};

	private static float EstimateBlockHeight(DocumentBlock block) => block switch
	{
		ParagraphBlock => DefaultRowHeightTwips,
		_ => DefaultRowHeightTwips,
	};

	/// <summary>
	/// Computes the <see cref="CellPosition"/> for every owner cell in the resolved grid.
	/// Each owner cell appears exactly once; continuation cells from merges are not included.
	/// </summary>
	/// <param name="layout">The computed table layout.</param>
	/// <returns>A list of <see cref="CellPosition"/> for each unique cell.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="layout"/> is <see langword="null"/>.</exception>
	internal static IReadOnlyList<CellPosition> ComputeCellPositions(TableLayoutResult layout)
	{
		ArgumentNullException.ThrowIfNull(layout);

		var table = layout.Table;
		if (table.Rows.Count == 0 || table.GridColumns.Count == 0)
		{
			return [];
		}

		var grid = TableGridResolver.Resolve(table);
		var rowCount = grid.GetLength(0);
		var colCount = grid.GetLength(1);

		// Compute row y-offsets (cumulative)
		var rowOffsets = new float[rowCount];
		if (rowCount > 0)
		{
			rowOffsets[0] = 0f;
			for (var r = 1; r < rowCount; r++)
			{
				rowOffsets[r] = rowOffsets[r - 1] + layout.RowHeights[r - 1];
			}
		}

		// Track visited owner cells to emit each once
		var visited = new HashSet<(int Row, int Col)>();
		var positions = new List<CellPosition>();

		for (var r = 0; r < rowCount; r++)
		{
			for (var c = 0; c < colCount; c++)
			{
				if (grid[r, c] is not { } resolved)
				{
					continue;
				}

				var ownerKey = (resolved.OwnerRowIndex, resolved.OwnerColumnIndex);
				if (!visited.Add(ownerKey))
				{
					continue;
				}

				// Compute width: sum of all columns this cell spans
				var cellWidth = 0f;
				for (var s = 0; s < resolved.Cell.GridSpan && resolved.OwnerColumnIndex + s < colCount; s++)
				{
					cellWidth += layout.ColumnWidths[resolved.OwnerColumnIndex + s];
				}

				// Compute height: sum of all rows this cell spans (via vertical merge)
				var spanRows = CountVerticalSpan(grid, resolved.OwnerRowIndex, resolved.OwnerColumnIndex, rowCount);
				var cellHeight = 0f;
				for (var sr = 0; sr < spanRows; sr++)
				{
					cellHeight += layout.RowHeights[resolved.OwnerRowIndex + sr];
				}

				positions.Add(new CellPosition(
					resolved.OwnerRowIndex,
					resolved.OwnerColumnIndex,
					layout.ColumnOffsets[resolved.OwnerColumnIndex],
					rowOffsets[resolved.OwnerRowIndex],
					cellWidth,
					cellHeight,
					resolved.Cell));
			}
		}

		return positions;
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
}
