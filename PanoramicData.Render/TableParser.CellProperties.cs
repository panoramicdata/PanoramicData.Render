namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class TableParser
{
	private static int ParseGridSpan(TableCellProperties? tcPr)
	{
		if (tcPr?.GridSpan?.Val?.Value is { } gs && gs > 0)
		{
			return gs;
		}

		return 1;
	}

	private static VerticalMergeState ParseVerticalMerge(TableCellProperties? tcPr)
	{
		var vMerge = tcPr?.VerticalMerge;
		if (vMerge is null)
		{
			return VerticalMergeState.None;
		}

		// w:vMerge val="restart" → Restart; w:vMerge (no val or val="continue") → Continue
		if (vMerge.Val?.Value == MergedCellValues.Restart)
		{
			return VerticalMergeState.Restart;
		}

		return VerticalMergeState.Continue;
	}

	internal static CellVerticalAlignment ParseCellVerticalAlignment(TableCellVerticalAlignment? vAlign)
	{
		if (vAlign?.Val?.Value is null)
		{
			return CellVerticalAlignment.Top;
		}

		if (vAlign.Val.Value == TableVerticalAlignmentValues.Center)
		{
			return CellVerticalAlignment.Center;
		}

		if (vAlign.Val.Value == TableVerticalAlignmentValues.Bottom)
		{
			return CellVerticalAlignment.Bottom;
		}

		return CellVerticalAlignment.Top;
	}
}
