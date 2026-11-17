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
	internal static int EstimateWrappedLineCount(ParagraphBlock paragraphBlock, float availableWidthTwips, string defaultFontFamily = "Times New Roman")
	{
		ArgumentNullException.ThrowIfNull(paragraphBlock);

		if (availableWidthTwips <= 0f)
		{
			return 1;
		}

		var defaultFont = new RenderFont(defaultFontFamily, 12f);
		var segments = BuildTextSegments(paragraphBlock.SourceElement, defaultFont, defaultFontFamily, 1, 1, DateTime.UnixEpoch);
		if (!CanWrapParagraph(paragraphBlock, segments, availableWidthTwips))
		{
			return 1;
		}

		var effectiveAlignment = paragraphBlock.Alignment
			?? (paragraphBlock.IsBiDi ? ParagraphAlignment.Right : ParagraphAlignment.Left);

		return CountWrappedLines(segments, availableWidthTwips, effectiveAlignment);
	}

	private static bool ShouldEmitWrappedParagraph(ParagraphBlock paragraphBlock, LayoutBlock layoutBlock, IReadOnlyList<TextSegment> segments, float availableWidthTwips)
	{
		if (layoutBlock.LineStartIndex > 0)
		{
			return CanWrapParagraph(paragraphBlock, segments, availableWidthTwips);
		}

		return layoutBlock.LineHeights is { Count: > 1 }
			&& CanWrapParagraph(paragraphBlock, segments, availableWidthTwips);
	}

	private static bool CanWrapParagraph(ParagraphBlock paragraphBlock, IReadOnlyList<TextSegment> segments, float availableWidthTwips)
	{
		if (availableWidthTwips <= 0f || paragraphBlock.IsBiDi || paragraphBlock.NumberingId is not null)
		{
			return false;
		}

		for (var i = 0; i < segments.Count; i++)
		{
			if (segments[i].IsTab || segments[i].IsRtl)
			{
				return false;
			}
		}

		return CountApproximateWrapTokens(segments) <= MaxWrappedParagraphTokenCount;
	}

	private static void EmitWrappedParagraph(
		LayoutBlock layoutBlock,
		ParagraphBlock paragraphBlock,
		LayoutBlockPlacement placement,
		IReadOnlyList<TextSegment> logicalSegments,
		IRenderTarget target)
	{
		var effectiveAlignment = paragraphBlock.Alignment ?? ParagraphAlignment.Left;
		var wrappedLines = BuildWrappedLines(logicalSegments, placement.ContentWidthTwips, effectiveAlignment, paragraphBlock.Indentation);
		if (wrappedLines.Count == 0)
		{
			return;
		}

		var lineHeights = layoutBlock.LineHeights ?? [layoutBlock.HeightTwips];
		var lineStartIndex = layoutBlock.LineStartIndex;
		if (lineStartIndex >= wrappedLines.Count)
		{
			return;
		}

		var lineCount = Math.Min(lineHeights.Count, wrappedLines.Count - lineStartIndex);
		var currentY = placement.YTwips + layoutBlock.SpaceBefore;
		for (var lineIndex = 0; lineIndex < lineCount; lineIndex++)
		{
			var lineHeight = lineHeights[lineIndex];
			var lineSegments = wrappedLines[lineStartIndex + lineIndex].Segments;
			var lineFontSize = lineSegments.Count > 0 ? lineSegments[0].Font.SizePoints : 12f;
			var baselineY = currentY + ComputeBaselineOffsetTwips(lineFontSize, lineHeight);
			foreach (var segment in lineSegments)
			{
				DrawTextRun(target, new TextDrawing(segment.Text, segment.Font, segment.Brush, segment.HighlightFillColor, segment.HyperlinkUri), placement.XTwips + segment.XOffset, baselineY, segment.WidthTwips);

			}

			currentY += lineHeight;
		}
	}
}
