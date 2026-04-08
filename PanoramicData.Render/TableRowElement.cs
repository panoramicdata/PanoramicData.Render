namespace PanoramicData.Render;

/// <summary>
/// Represents a parsed table row corresponding to a <c>w:tr</c> element.
/// </summary>
internal sealed class TableRowElement
{
	/// <summary>
	/// Gets the cells in this row.
	/// </summary>
	public required IReadOnlyList<TableCellElement> Cells { get; init; }
}
