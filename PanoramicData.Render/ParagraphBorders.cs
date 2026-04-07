namespace PanoramicData.Render;

/// <summary>
/// Represents the complete set of border definitions for a paragraph.
/// Each edge can have an independent border definition.
/// </summary>
/// <remarks>
/// Corresponds to OOXML w:pBdr element containing w:top, w:bottom, w:left, w:right, w:between, w:bar.
/// </remarks>
/// <param name="Top">The top border. Null means no top border specified.</param>
/// <param name="Bottom">The bottom border. Null means no bottom border specified.</param>
/// <param name="Left">The left border. Null means no left border specified.</param>
/// <param name="Right">The right border. Null means no right border specified.</param>
/// <param name="Between">The border between adjacent paragraphs. Null means none specified.</param>
/// <param name="Bar">A vertical bar drawn before the paragraph content. Null means none specified.</param>
internal readonly record struct ParagraphBorders(
	ParagraphBorder? Top = null,
	ParagraphBorder? Bottom = null,
	ParagraphBorder? Left = null,
	ParagraphBorder? Right = null,
	ParagraphBorder? Between = null,
	ParagraphBorder? Bar = null)
{
	/// <summary>
	/// A borders instance with no borders specified on any edge.
	/// </summary>
	public static readonly ParagraphBorders None = new();

	/// <summary>
	/// Gets whether any border edge has a visible style.
	/// </summary>
	public bool HasAnyVisibleBorder =>
		(Top?.IsVisible ?? false)
		|| (Bottom?.IsVisible ?? false)
		|| (Left?.IsVisible ?? false)
		|| (Right?.IsVisible ?? false)
		|| (Between?.IsVisible ?? false)
		|| (Bar?.IsVisible ?? false);

	/// <summary>
	/// Gets the border for a specific edge, or null if not defined.
	/// </summary>
	/// <param name="edge">The border edge to retrieve.</param>
	/// <returns>The border definition, or null if not specified.</returns>
	public ParagraphBorder? GetBorder(BorderEdge edge) => edge switch
	{
		BorderEdge.Top => Top,
		BorderEdge.Bottom => Bottom,
		BorderEdge.Left => Left,
		BorderEdge.Right => Right,
		BorderEdge.Between => Between,
		BorderEdge.Bar => Bar,
		_ => null
	};
}
