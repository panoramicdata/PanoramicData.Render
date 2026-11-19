namespace PanoramicData.Render;

internal static partial class TableLayoutEngine
{
	private readonly record struct CellSpan(CellPosition Position, int RowSpan, int ColumnSpan);

	private static List<CellSpan> ComputeCellSpans(IReadOnlyList<CellPosition> positions, ResolvedGridCell?[,] grid, int columnCount)
	{
		var rowCount = grid.GetLength(0);
		var spans = new List<CellSpan>(positions.Count);
		foreach (var position in positions)
		{
			var rowSpan = CountVerticalSpan(grid, position.RowIndex, position.ColumnIndex, rowCount);
			var columnSpan = Math.Min(position.Cell.GridSpan, Math.Max(0, columnCount - position.ColumnIndex));
			spans.Add(new CellSpan(position, rowSpan, columnSpan));
		}

		return spans;
	}
}