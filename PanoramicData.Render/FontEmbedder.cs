namespace PanoramicData.Render;

using System.Collections.Concurrent;

/// <summary>
/// Handles extraction and encoding of font files for embedding in SVG output.
/// </summary>
internal static class FontEmbedder
{
	private static readonly ConcurrentDictionary<string, string?> _fontCache = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Gets the Base64-encoded font data for a font family, loading from the configured font directories.
	/// </summary>
	/// <param name="familyName">The font family name (e.g., "Calibri").</param>
	/// <param name="fontDirectories">The directories to search for font files.</param>
	/// <returns>Base64-encoded font data (TTF), or null if the font file is not found or cannot be read.</returns>
	public static string? GetEmbeddedFontData(string familyName, IReadOnlyList<string> fontDirectories)
	{
		if (string.IsNullOrWhiteSpace(familyName) || fontDirectories.Count == 0)
		{
			return null;
		}

		// Check cache first
		var cacheKey = $"{familyName}:{string.Join("|", fontDirectories)}";
		if (_fontCache.TryGetValue(cacheKey, out var cached))
		{
			return cached;
		}

		// Search for the font file
		var fontPath = FindFontFile(familyName, fontDirectories);
		if (fontPath is null)
		{
			_fontCache.TryAdd(cacheKey, null);
			return null;
		}

		// Read and encode the font file
		try
		{
			var fontData = File.ReadAllBytes(fontPath);
			var base64 = Convert.ToBase64String(fontData);
			_fontCache.TryAdd(cacheKey, base64);
			return base64;
		}
		catch
		{
			// If we can't read the file, cache null to avoid repeated attempts
			_fontCache.TryAdd(cacheKey, null);
			return null;
		}
	}

	/// <summary>
	/// Finds the font file path for a given family name.
	/// </summary>
	/// <param name="familyName">The font family name.</param>
	/// <param name="fontDirectories">The directories to search.</param>
	/// <returns>The path to the font file, or null if not found.</returns>
	private static string? FindFontFile(string familyName, IReadOnlyList<string> fontDirectories)
	{
		var searchPatterns = new[]
		{
			$"{familyName}.ttf",
			$"{familyName}.otf",
			$"{familyName}.TTF",
			$"{familyName}.OTF"
		};

		foreach (var directory in fontDirectories)
		{
			if (!Directory.Exists(directory))
			{
				continue;
			}

			foreach (var pattern in searchPatterns)
			{
				var candidatePath = Path.Combine(directory, pattern);
				if (File.Exists(candidatePath))
				{
					return candidatePath;
				}
			}
		}

		return null;
	}
}
