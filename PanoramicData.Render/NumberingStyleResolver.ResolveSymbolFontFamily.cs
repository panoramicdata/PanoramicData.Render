namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class NumberingStyleResolver
{
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
	private static float? ParseIndentTwips(Indentation? indentation, bool hanging)
		=> TryParseTwips(hanging ? indentation?.Hanging?.Value : indentation?.Left?.Value);

	private static float? TryParseTwips(string? value)
		=> value is not null && float.TryParse(value, out var parsed) ? parsed : null;

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
