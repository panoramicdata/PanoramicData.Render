namespace PanoramicData.Render;

/// <summary>
/// Represents the combined text decoration state for a run, including underline
/// and strikethrough properties. Underline style and color come from the
/// <c>w:u</c> element; strikethrough flags are resolved from the toggle cascade.
/// </summary>
/// <param name="Underline">The underline style (default <see cref="UnderlineStyle.None"/>).</param>
/// <param name="UnderlineColor">
/// The explicit underline color as a hex RGB string (e.g., <c>"FF0000"</c>),
/// or <see langword="null"/> to inherit the text color.
/// </param>
/// <param name="Strikethrough">Whether single strikethrough is active (from toggle cascade).</param>
/// <param name="DoubleStrikethrough">Whether double strikethrough is active (from toggle cascade).</param>
internal readonly record struct TextDecoration(
	UnderlineStyle Underline = UnderlineStyle.None,
	string? UnderlineColor = null,
	bool Strikethrough = false,
	bool DoubleStrikethrough = false)
{
	/// <summary>
	/// A decoration with no visible effects.
	/// </summary>
	public static readonly TextDecoration None = default;

	/// <summary>
	/// Gets a value indicating whether any underline style is applied.
	/// </summary>
	public bool HasUnderline => Underline != UnderlineStyle.None;

	/// <summary>
	/// Gets a value indicating whether any form of strikethrough is active
	/// (single or double).
	/// </summary>
	public bool HasStrikethrough => Strikethrough || DoubleStrikethrough;

	/// <summary>
	/// Gets a value indicating whether any visible decoration is present.
	/// </summary>
	public bool HasAnyDecoration => HasUnderline || HasStrikethrough;
}
