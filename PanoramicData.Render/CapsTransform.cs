namespace PanoramicData.Render;

/// <summary>
/// Applies caps transformations to text and computes adjusted font sizes for small-caps rendering.
/// In Word, <c>w:caps</c> converts all text to uppercase at the same size, while
/// <c>w:smallCaps</c> converts lowercase to uppercase at a reduced size (≈80%).
/// </summary>
internal static class CapsTransform
{
	/// <summary>
	/// The default font scale applied to originally-lowercase characters in small-caps mode.
	/// Word uses approximately 80% of the parent size.
	/// </summary>
	public const float DefaultSmallCapsScale = 0.8f;

	/// <summary>
	/// Determines the <see cref="CapsMode"/> from resolved toggle state values.
	/// <c>Caps</c> takes precedence over <c>SmallCaps</c> when both are true.
	/// </summary>
	/// <param name="caps">Whether all-caps is active (from <see cref="ToggleState.Caps"/>).</param>
	/// <param name="smallCaps">Whether small-caps is active (from <see cref="ToggleState.SmallCaps"/>).</param>
	/// <returns>The resolved <see cref="CapsMode"/>.</returns>
	public static CapsMode Resolve(bool caps, bool smallCaps)
	{
		if (caps)
		{
			return CapsMode.AllCaps;
		}

		if (smallCaps)
		{
			return CapsMode.SmallCaps;
		}

		return CapsMode.None;
	}

	/// <summary>
	/// Transforms the display text according to the specified <paramref name="mode"/>.
	/// For <see cref="CapsMode.AllCaps"/> and <see cref="CapsMode.SmallCaps"/>,
	/// all characters are converted to uppercase.
	/// </summary>
	/// <param name="text">The original text.</param>
	/// <param name="mode">The caps transformation mode.</param>
	/// <returns>The transformed text, or the original unchanged for <see cref="CapsMode.None"/>.</returns>
	public static string TransformText(string text, CapsMode mode)
	{
		ArgumentNullException.ThrowIfNull(text);
		return mode switch
		{
			CapsMode.AllCaps => text.ToUpperInvariant(),
			CapsMode.SmallCaps => text.ToUpperInvariant(),
			_ => text
		};
	}

	/// <summary>
	/// Returns the font size for a character given the caps mode.
	/// For <see cref="CapsMode.SmallCaps"/>, characters that were originally lowercase
	/// use a reduced font size; originally-uppercase characters retain the parent size.
	/// </summary>
	/// <param name="originalChar">The character before caps transformation.</param>
	/// <param name="parentSizePoints">The normal font size in points.</param>
	/// <param name="mode">The caps transformation mode.</param>
	/// <param name="smallCapsScale">The scale applied to lowercase chars in small-caps mode (default: <see cref="DefaultSmallCapsScale"/>).</param>
	/// <returns>The font size in points for rendering this character.</returns>
	public static float ComputeCharacterFontSize(
		char originalChar,
		float parentSizePoints,
		CapsMode mode,
		float smallCapsScale = DefaultSmallCapsScale) => mode switch
	{
		CapsMode.SmallCaps when char.IsLower(originalChar) => parentSizePoints * smallCapsScale,
		_ => parentSizePoints
	};
}
