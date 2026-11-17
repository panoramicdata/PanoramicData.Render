namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class TableStyleResolver
{
	private static IReadOnlyList<TableStyleOverrideValues>? GetBand2FallbackConditionals(IReadOnlyList<TableStyleOverrideValues> conditionals)
	{
		var changed = false;
		var fallback = new List<TableStyleOverrideValues>(conditionals.Count);
		for (var i = 0; i < conditionals.Count; i++)
		{
			var conditional = conditionals[i];
			if (conditional == TableStyleOverrideValues.Band2Horizontal)
			{
				conditional = TableStyleOverrideValues.Band1Horizontal;
			}
			else if (conditional == TableStyleOverrideValues.Band2Vertical)
			{
				conditional = TableStyleOverrideValues.Band1Vertical;
			}

			changed |= conditional != conditionals[i];
			fallback.Add(conditional);
		}

		if (!changed)
		{
			return null;
		}

		return fallback;
	}

	internal static IReadOnlyList<TableStyleOverrideValues> GetCellConditionals(
		TableLookOptions look,
		int rowIndex,
		int columnIndex,
		int rowSpan,
		int columnSpan,
		int rowCount,
		int columnCount)
	{
		var conditionals = new List<TableStyleOverrideValues>();
		AddBandedRowConditional(conditionals, look, rowIndex, rowCount);
		AddBandedColumnConditional(conditionals, look, columnIndex, columnCount);

		AddRowEdgeConditionals(conditionals, look, rowIndex, rowSpan, rowCount);
		AddColumnEdgeConditionals(conditionals, look, columnIndex, columnSpan, columnCount);

		return conditionals;
	}

	private static void AddRowEdgeConditionals(List<TableStyleOverrideValues> conditionals, TableLookOptions look, int rowIndex, int rowSpan, int rowCount)
	{
		if (look.ApplyFirstRow && rowIndex == 0)
		{
			conditionals.Add(TableStyleOverrideValues.FirstRow);
		}

		if (look.ApplyLastRow && rowIndex + rowSpan >= rowCount)
		{
			conditionals.Add(TableStyleOverrideValues.LastRow);
		}
	}

	private static void AddColumnEdgeConditionals(List<TableStyleOverrideValues> conditionals, TableLookOptions look, int columnIndex, int columnSpan, int columnCount)
	{
		if (look.ApplyFirstColumn && columnIndex == 0)
		{
			conditionals.Add(TableStyleOverrideValues.FirstColumn);
		}

		if (look.ApplyLastColumn && columnIndex + columnSpan >= columnCount)
		{
			conditionals.Add(TableStyleOverrideValues.LastColumn);
		}
	}
	private static void AddBandedRowConditional(List<TableStyleOverrideValues> conditionals, TableLookOptions look, int rowIndex, int rowCount)
	{
		if (!look.ApplyBandedRows)
		{
			return;
		}

		var bodyStart = look.ApplyFirstRow ? 1 : 0;
		var bodyEndExclusive = look.ApplyLastRow ? rowCount - 1 : rowCount;
		if (rowIndex >= bodyStart && rowIndex < bodyEndExclusive)
		{
			conditionals.Add((rowIndex - bodyStart) % 2 == 0
				? TableStyleOverrideValues.Band1Horizontal
				: TableStyleOverrideValues.Band2Horizontal);
		}
	}
}
