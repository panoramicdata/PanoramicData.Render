namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public class ThemeColorResolverTests
{
	private static readonly ThemeInfo Theme = new()
	{
		MajorFont = new ThemeFontInfo { Latin = null, EastAsian = null, ComplexScript = null, ScriptFonts = new Dictionary<string, string>() },
		MinorFont = new ThemeFontInfo { Latin = null, EastAsian = null, ComplexScript = null, ScriptFonts = new Dictionary<string, string>() },
		Colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["accent1"] = "808080",
			["dk1"] = "112233",
			["hlink"] = "0563C1",
			["lt1"] = "windowText"
		}
	};

	[Fact]
	public void Resolve_WithNullThemeColor_ReturnsNull()
	{
		var result = ThemeColorResolver.Resolve(Theme, null, null, null);

		result.Should().BeNull();
	}

	[Fact]
	public void Resolve_WithMissingThemeColorSlot_ReturnsNull()
	{
		var result = ThemeColorResolver.Resolve(Theme, ThemeColorValues.Accent5, null, null);

		result.Should().BeNull();
	}

	[Fact]
	public void Resolve_WithDefaultThemeColorValue_ReturnsNull()
	{
		var result = ThemeColorResolver.Resolve(Theme, default(ThemeColorValues), null, null);

		result.Should().BeNull();
	}

	[Fact]
	public void Resolve_WithoutModifiers_ReturnsBaseRgb()
	{
		var result = ThemeColorResolver.Resolve(Theme, ThemeColorValues.Dark1, null, null);

		result.Should().Be("112233");
	}

	[Fact]
	public void Resolve_WithTint_LightensColor()
	{
		var result = ThemeColorResolver.Resolve(Theme, ThemeColorValues.Accent1, "80", null);

		result.Should().Be("BFBFBF");
	}

	[Fact]
	public void Resolve_WithShade_DarkensColor()
	{
		var result = ThemeColorResolver.Resolve(Theme, ThemeColorValues.Accent1, null, "80");

		result.Should().Be("404040");
	}

	[Fact]
	public void Resolve_WithTintAndShade_AppliesBoth()
	{
		var result = ThemeColorResolver.Resolve(Theme, ThemeColorValues.Accent1, "80", "80");

		result.Should().Be("9F9F9F");
	}

	[Fact]
	public void Resolve_WithInvalidModifier_IgnoresInvalidModifier()
	{
		var result = ThemeColorResolver.Resolve(Theme, ThemeColorValues.Dark1, "ZZ", null);

		result.Should().Be("112233");
	}

	[Fact]
	public void Resolve_WithNonRgbBaseColor_ReturnsNull()
	{
		var result = ThemeColorResolver.Resolve(Theme, ThemeColorValues.Light1, null, null);

		result.Should().BeNull();
	}

	[Fact]
	public void Resolve_WithRgbLengthNotSix_ReturnsNull()
	{
		var theme = new ThemeInfo
		{
			MajorFont = Theme.MajorFont,
			MinorFont = Theme.MinorFont,
			Colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				["dk1"] = "12345"
			}
		};

		var result = ThemeColorResolver.Resolve(theme, ThemeColorValues.Dark1, null, null);

		result.Should().BeNull();
	}

	[Fact]
	public void Resolve_WithInvalidRgbMiddlePair_ReturnsNull()
	{
		var theme = new ThemeInfo
		{
			MajorFont = Theme.MajorFont,
			MinorFont = Theme.MinorFont,
			Colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				["dk1"] = "11ZZ33"
			}
		};

		var result = ThemeColorResolver.Resolve(theme, ThemeColorValues.Dark1, null, null);

		result.Should().BeNull();
	}

	[Fact]
	public void Resolve_WithInvalidRgbFirstPair_ReturnsNull()
	{
		var theme = new ThemeInfo
		{
			MajorFont = Theme.MajorFont,
			MinorFont = Theme.MinorFont,
			Colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				["dk1"] = "ZZ2233"
			}
		};

		var result = ThemeColorResolver.Resolve(theme, ThemeColorValues.Dark1, null, null);

		result.Should().BeNull();
	}

	[Fact]
	public void Resolve_WithInvalidRgbLastPair_ReturnsNull()
	{
		var theme = new ThemeInfo
		{
			MajorFont = Theme.MajorFont,
			MinorFont = Theme.MinorFont,
			Colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				["dk1"] = "1122ZZ"
			}
		};

		var result = ThemeColorResolver.Resolve(theme, ThemeColorValues.Dark1, null, null);

		result.Should().BeNull();
	}

	[Fact]
	public void Resolve_NullTheme_ThrowsArgumentNullException()
	{
		var act = () => ThemeColorResolver.Resolve(null!, ThemeColorValues.Dark1, null, null);

		act.Should().Throw<ArgumentNullException>();
	}
}
