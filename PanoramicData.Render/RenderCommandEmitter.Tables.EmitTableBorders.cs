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
	private static void EmitTableBorders(TableEmitFrame frame, IRenderTarget target)
	{
		foreach (var segment in TableLayoutEngine.ComputeBorderSegments(frame.Layout))
		{
			if (segment.WidthTwips <= 0f || !TryParseRenderColor(segment.ColorHex, out var strokeColor))
			{
				continue;
			}

			target.DrawLine(
				new RenderPoint(frame.X + segment.X1, frame.Y + segment.Y1),
				new RenderPoint(frame.X + segment.X2, frame.Y + segment.Y2),
				new RenderStroke(strokeColor, segment.WidthTwips));
		}
	}

	private static void EmitTableCell(CellPosition position, TableEmitFrame frame, LayoutBlockPlacement placement, BlockEmitContext context)
	{
		var table = frame.Layout.Table;
		var target = context.Target;
		var rowCount = frame.Grid.GetLength(0);
		var columnCount = frame.Grid.GetLength(1);
		var contentWidth = TableLayoutEngine.ComputeContentWidth(position.Width, position.Cell.Margins, table.BorderSpacingTwips);
		if (contentWidth <= 0f)
		{
			return;
		}

		var (cellBlocks, totalHeight) = TableLayoutEngine.LayoutCellContent(position.Cell, table.BorderSpacingTwips, contentWidth);
		if (cellBlocks.Count == 0)
		{
			return;
		}

		var rowSpan = CountVerticalSpan(frame.Grid, position.RowIndex, position.ColumnIndex, rowCount);
		var remainingColumns = Math.Max(0, columnCount - position.ColumnIndex);
		var columnSpan = Math.Min(position.Cell.GridSpan, remainingColumns);

		var effectiveShading = position.Cell.Shading.HasVisibleShading
			? position.Cell.Shading
			: TableStyleResolver.ResolveCellShading(
				context.Styles,
				table,
				position.RowIndex,
				position.ColumnIndex,
				rowSpan,
				columnSpan,
				rowCount,
				columnCount);
		NormalizeLightCellTextColor(cellBlocks, effectiveShading);

		var verticalOffset = TableLayoutEngine.ComputeVerticalContentOffset(position.Height, totalHeight, position.Cell.VerticalAlignment);
		var contentX = frame.X + position.X + position.Cell.Margins.Left + table.BorderSpacingTwips;
		var currentY = frame.Y + position.Y + verticalOffset + position.Cell.Margins.Top + table.BorderSpacingTwips;

		target.PushClip(new RenderRect(frame.X + position.X, frame.Y + position.Y, position.Width, position.Height));
		foreach (var cellBlock in cellBlocks)
		{
			EmitLayoutBlock(cellBlock, new LayoutBlockPlacement(cellBlock, contentX, currentY, contentWidth, placement.ColumnIndex), context);
			currentY += cellBlock.HeightTwips;
		}

		target.PopClip();
	}
	private static TableLayoutResult? CreateRenderableTableLayout(TablePlaceholderBlock tableBlock, float availableWidthTwips, Styles? styles = null)
	{
		var parsedTable = TableParser.Parse(tableBlock.TableElement, styles);
		if (parsedTable.Rows.Count == 0)
		{
			return null;
		}

		var effectiveAlignment = ResolveEffectiveTableAlignment(parsedTable, styles);
		var fullAvailableWidth = Math.Max(0f, availableWidthTwips - parsedTable.IndentationTwips);
		var effectiveAvailableWidth = ApplyExplicitTableWidth(parsedTable.Width, fullAvailableWidth);

		var fixedLayout = TableLayoutEngine.Layout(parsedTable, effectiveAvailableWidth);
		if (fixedLayout.ColumnWidths.Count == 0 || fixedLayout.TableWidthTwips <= 0f)
		{
			var autoFitLayout = TableLayoutEngine.LayoutAutoFit(parsedTable, effectiveAvailableWidth);
			return autoFitLayout.ColumnWidths.Count == 0 || autoFitLayout.TableWidthTwips <= 0f
				? null
				: autoFitLayout;
		}

		return CreateFixedTableLayout(parsedTable, fixedLayout, effectiveAlignment, fullAvailableWidth);
	}

	// Resolve alignment from table style when not set via direct formatting.
	private static TableAlignment ResolveEffectiveTableAlignment(TableElement parsedTable, Styles? styles)
		=> parsedTable.Alignment == TableAlignment.Left && parsedTable.StyleId is not null && styles is not null
			? ResolveTableStyleAlignment(parsedTable.StyleId, styles)
			: parsedTable.Alignment;

	// Honour explicit table width (tblW) when specified.
	private static float ApplyExplicitTableWidth(TableWidthValue width, float availableWidth)
	{
		if (width.Type == TableWidthUnit.Dxa && width.Value > 0f)
		{
			return Math.Min(availableWidth, width.Value);
		}

		// Pct value is in fiftieths of a percent (5000 = 100%).
		return width.Type == TableWidthUnit.Pct && width.Value > 0f
			? Math.Min(availableWidth, availableWidth * width.Value / 5000f)
			: availableWidth;
	}
}
