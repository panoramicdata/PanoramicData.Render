using SkiaSharp;

namespace PanoramicData.Render;

/// <summary>
/// Scans configured directories for font files and builds a family-name index.
/// </summary>
internal sealed class FontResolver
{
	private static readonly HashSet<string> _supportedExtensions = new(StringComparer.OrdinalIgnoreCase)
	{
		".ttf",
		".otf",
		".ttc"
	};
	private static readonly string[] _preferredSansSerifFamilies =
	[
		"Arial",
		"Segoe UI",
		"Calibri",
		"Aptos",
		"Helvetica",
		"Verdana",
		"Tahoma",
		"Trebuchet MS",
		"Gill Sans",
		"Liberation Sans",
		"DejaVu Sans",
		"Noto Sans",
		"Open Sans",
		"Source Sans 3",
		"Roboto"
	];
	private static readonly HashSet<string> _eastAsianScripts = new(StringComparer.OrdinalIgnoreCase)
	{
		"Jpan",
		"Hang",
		"Hans",
		"Hant",
		"Kore"
	};
	private static readonly HashSet<string> _complexScripts = new(StringComparer.OrdinalIgnoreCase)
	{
		"Arab",
		"Hebr",
		"Thai",
		"Deva",
		"Beng",
		"Guru",
		"Gujr",
		"Orya",
		"Taml",
		"Telu",
		"Knda",
		"Mlym",
		"Sinh",
		"Syrc",
		"Thaa",
		"Laoo",
		"Tibt",
		"Mymr",
		"Khmr"
	};

	private readonly IReadOnlyDictionary<string, string> _fontSubstitutions;
	private readonly string _fallbackFontFamily;
	private readonly IFontMetadataReader _metadataReader;
	private readonly Func<string, bool, bool, SKTypeface?> _typefaceFactory;
	private readonly Dictionary<string, SKTypeface> _typefaceCache = new(StringComparer.Ordinal);

