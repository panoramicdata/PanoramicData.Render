namespace PanoramicData.Render;

/// <summary>
/// Scans configured directories for font files and builds a family-name index.
/// </summary>
internal sealed class FontResolver
{
	private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
	{
		".ttf",
		".otf",
		".ttc"
	};
	private static readonly string[] PreferredSansSerifFamilies =
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

	private readonly IReadOnlyDictionary<string, string> _familyIndex;
	private readonly IReadOnlyDictionary<string, string> _fontSubstitutions;
	private readonly string _fallbackFontFamily;
	private readonly IFontMetadataReader _metadataReader;

	/// <summary>
	/// Initializes a new instance of the <see cref="FontResolver"/> class.
	/// </summary>
	/// <param name="fontDirectories">Directories to scan for font files.</param>
	public FontResolver(IReadOnlyList<string>? fontDirectories)
	{
		_fontSubstitutions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		_fallbackFontFamily = string.Empty;
		_metadataReader = new SkiaFontMetadataReader();
		_familyIndex = BuildFamilyIndex(fontDirectories);
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
		_familyIndex = BuildFamilyIndex(options.FontDirectories);
	}

	internal FontResolver(IReadOnlyList<string>? fontDirectories, IFontMetadataReader metadataReader)
	{
		ArgumentNullException.ThrowIfNull(metadataReader);
		_fontSubstitutions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		_fallbackFontFamily = string.Empty;
		_metadataReader = metadataReader;
		_familyIndex = BuildFamilyIndex(fontDirectories);
	}

	/// <summary>
	/// Gets the indexed mapping of font family name to file path.
	/// </summary>
	public IReadOnlyDictionary<string, string> FamilyIndex => _familyIndex;

	/// <summary>
	/// Attempts to resolve a font family to a scanned font file path.
	/// </summary>
	/// <param name="familyName">The font family name.</param>
	/// <param name="path">When successful, receives the resolved font file path.</param>
	/// <returns><see langword="true"/> when the family is indexed; otherwise <see langword="false"/>.</returns>
	public bool TryGetFontPath(string familyName, out string? path)
	{
		if (string.IsNullOrWhiteSpace(familyName))
		{
			path = null;
			return false;
		}

		if (_familyIndex.TryGetValue(familyName, out var resolved))
		{
			path = resolved;
			return true;
		}

		if (_fontSubstitutions.TryGetValue(familyName, out var replacement)
			&& !string.IsNullOrWhiteSpace(replacement)
			&& _familyIndex.TryGetValue(replacement, out resolved))
		{
			path = resolved;
			return true;
		}

		if (!string.IsNullOrWhiteSpace(_fallbackFontFamily)
			&& _familyIndex.TryGetValue(_fallbackFontFamily, out resolved))
		{
			path = resolved;
			return true;
		}

		if (TryGetSansSerifFallbackPath(out resolved))
		{
			path = resolved;
			return true;
		}

		path = null;
		return false;
	}

	private bool TryGetSansSerifFallbackPath(out string? path)
	{
		foreach (var family in PreferredSansSerifFamilies)
		{
			if (_familyIndex.TryGetValue(family, out var resolved))
			{
				path = resolved;
				return true;
			}
		}

		foreach (var pair in _familyIndex)
		{
			if (pair.Key.Contains("sans", StringComparison.OrdinalIgnoreCase))
			{
				path = pair.Value;
				return true;
			}
		}

		path = null;
		return false;
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
				if (!SupportedExtensions.Contains(extension))
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
