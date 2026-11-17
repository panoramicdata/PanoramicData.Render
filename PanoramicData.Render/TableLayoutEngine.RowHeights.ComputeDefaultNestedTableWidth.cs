namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class TableLayoutEngine
{
	private static float ComputeDefaultNestedTableWidth(TableElement table)
	{
		if (table.GridColumns.Count == 0)
		{
			return DefaultBlockWidthTwips;
		}

		var sum = 0f;
		foreach (var column in table.GridColumns)
		{
			sum += Math.Max(0f, column.WidthTwips);
		}

		return sum > 0f ? sum : DefaultBlockWidthTwips;
	}
}
