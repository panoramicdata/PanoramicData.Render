namespace PanoramicData.Render;

/// <summary>
/// Metrics for an individual character at a specific font size, used for
/// superscript/subscript offset calculations and precise positioning.
/// All values are in typographic points (convert to twips via <see cref="TwipConverter"/>).
/// </summary>
/// <param name="AdvanceWidth">The horizontal advance width of the character.</param>
/// <param name="Ascent">The font ascent (distance from baseline to top of tallest glyph, as a positive value).</param>
/// <param name="Descent">The font descent (distance from baseline to bottom of lowest glyph, as a positive value).</param>
/// <param name="Leading">The leading (extra line spacing recommended by the font).</param>
/// <param name="LineHeight">The total line height: ascent + descent + leading.</param>
internal readonly record struct CharacterMetrics(
	float AdvanceWidth,
	float Ascent,
	float Descent,
	float Leading,
	float LineHeight);
