using SkiaSharp;
using System.Collections.Concurrent;

namespace PanoramicData.Render;

internal sealed partial class FontResolver
{
	private bool TryResolveFont(string familyName, out string? resolvedFamily, out string? path)
	{
		if (TryResolveConfiguredFamily(familyName, out resolvedFamily, out path))
		{
			return true;
		}

		if (TryResolveFallbackFamily(out resolvedFamily, out path))
		{
			return true;
		}

		if (TryGetSansSerifFallbackPath(out resolvedFamily, out path))
		{
			return true;
		}

		resolvedFamily = null;
		path = null;
		return false;
	}

	private bool TryGetSansSerifFallbackPath(out string? familyName, out string? path)
	{
		foreach (var family in _preferredSansSerifFamilies)
		{
			if (FamilyIndex.TryGetValue(family, out var resolved))
			{
				familyName = family;
				path = resolved;
				return true;
			}
		}

		foreach (var pair in FamilyIndex)
		{
			if (pair.Key.Contains("sans", StringComparison.OrdinalIgnoreCase))
			{
				familyName = pair.Key;
				path = pair.Value;
				return true;
			}
		}

		familyName = null;
		path = null;
		return false;
	}

	private static readonly Func<string, bool, bool, SKTypeface?> CreateTypefaceCore = (filePath, _, _) =>
	{
		try { return SKTypeface.FromFile(filePath); } catch { return null; }
	};

	private static IReadOnlyDictionary<string, string> CreateSubstitutionMap(IReadOnlyDictionary<string, string>? substitutions)
	{
		if (substitutions is null || substitutions.Count == 0)
		{
			return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}

		var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (var pair in substitutions)
		{
			if (string.IsNullOrWhiteSpace(pair.Key))
			{
				continue;
			}

			map[pair.Key] = pair.Value;
		}

		return map;
	}

	private IReadOnlyDictionary<string, string> BuildFamilyIndex(IReadOnlyList<string>? directories)
	{
		var index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (directories is null)
		{
			return index;
		}

		foreach (var directory in directories.Where(d => !string.IsNullOrWhiteSpace(d) && Directory.Exists(d)))
		{
			foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
			{
				if (_supportedExtensions.Contains(Path.GetExtension(file)))
				{
					IndexFontFile(index, file);
				}
			}
		}

		return index;
	}
}
