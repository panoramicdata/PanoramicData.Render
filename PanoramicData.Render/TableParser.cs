namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Parses OpenXML table elements into the internal <see cref="TableElement"/> model.
/// </summary>
internal static class TableParser
{
	/// <summary>
	/// Parses a <c>w:tbl</c> element into a <see cref="TableElement"/>.
	/// </summary>
	/// <param name="table">The OpenXML table element.</param>
	/// <returns>A parsed <see cref="TableElement"/>.</returns>
	public static TableElement Parse(Table table)
	{
		ArgumentNullException.ThrowIfNull(table);

		var tblPr = table.GetFirstChild<TableProperties>();

		return new TableElement
		{
			GridColumns = ParseGrid(table),
			Rows = ParseRows(table),
			StyleId = tblPr?.TableStyle?.Val?.Value,
			Width = ParseTableWidth(tblPr?.TableWidth),
			Alignment = ParseAlignment(tblPr?.TableJustification),
			IndentationTwips = ParseIndentation(tblPr?.TableIndentation),
		};
	}

	internal static TableWidthValue ParseTableWidth(TableWidth? tableWidth)
	{
		if (tableWidth is null)
		{
			return TableWidthValue.Auto;
		}

		var type = ParseWidthType(tableWidth.Type?.Value);
		var value = 0f;
		if (tableWidth.Width?.Value is { } w && float.TryParse(w, out var parsed))
		{
			value = parsed;
		}

		return new TableWidthValue(value, type);
	}

	private static TableWidthUnit ParseWidthType(TableWidthUnitValues? unit)
	{
		if (unit is null)
		{
			return TableWidthUnit.Auto;
		}

		if (unit == TableWidthUnitValues.Dxa)
		{
			return TableWidthUnit.Dxa;
		}

		if (unit == TableWidthUnitValues.Pct)
		{
			return TableWidthUnit.Pct;
		}

		if (unit == TableWidthUnitValues.Nil)
		{
			return TableWidthUnit.Nil;
		}

		return TableWidthUnit.Auto;
	}

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

	private static IReadOnlyList<TableRowElement> ParseRows(Table table)
	{
		var rows = new List<TableRowElement>();
		foreach (var tr in table.Elements<TableRow>())
		{
			rows.Add(new TableRowElement
			{
				Cells = ParseCells(tr),
			});
		}

		return rows;
	}

	private static IReadOnlyList<TableCellElement> ParseCells(TableRow row)
	{
		var cells = new List<TableCellElement>();
		foreach (var tc in row.Elements<TableCell>())
		{
			var tcPr = tc.TableCellProperties;

			cells.Add(new TableCellElement
			{
				Blocks = ParseCellContent(tc),
				GridSpan = ParseGridSpan(tcPr),
				VerticalMerge = ParseVerticalMerge(tcPr),
			});
		}

		return cells;
	}

	private static IReadOnlyList<DocumentBlock> ParseCellContent(TableCell cell)
	{
		var blocks = new List<DocumentBlock>();
		foreach (var element in cell.ChildElements)
		{
			switch (element)
			{
				case Paragraph paragraph:
					blocks.Add(DocumentBlockParser.CreateParagraphBlock(paragraph));
					break;

				case Table nestedTable:
					blocks.Add(new TablePlaceholderBlock { TableElement = nestedTable });
					break;
			}
		}

		return blocks;
	}

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
}
