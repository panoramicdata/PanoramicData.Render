namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class TableParser
{
	private static float ParseRowHeight(TableRowProperties? trPr)
	{
		var height = trPr?.GetFirstChild<TableRowHeight>();
		if (height?.Val?.Value is { } v)
		{
			return v;
		}

		return 0f;
	}

	internal static RowHeightRule ParseRowHeightRule(TableRowProperties? trPr)
	{
		var height = trPr?.GetFirstChild<TableRowHeight>();
		if (height?.HeightType?.Value is null)
		{
			return RowHeightRule.Auto;
		}

		if (height.HeightType.Value == HeightRuleValues.Exact)
		{
			return RowHeightRule.Exact;
		}

		if (height.HeightType.Value == HeightRuleValues.AtLeast)
		{
			return RowHeightRule.AtLeast;
		}

		return RowHeightRule.Auto;
	}

	private static bool IsOnOffSet(CantSplit? element) =>
		element is not null && (element.Val is null || element.Val == OnOffOnlyValues.On);

	private static bool IsOnOffSet(TableHeader? element) =>
		element is not null && (element.Val is null || element.Val == OnOffOnlyValues.On);

	private static IReadOnlyList<TableCellElement> ParseCells(TableRow row, CellMargins defaultCellMargins)
	{
		var cells = new List<TableCellElement>();
		foreach (var child in row.ChildElements)
		{
			IEnumerable<TableCell> tableCells = child switch
			{
				TableCell tc => [tc],
				SdtCell sdtCell => sdtCell.GetFirstChild<SdtContentCell>()?.Elements<TableCell>() ?? [],
				_ => [],
			};

			cells.AddRange(tableCells.Select(tc => ParseCell(tc, defaultCellMargins)));
		}

		return cells;
	}
}
