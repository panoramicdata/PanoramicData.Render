namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Computes effective paragraph and run formatting by applying the full style cascade.
/// </summary>
internal static class EffectiveFormattingResolver
{
	/// <summary>
	/// Computes effective formatting for a run within a paragraph.
	/// </summary>
	public static EffectiveFormatting Resolve(
		DocumentDefaults documentDefaults,
		ThemeInfo themeInfo,
		NumberingLevelStyle? numberingStyle,
		ResolvedTableStyle? tableStyle,
		ParagraphStyleHierarchy paragraphStyles,
		CharacterStyleHierarchy characterStyles,
		Paragraph paragraph,
		Run run)
	{
		ArgumentNullException.ThrowIfNull(documentDefaults);
		ArgumentNullException.ThrowIfNull(themeInfo);
		ArgumentNullException.ThrowIfNull(paragraphStyles);
		ArgumentNullException.ThrowIfNull(characterStyles);
		ArgumentNullException.ThrowIfNull(paragraph);
		ArgumentNullException.ThrowIfNull(run);

		var cascade = new StyleCascade();
		cascade.Apply(documentDefaults.ParagraphProperties, documentDefaults.RunProperties, documentDefaults.RunProperties);
		if (tableStyle is not null)
		{
			cascade.Apply(tableStyle.ParagraphProperties, tableStyle.RunProperties, tableStyle.RunProperties);
		}

		ApplyParagraphStyles(cascade, paragraphStyles, paragraph);
		ApplyCharacterStyles(cascade, characterStyles, run);
		cascade.Apply(paragraph.ParagraphProperties, run.RunProperties, run.RunProperties);

		ResolveThemeFonts(themeInfo, cascade.RunProperties);
		var resolvedRunColor = ResolveRunColor(themeInfo, cascade.RunProperties);

		return new EffectiveFormatting
		{
			ParagraphProperties = cascade.ParagraphProperties,
			RunProperties = cascade.RunProperties,
			ToggleState = cascade.ToggleState,
			ResolvedRunColor = resolvedRunColor,
			NumberingLevel = numberingStyle
		};
	}

	private static void ApplyParagraphStyles(StyleCascade cascade, ParagraphStyleHierarchy paragraphStyles, Paragraph paragraph)
	{
		var paragraphStyleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
		foreach (var styleId in paragraphStyles.GetInheritanceChain(paragraphStyleId ?? string.Empty).Reverse())
		{
			if (paragraphStyles.Styles.TryGetValue(styleId, out var style))
			{
				cascade.Apply(style.Properties, style.RunProperties, style.RunProperties);
			}
		}
	}

	private static void ApplyCharacterStyles(StyleCascade cascade, CharacterStyleHierarchy characterStyles, Run run)
	{
		var runStyleId = run.RunProperties?.RunStyle?.Val?.Value;
		foreach (var styleId in characterStyles.GetInheritanceChain(runStyleId ?? string.Empty).Reverse())
		{
			if (characterStyles.Styles.TryGetValue(styleId, out var style))
			{
				cascade.Apply(null, style.Properties, style.Properties);
			}
		}
	}
	internal static void Merge(OpenXmlCompositeElement target, OpenXmlCompositeElement? source)
	{
		if (source is null)
		{
			return;
		}

		foreach (var child in source.ChildElements)
		{
			var existing = target.ChildElements
				.FirstOrDefault(e => e.LocalName == child.LocalName && e.NamespaceUri == child.NamespaceUri);

			if (TryMergeLeaf(existing, child) || TryMergeComposite(existing, child))
			{
				continue;
			}

			existing?.Remove();
			target.Append(child.CloneNode(true));
		}
	}

	// Leaf elements carry their data in attributes: merge per attribute so a child style keeps inherited values.
	private static bool TryMergeLeaf(OpenXmlElement? existing, OpenXmlElement child)
	{
		if (existing is null || child.HasChildren || existing.HasChildren)
		{
			return false;
		}

		foreach (var attr in child.GetAttributes())
		{
			existing.SetAttribute(attr);
		}

		return true;
	}

	// Composite elements with children (for example numPr) merge recursively so inherited children survive.
	private static bool TryMergeComposite(OpenXmlElement? existing, OpenXmlElement child)
	{
		if (existing is not OpenXmlCompositeElement existingComposite
			|| child is not OpenXmlCompositeElement childComposite
			|| !existingComposite.HasChildren
			|| !childComposite.HasChildren)
		{
			return false;
		}

		Merge(existingComposite, childComposite);
		return true;
	}
	/// <summary>
	/// Resolves theme font references to concrete font names on the materialized RunProperties.
	/// Per OOXML spec, the concrete value (e.g. <c>ascii</c>) takes precedence; theme values
	/// are only used as a fallback when the concrete attribute is absent.
	/// </summary>
	private static void ResolveThemeFonts(ThemeInfo themeInfo, RunProperties runProperties)
	{
		var fonts = runProperties.GetFirstChild<RunFonts>();
		if (fonts is null)
		{
			return;
		}

		if (NeedsThemeFont(fonts.Ascii, fonts.AsciiTheme))
		{
			fonts.Ascii = ResolveThemeFont(themeInfo, fonts.AsciiTheme!);
		}

		if (NeedsThemeFont(fonts.HighAnsi, fonts.HighAnsiTheme))
		{
			fonts.HighAnsi = ResolveThemeFont(themeInfo, fonts.HighAnsiTheme!);
		}

		if (NeedsThemeFont(fonts.EastAsia, fonts.EastAsiaTheme))
		{
			fonts.EastAsia = ResolveThemeFont(themeInfo, fonts.EastAsiaTheme!);
		}

		if (NeedsThemeFont(fonts.ComplexScript, fonts.ComplexScriptTheme))
		{
			fonts.ComplexScript = ResolveThemeFont(themeInfo, fonts.ComplexScriptTheme!);
		}
	}

