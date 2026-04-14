namespace PanoramicData.Render;

using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Formats list labels from numbering styles and current level counters.
/// </summary>
internal static class ListNumberingFormatter
{
	private static readonly Regex PlaceholderRegex = new("%([1-9])", RegexOptions.Compiled | RegexOptions.CultureInvariant);

	/// <summary>
	/// Formats a list label for the current level.
	/// </summary>
	/// <param name="style">The numbering style for the current level.</param>
	/// <param name="countersByLevel">Current counters keyed by 0-based level index.</param>
	/// <returns>The formatted label text.</returns>
	public static string FormatLabel(NumberingLevelStyle style, IReadOnlyDictionary<int, int> countersByLevel)
	{
		ArgumentNullException.ThrowIfNull(style);
		ArgumentNullException.ThrowIfNull(countersByLevel);

		// For bullet format with literal lvlText (no %N placeholders), return the text as-is.
		// This handles real DOCX files where lvlText is a literal bullet character.
		if (string.Equals(style.NumberFormat, "bullet", StringComparison.OrdinalIgnoreCase)
			&& !string.IsNullOrEmpty(style.LevelText)
			&& !PlaceholderRegex.IsMatch(style.LevelText))
		{
			return style.LevelText;
		}

		var pattern = string.IsNullOrWhiteSpace(style.LevelText)
			? $"%{style.LevelIndex + 1}."
			: style.LevelText;

		return PlaceholderRegex.Replace(pattern, match =>
		{
			var levelIndex = int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) - 1;
			var rawValue = countersByLevel.TryGetValue(levelIndex, out var counter)
				? counter
				: 0;
			return FormatCounter(rawValue, style.NumberFormat);
		});
	}

	private static string FormatCounter(int value, string? numberFormat)
	{
		if (value <= 0)
		{
			return string.Empty;
		}

		var normalized = (numberFormat ?? "decimal").Trim();
		return normalized switch
		{
			"upperRoman" => ToRoman(value).ToUpperInvariant(),
			"lowerRoman" => ToRoman(value).ToLowerInvariant(),
			"upperLetter" => ToAlphabetic(value).ToUpperInvariant(),
			"lowerLetter" => ToAlphabetic(value).ToLowerInvariant(),
			"bullet" => "•",
			_ => value.ToString(System.Globalization.CultureInfo.InvariantCulture)
		};
	}

	private static string ToAlphabetic(int value)
	{
		var builder = new StringBuilder();
		var current = value;
		while (current > 0)
		{
			current--;
			builder.Insert(0, (char)('A' + (current % 26)));
			current /= 26;
		}

		return builder.ToString();
	}

	private static string ToRoman(int value)
	{
		if (value <= 0)
		{
			return string.Empty;
		}

		var numerals = new (int Value, string Symbol)[]
		{
			(1000, "M"),
			(900, "CM"),
			(500, "D"),
			(400, "CD"),
			(100, "C"),
			(90, "XC"),
			(50, "L"),
			(40, "XL"),
			(10, "X"),
			(9, "IX"),
			(5, "V"),
			(4, "IV"),
			(1, "I")
		};

		var builder = new StringBuilder();
		var remaining = value;
		foreach (var (numeralValue, symbol) in numerals)
		{
			while (remaining >= numeralValue)
			{
				builder.Append(symbol);
				remaining -= numeralValue;
			}
		}

		return builder.ToString();
	}
}
