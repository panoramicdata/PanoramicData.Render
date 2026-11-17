namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using Xunit;

public class ThemeInfoParserTests
{
	[Fact]
	public void Parse_WithNullThemePart_ReturnsEmptyThemeInfo()
		=> AssertEmpty(ThemeInfoParser.Parse(null));

	[Fact]
	public void Parse_WithThemePartWithoutTheme_ReturnsEmptyThemeInfo()
		=> AssertEmpty(ParseDocx(TestDocxBuilder.CreateDocxWithThemePartWithoutThemeRoot()));

	[Fact]
	public void Parse_WithThemeFontsAndColors_ParsesAllExpectedValues()
	{
		var info = ParseDocx(TestDocxBuilder.CreateDocxWithThemeFontsAndColors());

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
		=> AssertEmpty(ParseTheme(new Theme { Name = "NoElements" }));

	[Fact]
	public void Parse_WithMissingFontScheme_ReturnsEmptyFonts()
	{
		var info = ParseTheme(CreateTheme("ColorsOnlyTheme", new ThemeElements(new ColorScheme { Name = "ColorsOnly" })));

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

		var info = ParseTheme(CreateTheme("FontsOnlyTheme", new ThemeElements(fontScheme)));

		info.MajorFont.Latin.Should().Be("Headings");
		info.MinorFont.Latin.Should().Be("Body");
		info.Colors.Should().BeEmpty();
	}

	[Fact]
	public void Parse_SystemColorWithoutLastColor_UsesSystemColorToken()
	{
		var info = ParseTheme(CreateDark1ColorTheme("SystemColorTheme", new SystemColor { Val = SystemColorValues.WindowText }));

		info.Colors["dk1"].Should().Be("windowText");
	}

	[Fact]
	public void Parse_ColorWithoutRgbOrSystemColor_DoesNotAddColorEntry()
	{
		var info = ParseTheme(CreateDark1ColorTheme("UnsupportedColorTheme", new PresetColor { Val = PresetColorValues.Red }));

		info.Colors.Should().NotContainKey("dk1");
	}

	private static void AssertEmpty(ThemeInfo info)
	{
		info.MajorFont.Latin.Should().BeNull();
		info.MinorFont.Latin.Should().BeNull();
		info.Colors.Should().BeEmpty();
	}

	private static ThemeInfo ParseDocx(Stream docx)
	{
		using var stream = docx;
		using var doc = DocxDocument.Load(stream);
		return ThemeInfoParser.Parse(doc.ThemePart);
	}

	private static ThemeInfo ParseTheme(Theme theme) => ParseDocx(TestDocxBuilder.CreateDocxWithTheme(theme));

	private static Theme CreateTheme(string name, ThemeElements themeElements)
	{
		var theme = new Theme { Name = name };
		theme.Append(themeElements);
		return theme;
	}

	private static Theme CreateDark1ColorTheme(string name, OpenXmlCompositeElement colorChoice)
	{
		var colorScheme = new ColorScheme { Name = name };
		colorScheme.Append(new Dark1Color(colorChoice));

		var formatScheme = new FormatScheme { Name = "Fmt" };
		formatScheme.Append(new FillStyleList(), new LineStyleList(), new EffectStyleList(), new BackgroundFillStyleList());

		var themeElements = new ThemeElements();
		themeElements.Append(colorScheme, new FontScheme { Name = "Fonts" }, formatScheme);
		return CreateTheme(name, themeElements);
	}
}