	private static bool NeedsThemeFont(StringValue? concrete, EnumValue<ThemeFontValues>? theme) => concrete is null && theme is not null;

	private static string? ResolveThemeFont(ThemeInfo themeInfo, ThemeFontValues themeFont)
		=> ResolveMajorThemeFont(themeInfo, themeFont) ?? ResolveMinorThemeFont(themeInfo, themeFont);

	private static string? ResolveMajorThemeFont(ThemeInfo themeInfo, ThemeFontValues themeFont)
	{
		if (themeFont == ThemeFontValues.MajorHighAnsi || themeFont == ThemeFontValues.MajorAscii)
		{
			return themeInfo.MajorFont.Latin;
		}

		if (themeFont == ThemeFontValues.MajorEastAsia)
		{
			return themeInfo.MajorFont.EastAsian;
		}

		return themeFont == ThemeFontValues.MajorBidi ? themeInfo.MajorFont.ComplexScript : null;
	}

	private static string? ResolveMinorThemeFont(ThemeInfo themeInfo, ThemeFontValues themeFont)
	{
		if (themeFont == ThemeFontValues.MinorHighAnsi || themeFont == ThemeFontValues.MinorAscii)
		{
			return themeInfo.MinorFont.Latin;
		}

		if (themeFont == ThemeFontValues.MinorEastAsia)
		{
			return themeInfo.MinorFont.EastAsian;
		}

		return themeFont == ThemeFontValues.MinorBidi ? themeInfo.MinorFont.ComplexScript : null;
	}
	private static ToggleProperties ParseToggles(OpenXmlCompositeElement? properties)
	{
		if (properties is null)
		{
			return new ToggleProperties();
		}

		return new ToggleProperties
		{
			Bold = ParseInstruction(properties.GetFirstChild<Bold>()),
			Italic = ParseInstruction(properties.GetFirstChild<Italic>()),
			Caps = ParseInstruction(properties.GetFirstChild<Caps>()),
			SmallCaps = ParseInstruction(properties.GetFirstChild<SmallCaps>()),
			Strike = ParseInstruction(properties.GetFirstChild<Strike>()),
			DoubleStrike = ParseInstruction(properties.GetFirstChild<DoubleStrike>()),
			Vanish = ParseInstruction(properties.GetFirstChild<Vanish>()),
			Emboss = ParseInstruction(properties.GetFirstChild<Emboss>()),
			Imprint = ParseInstruction(properties.GetFirstChild<Imprint>()),
			Outline = ParseInstruction(properties.GetFirstChild<Outline>()),
			Shadow = ParseInstruction(properties.GetFirstChild<Shadow>())
		};
	}

	private static ToggleInstruction ParseInstruction(OnOffType? property)
	{
		if (property is null)
		{
			return ToggleInstruction.None;
		}

		var val = property.Val?.Value;
		if (val is null || val.Value)
		{
			return ToggleInstruction.Toggle;
		}

		return ToggleInstruction.SetFalse;
	}

	private static string? ResolveRunColor(ThemeInfo themeInfo, RunProperties runProperties)
	{
		var color = runProperties.GetFirstChild<Color>();
		if (color is null)
		{
			return null;
		}

		var directColor = color.Val?.Value;
		return string.IsNullOrWhiteSpace(directColor) ? ResolveThemeRunColor(themeInfo, color) : directColor;
	}

	private static string? ResolveThemeRunColor(ThemeInfo themeInfo, Color color)
	{
		var themeColor = color.ThemeColor?.Value;
		if (themeColor is null)
		{
			return null;
		}

		var resolved = ThemeColorResolver.Resolve(
			themeInfo,
			themeColor,
			color.ThemeTint?.Value,
			color.ThemeShade?.Value);
		if (!string.IsNullOrWhiteSpace(resolved))
		{
			color.Val = resolved;
		}

		return resolved;
	}

	private sealed class StyleCascade
	{
		public ParagraphProperties ParagraphProperties { get; } = new();

		public RunProperties RunProperties { get; } = new();

		public ToggleState ToggleState { get; private set; } = new();

		public void Apply(OpenXmlCompositeElement? paragraphSource, OpenXmlCompositeElement? runSource, OpenXmlCompositeElement? toggleSource)
		{
			Merge(ParagraphProperties, paragraphSource);
			Merge(RunProperties, runSource);
			ToggleState = TogglePropertyLogic.Apply(ToggleState, ParseToggles(toggleSource));
		}
	}
}