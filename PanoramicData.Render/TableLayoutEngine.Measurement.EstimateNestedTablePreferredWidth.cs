namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class TableLayoutEngine
{
	private static float EstimateNestedTablePreferredWidth(TablePlaceholderBlock tableBlock)
	{
		var nestedTable = TableParser.Parse(tableBlock.TableElement);
		var measurements = MeasureColumnWidths(nestedTable);
		if (measurements.Count == 0)
		{
			return DefaultBlockWidthTwips;
		}

		var preferred = 0f;
		foreach (var measurement in measurements)
		{
			preferred += measurement.PreferredWidthTwips;
		}

		return preferred > 0f ? preferred : DefaultBlockWidthTwips;
	}

	private static float EstimateNestedTableMinimumWidth(TablePlaceholderBlock tableBlock)
	{
		var nestedTable = TableParser.Parse(tableBlock.TableElement);
		var measurements = MeasureColumnWidths(nestedTable);
		if (measurements.Count == 0)
		{
			return MinimumColumnWidthTwips;
		}

		var minimum = 0f;
		foreach (var measurement in measurements)
		{
			minimum += measurement.MinimumWidthTwips;
		}

		return minimum > 0f ? minimum : MinimumColumnWidthTwips;
	}

	/// <summary>
	/// Computes the cumulative x-offset of each column (left edge position relative to the table's left edge).
	/// When <paramref name="isBiDi"/> is <see langword="true"/>, columns are mirrored right-to-left.
	/// </summary>
	internal static IReadOnlyList<float> ComputeColumnOffsets(IReadOnlyList<float> columnWidths, bool isBiDi = false)
	{
		if (columnWidths.Count == 0)
		{
			return [];
		}

		// Always compute LTR offsets first
		var offsets = new float[columnWidths.Count];
		offsets[0] = 0f;
		for (var i = 1; i < columnWidths.Count; i++)
		{
			offsets[i] = offsets[i - 1] + columnWidths[i - 1];
		}

		if (isBiDi)
		{
			// Mirror: column i's left edge is at totalWidth - ltrOffset[i] - columnWidth[i]
			var totalWidth = offsets[^1] + columnWidths[^1];
			for (var i = 0; i < offsets.Length; i++)
			{
				offsets[i] = totalWidth - offsets[i] - columnWidths[i];
			}
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

	private static float SumColumnWidths(IReadOnlyList<float> columnWidths)
	{
		var total = 0f;
		for (var i = 0; i < columnWidths.Count; i++)
		{
			total += columnWidths[i];
		}

		return total;
	}
}
