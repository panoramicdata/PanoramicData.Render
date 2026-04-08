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

	/// <summary>
	/// Gets the specified row height in twips, or zero if not specified.
	/// </summary>
	public float HeightTwips { get; init; }

	/// <summary>
	/// Gets the row height rule.
	/// </summary>
	public RowHeightRule HeightRule { get; init; } = RowHeightRule.Auto;

	/// <summary>
	/// Gets whether this row is a header row that repeats on each page.
	/// </summary>
	public bool IsHeaderRow { get; init; }

	/// <summary>
	/// Gets whether this row cannot be split across page boundaries.
	/// </summary>
	public bool CantSplit { get; init; }

	/// <summary>
	/// Gets the row-level border definitions.
	/// </summary>
	public TableBorderSet Borders { get; init; } = TableBorderSet.None;
}
