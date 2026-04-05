namespace PanoramicData.Render;

/// <summary>
/// Represents a themed font set (major or minor) with script-specific typefaces.
/// </summary>
internal sealed class ThemeFontInfo
{
	/// <summary>
	/// Gets the Latin typeface.
	/// </summary>
	public string? Latin { get; init; }

	/// <summary>
	/// Gets the East Asian typeface.
	/// </summary>
	public string? EastAsian { get; init; }

	/// <summary>
	/// Gets the complex-script typeface.
	/// </summary>
	public string? ComplexScript { get; init; }

	/// <summary>
	/// Gets supplemental script-to-typeface mappings (for example, Jpan, Hans).
	/// </summary>
	public required IReadOnlyDictionary<string, string> ScriptFonts { get; init; }
}
