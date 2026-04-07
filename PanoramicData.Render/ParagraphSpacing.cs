namespace PanoramicData.Render;

/// <summary>
/// Represents paragraph spacing (before/after) and line spacing settings, in twips.
/// </summary>
/// <remarks>
/// Corresponds to OOXML w:pPr/w:spacing with attributes w:before, w:after, w:line, w:lineRule.
/// All values are in twentieths of a point (= twips).
/// The single-spacing baseline for Auto rule is 240 twips.
/// </remarks>
/// <param name="SpaceBefore">Spacing above the paragraph in twips (w:before).</param>
/// <param name="SpaceAfter">Spacing below the paragraph in twips (w:after).</param>
/// <param name="LineSpacingTwips">Line spacing amount in twips (w:line). Zero means use default single spacing.</param>
/// <param name="LineRule">How to interpret <paramref name="LineSpacingTwips"/>. Null defaults to <see cref="LineSpacingRule.Auto"/>.</param>
internal readonly record struct ParagraphSpacing(
	float SpaceBefore = 0f,
	float SpaceAfter = 0f,
	float LineSpacingTwips = 0f,
	LineSpacingRule? LineRule = null)
{
	/// <summary>
	/// A spacing instance with all values at their defaults (zero spacing, auto rule).
	/// </summary>
	public static readonly ParagraphSpacing None = new();

	/// <summary>
	/// The single-spacing baseline in twips used for Auto line rule calculations.
	/// </summary>
	private const float SingleSpacingBaseline = 240f;

	/// <summary>
	/// Gets the effective line spacing rule, defaulting to <see cref="LineSpacingRule.Auto"/> when null.
	/// </summary>
	public LineSpacingRule EffectiveLineRule => LineRule ?? LineSpacingRule.Auto;

	/// <summary>
	/// Gets the line spacing multiplier for the Auto rule.
	/// Single spacing = 1.0 (240 twips), 1.5× = 1.5 (360 twips), double = 2.0 (480 twips), etc.
	/// Returns 1.0 when <see cref="LineSpacingTwips"/> is zero or negative.
	/// </summary>
	public float GetLineSpacingMultiplier() =>
		LineSpacingTwips > 0f ? LineSpacingTwips / SingleSpacingBaseline : 1f;

	/// <summary>
	/// Computes the effective line height given the natural (font-derived) line height in twips.
	/// </summary>
	/// <param name="naturalLineHeight">The natural line height based on font metrics (ascent + descent + leading), in twips.</param>
	/// <returns>The computed line height in twips.</returns>
	public float ComputeLineHeight(float naturalLineHeight) => EffectiveLineRule switch
	{
		LineSpacingRule.Exact => LineSpacingTwips > 0f ? LineSpacingTwips : naturalLineHeight,
		LineSpacingRule.AtLeast => LineSpacingTwips > 0f
			? Math.Max(naturalLineHeight, LineSpacingTwips)
			: naturalLineHeight,
		// Auto: multiply natural height by the spacing multiplier
		_ => naturalLineHeight * GetLineSpacingMultiplier()
	};

	/// <summary>
	/// Computes the total paragraph height including space before, all line heights, and space after.
	/// </summary>
	/// <param name="lineCount">The number of lines in the paragraph.</param>
	/// <param name="naturalLineHeight">The natural line height based on font metrics, in twips.</param>
	/// <returns>The total paragraph height in twips.</returns>
	public float ComputeParagraphHeight(int lineCount, float naturalLineHeight)
	{
		if (lineCount <= 0)
		{
			return 0f;
		}

		var lineHeight = ComputeLineHeight(naturalLineHeight);
		return SpaceBefore + lineCount * lineHeight + SpaceAfter;
	}
}
