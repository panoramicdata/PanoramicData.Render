namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class TableLayoutEngine
{
	private static float? ExplicitCellWidth(TableCellElement cell, float availableWidthTwips)
	{
		if (cell.Width.Value <= 0f)
		{
			return null;
		}

		return cell.Width.Type switch
		{
			TableWidthUnit.Dxa => cell.Width.Value,

			// Value is in fiftieths of a percent
			TableWidthUnit.Pct => availableWidthTwips * cell.Width.Value / 5000f,
			_ => null,
		};
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

		if (!fixedWidths.Any(fw => fw.HasValue))
		{
			return DistributeColumnWidths(measurements, availableWidthTwips);
		}

		// Separate fixed and auto columns
		var widths = new float[measurements.Count];
		var autoMeasurements = new List<(int Index, ColumnMeasurement Measurement)>();
		var fixedTotal = AssignFixedWidths(measurements, fixedWidths, widths, autoMeasurements);
		if (autoMeasurements.Count == 0)
		{
			return widths;
		}

		// Distribute remaining space among auto columns
		var remainingWidth = Math.Max(0f, availableWidthTwips - fixedTotal);
		var autoWidths = DistributeColumnWidths(autoMeasurements.Select(a => a.Measurement).ToArray(), remainingWidth);
		for (var i = 0; i < autoMeasurements.Count; i++)
		{
			widths[autoMeasurements[i].Index] = autoWidths[i];
		}

		return widths;
	}

	// Copies fixed widths into the result, collects the auto columns, and returns the fixed total.
	private static float AssignFixedWidths(
		IReadOnlyList<ColumnMeasurement> measurements,
		float?[] fixedWidths,
		float[] widths,
		List<(int Index, ColumnMeasurement Measurement)> autoMeasurements)
	{
		var fixedTotal = 0f;
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

		return fixedTotal;
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

				if (cell.VerticalMerge != VerticalMergeState.Continue)
				{
					MeasureCell(cell, colIndex, spacingInset, preferredWidths, minimumWidths);
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

	private static void MeasureCell(TableCellElement cell, int colIndex, float spacingInset, float[] preferredWidths, float[] minimumWidths)
	{
		var preferredWidth = EstimateCellPreferredWidth(cell) + spacingInset;
		var minimumWidth = EstimateCellMinimumWidth(cell) + spacingInset;

		if (cell.GridSpan == 1)
		{
			RaiseTo(preferredWidths, colIndex, preferredWidth);
			RaiseTo(minimumWidths, colIndex, minimumWidth);
			return;
		}

		// For spanned cells, distribute evenly across spanned columns
		var spanEnd = Math.Min(colIndex + cell.GridSpan, preferredWidths.Length);
		var spanCount = spanEnd - colIndex;
		for (var s = colIndex; s < spanEnd; s++)
		{
			RaiseTo(preferredWidths, s, preferredWidth / spanCount);
			RaiseTo(minimumWidths, s, minimumWidth / spanCount);
		}
	}

	private static void RaiseTo(float[] widths, int index, float value)
	{
		if (value > widths[index])
		{
			widths[index] = value;
		}
	}
}
