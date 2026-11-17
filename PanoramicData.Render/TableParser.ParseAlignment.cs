namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class TableParser
{
	internal static TableAlignment ParseAlignment(TableJustification? justification)
	{
		if (justification?.Val?.Value is null)
		{
			return TableAlignment.Left;
		}

		if (justification.Val.Value == TableRowAlignmentValues.Center)
		{
			return TableAlignment.Center;
		}

		if (justification.Val.Value == TableRowAlignmentValues.Right)
		{
			return TableAlignment.Right;
		}

		return TableAlignment.Left;
	}

	internal static float ParseIndentation(TableIndentation? indentation)
	{
		if (indentation?.Width is null)
		{
			return 0f;
		}

		return indentation.Width.Value;
	}

	private static IReadOnlyList<TableGridColumn> ParseGrid(Table table)
	{
		var tblGrid = table.GetFirstChild<TableGrid>();
		if (tblGrid is null)
		{
			return [];
		}

		var columns = new List<TableGridColumn>();
		foreach (var gridCol in tblGrid.Elements<GridColumn>())
		{
			var width = 0f;
			if (gridCol.Width?.Value is { } w && float.TryParse(w, out var parsed))
			{
				width = parsed;
			}

			columns.Add(new TableGridColumn(width));
		}

		return columns;
	}

	private static IReadOnlyList<TableRowElement> ParseRows(Table table, CellMargins defaultCellMargins)
	{
		var rows = new List<TableRowElement>();
		foreach (var child in table.ChildElements)
		{
			IEnumerable<TableRow> tableRows = child switch
			{
				TableRow tr => [tr],
				SdtRow sdtRow => sdtRow.GetFirstChild<SdtContentRow>()?.Elements<TableRow>() ?? [],
				_ => [],
			};

			foreach (var tr in tableRows)
			{
				var trPr = tr.TableRowProperties;
				var trPrEx = trPr?.GetFirstChild<TablePropertyExceptions>();
				rows.Add(new TableRowElement
				{
					Cells = ParseCells(tr, defaultCellMargins),
					HeightTwips = ParseRowHeight(trPr),
					HeightRule = ParseRowHeightRule(trPr),
					IsHeaderRow = IsOnOffSet(trPr?.GetFirstChild<TableHeader>()),
					CantSplit = IsOnOffSet(trPr?.GetFirstChild<CantSplit>()),
					Borders = ParseTableBorders(trPrEx?.GetFirstChild<TableBorders>()),
				});
			}
		}

		return rows;
	}
}
