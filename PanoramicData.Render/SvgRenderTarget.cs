namespace PanoramicData.Render;

using System.Security;
using System.Text;

/// <summary>
/// SVG render target that converts drawing commands to SVG elements.
/// </summary>
internal sealed partial class SvgRenderTarget : IRenderTarget
{
	private readonly float _pageWidthTwips;
	private readonly float _pageHeightTwips;
	private readonly StringBuilder _defs = new();
	private readonly StringBuilder _content = new();
	private readonly Stack<string> _clipStack = new();
	private readonly RenderOptions _options;
	private readonly HashSet<string> _usedFonts = new(StringComparer.OrdinalIgnoreCase);
	private readonly float _pixelsPerTwip;
	private int _externalImageCounter;
	private int _clipCounter;
	private const float AverageGlyphWidthFactor = 10f;
	private const float TwipsPerInch = 1440f;

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
		if (_options.TargetDpi <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(options.TargetDpi));
		}

		_pixelsPerTwip = (float)(_options.TargetDpi / TwipsPerInch);
	}

	/// <summary>
	/// Builds the SVG markup from emitted drawing commands.
	/// </summary>
	/// <returns>The SVG markup string.</returns>
	public string BuildSvg()
	{
		var pageWidthPx = ScaleTwips(_pageWidthTwips);
		var pageHeightPx = ScaleTwips(_pageHeightTwips);
		var svg = new StringBuilder();
		svg.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" ");
		svg.Append($"viewBox=\"0 0 {Format(pageWidthPx)} {Format(pageHeightPx)}\" ");
		svg.Append($"width=\"{Format(pageWidthPx)}\" height=\"{Format(pageHeightPx)}\">");

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
			AppendFontFace(defsBuilder, familyName, bold: false, italic: false);
			AppendFontFace(defsBuilder, familyName, bold: true, italic: false);
			AppendFontFace(defsBuilder, familyName, bold: false, italic: true);
			AppendFontFace(defsBuilder, familyName, bold: true, italic: true);
		}

		defsBuilder.Append("</style>");
	}

	private void AppendFontFace(StringBuilder defsBuilder, string familyName, bool bold, bool italic)
	{
		if (!TryGetFontData(familyName, bold, italic, out var base64, out var mimeType))
		{
			return;
		}

		defsBuilder.Append("@font-face {font-family: \"");
		defsBuilder.Append(Escape(familyName));
		defsBuilder.Append("\";");
		if (bold)
		{
			defsBuilder.Append(" font-weight: bold;");
		}

		if (italic)
		{
			defsBuilder.Append(" font-style: italic;");
		}

		defsBuilder.Append(" src: url('data:");
		defsBuilder.Append(mimeType);
		defsBuilder.Append(";base64,");
		defsBuilder.Append(base64);
		defsBuilder.Append("');}");
	}

	private bool TryGetFontData(string familyName, bool bold, bool italic, out string base64, out string mimeType)
	{
		var styleSuffix = (bold, italic) switch
		{
			(true, true) => " BoldItalic",
			(true, false) => " Bold",
			(false, true) => " Italic",
			_ => string.Empty
		};

		if (_options.ExtractedFontData.TryGetValue(familyName + styleSuffix, out var fontBytes))
		{
			base64 = Convert.ToBase64String(fontBytes);
			mimeType = IsOpenTypeFont(fontBytes) ? "font/otf" : "font/ttf";
			return true;
		}

		mimeType = "font/ttf";

		// For the regular variant, also try filesystem lookup
		var embedded = !bold && !italic ? FontEmbedder.GetEmbeddedFontData(familyName, _options.FontDirectories) : null;
		base64 = embedded ?? string.Empty;
		return embedded is not null;
	}

	// OpenType fonts with CFF outlines start with the "OTTO" magic bytes.
}
