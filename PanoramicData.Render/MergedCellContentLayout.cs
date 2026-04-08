namespace PanoramicData.Render;

/// <summary>
/// Represents the adjusted content layout geometry for a merged cell region.
/// </summary>
/// <param name="StartRowIndex">The zero-based starting row index of the merged region.</param>
/// <param name="StartColumnIndex">The zero-based starting column index of the merged region.</param>
/// <param name="RowSpan">The number of rows in the merged region.</param>
/// <param name="ColumnSpan">The number of columns in the merged region.</param>
/// <param name="CellX">The merged cell x-coordinate in twips.</param>
/// <param name="CellY">The merged cell y-coordinate in twips.</param>
/// <param name="CellWidth">The merged cell width in twips.</param>
/// <param name="CellHeight">The merged cell height in twips.</param>
/// <param name="ContentX">The adjusted content-area x-coordinate in twips.</param>
/// <param name="ContentY">The adjusted content-area y-coordinate in twips.</param>
/// <param name="ContentWidth">The adjusted content-area width in twips.</param>
/// <param name="ContentHeight">The content height (excluding top/bottom margins) in twips.</param>
/// <param name="Blocks">The laid-out content blocks for the merged cell.</param>
/// <param name="Cell">The source merged cell element.</param>
internal readonly record struct MergedCellContentLayout(int StartRowIndex, int StartColumnIndex, int RowSpan, int ColumnSpan, float CellX, float CellY, float CellWidth, float CellHeight, float ContentX, float ContentY, float ContentWidth, float ContentHeight, IReadOnlyList<LayoutBlock> Blocks, TableCellElement Cell);
