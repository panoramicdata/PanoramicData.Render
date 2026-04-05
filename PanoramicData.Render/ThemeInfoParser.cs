namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;

/// <summary>
/// Parses theme fonts and color scheme information from the OpenXML theme part.
/// </summary>
internal static class ThemeInfoParser
{
	/// <summary>
	/// Parses theme information from the given theme part.
	/// </summary>
	/// <param name="themePart">The theme part, or <see langword="null"/> if absent.</param>
	/// <returns>A <see cref="ThemeInfo"/> with parsed fonts and colors.</returns>
	public static ThemeInfo Parse(ThemePart? themePart)
	{
		var themeElements = themePart?.Theme?.ThemeElements;
		var fontScheme = themeElements?.FontScheme;
		var colors = ParseColors(themeElements?.ColorScheme);

		return new ThemeInfo
		{
			MajorFont = ParseFontInfo(fontScheme?.MajorFont),
			MinorFont = ParseFontInfo(fontScheme?.MinorFont),
			Colors = colors
		};
	}

	private static ThemeFontInfo ParseFontInfo(OpenXmlCompositeElement? fontElement)
	{
		if (fontElement is null)
		{
			return new ThemeFontInfo
			{
				Latin = null,
				EastAsian = null,
				ComplexScript = null,
				ScriptFonts = new Dictionary<string, string>()
			};
		}

		var scriptFonts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (var supplemental in fontElement.Elements<SupplementalFont>())
		{
			var script = supplemental.Script?.Value;
			var typeface = supplemental.Typeface?.Value;
			if (!string.IsNullOrWhiteSpace(script) && !string.IsNullOrWhiteSpace(typeface))
			{
				scriptFonts[script] = typeface;
			}
		}

		return new ThemeFontInfo
		{
			Latin = fontElement.GetFirstChild<LatinFont>()?.Typeface?.Value,
			EastAsian = fontElement.GetFirstChild<EastAsianFont>()?.Typeface?.Value,
			ComplexScript = fontElement.GetFirstChild<ComplexScriptFont>()?.Typeface?.Value,
			ScriptFonts = scriptFonts
		};
	}

	private static IReadOnlyDictionary<string, string> ParseColors(ColorScheme? colorScheme)
	{
		if (colorScheme is null)
		{
			return new Dictionary<string, string>();
		}

		var colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		AddColor(colors, "dk1", colorScheme.GetFirstChild<Dark1Color>());
		AddColor(colors, "lt1", colorScheme.GetFirstChild<Light1Color>());
		AddColor(colors, "dk2", colorScheme.GetFirstChild<Dark2Color>());
		AddColor(colors, "lt2", colorScheme.GetFirstChild<Light2Color>());
		AddColor(colors, "accent1", colorScheme.GetFirstChild<Accent1Color>());
		AddColor(colors, "accent2", colorScheme.GetFirstChild<Accent2Color>());
		AddColor(colors, "accent3", colorScheme.GetFirstChild<Accent3Color>());
		AddColor(colors, "accent4", colorScheme.GetFirstChild<Accent4Color>());
		AddColor(colors, "accent5", colorScheme.GetFirstChild<Accent5Color>());
		AddColor(colors, "accent6", colorScheme.GetFirstChild<Accent6Color>());
		AddColor(colors, "hlink", colorScheme.GetFirstChild<Hyperlink>());
		AddColor(colors, "folHlink", colorScheme.GetFirstChild<FollowedHyperlinkColor>());
		return colors;
	}

	private static void AddColor(IDictionary<string, string> colors, string key, OpenXmlCompositeElement? colorElement)
	{
		var color = ExtractColor(colorElement);
		if (!string.IsNullOrWhiteSpace(color))
		{
			colors[key] = color;
		}
	}

	private static string? ExtractColor(OpenXmlCompositeElement? colorElement)
	{
		if (colorElement is null)
		{
			return null;
		}

		var rgb = colorElement.GetFirstChild<RgbColorModelHex>()?.Val?.Value;
		if (!string.IsNullOrWhiteSpace(rgb))
		{
			return rgb;
		}

		var sys = colorElement.GetFirstChild<SystemColor>();
		if (sys is null)
		{
			return null;
		}

		var lastColor = sys.LastColor?.Value;
		if (!string.IsNullOrWhiteSpace(lastColor))
		{
			return lastColor;
		}

		return sys.Val?.InnerText;
	}
}
