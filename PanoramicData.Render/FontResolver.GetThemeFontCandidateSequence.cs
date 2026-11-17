using SkiaSharp;
using System.Collections.Concurrent;

namespace PanoramicData.Render;

internal sealed partial class FontResolver
{
	private static IEnumerable<string?> GetThemeFontCandidateSequence(ThemeFontInfo themeFont, string? script)
	{
		if (!string.IsNullOrWhiteSpace(script) && themeFont.ScriptFonts.TryGetValue(script, out var exactMatch))
		{
			yield return exactMatch;
		}

		if (IsEastAsianScript(script))
		{
			yield return themeFont.EastAsian;
		}

		if (IsComplexScript(script))
		{
			yield return themeFont.ComplexScript;
		}

		yield return themeFont.Latin;
		yield return themeFont.EastAsian;
		yield return themeFont.ComplexScript;
	}

	private static bool IsEastAsianScript(string? script)
	{
		return !string.IsNullOrWhiteSpace(script) && _eastAsianScripts.Contains(script);
	}

	private static bool IsComplexScript(string? script)
	{
		return !string.IsNullOrWhiteSpace(script) && _complexScripts.Contains(script);
	}

	private bool TryResolveConfiguredFamily(string familyName, out string? resolvedFamily, out string? path)
	{
		if (string.IsNullOrWhiteSpace(familyName))
		{
			resolvedFamily = null;
			path = null;
			return false;
		}

		if (FamilyIndex.TryGetValue(familyName, out var resolved))
		{
			resolvedFamily = familyName;
			path = resolved;
			return true;
		}

		if (_fontSubstitutions.TryGetValue(familyName, out var replacement)
			&& !string.IsNullOrWhiteSpace(replacement)
			&& FamilyIndex.TryGetValue(replacement, out resolved))
		{
			resolvedFamily = replacement;
			path = resolved;
			return true;
		}

		resolvedFamily = null;
		path = null;
		return false;
	}

	private bool TryResolveFallbackFamily(out string? resolvedFamily, out string? path)
	{
		if (!string.IsNullOrWhiteSpace(_fallbackFontFamily)
			&& FamilyIndex.TryGetValue(_fallbackFontFamily, out var resolved))
		{
			resolvedFamily = _fallbackFontFamily;
			path = resolved;
			return true;
		}

		resolvedFamily = null;
		path = null;
		return false;
	}

	private static string CreateTypefaceCacheKey(string familyName, bool bold, bool italic)
	{
		return string.Concat(familyName, "|", bold ? "1" : "0", "|", italic ? "1" : "0");
	}
}
