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
}
