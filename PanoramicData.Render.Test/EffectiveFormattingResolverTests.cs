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
		var tableStyle = CreateTableStyle(new StyleParagraphProperties(new Justification { Val = JustificationValues.Center }), null);
		var paragraphHierarchy = CreateParagraphHierarchy(
			("Base", null, new StyleParagraphProperties(new Justification { Val = JustificationValues.Right })),
			("Heading", "Base", new StyleParagraphProperties(new Justification { Val = JustificationValues.Both })));
		var paragraph = new Paragraph(
			new ParagraphProperties(
				new ParagraphStyleId { Val = "Heading" },
				new Justification { Val = JustificationValues.Distribute }));
		var run = new Run();
		var result = Resolve(defaults: defaults, tableStyle: tableStyle, paragraphStyles: paragraphHierarchy, paragraph: paragraph, run: run);
		result.ParagraphProperties.GetFirstChild<Justification>()?.Val?.Value.Should().Be(JustificationValues.Distribute);
	}

	[Fact]
	public void Resolve_AppliesRunPropertyPrecedenceThroughCascade()
	{
		var defaults = CreateDefaults(
			new ParagraphPropertiesBaseStyle(),
			new RunPropertiesBaseStyle(new Color { Val = "111111" }));
		var tableStyle = CreateTableStyle(null, new StyleRunProperties(new Color { Val = "222222" }));
		var characterHierarchy = CreateCharacterHierarchy(
			("Emphasis", null, new StyleRunProperties(new Color { Val = "333333" })),
			("Strong", "Emphasis", new StyleRunProperties(new Color { Val = "444444" })));
		var run = new Run(
			new RunProperties(
				new RunStyle { Val = "Strong" },
				new Color { Val = "555555" }));
		var result = Resolve(defaults: defaults, tableStyle: tableStyle, characterStyles: characterHierarchy, run: run);
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
		var result = Resolve(defaults: defaults, characterStyles: characterHierarchy, run: run);
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
		var result = Resolve(theme: CreateThemeInfo(("accent1", "808080")), run: run);
		result.ResolvedRunColor.Should().Be("BFBFBF");
		result.RunProperties.GetFirstChild<Color>()?.Val?.Value.Should().Be("BFBFBF");
	}

	[Fact]
	public void Resolve_PreservesDirectColorWithoutThemeResolution()
	{
		var run = new Run(new RunProperties(new Color { Val = "ABCDEF" }));
		var result = Resolve(theme: CreateThemeInfo(("accent1", "808080")), run: run);
		result.ResolvedRunColor.Should().Be("ABCDEF");
	}

	[Fact]
	public void Resolve_WithColorElementButNoDirectOrThemeColor_ReturnsNullColor()
	{
		var run = new Run(new RunProperties(new Color()));

		var result = Resolve(theme: CreateThemeInfo(("accent1", "808080")), run: run);

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
		var result = Resolve(numbering: numbering);
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
		var result = Resolve(defaults: defaults, paragraph: paragraph, run: run);
		result.ParagraphProperties.GetFirstChild<SpacingBetweenLines>()?.Before?.Value.Should().Be("120");
		result.RunProperties.GetFirstChild<Color>()?.Val?.Value.Should().Be("111111");
	}

	[Fact]
	public void Resolve_WhenNoColorElement_ReturnsNullResolvedRunColor()
	{
		var result = Resolve(theme: CreateThemeInfo(("accent1", "808080")));

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

		var result = Resolve(run: run);

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

		var result = Resolve(theme: CreateThemeInfo(("accent1", "808080")), run: run);

		result.ResolvedRunColor.Should().Be("404040");
	}

	[Fact]
	public void Resolve_TableRunPropertiesContributeToggleState()
	{
		var tableStyle = CreateTableStyle(null, new StyleRunProperties(new Bold()));

		var result = Resolve(tableStyle: tableStyle);

		result.ToggleState.Bold.Should().BeTrue();
	}

	[Fact]
	public void Resolve_DirectSetFalseWinsOverEarlierToggles()
	{
		var defaults = CreateDefaults(
			new ParagraphPropertiesBaseStyle(),
			new RunPropertiesBaseStyle(new Bold()));

		var run = new Run(new RunProperties(new Bold { Val = false }));

		var result = Resolve(defaults: defaults, run: run);

		result.ToggleState.Bold.Should().BeFalse();
	}

	[Fact]
	public void Resolve_ParagraphStyleAppliesWhenDirectParagraphFormattingAbsent()
	{
		var paragraphHierarchy = CreateParagraphHierarchy(
			("Body", null, new StyleParagraphProperties(new Justification { Val = JustificationValues.Center })));

		var paragraph = new Paragraph(new ParagraphProperties(new ParagraphStyleId { Val = "Body" }));

		var result = Resolve(paragraphStyles: paragraphHierarchy, paragraph: paragraph);

		result.ParagraphProperties.GetFirstChild<Justification>()?.Val?.Value.Should().Be(JustificationValues.Center);
	}

	[Fact]
	public void Resolve_CharacterStyleAppliesWhenDirectRunFormattingAbsent()
	{
		var characterHierarchy = CreateCharacterHierarchy(
			("Emphasis", null, new StyleRunProperties(new Color { Val = "333333" })));

		var run = new Run(new RunProperties(new RunStyle { Val = "Emphasis" }));

		var result = Resolve(characterStyles: characterHierarchy, run: run);

		result.RunProperties.GetFirstChild<Color>()?.Val?.Value.Should().Be("333333");
	}

	[Fact]
	public void Resolve_ParagraphStyleRunPropertiesApplyWhenDirectRunFormattingAbsent()
	{
		var paragraphHierarchy = CreateParagraphHierarchyWithRunProperties(
			("TOC1", null, new StyleParagraphProperties(), new StyleRunProperties(new Bold(), new FontSize { Val = "28" })));

		var paragraph = new Paragraph(new ParagraphProperties(new ParagraphStyleId { Val = "TOC1" }));

		var result = Resolve(paragraphStyles: paragraphHierarchy, paragraph: paragraph);

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

		var result = Resolve(paragraphStyles: paragraphHierarchy, paragraph: paragraph);

		result.ParagraphProperties.GetFirstChild<SpacingBetweenLines>()?.Before?.Value.Should().Be("360");
	}

	private static ResolvedTableStyle CreateTableStyle(StyleParagraphProperties? paragraphProperties, StyleRunProperties? runProperties)
		=> new()
		{
			StyleId = "TableGrid",
			TableProperties = null,
			TableRowProperties = null,
			TableCellProperties = null,
			ParagraphProperties = paragraphProperties,
			RunProperties = runProperties,
			AppliedConditionals = []
		};
}