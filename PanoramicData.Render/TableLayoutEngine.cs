namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Computes table layout geometry for fixed-width and auto-fit tables.
/// </summary>
internal static partial class TableLayoutEngine
{
	/// <summary>
	/// The default row height estimate in twips when no explicit height is specified
	/// and cell content is not yet measured. Corresponds to approximately 12pt single-spaced text.
	/// </summary>
	internal const float DefaultRowHeightTwips = 240f;

	/// <summary>
	/// The default estimated width per content block in twips (used as a placeholder
	/// until full text measurement is available).
	/// </summary>
	internal const float DefaultBlockWidthTwips = 2400f;

	/// <summary>
	/// The minimum column width in twips. Prevents columns from collapsing to zero.
	/// </summary>
	internal const float MinimumColumnWidthTwips = 360f;

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
		return BuildLayout(table, columnWidths, ComputeRowHeights(table), availableWidthTwips);
	}

	/// <summary>
	/// Computes the auto-fit layout of a table.
	/// First computes final auto-fit column widths, then re-lays out cell content
	/// against those widths to derive row heights.
	/// </summary>
	/// <param name="table">The parsed table element.</param>
	/// <param name="availableWidthTwips">The available width for the table in twips (page content width minus indentation).</param>
	/// <returns>A <see cref="TableLayoutResult"/> with auto-fit column widths and width-aware row heights.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="table"/> is <see langword="null"/>.</exception>
	internal static TableLayoutResult LayoutAutoFit(TableElement table, float availableWidthTwips)
	{
		ArgumentNullException.ThrowIfNull(table);

		var columnWidths = ComputeAutoFitColumnWidths(table, availableWidthTwips);
		return BuildLayout(table, columnWidths, ComputeRowHeights(table, columnWidths), availableWidthTwips);
	}

	private static TableLayoutResult BuildLayout(TableElement table, IReadOnlyList<float> columnWidths, IReadOnlyList<float> rowHeights, float availableWidthTwips)
	{
		var tableWidth = SumColumnWidths(columnWidths);
		var totalHeight = 0f;
		foreach (var rowHeight in rowHeights)
		{
			totalHeight += rowHeight;
		}

		return new TableLayoutResult
		{
			TableXOffset = ComputeTableXOffset(table, tableWidth, availableWidthTwips),
			TableWidthTwips = tableWidth,
			ColumnOffsets = ComputeColumnOffsets(columnWidths, table.IsBiDi),
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

		var widths = table.GridColumns.Select(column => column.WidthTwips > 0f ? column.WidthTwips : 0f).ToArray();
		if (!widths.Any(w => w > 0f))
		{
			// No explicit widths: distribute available width equally
			Array.Fill(widths, availableWidthTwips / widths.Length);
			return widths;
		}

		FillZeroWidthColumns(widths, availableWidthTwips);
		return widths;
	}

	// Fill any zero-width columns with an equal share of the remaining space.
	private static void FillZeroWidthColumns(float[] widths, float availableWidthTwips)
	{
		var totalExplicit = 0f;
		foreach (var w in widths)
		{
			if (w > 0f)
			{
				totalExplicit += w;
			}
		}

		var zeroCount = widths.Count(w => w <= 0f);
		if (zeroCount == 0)
		{
			return;
		}

		var share = Math.Max(0f, availableWidthTwips - totalExplicit) / zeroCount;
		for (var i = 0; i < widths.Length; i++)
		{
			if (Math.Abs(widths[i]) < 0.001f)
			{
				widths[i] = share;
			}
		}
	}

	/// <summary>
	/// Computes column widths for an auto-fit table layout.
	/// Distributes available width proportionally to preferred widths, respecting minimums.
	/// Handles percentage-based and fixed cell widths.
	/// </summary>
	/// <param name="table">The table element.</param>
	/// <param name="availableWidthTwips">The available width for the table in twips.</param>
	/// <returns>The computed column widths.</returns>
	internal static IReadOnlyList<float> ComputeAutoFitColumnWidths(TableElement table, float availableWidthTwips)
	{
		if (table.GridColumns.Count == 0)
		{
			return [];
		}

		// Resolve any percentage-based or explicit fixed column widths from cell definitions
		var colCount = table.GridColumns.Count;
		var fixedWidths = new float?[colCount];
		ResolveExplicitColumnWidths(table, availableWidthTwips, fixedWidths);

		var measurements = MeasureColumnWidths(table);

		// If there are any fixed/percentage columns, apply them and distribute remaining
		return DistributeWithFixedColumns(measurements, fixedWidths, availableWidthTwips);
	}

	/// <summary>
	/// Resolves explicit column widths from cell width specifications.
	/// For percentage-based widths (Pct), converts to absolute twips.
	/// For fixed widths (Dxa), uses the value directly.
	/// </summary>
	internal static void ResolveExplicitColumnWidths(
		TableElement table,
		float availableWidthTwips,
		float?[] fixedWidths)
	{
		foreach (var row in table.Rows)
		{
			var colIndex = 0;
			foreach (var cell in row.Cells)
			{
				if (colIndex >= fixedWidths.Length)
				{
					break;
				}

				if (cell.GridSpan == 1 && cell.VerticalMerge != VerticalMergeState.Continue)
				{
					fixedWidths[colIndex] ??= ExplicitCellWidth(cell, availableWidthTwips);
				}

				colIndex += cell.GridSpan;
			}
		}
	}
}
