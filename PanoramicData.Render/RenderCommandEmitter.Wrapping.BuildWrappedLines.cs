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
	private static IReadOnlyList<WrappedLine> BuildWrappedLines(
		IReadOnlyList<TextSegment> segments,
		float lineWidthTwips,
		ParagraphAlignment alignment,
		ParagraphIndentation indentation = default)
	{
		if (segments.Count == 0 || lineWidthTwips <= 0f)
		{
			return [];
		}

		var tokens = TokenizeWrappedSegments(segments);
		if (tokens.Count == 0)
		{
			return [];
		}

		var (items, itemTokenIndexes) = BuildWrapItems(tokens);
		var plan = new WrapPlan(tokens, items, itemTokenIndexes);

		var wrapWidthTwips = lineWidthTwips * WordLikeWrapWidthRelaxation;
		var lineBreaks = KnuthPlassAlgorithm.FindBreaks(items, wrapWidthTwips);
		var wrappedLines = new List<WrappedLine>(lineBreaks.Count);

		for (var lineIndex = 0; lineIndex < lineBreaks.Count; lineIndex++)
		{
			var isLastLine = lineIndex == lineBreaks.Count - 1;
			var isFirstLine = lineIndex == 0;
			var lineSegments = BuildWrappedLineSegments(plan, lineBreaks[lineIndex], wrapWidthTwips, alignment, indentation, isLastLine, isFirstLine);

			wrappedLines.Add(new WrappedLine(lineSegments));
		}

		return wrappedLines;
	}

	private static List<WrappedTextSegment> BuildWrappedLineSegments(WrapPlan plan, KnuthPlassLine line, float lineWidthTwips, ParagraphAlignment alignment, ParagraphIndentation indentation, bool isLastLine, bool isFirstLine)
	{
		var ratio = line.AdjustmentRatio;
		var effectiveAlignment = alignment == ParagraphAlignment.Justified && (isLastLine || ratio > MaxWordLikeJustificationRatio)
			? ParagraphAlignment.Left
			: alignment;

		var leftIndent = isFirstLine
			? indentation.GetFirstLineLeftIndent()
			: indentation.GetSubsequentLineLeftIndent();
		var effectiveLineWidth = lineWidthTwips - leftIndent - indentation.Right;
		if (effectiveLineWidth <= 0f)
		{
			effectiveLineWidth = 1f;
		}

		var contentWidth = ComputeLineContentWidth(plan.Items, line);
		var alignmentOffset = effectiveAlignment switch
		{
			ParagraphAlignment.Center => Math.Max(0f, (effectiveLineWidth - contentWidth) / 2f),
			ParagraphAlignment.Right => Math.Max(0f, effectiveLineWidth - contentWidth),
			_ => 0f,
		};

		return BuildLineSegmentsFromItems(plan, line, leftIndent + alignmentOffset, effectiveAlignment == ParagraphAlignment.Justified, ratio);
	}

	private static float ComputeLineContentWidth(IReadOnlyList<KnuthPlassItem> items, KnuthPlassLine line)
	{
		var contentWidth = 0f;
		for (var i = line.StartIndex; i < line.EndIndex && i < items.Count; i++)
		{
			contentWidth += items[i].Width;
		}

		if (line.EndIndex >= 0 && line.EndIndex < items.Count
			&& items[line.EndIndex] is KnuthPlassPenalty endPenalty)
		{
			contentWidth += endPenalty.Width;
		}

		return contentWidth;
	}

	private static List<WrappedTextSegment> BuildLineSegmentsFromItems(WrapPlan plan, KnuthPlassLine line, float startX, bool isJustified, float ratio)
	{
		var (tokens, items, itemTokenIndexes) = plan;
		var x = startX;
		var lineSegments = new List<WrappedTextSegment>();
		for (var i = line.StartIndex; i < line.EndIndex && i < items.Count; i++)
		{
			var tokenIndex = itemTokenIndexes[i];
			var itemWidth = items[i] switch
			{
				KnuthPlassBox box => box.Width,
				KnuthPlassGlue glue => ResolveGlueWidth(glue, isJustified, ratio),
				_ => (float?)null,
			};

			if (itemWidth is not { } width)
			{
				continue;
			}

			if (tokenIndex >= 0)
			{
				var token = tokens[tokenIndex];
				lineSegments.Add(new WrappedTextSegment(token.Text, x, width, token.Font, token.Brush, token.HyperlinkUri, token.HighlightFillColor));
			}

			x += width;
		}

		return lineSegments;
	}
}
