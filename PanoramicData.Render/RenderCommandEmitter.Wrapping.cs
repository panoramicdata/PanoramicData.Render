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

	private static float ResolveGlueWidth(KnuthPlassGlue glue, bool isJustified, float ratio)
	{
		if (!isJustified)
		{
			return glue.Width;
		}

		return ratio >= 0f ? glue.Width + (ratio * glue.Stretch) : glue.Width + (ratio * glue.Shrink);
	}
	private static int CountWrappedLines(
		IReadOnlyList<TextSegment> segments,
		float lineWidthTwips,
		ParagraphAlignment alignment)
	{
		_ = alignment;

		if (segments.Count == 0 || lineWidthTwips <= 0f)
		{
			return 1;
		}

		var tokens = TokenizeWrappedSegments(segments);
		if (tokens.Count == 0)
		{
			return 1;
		}

		// Use a simple word-break simulation: accumulate token widths and break when the
		// effective wrap width (matching BuildWrappedLines' WordLikeWrapWidthRelaxation) is
		// exceeded. Non-whitespace tokens that exceed the line width on their own are placed
		// on their own line (Knuth-Plass overflow behaviour).
		var wrapWidthTwips = lineWidthTwips * WordLikeWrapWidthRelaxation;
		var lineCount = 1;
		var currentLineWidth = 0f;

		for (var i = 0; i < tokens.Count; i++)
		{
			var token = tokens[i];
			if (token.IsWhitespace)
			{
				currentLineWidth += token.WidthTwips;
				continue;
			}

			if (currentLineWidth + token.WidthTwips > wrapWidthTwips && currentLineWidth > 0f)
			{
				lineCount++;
				currentLineWidth = token.WidthTwips;
			}
			else
			{
				currentLineWidth += token.WidthTwips;
			}
		}

		return Math.Max(1, lineCount);
	}

	private static (List<KnuthPlassItem> Items, List<int> ItemTokenIndexes) BuildWrapItems(IReadOnlyList<WrappedToken> tokens)
	{
		var items = new List<KnuthPlassItem>(tokens.Count + 2);
		var itemTokenIndexes = new List<int>(tokens.Count + 2);
		for (var tokenIndex = 0; tokenIndex < tokens.Count; tokenIndex++)
		{
			var token = tokens[tokenIndex];
			if (token.IsWhitespace)
			{
				items.Add(new KnuthPlassGlue(
					token.WidthTwips,
					token.WidthTwips * DefaultWrapStretchRatio,
					token.WidthTwips * DefaultWrapShrinkRatio));
			}
			else
			{
				items.Add(new KnuthPlassBox(token.WidthTwips));
			}

			itemTokenIndexes.Add(tokenIndex);
		}

		items.Add(new KnuthPlassGlue(0f, float.PositiveInfinity, 0f));
		itemTokenIndexes.Add(-1);
		items.Add(new KnuthPlassPenalty(0f, KnuthPlassPenalty.NegativeInfinity));
		itemTokenIndexes.Add(-1);
		return (items, itemTokenIndexes);
	}

	private static IReadOnlyList<WrappedToken> TokenizeWrappedSegments(IReadOnlyList<TextSegment> segments)
	{
		var tokens = new List<WrappedToken>();
		var measurementEngine = new MeasurementEngine();
		for (var segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
		{
			var segment = segments[segmentIndex];
			var start = 0;
			while (start < segment.Text.Length)
			{
				var isWhitespace = char.IsWhiteSpace(segment.Text[start]);
				var end = start + 1;
				while (end < segment.Text.Length && char.IsWhiteSpace(segment.Text[end]) == isWhitespace)
				{
					end++;
				}

				var tokenText = segment.Text[start..end];
				tokens.Add(new WrappedToken(
					tokenText,
					MeasureWrappedTokenWidthTwips(tokenText, segment.Font, measurementEngine),
					segment.Font,
					segment.Brush,
					segment.HyperlinkUri,
					segment.HighlightFillColor,
					isWhitespace));
				start = end;
			}
		}

		return tokens;
	}

	private static float MeasureWrappedTokenWidthTwips(string text, RenderFont font, MeasurementEngine measurementEngine)
	{
		if (string.IsNullOrEmpty(text))
		{
			return 0f;
		}

		if (OperatingSystem.IsBrowser())
		{
			// Skia native font APIs are unavailable in WASM demo runtime.
			return EstimateTextWidthTwips(text, font.SizePoints);
		}

		using var typeface = SKTypeface.FromFamilyName(font.Family, FontStyleFromRenderFont(font)) ?? SKTypeface.Default;
		var advances = measurementEngine.MeasureGlyphAdvancesInTwips(typeface, font.SizePoints, text);
		var width = 0f;
		for (var i = 0; i < advances.Count; i++)
		{
			width += advances[i];
		}

		if (width <= 0f)
		{
			return EstimateTextWidthTwips(text, font.SizePoints);
		}

		return width;
	}

	private static SKFontStyle FontStyleFromRenderFont(RenderFont font)
	{
		return new SKFontStyle(
			font.IsBold ? (int)SKFontStyleWeight.Bold : (int)SKFontStyleWeight.Normal,
			(int)SKFontStyleWidth.Normal,
			font.IsItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);
	}

	private static int CountApproximateWrapTokens(IReadOnlyList<TextSegment> segments)
	{
		var tokenCount = 0;
		for (var segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
		{
			var text = segments[segmentIndex].Text;
			var start = 0;
			while (start < text.Length)
			{
				var isWhitespace = char.IsWhiteSpace(text[start]);
				var end = start + 1;
				while (end < text.Length && char.IsWhiteSpace(text[end]) == isWhitespace)
				{
					end++;
				}

				tokenCount++;
				if (tokenCount > MaxWrappedParagraphTokenCount)
				{
					return tokenCount;
				}

				start = end;
			}
		}

		return tokenCount;
	}

	private static float ComputeBaselineOffsetTwips(float fontSizePoints, float lineHeightTwips)
	{
		var ascentTwips = TwipConverter.PointsToTwips(fontSizePoints * BaselineAscentFactor);
		var maxBaselineTwips = lineHeightTwips * MaxBaselineLineHeightFactor;
		return MathF.Min(ascentTwips, maxBaselineTwips);
	}
}
