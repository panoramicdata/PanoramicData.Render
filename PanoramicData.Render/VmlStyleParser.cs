namespace PanoramicData.Render;

using System.Globalization;

/// <summary>
/// Parses CSS-like VML style attributes into structured values.
/// </summary>
internal static class VmlStyleParser
{
	/// <summary>
	/// Parses a VML style string into a dictionary of property name to value.
	/// </summary>
	/// <param name="style">The VML style attribute value (semicolon-delimited key:value pairs).</param>
	/// <returns>A dictionary of style properties.</returns>
	public static Dictionary<string, string> Parse(string? style)
	{
		var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (string.IsNullOrWhiteSpace(style))
		{
			return result;
		}

		foreach (var part in style.Split(';', StringSplitOptions.RemoveEmptyEntries))
		{
			var colonIndex = part.IndexOf(':');
			if (colonIndex <= 0)
			{
				continue;
			}

			var key = part[..colonIndex].Trim();
			var value = part[(colonIndex + 1)..].Trim();
			if (!string.IsNullOrEmpty(key))
			{
				result[key] = value;
			}
		}

		return result;
	}

	private static readonly (string Suffix, Func<float, float> ToTwips)[] DimensionUnits =
	[
		("pt", points => TwipConverter.PointsToTwips(points)),
		("in", inches => inches * 1440f),
		("cm", cm => cm / 2.54f * 1440f),
		("mm", mm => mm / 25.4f * 1440f),
		("px", px => px * 15f), // 1px ≈ 15 twips at 96 DPI
	];

	/// <summary>
	/// Parses a dimension string (e.g. "527.85pt", "100px") to twips.
	/// </summary>
	/// <param name="value">The dimension string.</param>
	/// <returns>The value in twips, or 0 if parsing fails.</returns>
	public static float ParseDimensionToTwips(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return 0f;
		}

		value = value.Trim();

		foreach (var (suffix, toTwips) in DimensionUnits)
		{
			if (value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
			{
				return float.TryParse(value[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? toTwips(number) : 0f;
			}
		}

		return 0f;
	}
	/// <summary>
	/// Parses a rotation value from a VML style (e.g. "315", "-45").
	/// </summary>
	/// <param name="value">The rotation string.</param>
	/// <returns>The rotation in degrees, or 0 if parsing fails.</returns>
	public static float ParseRotation(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return 0f;
		}

		if (float.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var degrees))
		{
			return degrees;
		}

		return 0f;
	}
}
