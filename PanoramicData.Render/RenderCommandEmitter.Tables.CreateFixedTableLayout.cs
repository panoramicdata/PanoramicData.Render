namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using SkiaSharp;
using System.Globalization;
using System.Text;

internal static partial class RenderCommandEmitter
{
	private static TableLayoutResult CreateFixedTableLayout(TableElement parsedTable, TableLayoutResult fixedLayout, TableAlignment effectiveAlignment, float fullAvailableWidth)
	{
		var rowHeights = TableLayoutEngine.ComputeRowHeights(parsedTable, fixedLayout.ColumnWidths);
		var totalHeight = 0f;
		foreach (var rowHeight in rowHeights)
		{
			totalHeight += rowHeight;
		}

		// Recompute alignment offset using the full available width (before tblW capping)
		// so centered/right-aligned tables are positioned relative to the page content area.
		var tableWidth = SumColumnWidths(fixedLayout.ColumnWidths);
		var alignmentOffset = effectiveAlignment switch
		{
			TableAlignment.Center => Math.Max(0f, (fullAvailableWidth - tableWidth) / 2f),
			TableAlignment.Right => Math.Max(0f, fullAvailableWidth - tableWidth),
			_ => parsedTable.IndentationTwips > 0f ? 0f : fixedLayout.TableXOffset,
		};

		return new TableLayoutResult
		{
			TableXOffset = alignmentOffset,
			TableWidthTwips = fixedLayout.TableWidthTwips,
			ColumnOffsets = fixedLayout.ColumnOffsets,
			ColumnWidths = fixedLayout.ColumnWidths,
			RowHeights = rowHeights,
			TotalHeightTwips = totalHeight,
			Table = parsedTable,
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

	private static TableAlignment ResolveTableStyleAlignment(string styleId, Styles styles)
	{
		var visited = new HashSet<string>(StringComparer.Ordinal);
		var currentId = styleId;

		while (!string.IsNullOrEmpty(currentId) && visited.Add(currentId))
		{
			var style = FindStyle(styles, currentId, StyleValues.Table);
			if (style is null)
			{
				break;
			}

			var justification = GetTableJustification(style);
			if (justification is not null)
			{
				return MapTableJustification(justification.Value);
			}

			currentId = style.BasedOn?.Val?.Value;
		}

		return TableAlignment.Left;
	}

	private static TableRowAlignmentValues? GetTableJustification(Style style)
		=> style.StyleTableProperties?.TableJustification?.Val?.Value;

	private static TableAlignment MapTableJustification(TableRowAlignmentValues justification)
	{
		if (justification == TableRowAlignmentValues.Center)
		{
			return TableAlignment.Center;
		}

		return justification == TableRowAlignmentValues.Right ? TableAlignment.Right : TableAlignment.Left;
	}

	private static Style? FindStyle(Styles styles, string styleId, StyleValues type)
		=> styles.Elements<Style>().FirstOrDefault(s => s.StyleId?.Value == styleId && s.Type?.Value == type);
}
