namespace PanoramicData.Render;

/// <summary>
/// Represents parsed theme font and color scheme data.
/// </summary>
internal sealed class ThemeInfo
{
	/// <summary>
	/// Gets the major font family set (typically used for headings).
	/// </summary>
	public required ThemeFontInfo MajorFont { get; init; }

	/// <summary>
	/// Gets the minor font family set (typically used for body text).
	/// </summary>
	public required ThemeFontInfo MinorFont { get; init; }

	/// <summary>
	/// Gets the raw theme color map keyed by OOXML slot name (for example, <c>accent1</c>).
	/// </summary>
	public required IReadOnlyDictionary<string, string> Colors { get; init; }
}
