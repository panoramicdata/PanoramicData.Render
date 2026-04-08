namespace PanoramicData.Render;

/// <summary>
/// Represents a parsed table cell corresponding to a <c>w:tc</c> element.
/// </summary>
internal sealed class TableCellElement
{
	/// <summary>
	/// Gets the parsed block content of the cell (paragraphs, nested tables, etc.).
	/// </summary>
	public required IReadOnlyList<DocumentBlock> Blocks { get; init; }

	/// <summary>
	/// Gets the number of grid columns this cell spans (from <c>w:gridSpan</c>). Default is 1.
	/// </summary>
	public int GridSpan { get; init; } = 1;

	/// <summary>
	/// Gets the vertical merge state of this cell.
	/// </summary>
	public VerticalMergeState VerticalMerge { get; init; } = VerticalMergeState.None;
}
