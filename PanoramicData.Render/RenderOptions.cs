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
	/// Gets or sets optional numbering styles used for list label rendering. Keys use the format <c>{numId}:{level}</c>.
	/// </summary>
	internal Dictionary<string, NumberingLevelStyle> NumberingStyles { get; set; } = new(StringComparer.Ordinal);

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
	/// Gets or sets a value indicating whether automatic hyphenation using TeX patterns is enabled.
	/// When enabled, the line breaker inserts discretionary hyphen penalties at valid hyphenation points.
	/// </summary>
	public bool EnableHyphenation { get; set; }

	/// <summary>
	/// Gets or sets the optional field-update configuration.
	/// When <see langword="null"/>, field recalculation is disabled and cached field results are rendered as-is.
	/// </summary>
	public FieldUpdateOptions? FieldUpdate { get; set; }

	/// <summary>
	/// Gets or sets the original source filename to use for filename-based fields.
	/// When <see langword="null"/>, filename fields fall back to <c>(document)</c>.
	/// </summary>
	public string? SourceFilename { get; set; }

	/// <summary>
	/// Gets or sets an optional page range to render.
	/// </summary>
	public Range? PageRange { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether hidden text (runs with <c>w:vanish</c>) should
	/// be included in the layout. When <see langword="false"/> (the default), hidden runs are
	/// excluded from layout entirely, matching Word's normal display mode.
	/// </summary>
	public bool ShowHiddenText { get; set; }
}