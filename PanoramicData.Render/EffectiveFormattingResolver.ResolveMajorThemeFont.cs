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
	private static ToggleProperties ParseToggles(OpenXmlCompositeElement? properties)
	{
		if (properties is null)
		{
			return new ToggleProperties();
		}

		return new ToggleProperties
		{
			Bold = ParseInstruction(properties.GetFirstChild<Bold>()),
			Italic = ParseInstruction(properties.GetFirstChild<Italic>()),
			Caps = ParseInstruction(properties.GetFirstChild<Caps>()),
			SmallCaps = ParseInstruction(properties.GetFirstChild<SmallCaps>()),
			Strike = ParseInstruction(properties.GetFirstChild<Strike>()),
			DoubleStrike = ParseInstruction(properties.GetFirstChild<DoubleStrike>()),
			Vanish = ParseInstruction(properties.GetFirstChild<Vanish>()),
			Emboss = ParseInstruction(properties.GetFirstChild<Emboss>()),
			Imprint = ParseInstruction(properties.GetFirstChild<Imprint>()),
			Outline = ParseInstruction(properties.GetFirstChild<Outline>()),
			Shadow = ParseInstruction(properties.GetFirstChild<Shadow>())
		};
	}

	private static ToggleInstruction ParseInstruction(OnOffType? property)
	{
		if (property is null)
		{
			return ToggleInstruction.None;
		}

		var val = property.Val?.Value;
		if (val is null || val.Value)
		{
			return ToggleInstruction.Toggle;
		}

		return ToggleInstruction.SetFalse;
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
