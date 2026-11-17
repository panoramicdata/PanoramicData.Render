namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public partial class EffectiveFormattingResolverTests
{
	[Fact]
	public void Resolve_DirectRunFormattingOverridesCharacterStyle()
	{
		var characterHierarchy = CreateCharacterHierarchy(
			("Emphasis", null, new StyleRunProperties(new Color { Val = "222222" })));

		var run = new Run(new RunProperties(
			new RunStyle { Val = "Emphasis" },
			new Color { Val = "999999" }));

		var result = EffectiveFormattingResolver.Resolve(
			CreateDefaults(new ParagraphPropertiesBaseStyle(), new RunPropertiesBaseStyle()),
			CreateThemeInfo(),
			null,
			null,
			CreateParagraphHierarchy(),
			characterHierarchy,
			new Paragraph(),
			run);

		result.RunProperties.GetFirstChild<Color>()?.Val?.Value.Should().Be("999999");
	}

	[Fact]
	public void Resolve_WithDefaultsOnly_ProducesMergedPropertyCopies()
	{
		var defaults = CreateDefaults(
			new ParagraphPropertiesBaseStyle(new SpacingBetweenLines { Before = "80" }),
			new RunPropertiesBaseStyle(new FontSize { Val = "22" }));

		var result = EffectiveFormattingResolver.Resolve(
			defaults,
			CreateThemeInfo(),
			null,
			null,
			CreateParagraphHierarchy(),
			CreateCharacterHierarchy(),
			new Paragraph(),
			new Run());

		result.ParagraphProperties.GetFirstChild<SpacingBetweenLines>()?.Before?.Value.Should().Be("80");
		result.RunProperties.GetFirstChild<FontSize>()?.Val?.Value.Should().Be("22");
	}

	[Fact]
	public void Resolve_WithoutParagraphStyleId_UsesDefaultParagraphStyle()
	{
		var styles = new Dictionary<string, ParagraphStyleInfo>(StringComparer.OrdinalIgnoreCase)
		{
			["Normal"] = new ParagraphStyleInfo
			{
				StyleId = "Normal",
				Name = "Normal",
				BasedOnStyleId = null,
				IsDefault = true,
				Properties = new StyleParagraphProperties(new SpacingBetweenLines { Before = "240", Line = "360" }),
				RunProperties = null
			}
		};
		var chains = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
		{
			["Normal"] = ["Normal"]
		};
		var paragraphHierarchy = new ParagraphStyleHierarchy(styles, chains, "Normal");

		var result = EffectiveFormattingResolver.Resolve(
			CreateDefaults(new ParagraphPropertiesBaseStyle(), new RunPropertiesBaseStyle()),
			CreateThemeInfo(),
			null,
			null,
			paragraphHierarchy,
			CreateCharacterHierarchy(),
			new Paragraph(),
			new Run());

		result.ParagraphProperties.GetFirstChild<SpacingBetweenLines>()?.Before?.Value.Should().Be("240");
		result.ParagraphProperties.GetFirstChild<SpacingBetweenLines>()?.Line?.Value.Should().Be("360");
	}

	[Fact]
	public void Resolve_RunStyleIdIsPreservedInMergedRunProperties()
	{
		var run = new Run(new RunProperties(new RunStyle { Val = "Emphasis" }));

		var result = EffectiveFormattingResolver.Resolve(
			CreateDefaults(new ParagraphPropertiesBaseStyle(), new RunPropertiesBaseStyle()),
			CreateThemeInfo(),
			null,
			null,
			CreateParagraphHierarchy(),
			CreateCharacterHierarchy(("Emphasis", null, new StyleRunProperties(new Italic()))),
			new Paragraph(),
			run);

		result.RunProperties.GetFirstChild<RunStyle>()?.Val?.Value.Should().Be("Emphasis");
	}

	[Fact]
	public void Resolve_NullArguments_ThrowArgumentNullException()
	{
		var defaults = CreateDefaults(new ParagraphPropertiesBaseStyle(), new RunPropertiesBaseStyle());
		var theme = CreateThemeInfo();
		var pHierarchy = CreateParagraphHierarchy();
		var cHierarchy = CreateCharacterHierarchy();
		var paragraph = new Paragraph();
		var run = new Run();
		Action act1 = () => EffectiveFormattingResolver.Resolve(null!, theme, null, null, pHierarchy, cHierarchy, paragraph, run);
		Action act2 = () => EffectiveFormattingResolver.Resolve(defaults, null!, null, null, pHierarchy, cHierarchy, paragraph, run);
		Action act3 = () => EffectiveFormattingResolver.Resolve(defaults, theme, null, null, null!, cHierarchy, paragraph, run);
		Action act4 = () => EffectiveFormattingResolver.Resolve(defaults, theme, null, null, pHierarchy, null!, paragraph, run);
		Action act5 = () => EffectiveFormattingResolver.Resolve(defaults, theme, null, null, pHierarchy, cHierarchy, null!, run);
		Action act6 = () => EffectiveFormattingResolver.Resolve(defaults, theme, null, null, pHierarchy, cHierarchy, paragraph, null!);
		act1.Should().Throw<ArgumentNullException>();
		act2.Should().Throw<ArgumentNullException>();
		act3.Should().Throw<ArgumentNullException>();
		act4.Should().Throw<ArgumentNullException>();
		act5.Should().Throw<ArgumentNullException>();
		act6.Should().Throw<ArgumentNullException>();
	}

	private static DocumentDefaults CreateDefaults(
		ParagraphPropertiesBaseStyle paragraph,
		RunPropertiesBaseStyle run)
	{
		return new DocumentDefaults
		{
			ParagraphProperties = paragraph,
			RunProperties = run
		};
	}

	private static ThemeInfo CreateThemeInfo(params (string Key, string Value)[] colors)
	{
		var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (var color in colors)
		{
			map[color.Key] = color.Value;
		}
		return new ThemeInfo
		{
			MajorFont = new ThemeFontInfo { Latin = null, EastAsian = null, ComplexScript = null, ScriptFonts = new Dictionary<string, string>() },
			MinorFont = new ThemeFontInfo { Latin = null, EastAsian = null, ComplexScript = null, ScriptFonts = new Dictionary<string, string>() },
			Colors = map
		};
	}

	private static ParagraphStyleHierarchy CreateParagraphHierarchy(params (string Id, string? BasedOn, StyleParagraphProperties Props)[] styles)
	{
		var styleMap = new Dictionary<string, ParagraphStyleInfo>(StringComparer.OrdinalIgnoreCase);
		var chains = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
		foreach (var style in styles)
		{
			styleMap[style.Id] = new ParagraphStyleInfo
			{
				StyleId = style.Id,
				Name = style.Id,
				BasedOnStyleId = style.BasedOn,
				IsDefault = false,
				Properties = style.Props,
				RunProperties = null
			};
			var chain = new List<string> { style.Id };
			if (!string.IsNullOrWhiteSpace(style.BasedOn))
			{
				chain.Add(style.BasedOn!);
			}
			chains[style.Id] = chain;
		}
		return new ParagraphStyleHierarchy(styleMap, chains);
	}

	private static ParagraphStyleHierarchy CreateParagraphHierarchyWithRunProperties(params (string Id, string? BasedOn, StyleParagraphProperties Props, StyleRunProperties? RunProps)[] styles)
	{
		var styleMap = new Dictionary<string, ParagraphStyleInfo>(StringComparer.OrdinalIgnoreCase);
		var chains = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
		foreach (var style in styles)
		{
			styleMap[style.Id] = new ParagraphStyleInfo
			{
				StyleId = style.Id,
				Name = style.Id,
				BasedOnStyleId = style.BasedOn,
				IsDefault = false,
				Properties = style.Props,
				RunProperties = style.RunProps
			};
			var chain = new List<string> { style.Id };
			if (!string.IsNullOrWhiteSpace(style.BasedOn))
			{
				chain.Add(style.BasedOn!);
			}

			chains[style.Id] = chain;
		}

		return new ParagraphStyleHierarchy(styleMap, chains);
	}

	private static CharacterStyleHierarchy CreateCharacterHierarchy(params (string Id, string? BasedOn, StyleRunProperties Props)[] styles)
	{
		var styleMap = new Dictionary<string, CharacterStyleInfo>(StringComparer.OrdinalIgnoreCase);
		var chains = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
		foreach (var style in styles)
		{
			styleMap[style.Id] = new CharacterStyleInfo
			{
				StyleId = style.Id,
				Name = style.Id,
				BasedOnStyleId = style.BasedOn,
				IsDefault = false,
				Properties = style.Props
			};
			var chain = new List<string> { style.Id };
			if (!string.IsNullOrWhiteSpace(style.BasedOn))
			{
				chain.Add(style.BasedOn!);
			}
			chains[style.Id] = chain;
		}
		return new CharacterStyleHierarchy(styleMap, chains);
	}
}
