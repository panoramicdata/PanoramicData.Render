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
	private static (float LabelX, float TextStartX) ResolveListLabelPosition(
		ParagraphBlock paragraphBlock,
		LayoutBlockPlacement placement,
		NumberingLevelStyle listStyle,
		int numberingLevel,
		float labelWidth)
	{
		if (paragraphBlock.IsBiDi)
		{
			var labelX = placement.XTwips + placement.ContentWidthTwips - ((numberingLevel + 1) * DefaultListIndentStepTwips);
			return (labelX, labelX - DefaultListTextGapTwips - labelWidth);
		}

		if (paragraphBlock.Indentation.Hanging > 0f || paragraphBlock.Indentation.Left > 0f)
		{
			var textStartX = placement.XTwips + Math.Max(0f, paragraphBlock.Indentation.Left);
			var labelX = paragraphBlock.Indentation.Hanging > 0f
				? placement.XTwips + Math.Max(0f, paragraphBlock.Indentation.Left - paragraphBlock.Indentation.Hanging)
				: Math.Max(placement.XTwips, textStartX - labelWidth - DefaultListTextGapTwips);
			return (labelX, textStartX);
		}

		if (listStyle.IndentLeftTwips is { } indentLeft)
		{
			var hangingTwips = listStyle.HangingTwips ?? 0f;
			return (placement.XTwips + (indentLeft - hangingTwips), placement.XTwips + indentLeft);
		}

		var defaultTextStartX = placement.XTwips + ((numberingLevel + 1) * DefaultListIndentStepTwips) + DefaultListTextGapTwips;
		return (defaultTextStartX - labelWidth, defaultTextStartX);
	}

	private static float ResolveAlignedStartX(ParagraphBlock paragraphBlock, LayoutBlockPlacement placement, IReadOnlyList<TextSegment> segments, float currentX)
	{
		var effectiveAlignment = paragraphBlock.Alignment
			?? (paragraphBlock.IsBiDi ? ParagraphAlignment.Right : ParagraphAlignment.Left);
		if (paragraphBlock.NumberingId is not null || effectiveAlignment is not (ParagraphAlignment.Right or ParagraphAlignment.Center))
		{
			return currentX;
		}

		var totalWidth = ComputeTotalSegmentWidth(segments);
		return effectiveAlignment is ParagraphAlignment.Right
			? placement.XTwips + placement.ContentWidthTwips - totalWidth
			: placement.XTwips + ((placement.ContentWidthTwips - totalWidth) / 2f);
	}

	private static void EmitInlineSegments(
		IReadOnlyList<TextSegment> segments,
		ParagraphBlock paragraphBlock,
		LayoutBlockPlacement placement,
		float baselineY,
		float currentX,
		IRenderTarget target,
		bool includeHyperlinks = true)
	{
		var tabProfile = TabStopParser.ParseTabStops(paragraphBlock.SourceElement.ParagraphProperties);
		for (var segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
		{
			var segment = segments[segmentIndex];
			if (segment.IsTab)
			{
				currentX = ResolveTabAdvance(segments, segmentIndex, tabProfile, placement, currentX, baselineY, target);
				continue;
			}

			var segmentWidth = EstimateTextWidthTwips(segment.Text, segment.Font.SizePoints);
			DrawTextRun(target, new TextDrawing(segment.Text, segment.Font, segment.Brush, segment.HighlightFillColor, includeHyperlinks ? segment.HyperlinkUri : null), currentX, baselineY, segmentWidth);
			currentX += segmentWidth;
		}
	}

	private static float ResolveTabAdvance(
		IReadOnlyList<TextSegment> segments,
		int segmentIndex,
		TabStopProfile tabProfile,
		LayoutBlockPlacement placement,
		float currentX,
		float baselineY,
		IRenderTarget target)
	{
		var segment = segments[segmentIndex];
		var leaderStartX = currentX;
		if (segment.IsPtabRightMargin)
		{
			var contentAfterTab = GetTextAfterTab(segments, segmentIndex);
			var contentWidthAfterTab = EstimateTextWidthTwips(contentAfterTab, segment.Font.SizePoints);
			return placement.XTwips + placement.ContentWidthTwips - contentWidthAfterTab;
		}

		var tabStop = tabProfile.ResolveNextTabStop(currentX - placement.XTwips);
		var newX = placement.XTwips + ResolveTabStopOffset(tabStop, GetTextAfterTab(segments, segmentIndex), segment.Font.SizePoints);
		EmitLeaderCharacters(tabStop.Leader, leaderStartX, newX, baselineY, segment.Font, segment.Brush, target);
		return newX;
	}

	private static float ResolveTabStopOffset(TabStop tabStop, string contentAfterTab, float fontSizePoints)
	{
		if (tabStop.Type == TabStopType.Decimal)
		{
			var decimalIndex = contentAfterTab.IndexOf(TabStopResolver.DecimalSeparator);
			var widthBeforeDecimal = decimalIndex >= 0
				? EstimateTextWidthTwips(contentAfterTab[..decimalIndex], fontSizePoints)
				: EstimateTextWidthTwips(contentAfterTab, fontSizePoints);
			return TabStopResolver.ComputeContentStart(tabStop, 0f, widthBeforeDecimal);
		}

		if (tabStop.Type is TabStopType.Right or TabStopType.Center)
		{
			return TabStopResolver.ComputeContentStart(tabStop, EstimateTextWidthTwips(contentAfterTab, fontSizePoints));
		}

		return tabStop.PositionTwips;
	}
}
