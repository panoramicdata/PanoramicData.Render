namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public class EffectiveFormattingResolverTests
{
	[Fact]
	public void Resolve_AppliesParagraphPropertyPrecedenceThroughCascade()
	{
		var defaults = CreateDefaults(
			new ParagraphPropertiesBaseStyle(new Justification { Val = JustificationValues.Left }),
			new RunPropertiesBaseStyle());
		var tableStyle = new ResolvedTableStyle
		{
			StyleId = "TableGrid",
			TableProperties = null,
			TableRowProperties = null,
			TableCellProperties = null,
			ParagraphProperties = new StyleParagraphProperties(new Justification { Val = JustificationValues.Center }),
			RunProperties = null,
			AppliedConditionals = []
		};
		var paragraphHierarchy = CreateParagraphHierarchy(
			("Base", null, new StyleParagraphProperties(new Justification { Val = JustificationValues.Right })),
			("Heading", "Base", new StyleParagraphProperties(new Justification { Val = JustificationValues.Both })));
		var paragraph = new Paragraph(
			new ParagraphProperties(
				new ParagraphStyleId { Val = "Heading" },
				new Justification { Val = JustificationValues.Distribute }));
		var run = new Run();
		var result = EffectiveFormattingResolver.Resolve(
			defaults,
			CreateThemeInfo(),
			null,
			tableStyle,
			paragraphHierarchy,
			CreateCharacterHierarchy(),
			paragraph,
			run);
		result.ParagraphProperties.GetFirstChild<Justification>()?.Val?.Value.Should().Be(JustificationValues.Distribute);
	}

	[Fact]
	public void Resolve_AppliesRunPropertyPrecedenceThroughCascade()
	{
		var defaults = CreateDefaults(
			new ParagraphPropertiesBaseStyle(),
			new RunPropertiesBaseStyle(new Color { Val = "111111" }));
		var tableStyle = new ResolvedTableStyle
		{
			StyleId = "TableGrid",
			TableProperties = null,
			TableRowProperties = null,
			TableCellProperties = null,
			ParagraphProperties = null,
			RunProperties = new StyleRunProperties(new Color { Val = "222222" }),
			AppliedConditionals = []
		};
		var characterHierarchy = CreateCharacterHierarchy(
			("Emphasis", null, new StyleRunProperties(new Color { Val = "333333" })),
			("Strong", "Emphasis", new StyleRunProperties(new Color { Val = "444444" })));
		var run = new Run(
			new RunProperties(
				new RunStyle { Val = "Strong" },
				new Color { Val = "555555" }));
		var result = EffectiveFormattingResolver.Resolve(
			defaults,
			CreateThemeInfo(),
			null,
			tableStyle,
			CreateParagraphHierarchy(),
			characterHierarchy,
			new Paragraph(),
			run);
		result.RunProperties.GetFirstChild<Color>()?.Val?.Value.Should().Be("555555");
	}

	[Fact]
	public void Resolve_AppliesToggleSemanticsAcrossCascade()
	{
		var defaults = CreateDefaults(
			new ParagraphPropertiesBaseStyle(),
			new RunPropertiesBaseStyle(new Bold()));
		var characterHierarchy = CreateCharacterHierarchy(
			("Emphasis", null, new StyleRunProperties(new Bold())),
			("Strong", "Emphasis", new StyleRunProperties(new Bold { Val = false })));
		var run = new Run(new RunProperties(
			new RunStyle { Val = "Strong" },
			new Bold()));
		var result = EffectiveFormattingResolver.Resolve(
			defaults,
			CreateThemeInfo(),
			null,
			null,
			CreateParagraphHierarchy(),
			characterHierarchy,
			new Paragraph(),
			run);
		result.ToggleState.Bold.Should().BeTrue();
	}

	[Fact]
	public void Resolve_ResolvesThemeColorWhenDirectColorIsMissing()
	{
		var run = new Run(new RunProperties(
			new Color
			{
				ThemeColor = ThemeColorValues.Accent1,
				ThemeTint = "80"
			}));
		var result = EffectiveFormattingResolver.Resolve(
			CreateDefaults(new ParagraphPropertiesBaseStyle(), new RunPropertiesBaseStyle()),
			CreateThemeInfo(("accent1", "808080")),
			null,
			null,
			CreateParagraphHierarchy(),
			CreateCharacterHierarchy(),
			new Paragraph(),
			run);
		result.ResolvedRunColor.Should().Be("BFBFBF");
		result.RunProperties.GetFirstChild<Color>()?.Val?.Value.Should().Be("BFBFBF");
	}

	[Fact]
	public void Resolve_PreservesDirectColorWithoutThemeResolution()
	{
		var run = new Run(new RunProperties(new Color { Val = "ABCDEF" }));
		var result = EffectiveFormattingResolver.Resolve(
			CreateDefaults(new ParagraphPropertiesBaseStyle(), new RunPropertiesBaseStyle()),
			CreateThemeInfo(("accent1", "808080")),
			null,
			null,
			CreateParagraphHierarchy(),
			CreateCharacterHierarchy(),
			new Paragraph(),
			run);
		result.ResolvedRunColor.Should().Be("ABCDEF");
	}

	[Fact]
	public void Resolve_WithColorElementButNoDirectOrThemeColor_ReturnsNullColor()
	{
		var run = new Run(new RunProperties(new Color()));

		var result = EffectiveFormattingResolver.Resolve(
			CreateDefaults(new ParagraphPropertiesBaseStyle(), new RunPropertiesBaseStyle()),
			CreateThemeInfo(("accent1", "808080")),
			null,
			null,
			CreateParagraphHierarchy(),
			CreateCharacterHierarchy(),
			new Paragraph(),
			run);

		result.ResolvedRunColor.Should().BeNull();
	}

	[Fact]
	public void Resolve_IncludesNumberingStyleInResult()
	{
		var numbering = new NumberingLevelStyle
		{
			LevelIndex = 2,
			Start = 7,
			NumberFormat = "decimal",
			LevelText = "%3."
		};
		var result = EffectiveFormattingResolver.Resolve(
			CreateDefaults(new ParagraphPropertiesBaseStyle(), new RunPropertiesBaseStyle()),
			CreateThemeInfo(),
			numbering,
			null,
			CreateParagraphHierarchy(),
			CreateCharacterHierarchy(),
			new Paragraph(),
			new Run());
		result.NumberingLevel.Should().Be(numbering);
	}

	[Fact]
	public void Resolve_WithMissingStyleIds_UsesAvailableInputsOnly()
	{
		var defaults = CreateDefaults(
			new ParagraphPropertiesBaseStyle(new SpacingBetweenLines { Before = "120" }),
			new RunPropertiesBaseStyle(new Color { Val = "111111" }));
		var paragraph = new Paragraph(new ParagraphProperties(new ParagraphStyleId { Val = "MissingParagraphStyle" }));
		var run = new Run(new RunProperties(new RunStyle { Val = "MissingCharacterStyle" }));
		var result = EffectiveFormattingResolver.Resolve(
			defaults,
			CreateThemeInfo(),
			null,
			null,
			CreateParagraphHierarchy(),
			CreateCharacterHierarchy(),
			paragraph,
			run);
		result.ParagraphProperties.GetFirstChild<SpacingBetweenLines>()?.Before?.Value.Should().Be("120");
		result.RunProperties.GetFirstChild<Color>()?.Val?.Value.Should().Be("111111");
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
				Properties = style.Props
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
