namespace PanoramicData.Render;

using System.Security;
using System.Text;

internal sealed partial class SvgRenderTarget : IRenderTarget
{
	private static bool IsOpenTypeFont(byte[] fontBytes)
		=> fontBytes.Length >= 4 && fontBytes[0] == 0x4F && fontBytes[1] == 0x54 && fontBytes[2] == 0x54 && fontBytes[3] == 0x4F;
	/// <inheritdoc/>
	public void DrawText(string text, float baselineXTwips, float baselineYTwips, RenderFont font, RenderBrush brush)
	{
		ArgumentNullException.ThrowIfNull(text);
		ArgumentNullException.ThrowIfNull(font.Family);
		ArgumentNullException.ThrowIfNull(brush);

		// Track font usage for embedding
		if (_options.EmbedFonts)
		{
			_usedFonts.Add(font.Family);
		}

		var fill = BrushToFill(brush);
		_content.Append("<text");
		AppendClipAttribute();
		_content.Append($" x=\"{Format(ScaleTwips(baselineXTwips))}\" y=\"{Format(ScaleTwips(baselineYTwips))}\" ");
		AppendTextStyle(font, fill);
		_content.Append($">{Escape(text)}</text>");
		if (font.IsUnderline)
		{
			AppendDecorationLine(
				baselineXTwips,
				baselineYTwips + 20f,
				baselineXTwips + EstimateTextWidthTwips(text, font.SizePoints),
				fill.ColorHex,
				fill.Opacity);
		}

		if (font.IsStrikethrough)
		{
			AppendDecorationLine(
				baselineXTwips,
				baselineYTwips - (font.SizePoints * 10f),
				baselineXTwips + EstimateTextWidthTwips(text, font.SizePoints),
				fill.ColorHex,
				fill.Opacity);
		}
	}

	/// <inheritdoc/>
	public void DrawLine(RenderPoint from, RenderPoint to, RenderStroke stroke)
	{
		var strokeSpec = StrokeToSvg(stroke);
		_content.Append("<line");
		AppendClipAttribute();
		_content.Append($" x1=\"{Format(ScaleTwips(from.XTwips))}\" y1=\"{Format(ScaleTwips(from.YTwips))}\" x2=\"{Format(ScaleTwips(to.XTwips))}\" y2=\"{Format(ScaleTwips(to.YTwips))}\"");
		_content.Append($" stroke=\"{strokeSpec.ColorHex}\" stroke-width=\"{Format(ScaleTwips(stroke.WidthTwips))}\"");
		AppendOpacity("stroke", strokeSpec.Opacity);
		_content.Append(" />");
	}

	/// <inheritdoc/>
	public void DrawRect(RenderRect rect, RenderBrush? fill, RenderStroke? stroke)
	{
		_content.Append("<rect");
		AppendClipAttribute();
		_content.Append($" x=\"{Format(ScaleTwips(rect.XTwips))}\" y=\"{Format(ScaleTwips(rect.YTwips))}\" width=\"{Format(ScaleTwips(rect.WidthTwips))}\" height=\"{Format(ScaleTwips(rect.HeightTwips))}\"");
		AppendFillStroke(fill, stroke);
		_content.Append(" />");
	}

	/// <inheritdoc/>
	public void DrawImage(ImageData image, RenderRect rect)
	{
		ArgumentNullException.ThrowIfNull(image);

		var href = ResolveImageHref(image);
		_content.Append("<image");
		AppendClipAttribute();
		_content.Append($" x=\"{Format(ScaleTwips(rect.XTwips))}\" y=\"{Format(ScaleTwips(rect.YTwips))}\" width=\"{Format(ScaleTwips(rect.WidthTwips))}\" height=\"{Format(ScaleTwips(rect.HeightTwips))}\"");
		_content.Append($" xlink:href=\"{Escape(href)}\" />");
	}

	/// <inheritdoc/>
	public void DrawPath(string pathData, RenderBrush? fill, RenderStroke? stroke)
	{
		ArgumentNullException.ThrowIfNull(pathData);

		_content.Append("<path");
		AppendClipAttribute();
		_content.Append($" d=\"{Escape(pathData)}\"");
		AppendFillStroke(fill, stroke);
		_content.Append(" />");
	}

	/// <inheritdoc/>
	public void PushClip(RenderRect clipRect)
	{
		_clipCounter++;
		var clipId = $"clip{_clipCounter}";
		_defs.Append($"<clipPath id=\"{clipId}\">");
		_defs.Append($"<rect x=\"{Format(ScaleTwips(clipRect.XTwips))}\" y=\"{Format(ScaleTwips(clipRect.YTwips))}\" width=\"{Format(ScaleTwips(clipRect.WidthTwips))}\" height=\"{Format(ScaleTwips(clipRect.HeightTwips))}\" />");
		_defs.Append("</clipPath>");
		_clipStack.Push(clipId);
	}

	/// <inheritdoc/>
	public void PopClip()
	{
		if (_clipStack.Count == 0)
		{
			return;
		}

		_clipStack.Pop();
	}

	/// <inheritdoc/>
	public void SetHyperlink(RenderRect rect, string uri)
	{
		ArgumentNullException.ThrowIfNull(uri);

		_content.Append($"<a xlink:href=\"{Escape(uri)}\">");
		_content.Append($"<rect x=\"{Format(ScaleTwips(rect.XTwips))}\" y=\"{Format(ScaleTwips(rect.YTwips))}\" width=\"{Format(ScaleTwips(rect.WidthTwips))}\" height=\"{Format(ScaleTwips(rect.HeightTwips))}\" fill=\"none\" stroke=\"none\" />");
		_content.Append("</a>");
	}

	/// <inheritdoc/>
	public void SetNamedDestination(string name, float xTwips, float yTwips)
	{
		ArgumentNullException.ThrowIfNull(name);

		_content.Append($"<a id=\"{Escape(name)}\"><rect x=\"{Format(ScaleTwips(xTwips))}\" y=\"{Format(ScaleTwips(yTwips))}\" width=\"0\" height=\"0\" /></a>");
	}
}
