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
	private static float ResolveFontSizePoints(RunProperties? runProperties, float defaultSizePoints)
		=> int.TryParse(runProperties?.FontSize?.Val?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sizeHalfPoints)
			? sizeHalfPoints / 2f
			: defaultSizePoints;
	private static void AppendTextSegment(List<TextSegment> segments, string text, RenderFont font, RenderBrush brush, RenderColor? highlightFillColor, string? hyperlinkUri, bool isRtl = false)
	{
		if (segments.Count > 0
			&& !segments[^1].IsTab
			&& segments[^1].IsRtl == isRtl
			&& segments[^1].Font == font
			&& segments[^1].Brush == brush
			&& segments[^1].HighlightFillColor == highlightFillColor
			&& string.Equals(segments[^1].HyperlinkUri, hyperlinkUri, StringComparison.Ordinal))
		{
			segments[^1] = segments[^1] with { Text = segments[^1].Text + text };
		}
		else
		{
			segments.Add(new TextSegment(text, font, brush, hyperlinkUri, highlightFillColor, IsRtl: isRtl));
		}
	}

	private static readonly (HighlightColorValues Value, HighlightColor Color)[] HighlightColorTable =
	[
		(HighlightColorValues.Black, HighlightColor.Black),
		(HighlightColorValues.Blue, HighlightColor.Blue),
		(HighlightColorValues.Cyan, HighlightColor.Cyan),
		(HighlightColorValues.DarkBlue, HighlightColor.DarkBlue),
		(HighlightColorValues.DarkCyan, HighlightColor.DarkCyan),
		(HighlightColorValues.DarkGray, HighlightColor.DarkGray),
		(HighlightColorValues.DarkGreen, HighlightColor.DarkGreen),
		(HighlightColorValues.DarkMagenta, HighlightColor.DarkMagenta),
		(HighlightColorValues.DarkRed, HighlightColor.DarkRed),
		(HighlightColorValues.DarkYellow, HighlightColor.DarkYellow),
		(HighlightColorValues.Green, HighlightColor.Green),
		(HighlightColorValues.LightGray, HighlightColor.LightGray),
		(HighlightColorValues.Magenta, HighlightColor.Magenta),
		(HighlightColorValues.Red, HighlightColor.Red),
		(HighlightColorValues.White, HighlightColor.White),
		(HighlightColorValues.Yellow, HighlightColor.Yellow),
	];

	private static RenderColor? ResolveRunHighlightFillColor(RunProperties? runProperties)
	{
		var highlightColor = ResolveHighlightColor(runProperties?.Highlight?.Val?.Value);
		if (highlightColor == HighlightColor.None)
		{
			return null;
		}

		var highlightHex = HighlightColorMap.ToHexRgb(highlightColor);
		return TryParseRenderColor(highlightHex, out var highlightFillColor) ? highlightFillColor : null;
	}

	private static HighlightColor ResolveHighlightColor(HighlightColorValues? highlightValue)
	{
		foreach (var (value, color) in HighlightColorTable)
		{
			if (highlightValue == value)
			{
				return color;
			}
		}

		return HighlightColor.None;
	}
}
