namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Drawing;
using Xunit;

public class ThemeInfoParserTests
{
	[Fact]
	public void Parse_WithNullThemePart_ReturnsEmptyThemeInfo()
	{
		var info = ThemeInfoParser.Parse(null);

		info.MajorFont.Latin.Should().BeNull();
		info.MinorFont.Latin.Should().BeNull();
		info.Colors.Should().BeEmpty();
	}

	[Fact]
	public void Parse_WithThemePartWithoutTheme_ReturnsEmptyThemeInfo()
	{
		using var stream = TestDocxBuilder.CreateDocxWithThemePartWithoutThemeRoot();
		using var doc = DocxDocument.Load(stream);

		var info = ThemeInfoParser.Parse(doc.ThemePart);

		info.MajorFont.Latin.Should().BeNull();
		info.MinorFont.Latin.Should().BeNull();
		info.Colors.Should().BeEmpty();
	}

	[Fact]
	public void Parse_WithThemeFontsAndColors_ParsesAllExpectedValues()
	{
		using var stream = TestDocxBuilder.CreateDocxWithThemeFontsAndColors();
		using var doc = DocxDocument.Load(stream);

		var info = ThemeInfoParser.Parse(doc.ThemePart);

		info.MajorFont.Latin.Should().Be("Aptos Display");
		info.MajorFont.EastAsian.Should().Be("Yu Mincho");
		info.MajorFont.ComplexScript.Should().Be("Times New Roman");
		info.MajorFont.ScriptFonts["Jpan"].Should().Be("Yu Gothic");

		info.MinorFont.Latin.Should().Be("Aptos");
		info.MinorFont.EastAsian.Should().Be("Yu Gothic UI");
		info.MinorFont.ComplexScript.Should().Be("Arial");
		info.MinorFont.ScriptFonts["Hans"].Should().Be("Microsoft YaHei");

		info.Colors["dk1"].Should().Be("111111");
		info.Colors["dk2"].Should().Be("1F1F1F");
		info.Colors["accent1"].Should().Be("4472C4");
		info.Colors["hlink"].Should().Be("0563C1");
		info.Colors["folHlink"].Should().Be("954F72");
	}

	[Fact]
	public void Parse_WithThemeWithoutThemeElements_ReturnsEmptyThemeInfo()
	{
		var theme = new Theme { Name = "NoElements" };
		using var stream = TestDocxBuilder.CreateDocxWithTheme(theme);
		using var doc = DocxDocument.Load(stream);

		var info = ThemeInfoParser.Parse(doc.ThemePart);

		info.MajorFont.Latin.Should().BeNull();
		info.MinorFont.Latin.Should().BeNull();
		info.Colors.Should().BeEmpty();
	}

	[Fact]
	public void Parse_WithMissingFontScheme_ReturnsEmptyFonts()
	{
		var themeElements = new ThemeElements();
		themeElements.Append(new ColorScheme { Name = "ColorsOnly" });
		var theme = new Theme { Name = "ColorsOnlyTheme" };
		theme.Append(themeElements);

		using var stream = TestDocxBuilder.CreateDocxWithTheme(theme);
		using var doc = DocxDocument.Load(stream);

		var info = ThemeInfoParser.Parse(doc.ThemePart);

		info.MajorFont.Latin.Should().BeNull();
		info.MinorFont.Latin.Should().BeNull();
	}

	[Fact]
	public void Parse_WithMissingColorScheme_ReturnsEmptyColors()
	{
		var fontScheme = new FontScheme { Name = "FontsOnly" };
		fontScheme.Append(
			new MajorFont(new LatinFont { Typeface = "Headings" }),
			new MinorFont(new LatinFont { Typeface = "Body" }));
		var themeElements = new ThemeElements(fontScheme);
		var theme = new Theme { Name = "FontsOnlyTheme" };
		theme.Append(themeElements);

		using var stream = TestDocxBuilder.CreateDocxWithTheme(theme);
		using var doc = DocxDocument.Load(stream);

		var info = ThemeInfoParser.Parse(doc.ThemePart);

		info.MajorFont.Latin.Should().Be("Headings");
		info.MinorFont.Latin.Should().Be("Body");
		info.Colors.Should().BeEmpty();
	}

	[Fact]
	public void Parse_SystemColorWithoutLastColor_UsesSystemColorToken()
	{
		var colorScheme = new ColorScheme { Name = "SystemOnly" };
		colorScheme.Append(new Dark1Color(new SystemColor { Val = SystemColorValues.WindowText }));

		var formatScheme = new FormatScheme { Name = "Fmt" };
		formatScheme.Append(new FillStyleList(), new LineStyleList(), new EffectStyleList(), new BackgroundFillStyleList());

		var themeElements = new ThemeElements();
		themeElements.Append(colorScheme, new FontScheme { Name = "Fonts" }, formatScheme);
		var theme = new Theme { Name = "SystemColorTheme" };
		theme.Append(themeElements);

		using var stream = TestDocxBuilder.CreateDocxWithTheme(theme);
		using var doc = DocxDocument.Load(stream);

		var info = ThemeInfoParser.Parse(doc.ThemePart);

		info.Colors["dk1"].Should().Be("windowText");
	}

	[Fact]
	public void Parse_ColorWithoutRgbOrSystemColor_DoesNotAddColorEntry()
	{
		var colorScheme = new ColorScheme { Name = "UnsupportedColorNode" };
		colorScheme.Append(new Dark1Color(new PresetColor { Val = PresetColorValues.Red }));

		var formatScheme = new FormatScheme { Name = "Fmt" };
		formatScheme.Append(new FillStyleList(), new LineStyleList(), new EffectStyleList(), new BackgroundFillStyleList());

		var themeElements = new ThemeElements();
		themeElements.Append(colorScheme, new FontScheme { Name = "Fonts" }, formatScheme);
		var theme = new Theme { Name = "UnsupportedColorTheme" };
		theme.Append(themeElements);

		using var stream = TestDocxBuilder.CreateDocxWithTheme(theme);
		using var doc = DocxDocument.Load(stream);

		var info = ThemeInfoParser.Parse(doc.ThemePart);

		info.Colors.Should().NotContainKey("dk1");
	}
}
