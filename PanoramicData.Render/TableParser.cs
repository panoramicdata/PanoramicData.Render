namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Parses OpenXML table elements into the internal <see cref="TableElement"/> model.
/// </summary>
internal static class TableParser
{
	private const string WordprocessingNamespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

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
			Borders = ParseTableBorders(tblPr?.TableBorders),
			BorderSpacingTwips = ParseTableCellSpacing(tblPr?.TableCellSpacing),
			Look = ParseTableLook(tblPr?.TableLook),
		};
	}

	internal static float ParseTableCellSpacing(TableCellSpacing? spacing)
	{
		if (spacing?.Width?.Value is null)
		{
			return 0f;
		}

		if (spacing.Type?.Value == TableWidthUnitValues.Auto
			|| spacing.Type?.Value == TableWidthUnitValues.Nil
			|| spacing.Type?.Value == TableWidthUnitValues.Pct)
		{
			return 0f;
		}

		if (float.TryParse(spacing.Width.Value, out var parsed))
		{
			return parsed;
		}

		return 0f;
	}

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

	private static IReadOnlyList<TableRowElement> ParseRows(Table table)
	{
		var rows = new List<TableRowElement>();
		foreach (var tr in table.Elements<TableRow>())
		{
			var trPr = tr.TableRowProperties;
			var trPrEx = trPr?.GetFirstChild<TablePropertyExceptions>();
			rows.Add(new TableRowElement
			{
				Cells = ParseCells(tr),
				HeightTwips = ParseRowHeight(trPr),
				HeightRule = ParseRowHeightRule(trPr),
				IsHeaderRow = IsOnOffSet(trPr?.GetFirstChild<TableHeader>()),
				CantSplit = IsOnOffSet(trPr?.GetFirstChild<CantSplit>()),
				Borders = ParseTableBorders(trPrEx?.GetFirstChild<TableBorders>()),
			});
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
				Width = ParseTableWidth(tcPr?.TableCellWidth),
				VerticalAlignment = ParseCellVerticalAlignment(tcPr?.TableCellVerticalAlignment),
				TextDirection = ParseCellTextDirection(tcPr?.TextDirection),
				Margins = ParseCellMargins(tcPr?.TableCellMargin),
				Borders = ParseCellBorders(tcPr?.TableCellBorders),
				Shading = ParseShading(tcPr?.Shading),
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

	internal static CellTextDirection ParseCellTextDirection(TextDirection? textDir)
	{
		if (textDir?.Val?.Value is null)
		{
			return CellTextDirection.LeftToRightTopToBottom;
		}

		if (textDir.Val.Value == TextDirectionValues.TopToBottomRightToLeft
			|| textDir.Val.Value == TextDirectionValues.TopToBottomRightToLeftRotated)
		{
			return CellTextDirection.TopToBottomRightToLeft;
		}

		if (textDir.Val.Value == TextDirectionValues.BottomToTopLeftToRight
			|| textDir.Val.Value == TextDirectionValues.BottomToTopLeftToRight2010)
		{
			return CellTextDirection.BottomToTopLeftToRight;
		}

		return CellTextDirection.LeftToRightTopToBottom;
	}

	internal static CellMargins ParseCellMargins(TableCellMargin? margins)
	{
		if (margins is null)
		{
			return CellMargins.None;
		}

		return new CellMargins(
			ParseMarginWidth(margins.TopMargin),
			ParseMarginWidth(margins.RightMargin),
			ParseMarginWidth(margins.BottomMargin),
			ParseMarginWidth(margins.LeftMargin));
	}

	private static float ParseMarginWidth(TopMargin? margin)
	{
		if (margin?.Width?.Value is { } w && float.TryParse(w, out var parsed))
		{
			return parsed;
		}

		return 0f;
	}

	private static float ParseMarginWidth(RightMargin? margin)
	{
		if (margin?.Width?.Value is { } w && float.TryParse(w, out var parsed))
		{
			return parsed;
		}

		return 0f;
	}

	private static float ParseMarginWidth(BottomMargin? margin)
	{
		if (margin?.Width?.Value is { } w && float.TryParse(w, out var parsed))
		{
			return parsed;
		}

		return 0f;
	}

	private static float ParseMarginWidth(LeftMargin? margin)
	{
		if (margin?.Width?.Value is { } w && float.TryParse(w, out var parsed))
		{
			return parsed;
		}

		return 0f;
	}

	internal static TableBorderSet ParseTableBorders(TableBorders? borders)
	{
		if (borders is null)
		{
			return TableBorderSet.None;
		}

		return new TableBorderSet(
			Top: ParseBorderDefinition(borders.GetFirstChild<TopBorder>()),
			Bottom: ParseBorderDefinition(borders.GetFirstChild<BottomBorder>()),
			Left: ParseBorderDefinition(borders.GetFirstChild<LeftBorder>()),
			Right: ParseBorderDefinition(borders.GetFirstChild<RightBorder>()),
			InsideHorizontal: ParseBorderDefinition(borders.GetFirstChild<InsideHorizontalBorder>()),
			InsideVertical: ParseBorderDefinition(borders.GetFirstChild<InsideVerticalBorder>()));
	}

	internal static TableBorderSet ParseCellBorders(TableCellBorders? borders)
	{
		if (borders is null)
		{
			return TableBorderSet.None;
		}

		return new TableBorderSet(
			Top: ParseBorderDefinition(borders.GetFirstChild<TopBorder>()),
			Bottom: ParseBorderDefinition(borders.GetFirstChild<BottomBorder>()),
			Left: ParseBorderDefinition(borders.GetFirstChild<LeftBorder>()),
			Right: ParseBorderDefinition(borders.GetFirstChild<RightBorder>()));
	}

	internal static ParagraphShading ParseShading(Shading? shading)
	{
		if (shading is null)
		{
			return ParagraphShading.None;
		}

		var pattern = ParseShadingPattern(shading.GetAttribute("val", WordprocessingNamespace).Value);
		var patternColor = NormalizeShadingColor(shading.Color?.Value);
		var fillColor = NormalizeShadingColor(shading.Fill?.Value);

		if (pattern == ShadingPattern.Clear && patternColor is null && fillColor is null)
		{
			return ParagraphShading.None;
		}

		return new ParagraphShading(pattern, patternColor, fillColor);
	}

	private static string? NormalizeShadingColor(string? value)
	{
		if (string.IsNullOrWhiteSpace(value)
			|| value.Equals("auto", StringComparison.OrdinalIgnoreCase)
			|| value.Equals("nil", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}

		return value.ToUpperInvariant();
	}

	private static ShadingPattern ParseShadingPattern(string? value)
	{
		return value?.ToLowerInvariant() switch
		{
			"solid" => ShadingPattern.Solid,
			"horzstripe" => ShadingPattern.HorizontalStripe,
			"vertstripe" => ShadingPattern.VerticalStripe,
			"reversediagstripe" => ShadingPattern.ReverseDiagonalStripe,
			"diagstripe" => ShadingPattern.DiagonalStripe,
			"horzcross" => ShadingPattern.HorizontalCross,
			"diagcross" => ShadingPattern.DiagonalCross,
			"thinhorzstripe" => ShadingPattern.ThinHorizontalStripe,
			"thinvertstripe" => ShadingPattern.ThinVerticalStripe,
			"thinreversediagstripe" => ShadingPattern.ThinReverseDiagonalStripe,
			"thindiagstripe" => ShadingPattern.ThinDiagonalStripe,
			"thinhorzcross" => ShadingPattern.ThinHorizontalCross,
			"thindiagcross" => ShadingPattern.ThinDiagonalCross,
			"pct5" => ShadingPattern.Percent5,
			"pct10" => ShadingPattern.Percent10,
			"pct12" => ShadingPattern.Percent12,
			"pct15" => ShadingPattern.Percent15,
			"pct20" => ShadingPattern.Percent20,
			"pct25" => ShadingPattern.Percent25,
			"pct30" => ShadingPattern.Percent30,
			"pct35" => ShadingPattern.Percent35,
			"pct37" => ShadingPattern.Percent37,
			"pct40" => ShadingPattern.Percent40,
			"pct45" => ShadingPattern.Percent45,
			"pct50" => ShadingPattern.Percent50,
			"pct55" => ShadingPattern.Percent55,
			"pct60" => ShadingPattern.Percent60,
			"pct62" => ShadingPattern.Percent62,
			"pct65" => ShadingPattern.Percent65,
			"pct70" => ShadingPattern.Percent70,
			"pct75" => ShadingPattern.Percent75,
			"pct80" => ShadingPattern.Percent80,
			"pct85" => ShadingPattern.Percent85,
			"pct87" => ShadingPattern.Percent87,
			"pct90" => ShadingPattern.Percent90,
			"pct95" => ShadingPattern.Percent95,
			_ => ShadingPattern.Clear,
		};
	}

	internal static TableBorderDefinition? ParseBorderDefinition(OpenXmlElement? borderElement)
	{
		if (borderElement is null)
		{
			return null;
		}

		return borderElement switch
		{
			TopBorder b => ParseBorderDefinition(b.Val?.Value, b.Size?.Value, b.Color?.Value),
			BottomBorder b => ParseBorderDefinition(b.Val?.Value, b.Size?.Value, b.Color?.Value),
			LeftBorder b => ParseBorderDefinition(b.Val?.Value, b.Size?.Value, b.Color?.Value),
			RightBorder b => ParseBorderDefinition(b.Val?.Value, b.Size?.Value, b.Color?.Value),
			InsideHorizontalBorder b => ParseBorderDefinition(b.Val?.Value, b.Size?.Value, b.Color?.Value),
			InsideVerticalBorder b => ParseBorderDefinition(b.Val?.Value, b.Size?.Value, b.Color?.Value),
			_ => null,
		};
	}

	private static TableBorderDefinition ParseBorderDefinition(BorderValues? value, UInt32Value? size, StringValue? color)
	{
		var width = size?.Value is { } sz ? (int)sz : 0;
		return new TableBorderDefinition(ParseBorderStyle(value), width, color?.Value);
	}

	internal static BorderStyle ParseBorderStyle(BorderValues? value)
	{
		if (value is null || value == BorderValues.None || value == BorderValues.Nil)
		{
			return BorderStyle.None;
		}

		if (value == BorderValues.Single)
		{
			return BorderStyle.Single;
		}

		if (value == BorderValues.Double)
		{
			return BorderStyle.Double;
		}

		if (value == BorderValues.Dotted)
		{
			return BorderStyle.Dotted;
		}

		if (value == BorderValues.Dashed)
		{
			return BorderStyle.Dashed;
		}

		if (value == BorderValues.DotDash)
		{
			return BorderStyle.DotDash;
		}

		if (value == BorderValues.DotDotDash)
		{
			return BorderStyle.DotDotDash;
		}

		if (value == BorderValues.Triple)
		{
			return BorderStyle.Triple;
		}

		if (value == BorderValues.Thick)
		{
			return BorderStyle.Thick;
		}

		if (value == BorderValues.ThinThickSmallGap)
		{
			return BorderStyle.ThinThickSmallGap;
		}

		if (value == BorderValues.ThickThinSmallGap)
		{
			return BorderStyle.ThickThinSmallGap;
		}

		if (value == BorderValues.ThinThickThinSmallGap)
		{
			return BorderStyle.ThinThickThinSmallGap;
		}

		if (value == BorderValues.Wave)
		{
			return BorderStyle.Wave;
		}

		if (value == BorderValues.DoubleWave)
		{
			return BorderStyle.DoubleWave;
		}

		if (value == BorderValues.ThreeDEmboss)
		{
			return BorderStyle.ThreeDEmboss;
		}

		if (value == BorderValues.ThreeDEngrave)
		{
			return BorderStyle.ThreeDEngrave;
		}

		return BorderStyle.None;
	}
}
