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
	private static ParagraphShading? GetVisibleShading(Shading? shading)
	{
		if (shading is null)
		{
			return null;
		}

		var parsed = TableParser.ParseShading(shading);
		return parsed.HasVisibleShading ? parsed : null;
	}

	private static ParagraphShading ResolveParagraphShading(Styles? styles, string? styleId, Paragraph paragraph)
	{
		// Direct paragraph shading takes precedence over style-inherited shading.
		if (GetVisibleShading(paragraph.ParagraphProperties?.Shading) is { } direct)
		{
			return direct;
		}

		return styles is null || styleId is null
			? ParagraphShading.None
			: FindInheritedShading(styles, styleId);
	}

	// Walks the paragraph style inheritance chain to find shading.
	private static ParagraphShading FindInheritedShading(Styles styles, string styleId)
	{
		var currentId = styleId;
		var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		while (!string.IsNullOrEmpty(currentId) && visited.Add(currentId))
		{
			var style = FindStyle(styles, currentId, StyleValues.Paragraph);
			if (style is null)
			{
				break;
			}

			if (GetVisibleShading(style.StyleParagraphProperties?.GetFirstChild<Shading>()) is { } styleShading)
			{
				return styleShading;
			}

			currentId = style.BasedOn?.Val?.Value;
		}

		return ParagraphShading.None;
	}
	private static void EmitParagraphBorders(ParagraphBorders borders, float xTwips, float yTopTwips, float heightTwips, float widthTwips, IRenderTarget target)
	{
		if (borders.Bottom is { IsVisible: true } bottom)
		{
			var strokeColor = TryParseRenderColor(bottom.Color, out var color) ? color : DefaultTextColor;
			var strokeWidth = Math.Max(bottom.GetWidthTwips(), 1f);
			var y = yTopTwips + heightTwips + bottom.GetSpacingTwips();
			target.DrawLine(new RenderPoint(xTwips, y), new RenderPoint(xTwips + widthTwips, y), new RenderStroke(strokeColor, strokeWidth));
		}

		if (borders.Top is { IsVisible: true } top)
		{
			var strokeColor = TryParseRenderColor(top.Color, out var color) ? color : DefaultTextColor;
			var strokeWidth = Math.Max(top.GetWidthTwips(), 1f);
			var y = yTopTwips - top.GetSpacingTwips();
			target.DrawLine(new RenderPoint(xTwips, y), new RenderPoint(xTwips + widthTwips, y), new RenderStroke(strokeColor, strokeWidth));
		}
	}
}
