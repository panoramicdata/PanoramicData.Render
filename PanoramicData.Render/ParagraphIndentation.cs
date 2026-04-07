namespace PanoramicData.Render;

/// <summary>
/// Represents the indentation settings for a paragraph, in twips.
/// </summary>
/// <remarks>
/// In OOXML, indentation is specified via <c>w:pPr/w:ind</c> with attributes:
/// <c>w:left</c>, <c>w:right</c>, <c>w:firstLine</c>, and <c>w:hanging</c>.
/// <para>
/// First-line indent and hanging indent are mutually exclusive:
/// <list type="bullet">
/// <item><c>FirstLine &gt; 0</c>: the first line is indented further right than the left margin.</item>
/// <item><c>Hanging &gt; 0</c>: all lines except the first are indented further right (the first line
/// starts at <c>Left</c>, subsequent lines at <c>Left + Hanging</c>). Equivalently, the first
/// line appears to "hang" to the left of the paragraph body.</item>
/// </list>
/// </para>
/// </remarks>
/// <param name="Left">Left indentation (all lines) in twips. Default: 0.</param>
/// <param name="Right">Right indentation (all lines) in twips. Default: 0.</param>
/// <param name="FirstLine">First-line indent in twips (additive to <paramref name="Left"/>). Default: 0.</param>
/// <param name="Hanging">Hanging indent in twips. When set, lines 2+ are indented by this amount relative to line 1. Default: 0.</param>
internal readonly record struct ParagraphIndentation(
	float Left = 0f,
	float Right = 0f,
	float FirstLine = 0f,
	float Hanging = 0f)
{
	/// <summary>
	/// A default indentation with all values set to zero.
	/// </summary>
	public static readonly ParagraphIndentation None = new();

	/// <summary>
	/// Computes the effective left indent for the first line.
	/// </summary>
	/// <returns>Left margin for the first line, in twips.</returns>
	public float GetFirstLineLeftIndent()
	{
		// If hanging: first line starts at Left (not shifted further)
		// If firstLine: first line starts at Left + FirstLine
		if (Hanging > 0)
		{
			return Left;
		}

		return Left + FirstLine;
	}

	/// <summary>
	/// Computes the effective left indent for lines other than the first.
	/// </summary>
	/// <returns>Left margin for subsequent lines, in twips.</returns>
	public float GetSubsequentLineLeftIndent()
	{
		// If hanging: subsequent lines are at Left + Hanging
		// Otherwise: subsequent lines are at Left (firstLine only affects line 1)
		if (Hanging > 0)
		{
			return Left + Hanging;
		}

		return Left;
	}
}
