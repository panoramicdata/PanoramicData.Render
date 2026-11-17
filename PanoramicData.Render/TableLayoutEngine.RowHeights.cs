namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class TableLayoutEngine
{
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

				var contentHeight = MeasureCellContentHeight(cell) + (table.BorderSpacingTwips * 2f);
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
	/// Computes row heights using width-aware content measurement based on the provided
	/// final column widths (used by auto-fit layout).
	/// </summary>
	/// <param name="table">The table whose rows are measured.</param>
	/// <param name="columnWidths">Final computed column widths in twips.</param>
	/// <returns>Computed row heights in twips.</returns>
	internal static IReadOnlyList<float> ComputeRowHeights(TableElement table, IReadOnlyList<float> columnWidths)
	{
		if (columnWidths.Count == 0)
		{
			return ComputeRowHeights(table);
		}

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

			var maxContentHeight = MeasureRowContentHeight(table, row, columnWidths);
			var contentBasedHeight = maxContentHeight > 0f ? maxContentHeight : DefaultRowHeightTwips;
			heights[i] = Math.Max(specifiedHeight, contentBasedHeight);
		}

		return heights;
	}

	private static float MeasureRowContentHeight(TableElement table, TableRowElement row, IReadOnlyList<float> columnWidths)
	{
		var maxContentHeight = 0f;
		var colIndex = 0;
		foreach (var cell in row.Cells)
		{
			if (colIndex >= columnWidths.Count)
			{
				break;
			}

			var span = Math.Max(1, cell.GridSpan);
			if (cell.VerticalMerge != VerticalMergeState.Continue)
			{
				var cellWidth = SumSpannedWidth(columnWidths, colIndex, span);
				var adjustedCellWidth = Math.Max(0f, cellWidth - (table.BorderSpacingTwips * 2f));
				var contentHeight = MeasureCellContentHeight(cell, adjustedCellWidth) + (table.BorderSpacingTwips * 2f);
				maxContentHeight = Math.Max(maxContentHeight, contentHeight);
			}

			colIndex += span;
		}

		return maxContentHeight;
	}

	private static float SumSpannedWidth(IReadOnlyList<float> columnWidths, int colIndex, int span)
	{
		var spanEnd = Math.Min(colIndex + span, columnWidths.Count);
		var cellWidth = 0f;
		for (var s = colIndex; s < spanEnd; s++)
		{
			cellWidth += columnWidths[s];
		}

		return cellWidth;
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
}
