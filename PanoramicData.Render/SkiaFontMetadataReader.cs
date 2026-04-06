namespace PanoramicData.Render;

using SkiaSharp;

/// <summary>
/// Uses SkiaSharp to read family names from font files, including TTC face enumeration.
/// </summary>
internal sealed class SkiaFontMetadataReader : IFontMetadataReader
{
	private const int MaxTtcFacesToProbe = 64;
	private readonly Func<string, int, string?> _readFamilyName;

	/// <summary>
	/// Initializes a new instance of the <see cref="SkiaFontMetadataReader"/> class.
	/// </summary>
	public SkiaFontMetadataReader()
	{
		_readFamilyName = ReadFamilyNameCore;
	}

	internal SkiaFontMetadataReader(Func<string, int, string?> readFamilyName)
	{
		ArgumentNullException.ThrowIfNull(readFamilyName);
		_readFamilyName = readFamilyName;
	}

	/// <inheritdoc />
	public IReadOnlyList<string> ReadFamilyNames(string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
		{
			return [];
		}

		var extension = Path.GetExtension(filePath);
		if (extension.Equals(".ttc", StringComparison.OrdinalIgnoreCase))
		{
			return ReadTtcFamilyNames(filePath);
		}

		var name = _readFamilyName(filePath, 0);
		return string.IsNullOrWhiteSpace(name) ? [] : [name];
	}

	private IReadOnlyList<string> ReadTtcFamilyNames(string filePath)
	{
		var families = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		for (int index = 0; index < MaxTtcFacesToProbe; index++)
		{
			var family = _readFamilyName(filePath, index);
			if (string.IsNullOrWhiteSpace(family))
			{
				if (index == 0)
				{
					continue;
				}

				break;
			}

			families.Add(family);
		}

		return [.. families];
	}

	private static string? ReadFamilyNameCore(string filePath, int ttcIndex)
	{
		try
		{
			using var typeface = SKTypeface.FromFile(filePath, ttcIndex);
			return typeface!.FamilyName;
		}
		catch
		{
			return null;
		}
	}
}
