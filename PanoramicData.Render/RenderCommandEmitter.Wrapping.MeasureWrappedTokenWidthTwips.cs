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
