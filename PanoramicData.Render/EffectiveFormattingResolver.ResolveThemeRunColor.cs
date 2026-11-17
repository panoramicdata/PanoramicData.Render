namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class EffectiveFormattingResolver
{
	private static string? ResolveThemeRunColor(ThemeInfo themeInfo, Color color)
	{
		var themeColor = color.ThemeColor?.Value;
		if (themeColor is null)
		{
			return null;
		}

		var resolved = ThemeColorResolver.Resolve(
			themeInfo,
			themeColor,
			color.ThemeTint?.Value,
			color.ThemeShade?.Value);
		if (!string.IsNullOrWhiteSpace(resolved))
		{
			color.Val = resolved;
		}

		return resolved;
	}

	private sealed class StyleCascade
	{
		public ParagraphProperties ParagraphProperties { get; } = new();

		public RunProperties RunProperties { get; } = new();

		public ToggleState ToggleState { get; private set; } = new();

		public void Apply(OpenXmlCompositeElement? paragraphSource, OpenXmlCompositeElement? runSource, OpenXmlCompositeElement? toggleSource)
		{
			Merge(ParagraphProperties, paragraphSource);
			Merge(RunProperties, runSource);
			ToggleState = TogglePropertyLogic.Apply(ToggleState, ParseToggles(toggleSource));
		}
	}
}
