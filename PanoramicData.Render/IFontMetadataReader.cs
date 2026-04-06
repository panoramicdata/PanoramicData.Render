namespace PanoramicData.Render;

/// <summary>
/// Reads font family metadata from a font file.
/// </summary>
internal interface IFontMetadataReader
{
	/// <summary>
	/// Reads zero or more family names from the given font file.
	/// </summary>
	/// <param name="filePath">The font file path.</param>
	/// <returns>A list of discovered family names.</returns>
	IReadOnlyList<string> ReadFamilyNames(string filePath);
}
