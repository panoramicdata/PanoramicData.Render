namespace PanoramicData.Render;

/// <summary>
/// Computes adjusted font size and baseline offset for superscript and subscript text.
/// Word typically renders super/subscript at 2/3 of the parent font size with a
/// baseline offset of approximately 1/3 of the parent font size.
/// </summary>
internal static class SuperSubScriptCalculator
{
	/// <summary>
	/// The default scale factor applied to the parent font size for super/subscript rendering.
	/// Word uses approximately 2/3 (≈ 0.667) of the parent size.
	/// </summary>
	public const float DefaultSizeScale = 2f / 3f;

	/// <summary>
	/// The default baseline offset as a fraction of the parent font size.
	/// Positive values raise the text (superscript), negative values lower it (subscript).
	/// Word uses approximately 1/3 (≈ 0.333) of the parent size.
	/// </summary>
	public const float DefaultOffsetFraction = 1f / 3f;

	/// <summary>
	/// Computes the adjusted font size for a super/subscript run.
	/// </summary>
	/// <param name="parentSizePoints">The parent (normal) font size in points.</param>
	/// <param name="alignment">The vertical alignment mode.</param>
	/// <param name="sizeScale">The scale factor to apply (default: <see cref="DefaultSizeScale"/>).</param>
	/// <returns>The adjusted font size in points. Returns <paramref name="parentSizePoints"/> unchanged for <see cref="VerticalTextAlignment.Baseline"/>.</returns>
	public static float ComputeFontSize(
		float parentSizePoints,
		VerticalTextAlignment alignment,
		float sizeScale = DefaultSizeScale) =>
		alignment == VerticalTextAlignment.Baseline
			? parentSizePoints
			: parentSizePoints * sizeScale;

	/// <summary>
	/// Computes the baseline offset in points for a super/subscript run.
	/// </summary>
	/// <param name="parentSizePoints">The parent (normal) font size in points.</param>
	/// <param name="alignment">The vertical alignment mode.</param>
	/// <param name="offsetFraction">The offset as a fraction of the parent font size (default: <see cref="DefaultOffsetFraction"/>).</param>
	/// <returns>
	/// The baseline offset in points: positive for superscript (raised), negative for subscript (lowered),
	/// zero for baseline.
	/// </returns>
	public static float ComputeBaselineOffset(
		float parentSizePoints,
		VerticalTextAlignment alignment,
		float offsetFraction = DefaultOffsetFraction) => alignment switch
	{
		VerticalTextAlignment.Superscript => parentSizePoints * offsetFraction,
		VerticalTextAlignment.Subscript => -(parentSizePoints * offsetFraction),
		_ => 0f
	};
}
