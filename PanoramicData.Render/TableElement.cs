namespace PanoramicData.Render;

/// <summary>
/// Represents a fully parsed table element corresponding to a <c>w:tbl</c> element.
/// Contains grid definition, rows, and cells with their content blocks.
/// </summary>
internal sealed class TableElement
{
	/// <summary>
	/// Gets the grid column definitions for this table (from <c>w:tblGrid</c>).
	/// </summary>
	public required IReadOnlyList<TableGridColumn> GridColumns { get; init; }

	/// <summary>
	/// Gets the rows of this table.
	/// </summary>
	public required IReadOnlyList<TableRowElement> Rows { get; init; }

	/// <summary>
	/// Gets the table style ID, if specified.
	/// </summary>
	public string? StyleId { get; init; }
}
