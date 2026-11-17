namespace PanoramicData.Render;

using System.Security;
using System.Text;

internal sealed partial class SvgRenderTarget : IRenderTarget
{
	/// <inheritdoc/>
	public void DrawRotatedText(string text, float centerXTwips, float centerYTwips, float rotationDegrees, RenderFont font, RenderBrush brush)
	{
		ArgumentNullException.ThrowIfNull(text);
		ArgumentNullException.ThrowIfNull(font.Family);
		ArgumentNullException.ThrowIfNull(brush);

		var cx = Format(ScaleTwips(centerXTwips));
		var cy = Format(ScaleTwips(centerYTwips));
		var fill = BrushToFill(brush);

		_content.Append($"<text x=\"{cx}\" y=\"{cy}\" font-family=\"{Escape(font.Family)}\" font-size=\"{Format(font.SizePoints)}pt\"");
		if (font.IsBold)
		{
			_content.Append(" font-weight=\"bold\"");
		}

		if (font.IsItalic)
		{
			_content.Append(" font-style=\"italic\"");
		}

		_content.Append($" fill=\"{fill.ColorHex}\"");
		if (fill.Opacity is not null)
		{
			_content.Append($" fill-opacity=\"{Format(fill.Opacity.Value)}\"");
		}

		_content.Append($" text-anchor=\"middle\" dominant-baseline=\"central\" transform=\"rotate({Format(rotationDegrees)},{cx},{cy})\">");
		_content.Append(Escape(text));
		_content.Append("</text>");

		if (!string.IsNullOrWhiteSpace(font.Family))
		{
			_usedFonts.Add(font.Family);
		}
	}

	/// <inheritdoc/>
	public void DrawRotatedImage(ImageData image, float centerXTwips, float centerYTwips, float widthTwips, float heightTwips, float rotationDegrees, float opacity)
	{
		ArgumentNullException.ThrowIfNull(image);

		var href = ResolveImageHref(image);
		var cx = Format(ScaleTwips(centerXTwips));
		var cy = Format(ScaleTwips(centerYTwips));
		var w = Format(ScaleTwips(widthTwips));
		var h = Format(ScaleTwips(heightTwips));
		var x = Format(ScaleTwips(centerXTwips - widthTwips / 2f));
		var y = Format(ScaleTwips(centerYTwips - heightTwips / 2f));

		_content.Append($"<image x=\"{x}\" y=\"{y}\" width=\"{w}\" height=\"{h}\" xlink:href=\"{Escape(href)}\"");
		if (opacity < 1f)
		{
			_content.Append($" opacity=\"{Format(opacity)}\"");
		}

		if (Math.Abs(rotationDegrees) > 0.001f)
		{
			_content.Append($" transform=\"rotate({Format(rotationDegrees)},{cx},{cy})\"");
		}

		_content.Append(" />");
	}

	private void AppendClipAttribute()
	{
		if (_clipStack.Count == 0)
		{
			return;
		}

		_content.Append($" clip-path=\"url(#{_clipStack.Peek()})\"");
	}

	private void AppendFillStroke(RenderBrush? fill, RenderStroke? stroke)
	{
		if (fill is null)
		{
			_content.Append(" fill=\"none\"");
		}
		else
		{
			var fillSpec = BrushToFill(fill);
			_content.Append($" fill=\"{fillSpec.ColorHex}\"");
			if (fillSpec.Opacity is not null)
			{
				_content.Append($" fill-opacity=\"{Format(fillSpec.Opacity.Value)}\"");
			}
		}

		if (stroke is null)
		{
			_content.Append(" stroke=\"none\"");
		}
		else
		{
			var strokeSpec = StrokeToSvg(stroke.Value);
			_content.Append($" stroke=\"{strokeSpec.ColorHex}\" stroke-width=\"{Format(ScaleTwips(stroke.Value.WidthTwips))}\"");
			if (strokeSpec.Opacity is not null)
			{
				_content.Append($" stroke-opacity=\"{Format(strokeSpec.Opacity.Value)}\"");
			}
		}
	}

	private void AppendDecorationLine(float x1, float y, float x2, string colorHex, float? opacity)
	{
		_content.Append("<line");
		AppendClipAttribute();
		_content.Append($" x1=\"{Format(ScaleTwips(x1))}\" y1=\"{Format(ScaleTwips(y))}\" x2=\"{Format(ScaleTwips(x2))}\" y2=\"{Format(ScaleTwips(y))}\"");
		_content.Append($" stroke=\"{colorHex}\" stroke-width=\"{Format(ScaleTwips(20f))}\"");
		if (opacity is not null)
		{
			_content.Append($" stroke-opacity=\"{Format(opacity.Value)}\"");
		}

		_content.Append(" />");
	}

	private static (string ColorHex, float? Opacity) BrushToFill(RenderBrush brush)
	{
		return brush switch
		{
			SolidRenderBrush solid => ColorToSvg(solid.Color),
			LinearGradientRenderBrush gradient => ColorToSvg(gradient.StartColor),
			_ => ("#000000", null)
		};
	}

	private static (string ColorHex, float? Opacity) StrokeToSvg(RenderStroke stroke)
	{
		return ColorToSvg(stroke.Color);
	}

	private static (string ColorHex, float? Opacity) ColorToSvg(RenderColor color)
	{
		var hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
		if (color.A == 255)
		{
			return (hex, null);
		}

		return (hex, color.A / 255f);
	}

	private static string Escape(string value)
	{
		return SecurityElement.Escape(value) ?? string.Empty;
	}

	private static string Format(float value)
	{
		return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
	}
}
