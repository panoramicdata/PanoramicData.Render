namespace PanoramicData.Render;

using System.Security;
using System.Text;

/// <summary>
/// SVG render target that converts drawing commands to SVG elements.
/// </summary>
internal sealed class SvgRenderTarget : IRenderTarget
{
	private readonly float _pageWidthTwips;
	private readonly float _pageHeightTwips;
	private readonly StringBuilder _defs = new();
	private readonly StringBuilder _content = new();
	private readonly Stack<string> _clipStack = new();
	private readonly RenderOptions _options;
	private readonly HashSet<string> _usedFonts = new(StringComparer.OrdinalIgnoreCase);
	private int _clipCounter;
	private const float AverageGlyphWidthFactor = 10f;

	/// <summary>
	/// Initializes a new instance of the <see cref="SvgRenderTarget"/> class.
	/// </summary>
	/// <param name="pageWidthTwips">The page width in twips.</param>
	/// <param name="pageHeightTwips">The page height in twips.</param>
	/// <param name="options">Rendering options.</param>
	public SvgRenderTarget(float pageWidthTwips, float pageHeightTwips, RenderOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);

		if (pageWidthTwips <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(pageWidthTwips));
		}

		if (pageHeightTwips <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(pageHeightTwips));
		}

		_pageWidthTwips = pageWidthTwips;
		_pageHeightTwips = pageHeightTwips;
		_options = options;
	}

	/// <summary>
	/// Builds the SVG markup from emitted drawing commands.
	/// </summary>
	/// <returns>The SVG markup string.</returns>
	public string BuildSvg()
	{
		var svg = new StringBuilder();
		svg.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" ");
		svg.Append($"viewBox=\"0 0 {Format(_pageWidthTwips)} {Format(_pageHeightTwips)}\" ");
		svg.Append($"width=\"{Format(_pageWidthTwips)}\" height=\"{Format(_pageHeightTwips)}\">");

		// Build style and defs sections
		var defsBuilder = new StringBuilder(_defs.ToString());
		if (_options.EmbedFonts && _usedFonts.Count > 0)
		{
			AppendEmbeddedFonts(defsBuilder);
		}

		if (defsBuilder.Length > 0)
		{
			svg.Append("<defs>");
			svg.Append(defsBuilder);
			svg.Append("</defs>");
		}

		svg.Append(_content);
		svg.Append("</svg>");
		return svg.ToString();
	}

	private void AppendEmbeddedFonts(StringBuilder defsBuilder)
	{
		defsBuilder.Append("<style>");
		foreach (var familyName in _usedFonts.OrderBy(x => x))
		{
			var base64 = FontEmbedder.GetEmbeddedFontData(familyName, _options.FontDirectories);
			if (base64 is not null)
			{
				defsBuilder.Append($"@font-face {{font-family: \"{Escape(familyName)}\"; src: url('data:font/ttf;base64,{base64}');}}");
			}
		}

		defsBuilder.Append("</style>");
	}

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
		_content.Append($" x=\"{Format(baselineXTwips)}\" y=\"{Format(baselineYTwips)}\" ");
		_content.Append($"font-family=\"{Escape(font.Family)}\" font-size=\"{Format(font.SizePoints)}pt\"");
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
		_content.Append($" x1=\"{Format(from.XTwips)}\" y1=\"{Format(from.YTwips)}\" x2=\"{Format(to.XTwips)}\" y2=\"{Format(to.YTwips)}\"");
		_content.Append($" stroke=\"{strokeSpec.ColorHex}\" stroke-width=\"{Format(stroke.WidthTwips)}\"");
		if (strokeSpec.Opacity is not null)
		{
			_content.Append($" stroke-opacity=\"{Format(strokeSpec.Opacity.Value)}\"");
		}

		_content.Append(" />");
	}

	/// <inheritdoc/>
	public void DrawRect(RenderRect rect, RenderBrush? fill, RenderStroke? stroke)
	{
		_content.Append("<rect");
		AppendClipAttribute();
		_content.Append($" x=\"{Format(rect.XTwips)}\" y=\"{Format(rect.YTwips)}\" width=\"{Format(rect.WidthTwips)}\" height=\"{Format(rect.HeightTwips)}\"");
		AppendFillStroke(fill, stroke);
		_content.Append(" />");
	}

	/// <inheritdoc/>
	public void DrawImage(ImageData image, RenderRect rect)
	{
		ArgumentNullException.ThrowIfNull(image);

		var base64 = Convert.ToBase64String(image.Data);
		var href = $"data:{image.ContentType};base64,{base64}";
		_content.Append("<image");
		AppendClipAttribute();
		_content.Append($" x=\"{Format(rect.XTwips)}\" y=\"{Format(rect.YTwips)}\" width=\"{Format(rect.WidthTwips)}\" height=\"{Format(rect.HeightTwips)}\"");
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
		_defs.Append($"<rect x=\"{Format(clipRect.XTwips)}\" y=\"{Format(clipRect.YTwips)}\" width=\"{Format(clipRect.WidthTwips)}\" height=\"{Format(clipRect.HeightTwips)}\" />");
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
		_content.Append($"<rect x=\"{Format(rect.XTwips)}\" y=\"{Format(rect.YTwips)}\" width=\"{Format(rect.WidthTwips)}\" height=\"{Format(rect.HeightTwips)}\" fill=\"none\" stroke=\"none\" />");
		_content.Append("</a>");
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
			_content.Append($" stroke=\"{strokeSpec.ColorHex}\" stroke-width=\"{Format(stroke.Value.WidthTwips)}\"");
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
		_content.Append($" x1=\"{Format(x1)}\" y1=\"{Format(y)}\" x2=\"{Format(x2)}\" y2=\"{Format(y)}\"");
		_content.Append($" stroke=\"{colorHex}\" stroke-width=\"20\"");
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

	private static float EstimateTextWidthTwips(string text, float sizePoints)
	{
		return text.Length * sizePoints * AverageGlyphWidthFactor;
	}
}
