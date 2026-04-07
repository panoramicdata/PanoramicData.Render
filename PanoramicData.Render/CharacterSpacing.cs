namespace PanoramicData.Render;

/// <summary>
/// Represents character spacing adjustments from the <c>w:spacing</c> element on <c>w:rPr</c>.
/// OpenXML stores the spacing value in twips; positive values expand spacing,
/// negative values condense it.
/// </summary>
/// <param name="ValueTwips">
/// The character spacing adjustment in twips. Positive = expanded, negative = condensed, zero = normal.
/// </param>
internal readonly record struct CharacterSpacing(float ValueTwips = 0f)
{
	/// <summary>
	/// Normal character spacing (no adjustment).
	/// </summary>
	public static readonly CharacterSpacing Normal = default;

	/// <summary>
	/// Gets a value indicating whether the spacing is expanded (positive).
	/// </summary>
	public bool IsExpanded => ValueTwips > 0f;

	/// <summary>
	/// Gets a value indicating whether the spacing is condensed (negative).
	/// </summary>
	public bool IsCondensed => ValueTwips < 0f;

	/// <summary>
	/// Gets a value indicating whether the spacing is the default (zero).
	/// </summary>
	public bool IsNormal => ValueTwips == 0f;

	/// <summary>
	/// Gets the spacing value in typographic points.
	/// </summary>
	public float ValuePoints => TwipConverter.TwipsToPoints(ValueTwips);

	/// <summary>
	/// Computes the total advance width adjustment for a sequence of characters.
	/// The spacing is applied between characters, so for <paramref name="characterCount"/>
	/// characters there are <c>characterCount - 1</c> gaps (spacing is not added after the last character).
	/// </summary>
	/// <param name="characterCount">The number of characters in the run.</param>
	/// <returns>The total spacing adjustment in twips.</returns>
	public float ComputeTotalAdjustment(int characterCount) =>
		characterCount <= 1 ? 0f : ValueTwips * (characterCount - 1);
}
