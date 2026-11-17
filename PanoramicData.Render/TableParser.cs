namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Parses OpenXML table elements into the internal <see cref="TableElement"/> model.
/// </summary>
internal static partial class TableParser
{
	private const string WordprocessingNamespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

	/// <summary>
	/// Parses a <c>w:tbl</c> element into a <see cref="TableElement"/>.
	/// </summary>
	/// <param name="table">The OpenXML table element.</param>
	/// <param name="styles">Optional styles document used to resolve default cell margins from the table style chain.</param>
	/// <returns>A parsed <see cref="TableElement"/>.</returns>
	public static TableElement Parse(Table table, Styles? styles = null)
	{
		ArgumentNullException.ThrowIfNull(table);

		var tblPr = table.GetFirstChild<TableProperties>();
		var styleId = tblPr?.TableStyle?.Val?.Value;
		var defaultCellMargins = ResolveTableDefaultCellMargins(tblPr, styles, styleId);

		return new TableElement
		{
			GridColumns = ParseGrid(table),
			Rows = ParseRows(table, defaultCellMargins),
			StyleId = styleId,
			Width = ParseTableWidthOf(tblPr),
			Alignment = ParseAlignmentOf(tblPr),
			IndentationTwips = ParseIndentationOf(tblPr),
			Borders = ParseTableBordersOf(tblPr),
			BorderSpacingTwips = ParseTableCellSpacing(tblPr?.TableCellSpacing),
			DefaultCellMargins = defaultCellMargins,
			Look = ParseTableLookOf(tblPr),
			IsBiDi = IsBiDiTable(tblPr),
		};
	}

	private static TableWidthValue ParseTableWidthOf(TableProperties? tblPr) => ParseTableWidth(tblPr?.TableWidth);

	private static TableAlignment ParseAlignmentOf(TableProperties? tblPr) => ParseAlignment(tblPr?.TableJustification);

	private static float ParseIndentationOf(TableProperties? tblPr) => ParseIndentation(tblPr?.TableIndentation);

	private static TableBorderSet ParseTableBordersOf(TableProperties? tblPr) => ParseTableBorders(tblPr?.TableBorders);

	private static TableLookOptions ParseTableLookOf(TableProperties? tblPr) => ParseTableLook(tblPr?.TableLook);

	// When the table element itself defines no margins, inherit them from the table style chain
	// (for example TableNormal defines 108-twip left and right margins by default).
	private static CellMargins ResolveTableDefaultCellMargins(TableProperties? tblPr, Styles? styles, string? styleId)
	{
		var defaultCellMargins = ParseDefaultCellMargins(tblPr?.TableCellMarginDefault);
		return defaultCellMargins == CellMargins.None
			? TableStyleResolver.ResolveDefaultCellMargins(styles, styleId)
			: defaultCellMargins;
	}

	private static bool IsBiDiTable(TableProperties? tblPr)
		=> tblPr?.BiDiVisual is { } bidi && (bidi.Val is null || bidi.Val.Value == OnOffOnlyValues.On);

	internal static float ParseTableCellSpacing(TableCellSpacing? spacing)
	{
		if (spacing?.Width?.Value is null || IsNonAbsoluteWidthType(spacing.Type?.Value))
		{
			return 0f;
		}

		return float.TryParse(spacing.Width.Value, out var parsed) ? parsed : 0f;
	}

	private static bool IsNonAbsoluteWidthType(TableWidthUnitValues? type)
		=> type == TableWidthUnitValues.Auto || type == TableWidthUnitValues.Nil || type == TableWidthUnitValues.Pct;
	internal static TableLookOptions ParseTableLook(TableLook? tableLook)
	{
		if (tableLook is null)
		{
			return TableLookOptions.None;
		}

		return new TableLookOptions(
			ApplyFirstRow: ParseOnOffAttribute(tableLook, "firstRow"),
			ApplyLastRow: ParseOnOffAttribute(tableLook, "lastRow"),
			ApplyFirstColumn: ParseOnOffAttribute(tableLook, "firstColumn"),
			ApplyLastColumn: ParseOnOffAttribute(tableLook, "lastColumn"),
			ApplyBandedRows: !ParseOnOffAttribute(tableLook, "noHBand"),
			ApplyBandedColumns: !ParseOnOffAttribute(tableLook, "noVBand"));
	}

	private static bool ParseOnOffAttribute(OpenXmlElement element, string localName)
	{
		var value = element.GetAttributes()
			.FirstOrDefault(attribute => attribute.LocalName == localName && attribute.NamespaceUri == WordprocessingNamespace)
			.Value;
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}

		return value.Equals("1", StringComparison.Ordinal)
			|| value.Equals("true", StringComparison.OrdinalIgnoreCase)
			|| value.Equals("on", StringComparison.OrdinalIgnoreCase);
	}

	internal static TableWidthValue ParseTableWidth(TableWidthType? tableWidth)
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

	private static TableCellElement ParseCell(TableCell tc, CellMargins defaultCellMargins)
	{
		var tcPr = tc.TableCellProperties;
		var cellMargins = ParseCellMargins(tcPr?.TableCellMargin);

		return new TableCellElement
		{
			Blocks = ParseCellContent(tc),
			GridSpan = ParseGridSpan(tcPr),
			VerticalMerge = ParseVerticalMerge(tcPr),
			Width = ParseTableWidth(tcPr?.TableCellWidth),
			VerticalAlignment = ParseCellVerticalAlignment(tcPr?.TableCellVerticalAlignment),
			TextDirection = ParseCellTextDirection(tcPr?.TextDirection),

			// Fall back to table-level default cell margins when the cell has none.
			Margins = cellMargins == CellMargins.None ? defaultCellMargins : cellMargins,
			Borders = ParseCellBorders(tcPr?.TableCellBorders),
			Shading = ParseShading(tcPr?.Shading),
		};
	}
	private static IReadOnlyList<DocumentBlock> ParseCellContent(TableCell cell)
	{
		var blocks = new List<DocumentBlock>();
		ParseCellElements(cell.ChildElements, blocks);
		return blocks;
	}

	private static void ParseCellElements(DocumentFormat.OpenXml.OpenXmlElementList elements, List<DocumentBlock> blocks)
	{
		foreach (var element in elements)
		{
			if (element is Paragraph paragraph)
			{
				blocks.Add(DocumentBlockParser.CreateParagraphBlock(paragraph));
			}
			else if (element is Table nestedTable)
			{
				blocks.Add(new TablePlaceholderBlock { TableElement = nestedTable });
			}
			else if (element is SdtBlock sdtBlock)
			{
				// Unwrap block-level content controls inside cells
				var sdtContent = sdtBlock.SdtContentBlock;
				if (sdtContent is not null)
				{
					ParseCellElements(sdtContent.ChildElements, blocks);
				}
			}
		}
	}
}
