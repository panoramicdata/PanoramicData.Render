namespace PanoramicData.Render;

/// <summary>
/// Configuration for document rendering.
/// </summary>
public class RenderOptions
{
	/// <summary>
	/// Gets or sets the directories to search for font files.
	/// </summary>
	public List<string> FontDirectories { get; set; } = [];

	/// <summary>
	/// Gets or sets explicit font name substitutions where the key is the requested family and the value is the replacement family.
	/// </summary>
	public Dictionary<string, string> FontSubstitutions { get; set; } = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Gets or sets the font family to use when no match is found.
	/// </summary>
	public string FallbackFontFamily { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the target DPI for SVG output.
	/// </summary>
	public double TargetDpi { get; set; } = 96;

	/// <summary>
	/// Gets or sets a value indicating whether fonts should be embedded in SVG output.
	/// </summary>
	public bool EmbedFonts { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether images should be embedded as data URIs in SVG output.
	/// </summary>
	public bool EmbedImages { get; set; } = true;

	/// <summary>
	/// Gets or sets an optional page range to render.
	/// </summary>
	public Range? PageRange { get; set; }
}