	/// <summary>
	/// Initializes a new instance of the <see cref="FontResolver"/> class.
	/// </summary>
	/// <param name="fontDirectories">Directories to scan for font files.</param>
	public FontResolver(IReadOnlyList<string>? fontDirectories)
	{
		_fontSubstitutions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		_fallbackFontFamily = string.Empty;
		_metadataReader = new SkiaFontMetadataReader();
		_typefaceFactory = CreateTypefaceCore;
		FamilyIndex = BuildFamilyIndex(fontDirectories);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="FontResolver"/> class.
	/// </summary>
	/// <param name="options">Rendering options containing font directories and substitutions.</param>
	public FontResolver(RenderOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		_fontSubstitutions = CreateSubstitutionMap(options.FontSubstitutions);
		_fallbackFontFamily = options.FallbackFontFamily;
		_metadataReader = new SkiaFontMetadataReader();
		_typefaceFactory = CreateTypefaceCore;
		FamilyIndex = BuildFamilyIndex(options.FontDirectories);
	}

	internal FontResolver(IReadOnlyList<string>? fontDirectories, IFontMetadataReader metadataReader)
	{
		ArgumentNullException.ThrowIfNull(metadataReader);
		_fontSubstitutions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		_fallbackFontFamily = string.Empty;
		_metadataReader = metadataReader;
		_typefaceFactory = CreateTypefaceCore;
		FamilyIndex = BuildFamilyIndex(fontDirectories);
	}

	internal FontResolver(RenderOptions options, IFontMetadataReader metadataReader, Func<string, bool, bool, SKTypeface?> typefaceFactory)
	{
		ArgumentNullException.ThrowIfNull(options);
		ArgumentNullException.ThrowIfNull(metadataReader);
		ArgumentNullException.ThrowIfNull(typefaceFactory);

		_fontSubstitutions = CreateSubstitutionMap(options.FontSubstitutions);
		_fallbackFontFamily = options.FallbackFontFamily;
		_metadataReader = metadataReader;
		_typefaceFactory = typefaceFactory;
		FamilyIndex = BuildFamilyIndex(options.FontDirectories);
	}

	/// <summary>
	/// Gets the indexed mapping of font family name to file path.
	/// </summary>
	public IReadOnlyDictionary<string, string> FamilyIndex { get; }

	/// <summary>
	/// Attempts to resolve a font family to a scanned font file path.
	/// </summary>
	/// <param name="familyName">The font family name.</param>
	/// <param name="path">When successful, receives the resolved font file path.</param>
	/// <returns><see langword="true"/> when the family is indexed; otherwise <see langword="false"/>.</returns>
	public bool TryGetFontPath(string familyName, out string? path)
	{
		var resolved = TryResolveFont(familyName, out _, out path);
		return resolved;
	}

	/// <summary>
	/// Attempts to resolve and create a typeface for the requested family and style.
	/// </summary>
	/// <param name="familyName">The requested font family name.</param>
	/// <param name="bold">A value indicating whether bold styling is requested.</param>
	/// <param name="italic">A value indicating whether italic styling is requested.</param>
	/// <param name="typeface">When successful, receives the cached or newly created typeface.</param>
	/// <returns><see langword="true"/> when a typeface could be resolved and created; otherwise <see langword="false"/>.</returns>
	public bool TryGetTypeface(string familyName, bool bold, bool italic, out SKTypeface? typeface)
	{
		if (!TryResolveFont(familyName, out var resolvedFamily, out var path))
		{
			typeface = null;
			return false;
		}

		var cacheKey = CreateTypefaceCacheKey(resolvedFamily!, bold, italic);
		if (_typefaceCache.TryGetValue(cacheKey, out var cached))
		{
			typeface = cached;
			return true;
		}

		typeface = _typefaceFactory(path!, bold, italic);
		if (typeface is null)
		{
			return false;
		}

		_typefaceCache[cacheKey] = typeface;
		return true;
	}

	/// <summary>
	/// Attempts to resolve a concrete font family for a theme major/minor font set and script.
	/// </summary>
	/// <param name="themeInfo">The parsed theme information.</param>
	/// <param name="useMajorFont"><see langword="true"/> to resolve from the major font set; otherwise the minor font set.</param>
	/// <param name="script">Optional script tag such as <c>Jpan</c>, <c>Hans</c>, or <c>Arab</c>.</param>
	/// <param name="familyName">When successful, receives the resolved concrete family name.</param>
	/// <returns><see langword="true"/> when a usable family name could be resolved; otherwise <see langword="false"/>.</returns>
	public bool TryResolveThemeFontFamily(ThemeInfo themeInfo, bool useMajorFont, string? script, out string? familyName)
	{
		ArgumentNullException.ThrowIfNull(themeInfo);

		var themeFont = useMajorFont ? themeInfo.MajorFont : themeInfo.MinorFont;
		foreach (var candidate in GetThemeFontCandidates(themeFont, script))
		{
			if (TryResolveConfiguredFamily(candidate, out familyName, out _))
			{
				return true;
			}
		}

		if (TryResolveFallbackFamily(out familyName, out _))
		{
			return true;
		}

		if (TryGetSansSerifFallbackPath(out familyName, out _))
		{
			return true;
		}

		familyName = null;
		return false;
	}

	private static IEnumerable<string> GetThemeFontCandidates(ThemeFontInfo themeFont, string? script)
	{
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var candidate in GetThemeFontCandidateSequence(themeFont, script))
		{
			if (!string.IsNullOrWhiteSpace(candidate) && seen.Add(candidate))
			{
				yield return candidate;
			}
		}
	}

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

	private static SKTypeface? CreateTypefaceCore(string filePath, bool bold, bool italic)
	{
		try { return SKTypeface.FromFile(filePath); } catch { return null; }
	}

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

		foreach (var directory in directories)
		{
			if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
			{
				continue;
			}

			foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
			{
				var extension = Path.GetExtension(file);
				if (!_supportedExtensions.Contains(extension))
				{
					continue;
				}

				var familyNames = _metadataReader.ReadFamilyNames(file);
				if (familyNames.Count == 0)
				{
					var fallback = Path.GetFileNameWithoutExtension(file);
					familyNames = string.IsNullOrWhiteSpace(fallback) ? [] : [fallback];
				}

				foreach (var familyName in familyNames)
				{
					if (string.IsNullOrWhiteSpace(familyName) || index.ContainsKey(familyName))
					{
						continue;
					}

					index[familyName] = file;
				}
			}
		}

		return index;
	}
}
