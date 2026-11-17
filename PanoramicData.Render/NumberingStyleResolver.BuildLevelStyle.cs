namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class NumberingStyleResolver
{
	private static NumberingLevelStyle BuildLevelStyle(Level effectiveLevel, LevelOverride? levelOverride, int levelIndex)
	{
		var (fontFamily, levelText) = ResolveLevelText(effectiveLevel);
		var levelIndent = GetLevelIndentation(effectiveLevel);

		return new NumberingLevelStyle
		{
			LevelIndex = levelIndex,
			Start = ResolveStart(effectiveLevel, levelOverride),
			NumberFormat = effectiveLevel.NumberingFormat?.Val?.InnerText,
			LevelText = levelText,
			RestartAfterLevel = effectiveLevel.LevelRestart?.Val?.Value,
			FontFamily = fontFamily,
			IndentLeftTwips = ParseIndentTwips(levelIndent, hanging: false),
			HangingTwips = ParseIndentTwips(levelIndent, hanging: true),
		};
	}

	private static int ResolveStart(Level effectiveLevel, LevelOverride? levelOverride)
		=> levelOverride?.StartOverrideNumberingValue?.Val?.Value ?? effectiveLevel.StartNumberingValue?.Val?.Value ?? 1;

	private static Indentation? GetLevelIndentation(Level effectiveLevel)
		=> effectiveLevel.GetFirstChild<PreviousParagraphProperties>()?.Indentation
			?? effectiveLevel.GetFirstChild<ParagraphProperties>()?.Indentation;

	private static (string? FontFamily, string? LevelText) ResolveLevelText(Level effectiveLevel)
	{
		var fontFamily = ResolveSymbolFontFamily(effectiveLevel);
		var levelText = effectiveLevel.LevelText?.Val?.Value;

		// Use the default font when the text has been mapped to standard Unicode.
		return TryMapSymbolPua(levelText, fontFamily, out var mapped) ? (null, mapped) : (fontFamily, levelText);
	}
}
