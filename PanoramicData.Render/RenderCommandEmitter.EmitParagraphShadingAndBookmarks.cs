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
	private static void EmitParagraphShadingAndBookmarks(ParagraphBlock paragraphBlock, LayoutBlock layoutBlock, LayoutBlockPlacement placement, BlockEmitContext context)
	{
		var target = context.Target;
		var shading = ResolveParagraphShading(context.Styles, paragraphBlock.StyleId, paragraphBlock.SourceElement);
		if (shading.HasVisibleShading && TryParseRenderColor(shading.GetEffectiveBackgroundColor(), out var shadingFillColor))
		{
			target.DrawRect(
				new RenderRect(placement.XTwips, placement.YTwips, placement.ContentWidthTwips, layoutBlock.HeightTwips),
				new SolidRenderBrush(shadingFillColor),
				null);
		}

		foreach (var bookmark in paragraphBlock.BookmarkStarts)
		{
			target.SetNamedDestination(bookmark.Name, placement.XTwips, placement.YTwips);
		}
	}

	private static void EmitParagraphImagesIfAny(ParagraphBlock paragraphBlock, LayoutBlockPlacement placement, BlockEmitContext context)
	{
		if (context.Images is { Count: > 0 } images)
		{
			EmitParagraphImages(paragraphBlock, placement, context.Target, context.DefaultFont, context.Section, images);
		}
	}

	private static float EmitListLabel(
		ParagraphBlock paragraphBlock,
		LayoutBlockPlacement placement,
		BlockEmitContext context,
		IReadOnlyList<TextSegment> logicalSegments,
		float baselineY,
		float currentX)
	{
		if (paragraphBlock.NumberingId is not int numberingId || paragraphBlock.NumberingLevel is not int numberingLevel)
		{
			return currentX;
		}

		var listStyle = ResolveListStyle(context.RenderOptions, numberingId, numberingLevel);
		var labelText = AdvanceListLabelText(context, numberingId, listStyle);
		if (string.IsNullOrEmpty(labelText))
		{
			return currentX;
		}

		var (labelFont, labelBrush) = ResolveLabelStyle(logicalSegments, listStyle, context);
		var labelWidth = EstimateTextWidthTwips(labelText, labelFont.SizePoints);
		var (labelX, textStartX) = ResolveListLabelPosition(paragraphBlock, placement, listStyle, numberingLevel, labelWidth);
		context.Target.DrawText(labelText, labelX, baselineY, labelFont, labelBrush);
		return textStartX;
	}

	private static string AdvanceListLabelText(BlockEmitContext context, int numberingId, NumberingLevelStyle listStyle)
	{
		var canonicalNumberingId = context.RenderOptions.NumberingIdNormalization.TryGetValue(numberingId, out var cid) ? cid : numberingId;
		var labelResult = context.ListState.Advance(canonicalNumberingId, listStyle);
		return string.IsNullOrEmpty(labelResult.Label) ? string.Empty : labelResult.Label + " ";
	}

	private static (RenderFont Font, RenderBrush Brush) ResolveLabelStyle(IReadOnlyList<TextSegment> logicalSegments, NumberingLevelStyle listStyle, BlockEmitContext context)
	{
		TextSegment? labelStyleSource = logicalSegments.Any(IsLabelStyleSource)
			? logicalSegments.First(IsLabelStyleSource)
			: null;
		return (
			ResolveLabelFont(labelStyleSource?.Font ?? context.DefaultFont, listStyle),
			labelStyleSource?.Brush ?? context.DefaultBrush);
	}
	private static bool IsLabelStyleSource(TextSegment segment) => !segment.IsTab && !string.IsNullOrEmpty(segment.Text);

	private static RenderFont ResolveLabelFont(RenderFont baseFont, NumberingLevelStyle listStyle)
		=> string.IsNullOrWhiteSpace(listStyle.FontFamily) ? baseFont : baseFont with { Family = listStyle.FontFamily };
}
