namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class EffectiveFormattingResolver
{
	private static bool TryMergeLeaf(OpenXmlElement? existing, OpenXmlElement child)
	{
		if (existing is null || child.HasChildren || existing.HasChildren)
		{
			return false;
		}

		foreach (var attr in child.GetAttributes())
		{
			existing.SetAttribute(attr);
		}

		return true;
	}

	// Composite elements with children (for example numPr) merge recursively so inherited children survive.
	private static bool TryMergeComposite(OpenXmlElement? existing, OpenXmlElement child)
	{
		if (existing is not OpenXmlCompositeElement existingComposite
			|| child is not OpenXmlCompositeElement childComposite
			|| !existingComposite.HasChildren
			|| !childComposite.HasChildren)
		{
			return false;
		}

		Merge(existingComposite, childComposite);
		return true;
	}
	/// <summary>
	/// Resolves theme font references to concrete font names on the materialized RunProperties.
	/// Per OOXML spec, the concrete value (e.g. <c>ascii</c>) takes precedence; theme values
	/// are only used as a fallback when the concrete attribute is absent.
	/// </summary>
	private static void ResolveThemeFonts(ThemeInfo themeInfo, RunProperties runProperties)
	{
		var fonts = runProperties.GetFirstChild<RunFonts>();
		if (fonts is null)
		{
			return;
		}

		if (NeedsThemeFont(fonts.Ascii, fonts.AsciiTheme))
		{
			fonts.Ascii = ResolveThemeFont(themeInfo, fonts.AsciiTheme!);
		}

		if (NeedsThemeFont(fonts.HighAnsi, fonts.HighAnsiTheme))
		{
			fonts.HighAnsi = ResolveThemeFont(themeInfo, fonts.HighAnsiTheme!);
		}

		if (NeedsThemeFont(fonts.EastAsia, fonts.EastAsiaTheme))
		{
			fonts.EastAsia = ResolveThemeFont(themeInfo, fonts.EastAsiaTheme!);
		}

		if (NeedsThemeFont(fonts.ComplexScript, fonts.ComplexScriptTheme))
		{
			fonts.ComplexScript = ResolveThemeFont(themeInfo, fonts.ComplexScriptTheme!);
		}
	}

	private static bool NeedsThemeFont(StringValue? concrete, EnumValue<ThemeFontValues>? theme) => concrete is null && theme is not null;

	private static string? ResolveThemeFont(ThemeInfo themeInfo, ThemeFontValues themeFont)
		=> ResolveMajorThemeFont(themeInfo, themeFont) ?? ResolveMinorThemeFont(themeInfo, themeFont);
}
