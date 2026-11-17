namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public partial class EffectiveFormattingResolverTests
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
	public void Resolve_WhenNoColorElement_ReturnsNullResolvedRunColor()
	{
		var result = EffectiveFormattingResolver.Resolve(
			CreateDefaults(new ParagraphPropertiesBaseStyle(), new RunPropertiesBaseStyle()),
			CreateThemeInfo(("accent1", "808080")),
			null,
			null,
			CreateParagraphHierarchy(),
			CreateCharacterHierarchy(),
			new Paragraph(),
			new Run());

		result.ResolvedRunColor.Should().BeNull();
	}

	[Fact]
	public void Resolve_WhenThemeColorNotInThemeMap_ReturnsNullResolvedRunColor()
	{
		var run = new Run(new RunProperties(
			new Color
			{
				ThemeColor = ThemeColorValues.Accent1,
				ThemeTint = "80"
			}));

		var result = EffectiveFormattingResolver.Resolve(
			CreateDefaults(new ParagraphPropertiesBaseStyle(), new RunPropertiesBaseStyle()),
			CreateThemeInfo(),
			null,
			null,
			CreateParagraphHierarchy(),
			CreateCharacterHierarchy(),
			new Paragraph(),
			run);

		result.ResolvedRunColor.Should().BeNull();
	}

	[Fact]
	public void Resolve_AppliesThemeShadeModifier()
	{
		var run = new Run(new RunProperties(
			new Color
			{
				ThemeColor = ThemeColorValues.Accent1,
				ThemeShade = "80"
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

		result.ResolvedRunColor.Should().Be("404040");
	}

	[Fact]
	public void Resolve_TableRunPropertiesContributeToggleState()
	{
		var tableStyle = new ResolvedTableStyle
		{
			StyleId = "TableGrid",
			TableProperties = null,
			TableRowProperties = null,
			TableCellProperties = null,
			ParagraphProperties = null,
			RunProperties = new StyleRunProperties(new Bold()),
			AppliedConditionals = []
		};

		var result = EffectiveFormattingResolver.Resolve(
			CreateDefaults(new ParagraphPropertiesBaseStyle(), new RunPropertiesBaseStyle()),
			CreateThemeInfo(),
			null,
			tableStyle,
			CreateParagraphHierarchy(),
			CreateCharacterHierarchy(),
			new Paragraph(),
			new Run());

		result.ToggleState.Bold.Should().BeTrue();
	}

	[Fact]
	public void Resolve_DirectSetFalseWinsOverEarlierToggles()
	{
		var defaults = CreateDefaults(
			new ParagraphPropertiesBaseStyle(),
			new RunPropertiesBaseStyle(new Bold()));

		var run = new Run(new RunProperties(new Bold { Val = false }));

		var result = EffectiveFormattingResolver.Resolve(
			defaults,
			CreateThemeInfo(),
			null,
			null,
			CreateParagraphHierarchy(),
			CreateCharacterHierarchy(),
			new Paragraph(),
			run);

		result.ToggleState.Bold.Should().BeFalse();
	}

	[Fact]
	public void Resolve_ParagraphStyleAppliesWhenDirectParagraphFormattingAbsent()
	{
		var paragraphHierarchy = CreateParagraphHierarchy(
			("Body", null, new StyleParagraphProperties(new Justification { Val = JustificationValues.Center })));

		var paragraph = new Paragraph(new ParagraphProperties(new ParagraphStyleId { Val = "Body" }));

		var result = EffectiveFormattingResolver.Resolve(
			CreateDefaults(new ParagraphPropertiesBaseStyle(), new RunPropertiesBaseStyle()),
			CreateThemeInfo(),
			null,
			null,
			paragraphHierarchy,
			CreateCharacterHierarchy(),
			paragraph,
			new Run());

		result.ParagraphProperties.GetFirstChild<Justification>()?.Val?.Value.Should().Be(JustificationValues.Center);
	}

	[Fact]
	public void Resolve_CharacterStyleAppliesWhenDirectRunFormattingAbsent()
	{
		var characterHierarchy = CreateCharacterHierarchy(
			("Emphasis", null, new StyleRunProperties(new Color { Val = "333333" })));

		var run = new Run(new RunProperties(new RunStyle { Val = "Emphasis" }));

		var result = EffectiveFormattingResolver.Resolve(
			CreateDefaults(new ParagraphPropertiesBaseStyle(), new RunPropertiesBaseStyle()),
			CreateThemeInfo(),
			null,
			null,
			CreateParagraphHierarchy(),
			characterHierarchy,
			new Paragraph(),
			run);

		result.RunProperties.GetFirstChild<Color>()?.Val?.Value.Should().Be("333333");
	}

	[Fact]
	public void Resolve_ParagraphStyleRunPropertiesApplyWhenDirectRunFormattingAbsent()
	{
		var paragraphHierarchy = CreateParagraphHierarchyWithRunProperties(
			("TOC1", null, new StyleParagraphProperties(), new StyleRunProperties(new Bold(), new FontSize { Val = "28" })));

		var paragraph = new Paragraph(new ParagraphProperties(new ParagraphStyleId { Val = "TOC1" }));

		var result = EffectiveFormattingResolver.Resolve(
			CreateDefaults(new ParagraphPropertiesBaseStyle(), new RunPropertiesBaseStyle()),
			CreateThemeInfo(),
			null,
			null,
			paragraphHierarchy,
			CreateCharacterHierarchy(),
			paragraph,
			new Run());

		result.RunProperties.GetFirstChild<FontSize>()?.Val?.Value.Should().Be("28");
		result.RunProperties.GetFirstChild<Bold>().Should().NotBeNull();
		result.ToggleState.Bold.Should().BeTrue();
	}

	[Fact]
	public void Resolve_DirectParagraphFormattingOverridesParagraphStyle()
	{
		var paragraphHierarchy = CreateParagraphHierarchy(
			("Body", null, new StyleParagraphProperties(new SpacingBetweenLines { Before = "120" })));

		var paragraph = new Paragraph(new ParagraphProperties(
			new ParagraphStyleId { Val = "Body" },
			new SpacingBetweenLines { Before = "360" }));

		var result = EffectiveFormattingResolver.Resolve(
			CreateDefaults(new ParagraphPropertiesBaseStyle(), new RunPropertiesBaseStyle()),
			CreateThemeInfo(),
			null,
			null,
			paragraphHierarchy,
			CreateCharacterHierarchy(),
			paragraph,
			new Run());

		result.ParagraphProperties.GetFirstChild<SpacingBetweenLines>()?.Before?.Value.Should().Be("360");
	}
}
