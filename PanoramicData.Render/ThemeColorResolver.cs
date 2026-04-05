namespace PanoramicData.Render;

using System.Globalization;
using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Resolves theme color slots and tint/shade modifiers into concrete RGB values.
/// </summary>
internal static class ThemeColorResolver
{
	private static readonly IReadOnlyDictionary<ThemeColorValues, string> ThemeColorMap =
		new Dictionary<ThemeColorValues, string>
		{
			[ThemeColorValues.Dark1] = "dk1",
			[ThemeColorValues.Light1] = "lt1",
			[ThemeColorValues.Dark2] = "dk2",
			[ThemeColorValues.Light2] = "lt2",
			[ThemeColorValues.Accent1] = "accent1",
			[ThemeColorValues.Accent2] = "accent2",
			[ThemeColorValues.Accent3] = "accent3",
			[ThemeColorValues.Accent4] = "accent4",
			[ThemeColorValues.Accent5] = "accent5",
			[ThemeColorValues.Accent6] = "accent6",
			[ThemeColorValues.Hyperlink] = "hlink",
			[ThemeColorValues.FollowedHyperlink] = "folHlink"
		};

	/// <summary>
	/// Resolves a theme color and optional tint/shade modifiers to a six-digit RGB color.
	/// </summary>
	/// <param name="theme">Parsed theme information.</param>
	/// <param name="themeColor">The theme color slot.</param>
	/// <param name="themeTint">Optional tint modifier as a 2-digit hex byte.</param>
	/// <param name="themeShade">Optional shade modifier as a 2-digit hex byte.</param>
	/// <returns>A six-digit uppercase RGB value, or <see langword="null"/> when not resolvable.</returns>
	public static string? Resolve(
		ThemeInfo theme,
		ThemeColorValues? themeColor,
		string? themeTint,
		string? themeShade)
	{
		ArgumentNullException.ThrowIfNull(theme);

		if (themeColor is null)
		{
			return null;
		}

		var colorKey = MapThemeColorKey(themeColor.Value);
		if (string.IsNullOrEmpty(colorKey))
		{
			return null;
		}

		if (!theme.Colors.TryGetValue(colorKey, out var baseColor))
		{
			return null;
		}

		if (!TryParseRgb(baseColor, out var r, out var g, out var b))
		{
			return null;
		}

		if (TryParseModifier(themeShade, out var shadeValue))
		{
			r = ApplyShade(r, shadeValue);
			g = ApplyShade(g, shadeValue);
			b = ApplyShade(b, shadeValue);
		}

		if (TryParseModifier(themeTint, out var tintValue))
		{
			r = ApplyTint(r, tintValue);
			g = ApplyTint(g, tintValue);
			b = ApplyTint(b, tintValue);
		}

		return string.Create(CultureInfo.InvariantCulture, $"{r:X2}{g:X2}{b:X2}");
	}

	private static bool TryParseRgb(string value, out byte r, out byte g, out byte b)
	{
		r = 0;
		g = 0;
		b = 0;

		if (value.Length != 6)
		{
			return false;
		}

		if (!byte.TryParse(value.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r))
		{
			return false;
		}

		if (!byte.TryParse(value.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g))
		{
			return false;
		}

		if (!byte.TryParse(value.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b))
		{
			return false;
		}

		return true;
	}

	private static bool TryParseModifier(string? modifier, out byte value)
	{
		value = 0;
		if (string.IsNullOrWhiteSpace(modifier) || modifier.Length != 2)
		{
			return false;
		}

		return byte.TryParse(modifier, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
	}

	private static byte ApplyShade(byte channel, byte shade) =>
		(byte)Math.Clamp((channel * shade) / 255, 0, 255);

	private static byte ApplyTint(byte channel, byte tint) =>
		(byte)Math.Clamp(channel + ((255 - channel) * tint) / 255, 0, 255);

	private static string? MapThemeColorKey(ThemeColorValues value)
	{
		return ThemeColorMap.TryGetValue(value, out var key) ? key : null;
	}
}
