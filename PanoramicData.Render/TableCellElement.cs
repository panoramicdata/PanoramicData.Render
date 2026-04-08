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

	/// <summary>
	/// Gets the cell width specification.
	/// </summary>
	public TableWidthValue Width { get; init; } = TableWidthValue.Auto;

	/// <summary>
	/// Gets the vertical alignment of content within this cell.
	/// </summary>
	public CellVerticalAlignment VerticalAlignment { get; init; } = CellVerticalAlignment.Top;

	/// <summary>
	/// Gets the text direction within this cell.
	/// </summary>
	public CellTextDirection TextDirection { get; init; } = CellTextDirection.LeftToRightTopToBottom;

	/// <summary>
	/// Gets the cell margins (padding).
	/// </summary>
	public CellMargins Margins { get; init; } = CellMargins.None;

	/// <summary>
	/// Gets the cell border definitions.
	/// </summary>
	public TableBorderSet Borders { get; init; } = TableBorderSet.None;
}
