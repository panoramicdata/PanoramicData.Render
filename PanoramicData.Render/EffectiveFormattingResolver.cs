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

		var paragraphProperties = new ParagraphProperties();
		var runProperties = new RunProperties();
		var toggleState = new ToggleState();

		Merge(paragraphProperties, documentDefaults.ParagraphProperties);
		Merge(runProperties, documentDefaults.RunProperties);
		toggleState = TogglePropertyLogic.Apply(toggleState, ParseToggles(documentDefaults.RunProperties));

		if (tableStyle is not null)
		{
			Merge(paragraphProperties, tableStyle.ParagraphProperties);
			Merge(runProperties, tableStyle.RunProperties);
			toggleState = TogglePropertyLogic.Apply(toggleState, ParseToggles(tableStyle.RunProperties));
		}

		var paragraphStyleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
		foreach (var styleId in paragraphStyles.GetInheritanceChain(paragraphStyleId ?? string.Empty).Reverse())
		{
			if (!paragraphStyles.Styles.TryGetValue(styleId, out var style))
			{
				continue;
			}

			Merge(paragraphProperties, style.Properties);
			Merge(runProperties, style.RunProperties);
			toggleState = TogglePropertyLogic.Apply(toggleState, ParseToggles(style.RunProperties));
		}

		var runStyleId = run.RunProperties?.RunStyle?.Val?.Value;
		foreach (var styleId in characterStyles.GetInheritanceChain(runStyleId ?? string.Empty).Reverse())
		{
			if (!characterStyles.Styles.TryGetValue(styleId, out var style))
			{
				continue;
			}

			Merge(runProperties, style.Properties);
			toggleState = TogglePropertyLogic.Apply(toggleState, ParseToggles(style.Properties));
		}

		Merge(paragraphProperties, paragraph.ParagraphProperties);
		Merge(runProperties, run.RunProperties);
		toggleState = TogglePropertyLogic.Apply(toggleState, ParseToggles(run.RunProperties));

		ResolveThemeFonts(themeInfo, runProperties);
		var resolvedRunColor = ResolveRunColor(themeInfo, runProperties);

		return new EffectiveFormatting
		{
			ParagraphProperties = paragraphProperties,
			RunProperties = runProperties,
			ToggleState = toggleState,
			ResolvedRunColor = resolvedRunColor,
			NumberingLevel = numberingStyle
		};
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

			// Leaf elements (no child elements) carry their data in XML attributes.
			// Merge at the attribute level so that a child style setting only some
			// attributes does not wipe inherited values from the parent.
			// This handles RunFonts, SpacingBetweenLines, Indentation, Color,
			// Shading, Languages, Underline, and all other leaf elements.
			if (existing is not null && !child.HasChildren && !existing.HasChildren)
			{
				foreach (var attr in child.GetAttributes())
				{
					existing.SetAttribute(attr);
				}

				continue;
			}

			// Composite elements with children (e.g. numPr containing numId + ilvl)
			// should be merged recursively so that a child style adding only ilvl
			// does not discard the inherited numId from the parent style.
			if (existing is OpenXmlCompositeElement existingComposite
				&& child is OpenXmlCompositeElement childComposite
				&& existingComposite.HasChildren
				&& childComposite.HasChildren)
			{
				Merge(existingComposite, childComposite);
				continue;
			}

			existing?.Remove();
			target.Append(child.CloneNode(true));
		}
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

		if (fonts.Ascii is null && fonts.AsciiTheme is not null)
		{
			fonts.Ascii = ResolveThemeFont(themeInfo, fonts.AsciiTheme);
		}

		if (fonts.HighAnsi is null && fonts.HighAnsiTheme is not null)
		{
			fonts.HighAnsi = ResolveThemeFont(themeInfo, fonts.HighAnsiTheme);
		}

		if (fonts.EastAsia is null && fonts.EastAsiaTheme is not null)
		{
			fonts.EastAsia = ResolveThemeFont(themeInfo, fonts.EastAsiaTheme);
		}

		if (fonts.ComplexScript is null && fonts.ComplexScriptTheme is not null)
		{
			fonts.ComplexScript = ResolveThemeFont(themeInfo, fonts.ComplexScriptTheme);
		}
	}

	private static string? ResolveThemeFont(ThemeInfo themeInfo, ThemeFontValues themeFont)
	{
		if (themeFont == ThemeFontValues.MajorHighAnsi || themeFont == ThemeFontValues.MajorAscii)
		{
			return themeInfo.MajorFont.Latin;
		}

		if (themeFont == ThemeFontValues.MinorHighAnsi || themeFont == ThemeFontValues.MinorAscii)
		{
			return themeInfo.MinorFont.Latin;
		}

		if (themeFont == ThemeFontValues.MajorEastAsia)
		{
			return themeInfo.MajorFont.EastAsian;
		}

		if (themeFont == ThemeFontValues.MinorEastAsia)
		{
			return themeInfo.MinorFont.EastAsian;
		}

		if (themeFont == ThemeFontValues.MajorBidi)
		{
			return themeInfo.MajorFont.ComplexScript;
		}

		if (themeFont == ThemeFontValues.MinorBidi)
		{
			return themeInfo.MinorFont.ComplexScript;
		}

		return null;
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
		if (!string.IsNullOrWhiteSpace(directColor))
		{
			return directColor;
		}

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
}
