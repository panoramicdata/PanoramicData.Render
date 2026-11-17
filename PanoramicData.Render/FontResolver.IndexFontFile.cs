using SkiaSharp;
using System.Collections.Concurrent;

namespace PanoramicData.Render;

internal sealed partial class FontResolver
{
	private void IndexFontFile(Dictionary<string, string> index, string file)
	{
		var familyNames = _metadataReader.ReadFamilyNames(file);
		if (familyNames.Count == 0)
		{
			var fallback = Path.GetFileNameWithoutExtension(file);
			familyNames = string.IsNullOrWhiteSpace(fallback) ? [] : [fallback];
		}

		foreach (var familyName in familyNames)
		{
			if (!string.IsNullOrWhiteSpace(familyName))
			{
				index.TryAdd(familyName, file);
			}
		}
	}
}
