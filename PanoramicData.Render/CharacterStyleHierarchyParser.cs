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
		var chains = ResolveChains(styles);
		return new CharacterStyleHierarchy(styles, chains);
	}

	private static IReadOnlyDictionary<string, CharacterStyleInfo> ParseStyles(StyleDefinitionsPart? stylesPart)
	{
		var styleElements = stylesPart?.Styles?.Elements<Style>() ?? Enumerable.Empty<Style>();
		var result = new Dictionary<string, CharacterStyleInfo>(StringComparer.OrdinalIgnoreCase);

		foreach (var style in styleElements)
		{
			if (style.Type?.Value != StyleValues.Character)
			{
				continue;
			}

			var styleId = style.StyleId?.Value;
			if (string.IsNullOrWhiteSpace(styleId))
			{
				continue;
			}

			var properties = style.StyleRunProperties is null
				? new StyleRunProperties()
				: (StyleRunProperties)style.StyleRunProperties.CloneNode(true);

			result[styleId] = new CharacterStyleInfo
			{
				StyleId = styleId,
				Name = style.StyleName?.Val?.Value,
				BasedOnStyleId = style.BasedOn?.Val?.Value,
				IsDefault = style.Default?.Value ?? false,
				Properties = properties
			};
		}

		return result;
	}

	private static IReadOnlyDictionary<string, IReadOnlyList<string>> ResolveChains(
		IReadOnlyDictionary<string, CharacterStyleInfo> styles)
	{
		var chains = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
		foreach (var styleId in styles.Keys)
		{
			chains[styleId] = ResolveChain(styleId, styles);
		}

		return chains;
	}

	private static IReadOnlyList<string> ResolveChain(
		string styleId,
		IReadOnlyDictionary<string, CharacterStyleInfo> styles)
	{
		var chain = new List<string>();
		var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var currentStyleId = styleId;

		while (!string.IsNullOrWhiteSpace(currentStyleId) && visited.Add(currentStyleId))
		{
			if (!styles.TryGetValue(currentStyleId, out var style))
			{
				break;
			}

			chain.Add(currentStyleId);
			currentStyleId = style.BasedOnStyleId;
		}

		return chain;
	}
}
