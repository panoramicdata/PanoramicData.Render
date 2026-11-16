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
	private static void NormalizeLightCellTextColor(IReadOnlyList<LayoutBlock> cellBlocks, ParagraphShading shading)
	{
		if (!IsLightBackground(shading.GetEffectiveBackgroundColor()))
		{
			return;
		}

		var runs = cellBlocks
			.Select(cellBlock => cellBlock.Block)
			.OfType<ParagraphBlock>()
			.SelectMany(paragraphBlock => paragraphBlock.SourceElement.Descendants<Run>());
		foreach (var run in runs.Where(r => IsWhiteColor(r.RunProperties?.Color?.Val?.Value)))
		{
			run.RunProperties ??= new RunProperties();
			run.RunProperties.Color ??= new Color();
			run.RunProperties.Color.Val = "000000";
		}
	}

	private static bool IsWhiteColor(string? colorValue)
		=> string.Equals(colorValue, "FFFFFF", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(colorValue, "WHITE", StringComparison.OrdinalIgnoreCase);
	private static bool IsLightBackground(string? backgroundColor)
	{
		if (!TryParseRenderColor(backgroundColor, out var color))
		{
			// Treat missing/unknown shading as a light background.
			return true;
		}

		var luminance = (0.2126f * color.R + 0.7152f * color.G + 0.0722f * color.B) / 255f;
		return luminance >= LightBackgroundLuminanceThreshold;
	}

	private static int CountVerticalSpan(ResolvedGridCell?[,] grid, int ownerRow, int ownerColumn, int rowCount)
	{
		var span = 1;
		for (var row = ownerRow + 1; row < rowCount; row++)
		{
			if (grid[row, ownerColumn] is { } cell
				&& cell.OwnerRowIndex == ownerRow
				&& cell.OwnerColumnIndex == ownerColumn)
			{
				span++;
			}
			else
			{
				break;
			}
		}

		return span;
	}

	private static void EmitTableBlock(TablePlaceholderBlock tableBlock, LayoutBlock layoutBlock, LayoutBlockPlacement placement, BlockEmitContext context)
	{
		var target = context.Target;
		var tableLayout = CreateRenderableTableLayout(tableBlock, placement.ContentWidthTwips, context.Styles);
		if (tableLayout is null)
		{
			var heightTwips = MathF.Max(layoutBlock.HeightTwips, 1f);
			target.DrawRect(new RenderRect(placement.XTwips, placement.YTwips, placement.ContentWidthTwips, heightTwips), null, context.DefaultStroke);
			return;
		}

		var frame = new TableEmitFrame(tableLayout, TableGridResolver.Resolve(tableLayout.Table), placement.XTwips + tableLayout.TableXOffset, placement.YTwips);
		EmitTableBackgrounds(frame, context);
		EmitTableBorders(frame, target);

		foreach (var position in TableLayoutEngine.ComputeCellPositions(tableLayout))
		{
			EmitTableCell(position, frame, placement, context);
		}
	}

	private static void EmitTableBackgrounds(TableEmitFrame frame, BlockEmitContext context)
	{
		foreach (var background in TableLayoutEngine.ComputeCellBackgrounds(frame.Layout, context.Styles))
		{
			if (!TryParseRenderColor(background.Shading.GetEffectiveBackgroundColor(), out var fillColor))
			{
				continue;
			}

			context.Target.DrawRect(
				new RenderRect(frame.X + background.X, frame.Y + background.Y, background.Width, background.Height),
				new SolidRenderBrush(fillColor),
				null);
		}
	}

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

		// Resolve alignment from table style when not set via direct formatting.
		var effectiveAlignment = parsedTable.Alignment;
		if (effectiveAlignment == TableAlignment.Left && parsedTable.StyleId is not null && styles is not null)
		{
			effectiveAlignment = ResolveTableStyleAlignment(parsedTable.StyleId, styles);
		}

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

	private static ParagraphShading? GetVisibleShading(Shading? shading)
	{
		if (shading is null)
		{
			return null;
		}

		var parsed = TableParser.ParseShading(shading);
		return parsed.HasVisibleShading ? parsed : null;
	}

	private static ParagraphShading ResolveParagraphShading(Styles? styles, string? styleId, Paragraph paragraph)
	{
		// Direct paragraph shading takes precedence over style-inherited shading.
		if (GetVisibleShading(paragraph.ParagraphProperties?.Shading) is { } direct)
		{
			return direct;
		}

		return styles is null || styleId is null
			? ParagraphShading.None
			: FindInheritedShading(styles, styleId);
	}

	// Walks the paragraph style inheritance chain to find shading.
	private static ParagraphShading FindInheritedShading(Styles styles, string styleId)
	{
		var currentId = styleId;
		var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		while (!string.IsNullOrEmpty(currentId) && visited.Add(currentId))
		{
			var style = FindStyle(styles, currentId, StyleValues.Paragraph);
			if (style is null)
			{
				break;
			}

			if (GetVisibleShading(style.StyleParagraphProperties?.GetFirstChild<Shading>()) is { } styleShading)
			{
				return styleShading;
			}

			currentId = style.BasedOn?.Val?.Value;
		}

		return ParagraphShading.None;
	}
	private static void EmitParagraphBorders(ParagraphBorders borders, float xTwips, float yTopTwips, float heightTwips, float widthTwips, IRenderTarget target)
	{
		if (borders.Bottom is { IsVisible: true } bottom)
		{
			var strokeColor = TryParseRenderColor(bottom.Color, out var color) ? color : DefaultTextColor;
			var strokeWidth = Math.Max(bottom.GetWidthTwips(), 1f);
			var y = yTopTwips + heightTwips + bottom.GetSpacingTwips();
			target.DrawLine(new RenderPoint(xTwips, y), new RenderPoint(xTwips + widthTwips, y), new RenderStroke(strokeColor, strokeWidth));
		}

		if (borders.Top is { IsVisible: true } top)
		{
			var strokeColor = TryParseRenderColor(top.Color, out var color) ? color : DefaultTextColor;
			var strokeWidth = Math.Max(top.GetWidthTwips(), 1f);
			var y = yTopTwips - top.GetSpacingTwips();
			target.DrawLine(new RenderPoint(xTwips, y), new RenderPoint(xTwips + widthTwips, y), new RenderStroke(strokeColor, strokeWidth));
		}
	}
}
