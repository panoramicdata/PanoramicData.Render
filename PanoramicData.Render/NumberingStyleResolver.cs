namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Resolves numbering styles by applying numbering instance links and level overrides.
/// </summary>
internal static class NumberingStyleResolver
{
	/// <summary>
	/// Resolves the effective numbering level style for a numbering instance and level index.
	/// </summary>
	/// <param name="numberingPart">The numbering definitions part.</param>
	/// <param name="numberingId">The numbering instance ID (<c>w:numId</c>).</param>
	/// <param name="levelIndex">The requested numbering level index (<c>w:ilvl</c>).</param>
	/// <returns>A resolved <see cref="NumberingLevelStyle"/>, or <see langword="null"/> when resolution fails.</returns>
	public static NumberingLevelStyle? ResolveLevel(
		NumberingDefinitionsPart? numberingPart,
		int numberingId,
		int levelIndex)
	{
		if (levelIndex < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(levelIndex));
		}

		var numbering = numberingPart?.Numbering;
		if (numbering is null)
		{
			return null;
		}

		var numberingInstance = numbering.Elements<NumberingInstance>()
			.FirstOrDefault(n => n.NumberID?.Value == numberingId);
		var abstractNum = FindAbstractNum(numbering, numberingInstance);
		if (numberingInstance is null || abstractNum is null)
		{
			return null;
		}

		var levelOverride = numberingInstance.Elements<LevelOverride>()
			.FirstOrDefault(o => o.LevelIndex?.Value == levelIndex);
		var effectiveLevel = levelOverride?.GetFirstChild<Level>()
			?? abstractNum.Elements<Level>().FirstOrDefault(l => l.LevelIndex?.Value == levelIndex);

		return effectiveLevel is null ? null : BuildLevelStyle(effectiveLevel, levelOverride, levelIndex);
	}

	private static AbstractNum? FindAbstractNum(Numbering numbering, NumberingInstance? numberingInstance)
	{
		var abstractNumId = numberingInstance?.AbstractNumId?.Val?.Value;
		return abstractNumId is null
			? null
			: numbering.Elements<AbstractNum>().FirstOrDefault(a => a.AbstractNumberId?.Value == abstractNumId.Value);
	}

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

	private static string? ResolveSymbolFontFamily(Level effectiveLevel)
	{
		var symbolFonts = effectiveLevel.NumberingSymbolRunProperties?.RunFonts;
		return symbolFonts?.Ascii?.Value ?? symbolFonts?.HighAnsi?.Value;
	}

	// Normalizes Symbol/Wingdings PUA characters (U+F0xx) to standard Unicode so any font can render them.
	private static bool TryMapSymbolPua(string? levelText, string? fontFamily, out string mapped)
	{
		mapped = string.Empty;
		if (levelText is not [var ch] || ch < '\uF000' || ch > '\uF0FF' || !IsSymbolFont(fontFamily))
		{
			return false;
		}

		mapped = MapSymbolPuaToUnicode(ch).ToString();
		return true;
	}
	/// <summary>
	/// Determines whether the given font family is a legacy symbol font that uses PUA encoding.
	/// </summary>
	private static bool IsSymbolFont(string? fontFamily) =>
		fontFamily is not null && fontFamily.Equals("Symbol", StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// Maps a Symbol font Private Use Area character to its standard Unicode equivalent.
	/// </summary>
	private static char MapSymbolPuaToUnicode(char pua) => pua switch
	{
		'\uF0B7' => '\u2022', // • bullet
		'\uF0A7' => '\u00A7', // § section sign
		'\uF0D8' => '\u2191', // ↑ up arrow
		'\uF0FC' => '\u2713', // ✓ check mark
		'\uF0E0' => '\u2660', // ♠ spade
		'\uF0A8' => '\u00A8', // ¨ diaeresis
		'\uF076' => '\u2014', // — em dash
		'\uF0B0' => '\u00B0', // ° degree
		_ => pua, // Return unmapped characters as-is
	};
}
