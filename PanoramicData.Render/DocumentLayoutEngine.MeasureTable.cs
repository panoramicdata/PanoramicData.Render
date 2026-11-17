namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;

internal static partial class DocumentLayoutEngine
{
	private static LayoutBlock MeasureTable(TablePlaceholderBlock table, float? availableWidthTwips, Styles? styles)
	{
		// Parse the table and compute row heights for pagination.
		var parsedTable = TableParser.Parse(table.TableElement, styles);

		IReadOnlyList<float> rowHeights;
		if (availableWidthTwips is > 0f)
		{
			// Honour explicit table width when specified.
			var effectiveWidth = availableWidthTwips.Value - parsedTable.IndentationTwips;
			if (parsedTable.Width.Type == TableWidthUnit.Dxa && parsedTable.Width.Value > 0f)
			{
				effectiveWidth = Math.Min(effectiveWidth, parsedTable.Width.Value);
			}
			else if (parsedTable.Width.Type == TableWidthUnit.Pct && parsedTable.Width.Value > 0f)
			{
				effectiveWidth = Math.Min(effectiveWidth, effectiveWidth * parsedTable.Width.Value / 5000f);
			}

			var layout = TableLayoutEngine.Layout(parsedTable, Math.Max(0f, effectiveWidth));
			rowHeights = layout.ColumnWidths.Count > 0
				? TableLayoutEngine.ComputeRowHeights(parsedTable, layout.ColumnWidths)
				: TableLayoutEngine.ComputeRowHeights(parsedTable);
		}
		else
		{
			rowHeights = TableLayoutEngine.ComputeRowHeights(parsedTable);
		}

		var height = 0f;
		foreach (var rh in rowHeights)
		{
			height += rh;
		}

		return new LayoutBlock(table, height);
	}
}
