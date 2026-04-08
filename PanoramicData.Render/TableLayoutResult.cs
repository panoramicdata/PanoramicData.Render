namespace PanoramicData.Render;

/// <summary>
/// Contains the computed layout of a table, including column x-offsets
/// and per-row heights, ready for positioning cells during rendering.
/// </summary>
internal sealed class TableLayoutResult
{
	/// <summary>
	/// Gets the x-offset of the table from the left content edge, in twips.
	/// </summary>
	public float TableXOffset { get; init; }

	/// <summary>
	/// Gets the total width of the table in twips.
	/// </summary>
	public float TableWidthTwips { get; init; }

	/// <summary>
	/// Gets the column x-offsets within the table (relative to the table's left edge), in twips.
	/// Element <c>i</c> is the left edge of column <c>i</c>; the right edge of the last column
	/// is <see cref="TableWidthTwips"/>.
	/// </summary>
	public required IReadOnlyList<float> ColumnOffsets { get; init; }

	/// <summary>
	/// Gets the column widths in twips (one per grid column).
	/// </summary>
	public required IReadOnlyList<float> ColumnWidths { get; init; }

	/// <summary>
	/// Gets the per-row computed heights in twips.
	/// </summary>
	public required IReadOnlyList<float> RowHeights { get; init; }

	/// <summary>
	/// Gets the total height of all rows in twips.
	/// </summary>
	public float TotalHeightTwips { get; init; }

	/// <summary>
	/// Gets the source table element this layout was computed from.
	/// </summary>
	public required TableElement Table { get; init; }
}
