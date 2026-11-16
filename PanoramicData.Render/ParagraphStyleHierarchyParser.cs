namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Parses paragraph styles and resolves <c>w:basedOn</c> inheritance chains.
/// </summary>
internal static class ParagraphStyleHierarchyParser
{
	/// <summary>
	/// Parses paragraph styles from the styles part and resolves inheritance chains.
	/// </summary>
	/// <param name="stylesPart">The styles part, or <see langword="null"/> if missing.</param>
	/// <returns>A parsed paragraph style hierarchy.</returns>
	public static ParagraphStyleHierarchy Parse(StyleDefinitionsPart? stylesPart)
	{
		var styles = ParseStyles(stylesPart);
		var chains = StyleChainResolver.ResolveChains(styles, static s => s.BasedOnStyleId);
		var defaultStyleId = styles.Values.FirstOrDefault(s => s.IsDefault)?.StyleId;
		return new ParagraphStyleHierarchy(styles, chains, defaultStyleId);
	}

	private static IReadOnlyDictionary<string, ParagraphStyleInfo> ParseStyles(StyleDefinitionsPart? stylesPart)
	{
		var styleElements = stylesPart?.Styles?.Elements<Style>() ?? Enumerable.Empty<Style>();
		var result = new Dictionary<string, ParagraphStyleInfo>(StringComparer.OrdinalIgnoreCase);

		foreach (var style in styleElements)
		{
			if (StyleChainResolver.TryGetStyleId(style, StyleValues.Paragraph, out var styleId))
			{
				result[styleId] = CreateStyleInfo(style, styleId);
			}
		}

		return result;
	}

	private static ParagraphStyleInfo CreateStyleInfo(Style style, string styleId)
	{
		var properties = style.StyleParagraphProperties is null
			? new StyleParagraphProperties()
			: (StyleParagraphProperties)style.StyleParagraphProperties.CloneNode(true);
		var runProperties = style.StyleRunProperties is null
			? null
			: (StyleRunProperties)style.StyleRunProperties.CloneNode(true);

		return new ParagraphStyleInfo
		{
			StyleId = styleId,
			Name = style.StyleName?.Val?.Value,
			BasedOnStyleId = style.BasedOn?.Val?.Value,
			IsDefault = style.Default?.Value ?? false,
			Properties = properties,
			RunProperties = runProperties
		};
	}
}