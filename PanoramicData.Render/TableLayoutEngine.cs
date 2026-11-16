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
		var columnOffsets = ComputeColumnOffsets(columnWidths, table.IsBiDi);
		var tableWidth = SumColumnWidths(columnWidths);
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
		var columnOffsets = ComputeColumnOffsets(columnWidths, table.IsBiDi);
		var tableWidth = SumColumnWidths(columnWidths);
		var tableXOffset = ComputeTableXOffset(table, tableWidth, availableWidthTwips);
		var rowHeights = ComputeRowHeights(table, columnWidths);
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
					if (Math.Abs(widths[i]) < 0.001f)
					{
						widths[i] = share;
					}
				}
			}
		}

		return widths;
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
					if (cell.Width.Type == TableWidthUnit.Dxa && cell.Width.Value > 0f)
					{
						fixedWidths[colIndex] ??= cell.Width.Value;
					}
					else if (cell.Width.Type == TableWidthUnit.Pct && cell.Width.Value > 0f)
					{
						// Value is in fiftieths of a percent
						var pctWidth = availableWidthTwips * cell.Width.Value / 5000f;
						fixedWidths[colIndex] ??= pctWidth;
					}
				}

				colIndex += cell.GridSpan;
			}
		}
	}

	/// <summary>
	/// Distributes available width across columns, respecting any fixed column widths.
	/// Fixed columns keep their explicit width; auto columns share the remaining space
	/// proportionally to their preferred widths.
	/// </summary>
	internal static IReadOnlyList<float> DistributeWithFixedColumns(
		IReadOnlyList<ColumnMeasurement> measurements,
		float?[] fixedWidths,
		float availableWidthTwips)
	{
		if (measurements.Count == 0)
		{
			return [];
		}

		var hasAnyFixed = false;
		foreach (var fw in fixedWidths)
		{
			if (fw.HasValue)
			{
				hasAnyFixed = true;
				break;
			}
		}

		if (!hasAnyFixed)
		{
			return DistributeColumnWidths(measurements, availableWidthTwips);
		}

		// Separate fixed and auto columns
		var widths = new float[measurements.Count];
		var fixedTotal = 0f;
		var autoMeasurements = new List<(int Index, ColumnMeasurement Measurement)>();

		for (var i = 0; i < measurements.Count; i++)
		{
			if (fixedWidths[i].HasValue)
			{
				widths[i] = fixedWidths[i]!.Value;
				fixedTotal += widths[i];
			}
			else
			{
				autoMeasurements.Add((i, measurements[i]));
			}
		}

		if (autoMeasurements.Count == 0)
		{
			return widths;
		}

		// Distribute remaining space among auto columns
		var remainingWidth = Math.Max(0f, availableWidthTwips - fixedTotal);
		var autoColumnMeasurements = new ColumnMeasurement[autoMeasurements.Count];
		for (var i = 0; i < autoMeasurements.Count; i++)
		{
			autoColumnMeasurements[i] = autoMeasurements[i].Measurement;
		}

		var autoWidths = DistributeColumnWidths(autoColumnMeasurements, remainingWidth);

		for (var i = 0; i < autoMeasurements.Count; i++)
		{
			widths[autoMeasurements[i].Index] = autoWidths[i];
		}

		return widths;
	}

	/// <summary>
	/// Measures the preferred and minimum widths for each column in the table
	/// by examining cell content across all rows.
	/// </summary>
	internal static IReadOnlyList<ColumnMeasurement> MeasureColumnWidths(TableElement table)
	{
		var colCount = table.GridColumns.Count;
		var preferredWidths = new float[colCount];
		var minimumWidths = new float[colCount];
		var spacingInset = table.BorderSpacingTwips * 2f;

		foreach (var row in table.Rows)
		{
			var colIndex = 0;
			foreach (var cell in row.Cells)
			{
				if (colIndex >= colCount)
				{
					break;
				}

				if (cell.VerticalMerge == VerticalMergeState.Continue)
				{
					colIndex += cell.GridSpan;
					continue;
				}

				var preferredWidth = EstimateCellPreferredWidth(cell) + spacingInset;
				var minimumWidth = EstimateCellMinimumWidth(cell) + spacingInset;

				if (cell.GridSpan == 1)
				{
					if (preferredWidth > preferredWidths[colIndex])
					{
						preferredWidths[colIndex] = preferredWidth;
					}

					if (minimumWidth > minimumWidths[colIndex])
					{
						minimumWidths[colIndex] = minimumWidth;
					}
				}
				else
				{
					// For spanned cells, distribute evenly across spanned columns
					var spanEnd = Math.Min(colIndex + cell.GridSpan, colCount);
					var spanCount = spanEnd - colIndex;
					var perColPreferred = preferredWidth / spanCount;
					var perColMinimum = minimumWidth / spanCount;

					for (var s = colIndex; s < spanEnd; s++)
					{
						if (perColPreferred > preferredWidths[s])
						{
							preferredWidths[s] = perColPreferred;
						}

						if (perColMinimum > minimumWidths[s])
						{
							minimumWidths[s] = perColMinimum;
						}
					}
				}

				colIndex += cell.GridSpan;
			}
		}

		var result = new ColumnMeasurement[colCount];
		for (var i = 0; i < colCount; i++)
		{
			result[i] = new ColumnMeasurement(
				Math.Max(preferredWidths[i], MinimumColumnWidthTwips),
				Math.Max(minimumWidths[i], MinimumColumnWidthTwips));
		}

		return result;
	}

	/// <summary>
	/// Distributes available width across columns proportionally to preferred widths,
	/// respecting minimum widths.
	/// </summary>
	internal static IReadOnlyList<float> DistributeColumnWidths(
		IReadOnlyList<ColumnMeasurement> measurements,
		float availableWidthTwips)
	{
		if (measurements.Count == 0)
		{
			return [];
		}

		var widths = new float[measurements.Count];

		// Start with minimum widths
		var totalMinimum = 0f;
		var totalPreferred = 0f;
		for (var i = 0; i < measurements.Count; i++)
		{
			widths[i] = measurements[i].MinimumWidthTwips;
			totalMinimum += measurements[i].MinimumWidthTwips;
			totalPreferred += measurements[i].PreferredWidthTwips;
		}

		// If minimums exceed available width, use minimums as-is
		if (totalMinimum >= availableWidthTwips)
		{
			return widths;
		}

		// If preferred fits within available, use preferred widths
		if (totalPreferred <= availableWidthTwips)
		{
			// Distribute the remaining space proportionally to preferred widths
			var remaining = availableWidthTwips - totalPreferred;
			for (var i = 0; i < measurements.Count; i++)
			{
				widths[i] = measurements[i].PreferredWidthTwips + (remaining * measurements[i].PreferredWidthTwips / totalPreferred);
			}

			return widths;
		}

		// Preferred exceeds available: distribute proportionally between minimum and preferred
		var excessOverMinimum = availableWidthTwips - totalMinimum;
		var totalStretch = totalPreferred - totalMinimum;

		for (var i = 0; i < measurements.Count; i++)
		{
			var stretch = measurements[i].PreferredWidthTwips - measurements[i].MinimumWidthTwips;
			widths[i] = measurements[i].MinimumWidthTwips + (excessOverMinimum * stretch / totalStretch);
		}

		return widths;
	}

	/// <summary>
	/// The average estimated character width in twips for width estimation.
	/// Based on approximately 7pt per character in 12pt text (typical proportional font).
	/// </summary>
	internal const float AverageCharWidthTwips = 140f;

	/// <summary>
	/// The average estimated character width in twips for height (line-wrapping) estimation.
	/// More conservative than <see cref="AverageCharWidthTwips"/> to avoid overestimating
	/// row heights that push content to extra pages.
	/// </summary>
	private const float AverageCharWidthForHeightTwips = 100f;
}
