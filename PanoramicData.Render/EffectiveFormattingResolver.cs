namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Computes effective paragraph and run formatting by applying the full style cascade.
/// </summary>
internal static partial class EffectiveFormattingResolver
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
}
