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

	private readonly IReadOnlyDictionary<string, string> _familyIndex;

	/// <summary>
	/// Initializes a new instance of the <see cref="FontResolver"/> class.
	/// </summary>
	/// <param name="fontDirectories">Directories to scan for font files.</param>
	public FontResolver(IReadOnlyList<string>? fontDirectories)
	{
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

		path = null;
		return false;
	}

	private static IReadOnlyDictionary<string, string> BuildFamilyIndex(IReadOnlyList<string>? directories)
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

				var familyName = Path.GetFileNameWithoutExtension(file);
				if (string.IsNullOrWhiteSpace(familyName))
				{
					continue;
				}

				if (!index.ContainsKey(familyName))
				{
					index[familyName] = file;
				}
			}
		}

		return index;
	}
}
