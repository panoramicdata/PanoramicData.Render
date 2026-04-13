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

	/// <summary>
	/// Gets the table width specification.
	/// </summary>
	public TableWidthValue Width { get; init; } = TableWidthValue.Auto;

	/// <summary>
	/// Gets the horizontal alignment of the table.
	/// </summary>
	public TableAlignment Alignment { get; init; } = TableAlignment.Left;

	/// <summary>
	/// Gets the table indentation from the leading margin in twips.
	/// </summary>
	public float IndentationTwips { get; init; }

	/// <summary>
	/// Gets the parsed table border definitions.
	/// </summary>
	public TableBorderSet Borders { get; init; } = TableBorderSet.None;

	/// <summary>
	/// Gets the table cell spacing (border spacing) in twips.
	/// </summary>
	public float BorderSpacingTwips { get; init; }

	/// <summary>
	/// Gets the default cell margins for all cells in this table (<c>w:tblCellMar</c>).
	/// Cells that have no explicit cell-level margins inherit these table-level defaults.
	/// </summary>
	public CellMargins DefaultCellMargins { get; init; } = CellMargins.None;

	/// <summary>
	/// Gets the enabled table-style conditional formatting regions.
	/// </summary>
	public TableLookOptions Look { get; init; } = TableLookOptions.None;

	/// <summary>
	/// Gets a value indicating whether the table uses visual BiDi layout (<c>w:bidiVisual</c>),
	/// meaning columns are ordered right-to-left.
	/// </summary>
	public bool IsBiDi { get; init; }
}
