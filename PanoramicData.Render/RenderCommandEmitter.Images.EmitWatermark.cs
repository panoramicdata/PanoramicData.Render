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
	private static void EmitWatermark(LayoutPage page, WatermarkInfo watermark, IRenderTarget target)
	{
		var centerX = watermark.IsHorizontallyCentered
			? page.Section.PageWidth / 2f
			: page.Section.MarginLeft;
		var centerY = watermark.IsVerticallyCentered
			? page.Section.PageHeight / 2f
			: page.Section.MarginTop;

		if (watermark.Kind == WatermarkKind.Text && !string.IsNullOrWhiteSpace(watermark.Text))
		{
			EmitTextWatermark(watermark, centerX, centerY, target);
		}
		else if (watermark.Kind == WatermarkKind.Image && watermark.ResolvedImageData is not null)
		{
			target.DrawRotatedImage(
				watermark.ResolvedImageData,
				centerX,
				centerY,
				watermark.WidthTwips,
				watermark.HeightTwips,
				watermark.RotationDegrees,
				watermark.Opacity);
		}
	}

	private static void EmitTextWatermark(WatermarkInfo watermark, float centerX, float centerY, IRenderTarget target)
	{
		var opacity = (byte)Math.Clamp(watermark.Opacity * 255f, 0, 255);
		var color = ResolveWatermarkColor(watermark.FillColor, opacity);
		var brush = new SolidRenderBrush(color);
		var fontFamily = string.IsNullOrWhiteSpace(watermark.FontFamily) ? "Calibri" : watermark.FontFamily;
		var fontSize = EstimateWatermarkFontSize(watermark);
		var font = new RenderFont(fontFamily, fontSize);

		target.DrawRotatedText(watermark.Text!, centerX, centerY, watermark.RotationDegrees, font, brush);
	}

	private static RenderColor ResolveWatermarkColor(string? fillColor, byte opacity)
	{
		if (string.IsNullOrWhiteSpace(fillColor))
		{
			return new RenderColor(192, 192, 192, opacity);
		}

		if (fillColor.StartsWith('#') && fillColor.Length == 7)
		{
			var r = byte.Parse(fillColor.AsSpan(1, 2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture);
			var g = byte.Parse(fillColor.AsSpan(3, 2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture);
			var b = byte.Parse(fillColor.AsSpan(5, 2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture);
			return new RenderColor(r, g, b, opacity);
		}

		return fillColor.ToUpperInvariant() switch
		{
			"SILVER" => new RenderColor(192, 192, 192, opacity),
			"GRAY" or "GREY" => new RenderColor(128, 128, 128, opacity),
			"RED" => new RenderColor(255, 0, 0, opacity),
			"BLUE" => new RenderColor(0, 0, 255, opacity),
			"GREEN" => new RenderColor(0, 128, 0, opacity),
			"BLACK" => new RenderColor(0, 0, 0, opacity),
			"WHITE" => new RenderColor(255, 255, 255, opacity),
			_ => new RenderColor(192, 192, 192, opacity)
		};
	}

	private static float EstimateWatermarkFontSize(WatermarkInfo watermark)
	{
		if (watermark.WidthTwips <= 0 || string.IsNullOrEmpty(watermark.Text))
		{
			return 72f;
		}

		// Estimate: width covers the text at approximately 0.6 * fontSize per character
		var charCount = watermark.Text.Length;
		var estimatedSizePoints = TwipConverter.TwipsToPoints(watermark.WidthTwips) / (charCount * 0.6f);
		return Math.Clamp(estimatedSizePoints, 8f, 200f);
	}
}
