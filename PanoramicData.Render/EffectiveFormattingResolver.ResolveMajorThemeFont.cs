namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class EffectiveFormattingResolver
{
	private static string? ResolveMajorThemeFont(ThemeInfo themeInfo, ThemeFontValues themeFont)
	{
		if (themeFont == ThemeFontValues.MajorHighAnsi || themeFont == ThemeFontValues.MajorAscii)
		{
			return themeInfo.MajorFont.Latin;
		}

		if (themeFont == ThemeFontValues.MajorEastAsia)
		{
			return themeInfo.MajorFont.EastAsian;
		}

		return themeFont == ThemeFontValues.MajorBidi ? themeInfo.MajorFont.ComplexScript : null;
	}

	private static string? ResolveMinorThemeFont(ThemeInfo themeInfo, ThemeFontValues themeFont)
	{
		if (themeFont == ThemeFontValues.MinorHighAnsi || themeFont == ThemeFontValues.MinorAscii)
		{
			return themeInfo.MinorFont.Latin;
		}

		if (themeFont == ThemeFontValues.MinorEastAsia)
		{
			return themeInfo.MinorFont.EastAsian;
		}

		return themeFont == ThemeFontValues.MinorBidi ? themeInfo.MinorFont.ComplexScript : null;
	}
	private static string? ResolveRunColor(ThemeInfo themeInfo, RunProperties runProperties)
	{
		var color = runProperties.GetFirstChild<Color>();
		if (color is null)
		{
			return null;
		}

		var directColor = color.Val?.Value;
		return string.IsNullOrWhiteSpace(directColor) ? ResolveThemeRunColor(themeInfo, color) : directColor;
	}
}
