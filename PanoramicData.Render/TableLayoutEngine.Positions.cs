namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class TableLayoutEngine
{
	/// <summary>
	/// Computes the <see cref="CellPosition"/> for every owner cell in the resolved grid.
	/// Each owner cell appears exactly once; continuation cells from merges are not included.
	/// </summary>
	/// <param name="layout">The computed table layout.</param>
	/// <returns>A list of <see cref="CellPosition"/> for each unique cell.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="layout"/> is <see langword="null"/>.</exception>
	internal static IReadOnlyList<CellPosition> ComputeCellPositions(TableLayoutResult layout)
	{
		ArgumentNullException.ThrowIfNull(layout);

		var table = layout.Table;
		if (table.Rows.Count == 0 || table.GridColumns.Count == 0)
		{
			return [];
		}

		var grid = TableGridResolver.Resolve(table);
		var rowCount = grid.GetLength(0);
		var colCount = grid.GetLength(1);
		var rowOffsets = ComputeRowOffsets(layout, rowCount);

		// Track visited owner cells to emit each once
		var visited = new HashSet<(int Row, int Col)>();
		var positions = new List<CellPosition>();

		for (var r = 0; r < rowCount; r++)
		{
			for (var c = 0; c < colCount; c++)
			{
				if (grid[r, c] is not { } resolved || !visited.Add((resolved.OwnerRowIndex, resolved.OwnerColumnIndex)))
				{
					continue;
				}

				positions.Add(CreateCellPosition(layout, grid, resolved, rowOffsets));
			}
		}

		return positions;
	}

	// Cumulative row y-offsets.
	private static float[] ComputeRowOffsets(TableLayoutResult layout, int rowCount)
	{
		var rowOffsets = new float[rowCount];
		for (var r = 1; r < rowCount; r++)
		{
			rowOffsets[r] = rowOffsets[r - 1] + layout.RowHeights[r - 1];
		}

		return rowOffsets;
	}

	private static CellPosition CreateCellPosition(TableLayoutResult layout, ResolvedGridCell?[,] grid, ResolvedGridCell resolved, float[] rowOffsets)
	{
		var rowCount = grid.GetLength(0);
		var colCount = grid.GetLength(1);

		// Width: sum of all columns this cell spans
		var cellWidth = 0f;
		for (var s = 0; s < resolved.Cell.GridSpan && resolved.OwnerColumnIndex + s < colCount; s++)
		{
			cellWidth += layout.ColumnWidths[resolved.OwnerColumnIndex + s];
		}

		// Height: sum of all rows this cell spans (via vertical merge)
		var spanRows = CountVerticalSpan(grid, resolved.OwnerRowIndex, resolved.OwnerColumnIndex, rowCount);
		var cellHeight = 0f;
		for (var sr = 0; sr < spanRows; sr++)
		{
			cellHeight += layout.RowHeights[resolved.OwnerRowIndex + sr];
		}

		return new CellPosition(
			resolved.OwnerRowIndex,
			resolved.OwnerColumnIndex,
			layout.ColumnOffsets[resolved.OwnerColumnIndex],
			rowOffsets[resolved.OwnerRowIndex],
			cellWidth,
			cellHeight,
			resolved.Cell);
	}
	/// <summary>
	/// Computes horizontally merged cell regions from a computed table layout.
	/// A horizontal merge is any owner cell whose <see cref="TableCellElement.GridSpan"/>
	/// is greater than 1.
	/// </summary>
	/// <param name="layout">The computed table layout.</param>
	/// <returns>The horizontally merged regions in reading order.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="layout"/> is <see langword="null"/>.</exception>
	internal static IReadOnlyList<HorizontalMergeRegion> ComputeHorizontalMergeRegions(TableLayoutResult layout)
	{
		ArgumentNullException.ThrowIfNull(layout);

		var positions = ComputeCellPositions(layout);
		if (positions.Count == 0)
		{
			return [];
		}

		var regions = new List<HorizontalMergeRegion>();
		foreach (var position in positions)
		{
			if (position.Cell.GridSpan <= 1)
			{
				continue;
			}

			var remainingColumns = Math.Max(0, layout.ColumnWidths.Count - position.ColumnIndex);
			var effectiveSpan = Math.Min(position.Cell.GridSpan, remainingColumns);
			regions.Add(new HorizontalMergeRegion(
				position.RowIndex,
				position.ColumnIndex,
				effectiveSpan,
				position.X,
				position.Y,
				position.Width,
				position.Height,
				position.Cell));
		}

		return regions;
	}

	/// <summary>
	/// Computes vertically merged cell regions from a computed table layout.
	/// A vertical merge is any owner cell that spans more than one row.
	/// </summary>
	/// <param name="layout">The computed table layout.</param>
	/// <returns>The vertically merged regions in reading order.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="layout"/> is <see langword="null"/>.</exception>
	internal static IReadOnlyList<VerticalMergeRegion> ComputeVerticalMergeRegions(TableLayoutResult layout)
	{
		ArgumentNullException.ThrowIfNull(layout);

		var positions = ComputeCellPositions(layout);
		if (positions.Count == 0)
		{
			return [];
		}

		var grid = TableGridResolver.Resolve(layout.Table);
		var rowCount = grid.GetLength(0);

		var regions = new List<VerticalMergeRegion>();
		foreach (var position in positions)
		{
			var rowSpan = CountVerticalSpan(grid, position.RowIndex, position.ColumnIndex, rowCount);
			if (rowSpan <= 1)
			{
				continue;
			}

			regions.Add(new VerticalMergeRegion(
				position.RowIndex,
				position.ColumnIndex,
				rowSpan,
				position.X,
				position.Y,
				position.Width,
				position.Height,
				position.Cell));
		}

		return regions;
	}

	/// <summary>
	/// Computes merged cell regions that combine horizontal and vertical spans
	/// into a single rectangular region model.
	/// </summary>
	/// <param name="layout">The computed table layout.</param>
	/// <returns>The merged regions in reading order.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="layout"/> is <see langword="null"/>.</exception>
	internal static IReadOnlyList<MergedCellRegion> ComputeMergedCellRegions(TableLayoutResult layout)
	{
		ArgumentNullException.ThrowIfNull(layout);

		var positions = ComputeCellPositions(layout);
		if (positions.Count == 0)
		{
			return [];
		}

		var grid = TableGridResolver.Resolve(layout.Table);
		var rowCount = grid.GetLength(0);

		var regions = new List<MergedCellRegion>();
		foreach (var position in positions)
		{
			var rowSpan = CountVerticalSpan(grid, position.RowIndex, position.ColumnIndex, rowCount);
			var remainingColumns = Math.Max(0, layout.ColumnWidths.Count - position.ColumnIndex);
			var columnSpan = Math.Min(position.Cell.GridSpan, remainingColumns);

			if (rowSpan <= 1 && columnSpan <= 1)
			{
				continue;
			}

			regions.Add(new MergedCellRegion(
				position.RowIndex,
				position.ColumnIndex,
				rowSpan,
				columnSpan,
				position.X,
				position.Y,
				position.Width,
				position.Height,
				position.Cell));
		}

		return regions;
	}

	/// <summary>
	/// Computes adjusted content layout areas and positions for merged cells.
	/// The resulting geometry accounts for merged cell dimensions, cell margins,
	/// and vertical alignment.
	/// </summary>
	/// <param name="layout">The computed table layout.</param>
	/// <returns>The merged-cell content layouts in reading order.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="layout"/> is <see langword="null"/>.</exception>
	internal static IReadOnlyList<MergedCellContentLayout> ComputeMergedCellContentLayouts(TableLayoutResult layout)
	{
		ArgumentNullException.ThrowIfNull(layout);

		var mergedRegions = ComputeMergedCellRegions(layout);
		if (mergedRegions.Count == 0)
		{
			return [];
		}

		var result = new List<MergedCellContentLayout>();
		foreach (var region in mergedRegions)
		{
			var (blocks, totalHeightWithMargins) = LayoutCellContent(region.Cell);
			var contentWidth = ComputeContentWidth(region.Width, region.Cell.Margins);
			var contentHeight = Math.Max(0f, totalHeightWithMargins - region.Cell.Margins.Top - region.Cell.Margins.Bottom);
			var verticalOffset = ComputeVerticalContentOffset(region.Height, totalHeightWithMargins, region.Cell.VerticalAlignment);

			result.Add(new MergedCellContentLayout(
				region.StartRowIndex,
				region.StartColumnIndex,
				region.RowSpan,
				region.ColumnSpan,
				region.X,
				region.Y,
				region.Width,
				region.Height,
				region.X + region.Cell.Margins.Left,
				region.Y + verticalOffset + region.Cell.Margins.Top,
				contentWidth,
				contentHeight,
				blocks,
				region.Cell));
		}

		return result;
	}

	/// <summary>
	/// Computes table cell background fill regions for cells with visible shading.
	/// The returned regions are in reading order and should be painted before cell content.
	/// </summary>
	/// <param name="layout">The computed table layout.</param>
	/// <returns>Background rectangles in table-relative coordinates.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="layout"/> is <see langword="null"/>.</exception>
	internal static IReadOnlyList<TableCellBackground> ComputeCellBackgrounds(TableLayoutResult layout)
		=> ComputeCellBackgrounds(layout, null);

	/// <summary>
	/// Computes table cell background fill regions for cells with visible direct or conditional style shading.
	/// The returned regions are in reading order and should be painted before cell content.
	/// </summary>
	/// <param name="layout">The computed table layout.</param>
	/// <param name="styles">The styles element used to resolve table-style conditional shading.</param>
	/// <returns>Background rectangles in table-relative coordinates.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="layout"/> is <see langword="null"/>.</exception>
	internal static IReadOnlyList<TableCellBackground> ComputeCellBackgrounds(TableLayoutResult layout, Styles? styles)
	{
		ArgumentNullException.ThrowIfNull(layout);

		if (layout.Table.Rows.Count == 0 || layout.Table.GridColumns.Count == 0)
		{
			return [];
		}

		var positions = ComputeCellPositions(layout);
		if (positions.Count == 0)
		{
			return [];
		}

		var grid = TableGridResolver.Resolve(layout.Table);
		var rowCount = grid.GetLength(0);
		var colCount = grid.GetLength(1);
		var backgrounds = new List<TableCellBackground>();
		foreach (var position in positions)
		{
			var rowSpan = CountVerticalSpan(grid, position.RowIndex, position.ColumnIndex, rowCount);
			var remainingColumns = Math.Max(0, colCount - position.ColumnIndex);
			var columnSpan = Math.Min(position.Cell.GridSpan, remainingColumns);

			var shading = position.Cell.Shading.HasVisibleShading
				? position.Cell.Shading
				: TableStyleResolver.ResolveCellShading(
					styles,
					layout.Table,
					position.RowIndex,
					position.ColumnIndex,
					rowSpan,
					columnSpan,
					rowCount,
					colCount);

			if (!shading.HasVisibleShading)
			{
				continue;
			}

			backgrounds.Add(new TableCellBackground(
				position.RowIndex,
				position.ColumnIndex,
				position.X,
				position.Y,
				position.Width,
				position.Height,
				shading,
				position.Cell));
		}

		return backgrounds;
	}
}
