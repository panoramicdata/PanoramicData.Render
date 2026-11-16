namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Parses character styles and resolves <c>w:basedOn</c> inheritance chains.
/// </summary>
internal static class CharacterStyleHierarchyParser
{
	/// <summary>
	/// Parses character styles from the styles part and resolves inheritance chains.
	/// </summary>
	/// <param name="stylesPart">The styles part, or <see langword="null"/> if missing.</param>
	/// <returns>A parsed character style hierarchy.</returns>
	public static CharacterStyleHierarchy Parse(StyleDefinitionsPart? stylesPart)
	{
		var styles = ParseStyles(stylesPart);
		var chains = StyleChainResolver.ResolveChains(styles, static s => s.BasedOnStyleId);
		return new CharacterStyleHierarchy(styles, chains);
	}

	private static IReadOnlyDictionary<string, CharacterStyleInfo> ParseStyles(StyleDefinitionsPart? stylesPart)
	{
		var styleElements = stylesPart?.Styles?.Elements<Style>() ?? Enumerable.Empty<Style>();
		var result = new Dictionary<string, CharacterStyleInfo>(StringComparer.OrdinalIgnoreCase);

		foreach (var style in styleElements)
		{
			if (StyleChainResolver.TryGetStyleId(style, StyleValues.Character, out var styleId))
			{
				result[styleId] = CreateStyleInfo(style, styleId);
			}
		}

		return result;
	}

	private static CharacterStyleInfo CreateStyleInfo(Style style, string styleId)
	{
		var properties = style.StyleRunProperties is null
			? new StyleRunProperties()
			: (StyleRunProperties)style.StyleRunProperties.CloneNode(true);

		return new CharacterStyleInfo
		{
			StyleId = styleId,
			Name = style.StyleName?.Val?.Value,
			BasedOnStyleId = style.BasedOn?.Val?.Value,
			IsDefault = style.Default?.Value ?? false,
			Properties = properties
		};
	}
}