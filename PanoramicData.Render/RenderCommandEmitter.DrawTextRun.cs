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
	private static void DrawTextRun(IRenderTarget target, TextDrawing text, float x, float baselineY, float widthTwips)
	{
		if (text.HighlightFillColor is { } highlightFillColor)
		{
			var highlightTopOffset = EstimateHighlightTopOffsetTwips(text.Font.SizePoints);
			var highlightHeight = EstimateHighlightHeightTwips(text.Font.SizePoints);
			target.DrawRect(new RenderRect(x, baselineY - highlightTopOffset, widthTwips, highlightHeight), new SolidRenderBrush(highlightFillColor), null);
		}

		target.DrawText(text.Text, x, baselineY, text.Font, text.Brush);
		if (!string.IsNullOrWhiteSpace(text.HyperlinkUri))
		{
			var textHeight = EstimateTextHeightTwips(text.Font.SizePoints);
			target.SetHyperlink(new RenderRect(x, baselineY - textHeight, widthTwips, textHeight), text.HyperlinkUri);
		}
	}
}
