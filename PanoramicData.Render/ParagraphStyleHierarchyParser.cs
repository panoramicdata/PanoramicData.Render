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
		var chains = ResolveChains(styles);
		return new ParagraphStyleHierarchy(styles, chains);
	}

	private static IReadOnlyDictionary<string, ParagraphStyleInfo> ParseStyles(StyleDefinitionsPart? stylesPart)
	{
		var styleElements = stylesPart?.Styles?.Elements<Style>() ?? Enumerable.Empty<Style>();
		var result = new Dictionary<string, ParagraphStyleInfo>(StringComparer.OrdinalIgnoreCase);

		foreach (var style in styleElements)
		{
			if (style.Type?.Value != StyleValues.Paragraph)
			{
				continue;
			}

			var styleId = style.StyleId?.Value;
			if (string.IsNullOrWhiteSpace(styleId))
			{
				continue;
			}

			var properties = style.StyleParagraphProperties is null
				? new StyleParagraphProperties()
				: (StyleParagraphProperties)style.StyleParagraphProperties.CloneNode(true);
			var runProperties = style.StyleRunProperties is null
				? null
				: (StyleRunProperties)style.StyleRunProperties.CloneNode(true);

			result[styleId] = new ParagraphStyleInfo
			{
				StyleId = styleId,
				Name = style.StyleName?.Val?.Value,
				BasedOnStyleId = style.BasedOn?.Val?.Value,
				IsDefault = style.Default?.Value ?? false,
				Properties = properties,
				RunProperties = runProperties
			};
		}

		return result;
	}

	private static IReadOnlyDictionary<string, IReadOnlyList<string>> ResolveChains(
		IReadOnlyDictionary<string, ParagraphStyleInfo> styles)
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
		IReadOnlyDictionary<string, ParagraphStyleInfo> styles)
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
