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
			existing?.Remove();
			target.Append(child.CloneNode(true));
		}
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
