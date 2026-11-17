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
}
