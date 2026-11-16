namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Shared style-id and based-on chain logic for the style hierarchy parsers.
/// </summary>
internal static class StyleChainResolver
{
	internal static bool TryGetStyleId(Style style, StyleValues expectedType, out string styleId)
	{
		styleId = style.StyleId?.Value ?? string.Empty;
		return style.Type?.Value == expectedType && !string.IsNullOrWhiteSpace(styleId);
	}

	internal static string? GetName(Style style) => style.StyleName?.Val?.Value;

	internal static string? GetBasedOnId(Style style) => style.BasedOn?.Val?.Value;

	internal static bool IsDefault(Style style) => style.Default?.Value ?? false;

	internal static IReadOnlyDictionary<string, IReadOnlyList<string>> ResolveChains<TStyle>(
		IReadOnlyDictionary<string, TStyle> styles,
		Func<TStyle, string?> basedOnSelector)
	{
		var chains = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
		foreach (var styleId in styles.Keys)
		{
			chains[styleId] = ResolveChain(styleId, styles, basedOnSelector);
		}

		return chains;
	}

	private static List<string> ResolveChain<TStyle>(
		string styleId,
		IReadOnlyDictionary<string, TStyle> styles,
		Func<TStyle, string?> basedOnSelector)
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
			currentStyleId = basedOnSelector(style);
		}

		return chain;
	}
}