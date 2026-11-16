namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class TableLayoutEngine
{
	/// <summary>
	/// Computes the height of each row. For rows with <see cref="RowHeightRule.Exact"/>,
	/// uses the specified height. For <see cref="RowHeightRule.AtLeast"/> or <see cref="RowHeightRule.Auto"/>,
	/// uses the maximum of the specified height and the tallest cell content in the row.
	/// </summary>
	internal static IReadOnlyList<float> ComputeRowHeights(TableElement table)
	{
		var heights = new float[table.Rows.Count];
		for (var i = 0; i < table.Rows.Count; i++)
		{
			var row = table.Rows[i];
			var specifiedHeight = row.HeightTwips > 0f ? row.HeightTwips : 0f;

			if (row.HeightRule == RowHeightRule.Exact && specifiedHeight > 0f)
			{
				heights[i] = specifiedHeight;
				continue;
			}

			// Measure content height of each cell in this row
			var maxContentHeight = 0f;
			foreach (var cell in row.Cells)
			{
				if (cell.VerticalMerge == VerticalMergeState.Continue)
				{
					continue;
				}

				var contentHeight = MeasureCellContentHeight(cell) + (table.BorderSpacingTwips * 2f);
				if (contentHeight > maxContentHeight)
				{
					maxContentHeight = contentHeight;
				}
			}

			var contentBasedHeight = maxContentHeight > 0f ? maxContentHeight : DefaultRowHeightTwips;
			heights[i] = Math.Max(specifiedHeight, contentBasedHeight);
		}

		return heights;
	}

	/// <summary>
	/// Computes row heights using width-aware content measurement based on the provided
	/// final column widths (used by auto-fit layout).
	/// </summary>
	/// <param name="table">The table whose rows are measured.</param>
	/// <param name="columnWidths">Final computed column widths in twips.</param>
	/// <returns>Computed row heights in twips.</returns>
	internal static IReadOnlyList<float> ComputeRowHeights(TableElement table, IReadOnlyList<float> columnWidths)
	{
		if (columnWidths.Count == 0)
		{
			return ComputeRowHeights(table);
		}

		var heights = new float[table.Rows.Count];
		for (var i = 0; i < table.Rows.Count; i++)
		{
			var row = table.Rows[i];
			var specifiedHeight = row.HeightTwips > 0f ? row.HeightTwips : 0f;

			if (row.HeightRule == RowHeightRule.Exact && specifiedHeight > 0f)
			{
				heights[i] = specifiedHeight;
				continue;
			}

			var maxContentHeight = MeasureRowContentHeight(table, row, columnWidths);
			var contentBasedHeight = maxContentHeight > 0f ? maxContentHeight : DefaultRowHeightTwips;
			heights[i] = Math.Max(specifiedHeight, contentBasedHeight);
		}

		return heights;
	}

	private static float MeasureRowContentHeight(TableElement table, TableRowElement row, IReadOnlyList<float> columnWidths)
	{
		var maxContentHeight = 0f;
		var colIndex = 0;
		foreach (var cell in row.Cells)
		{
			if (colIndex >= columnWidths.Count)
			{
				break;
			}

			var span = Math.Max(1, cell.GridSpan);
			if (cell.VerticalMerge != VerticalMergeState.Continue)
			{
				var cellWidth = SumSpannedWidth(columnWidths, colIndex, span);
				var adjustedCellWidth = Math.Max(0f, cellWidth - (table.BorderSpacingTwips * 2f));
				var contentHeight = MeasureCellContentHeight(cell, adjustedCellWidth) + (table.BorderSpacingTwips * 2f);
				maxContentHeight = Math.Max(maxContentHeight, contentHeight);
			}

			colIndex += span;
		}

		return maxContentHeight;
	}

	private static float SumSpannedWidth(IReadOnlyList<float> columnWidths, int colIndex, int span)
	{
		var spanEnd = Math.Min(colIndex + span, columnWidths.Count);
		var cellWidth = 0f;
		for (var s = colIndex; s < spanEnd; s++)
		{
			cellWidth += columnWidths[s];
		}

		return cellWidth;
	}
	/// <summary>
	/// Measures the total height of a cell's content by laying out its blocks,
	/// including top and bottom cell margins.
	/// </summary>
	internal static float MeasureCellContentHeight(TableCellElement cell)
	{
		var totalHeight = 0f;
		foreach (var block in cell.Blocks)
		{
			totalHeight += EstimateBlockHeight(block);
		}

		totalHeight += cell.Margins.Top + cell.Margins.Bottom;

		return totalHeight;
	}

	/// <summary>
	/// Measures the total height of a cell's content for a specific cell width,
	/// allowing paragraph height estimation to reflect wrapping at final widths.
	/// Includes top and bottom cell margins.
	/// </summary>
	internal static float MeasureCellContentHeight(TableCellElement cell, float cellWidthTwips)
	{
		var totalHeight = 0f;
		var contentWidth = ComputeContentWidth(cellWidthTwips, cell.Margins);
		foreach (var block in cell.Blocks)
		{
			totalHeight += EstimateBlockHeight(block, contentWidth);
		}

		totalHeight += cell.Margins.Top + cell.Margins.Bottom;

		return totalHeight;
	}

	/// <summary>
	/// Lays out the content of a cell into <see cref="LayoutBlock"/> instances.
	/// Returns the blocks and total content height including top and bottom margins.
	/// </summary>
	internal static (IReadOnlyList<LayoutBlock> Blocks, float TotalHeight) LayoutCellContent(TableCellElement cell, float borderSpacingTwips = 0f)
		=> LayoutCellContent(cell, borderSpacingTwips, null);

	/// <summary>
	/// Lays out the content of a cell into <see cref="LayoutBlock"/> instances with width-aware height estimation.
	/// When <paramref name="contentWidthTwips"/> is provided, multi-line text wrapping is estimated.
	/// Returns the blocks and total content height including top and bottom margins.
	/// </summary>
	internal static (IReadOnlyList<LayoutBlock> Blocks, float TotalHeight) LayoutCellContent(TableCellElement cell, float borderSpacingTwips, float? contentWidthTwips)
	{
		ArgumentNullException.ThrowIfNull(cell);

		if (cell.Blocks.Count == 0)
		{
			var marginHeight = cell.Margins.Top + cell.Margins.Bottom + (borderSpacingTwips * 2f);
			return ([], marginHeight);
		}

		var layoutBlocks = new List<LayoutBlock>();
		var totalHeight = cell.Margins.Top + borderSpacingTwips;

		foreach (var block in cell.Blocks)
		{
			var height = contentWidthTwips is > 0f
				? EstimateBlockHeight(block, contentWidthTwips.Value)
				: EstimateBlockHeight(block);

			// For multi-line cell paragraphs, compute LineHeights so wrapping triggers later.
			var lineCount = Math.Max(1, (int)MathF.Round(height / DefaultRowHeightTwips));
			List<float>? lineHeights = null;
			if (lineCount > 1)
			{
				lineHeights = new List<float>(lineCount);
				for (var i = 0; i < lineCount; i++)
				{
					lineHeights.Add(DefaultRowHeightTwips);
				}
			}

			layoutBlocks.Add(new LayoutBlock(block, height, LineHeights: lineHeights));
			totalHeight += height;
		}

		totalHeight += cell.Margins.Bottom + borderSpacingTwips;

		return (layoutBlocks, totalHeight);
	}

	/// <summary>
	/// Computes the effective content width of a cell, accounting for left and right margins.
	/// </summary>
	/// <param name="cellWidth">The total cell width in twips.</param>
	/// <param name="margins">The cell margins.</param>
	/// <param name="borderSpacingTwips">The table border spacing to subtract from both sides.</param>
	/// <returns>The content area width in twips (never less than zero).</returns>
	internal static float ComputeContentWidth(float cellWidth, CellMargins margins, float borderSpacingTwips = 0f)
		=> Math.Max(0f, cellWidth - margins.Left - margins.Right - (borderSpacingTwips * 2f));

	/// <summary>
	/// Computes the vertical offset for cell content based on vertical alignment.
	/// The offset is relative to the cell's top edge.
	/// </summary>
	/// <param name="cellHeight">The total cell height in twips.</param>
	/// <param name="contentHeight">The total content height in twips (including margins).</param>
	/// <param name="alignment">The vertical alignment of the cell.</param>
	/// <returns>The vertical offset for positioning content within the cell.</returns>
	internal static float ComputeVerticalContentOffset(float cellHeight, float contentHeight, CellVerticalAlignment alignment)
		=> alignment switch
		{
			CellVerticalAlignment.Center => Math.Max(0f, (cellHeight - contentHeight) / 2f),
			CellVerticalAlignment.Bottom => Math.Max(0f, cellHeight - contentHeight),
			_ => 0f, // Top (default)
		};

	private static float EstimateBlockHeight(DocumentBlock block) => block switch
	{
		ParagraphBlock => DefaultRowHeightTwips,
		TablePlaceholderBlock tableBlock => EstimateNestedTableHeight(tableBlock),
		_ => DefaultRowHeightTwips,
	};

	private static float EstimateBlockHeight(DocumentBlock block, float contentWidthTwips)
	{
		if (block is ParagraphBlock paragraphBlock)
		{
			var text = paragraphBlock.SourceElement.InnerText;
			if (text.Length == 0)
			{
				return DefaultRowHeightTwips;
			}

			if (contentWidthTwips <= 0f)
			{
				return DefaultRowHeightTwips;
			}

			var lineCount = RenderCommandEmitter.EstimateWrappedLineCount(paragraphBlock, contentWidthTwips);
			return lineCount * DefaultRowHeightTwips;
		}

		if (block is TablePlaceholderBlock tableBlock)
		{
			return EstimateNestedTableHeight(tableBlock, contentWidthTwips);
		}

		return DefaultRowHeightTwips;
	}

	private static float EstimateNestedTableHeight(TablePlaceholderBlock tableBlock, float? availableWidthTwips = null)
	{
		var nestedTable = TableParser.Parse(tableBlock.TableElement);
		var availableWidth = availableWidthTwips is > 0f
			? availableWidthTwips.Value
			: ComputeDefaultNestedTableWidth(nestedTable);

		var nestedLayout = LayoutAutoFit(nestedTable, availableWidth);
		return nestedLayout.TotalHeightTwips > 0f ? nestedLayout.TotalHeightTwips : DefaultRowHeightTwips;
	}

	private static float ComputeDefaultNestedTableWidth(TableElement table)
	{
		if (table.GridColumns.Count == 0)
		{
			return DefaultBlockWidthTwips;
		}

		var sum = 0f;
		foreach (var column in table.GridColumns)
		{
			sum += Math.Max(0f, column.WidthTwips);
		}

		return sum > 0f ? sum : DefaultBlockWidthTwips;
	}
}
