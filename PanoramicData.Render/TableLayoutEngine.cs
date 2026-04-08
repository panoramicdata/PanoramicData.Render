namespace PanoramicData.Render;

/// <summary>
/// Computes table layout geometry for fixed-width and auto-fit tables.
/// </summary>
internal static class TableLayoutEngine
{
	/// <summary>
	/// The default row height estimate in twips when no explicit height is specified
	/// and cell content is not yet measured. Corresponds to approximately 12pt single-spaced text.
	/// </summary>
	internal const float DefaultRowHeightTwips = 240f;

	/// <summary>
	/// The default estimated width per content block in twips (used as a placeholder
	/// until full text measurement is available).
	/// </summary>
	internal const float DefaultBlockWidthTwips = 2400f;

	/// <summary>
	/// The minimum column width in twips. Prevents columns from collapsing to zero.
	/// </summary>
	internal const float MinimumColumnWidthTwips = 360f;

	/// <summary>
	/// Computes the fixed-width layout of a table.
	/// </summary>
	/// <param name="table">The parsed table element.</param>
	/// <param name="availableWidthTwips">The available width for the table in twips (page content width minus indentation).</param>
	/// <returns>A <see cref="TableLayoutResult"/> with computed column offsets, widths, and row heights.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="table"/> is <see langword="null"/>.</exception>
	internal static TableLayoutResult Layout(TableElement table, float availableWidthTwips)
	{
		ArgumentNullException.ThrowIfNull(table);

		var columnWidths = ComputeFixedColumnWidths(table, availableWidthTwips);
		var columnOffsets = ComputeColumnOffsets(columnWidths);
		var tableWidth = columnWidths.Count > 0 ? columnOffsets[^1] + columnWidths[^1] : 0f;
		var tableXOffset = ComputeTableXOffset(table, tableWidth, availableWidthTwips);
		var rowHeights = ComputeRowHeights(table);
		var totalHeight = 0f;
		foreach (var rh in rowHeights)
		{
			totalHeight += rh;
		}

		return new TableLayoutResult
		{
			TableXOffset = tableXOffset,
			TableWidthTwips = tableWidth,
			ColumnOffsets = columnOffsets,
			ColumnWidths = columnWidths,
			RowHeights = rowHeights,
			TotalHeightTwips = totalHeight,
			Table = table,
		};
	}

	/// <summary>
	/// Computes the auto-fit layout of a table.
	/// First computes final auto-fit column widths, then re-lays out cell content
	/// against those widths to derive row heights.
	/// </summary>
	/// <param name="table">The parsed table element.</param>
	/// <param name="availableWidthTwips">The available width for the table in twips (page content width minus indentation).</param>
	/// <returns>A <see cref="TableLayoutResult"/> with auto-fit column widths and width-aware row heights.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="table"/> is <see langword="null"/>.</exception>
	internal static TableLayoutResult LayoutAutoFit(TableElement table, float availableWidthTwips)
	{
		ArgumentNullException.ThrowIfNull(table);

		var columnWidths = ComputeAutoFitColumnWidths(table, availableWidthTwips);
		var columnOffsets = ComputeColumnOffsets(columnWidths);
		var tableWidth = columnWidths.Count > 0 ? columnOffsets[^1] + columnWidths[^1] : 0f;
		var tableXOffset = ComputeTableXOffset(table, tableWidth, availableWidthTwips);
		var rowHeights = ComputeRowHeights(table, columnWidths);
		var totalHeight = 0f;
		foreach (var rh in rowHeights)
		{
			totalHeight += rh;
		}

		return new TableLayoutResult
		{
			TableXOffset = tableXOffset,
			TableWidthTwips = tableWidth,
			ColumnOffsets = columnOffsets,
			ColumnWidths = columnWidths,
			RowHeights = rowHeights,
			TotalHeightTwips = totalHeight,
			Table = table,
		};
	}

	/// <summary>
	/// Computes column widths for a fixed-width table layout.
	/// Uses the grid column widths directly from <c>w:tblGrid</c>.
	/// </summary>
	internal static IReadOnlyList<float> ComputeFixedColumnWidths(TableElement table, float availableWidthTwips)
	{
		if (table.GridColumns.Count == 0)
		{
			return [];
		}

		var widths = new float[table.GridColumns.Count];
		var hasAnyExplicit = false;

		for (var i = 0; i < table.GridColumns.Count; i++)
		{
			var w = table.GridColumns[i].WidthTwips;
			if (w > 0f)
			{
				widths[i] = w;
				hasAnyExplicit = true;
			}
		}

		if (!hasAnyExplicit)
		{
			// No explicit widths: distribute available width equally
			var equalWidth = availableWidthTwips / table.GridColumns.Count;
			for (var i = 0; i < widths.Length; i++)
			{
				widths[i] = equalWidth;
			}
		}
		else
		{
			// Fill any zero-width columns with equal share of remaining space
			var totalExplicit = 0f;
			var zeroCount = 0;
			foreach (var w in widths)
			{
				if (w > 0f)
				{
					totalExplicit += w;
				}
				else
				{
					zeroCount++;
				}
			}

			if (zeroCount > 0)
			{
				var remaining = Math.Max(0f, availableWidthTwips - totalExplicit);
				var share = remaining / zeroCount;
				for (var i = 0; i < widths.Length; i++)
				{
					if (widths[i] == 0f)
					{
						widths[i] = share;
					}
				}
			}
		}

		return widths;
	}

	/// <summary>
	/// Computes column widths for an auto-fit table layout.
	/// Distributes available width proportionally to preferred widths, respecting minimums.
	/// Handles percentage-based and fixed cell widths.
	/// </summary>
	/// <param name="table">The table element.</param>
	/// <param name="availableWidthTwips">The available width for the table in twips.</param>
	/// <returns>The computed column widths.</returns>
	internal static IReadOnlyList<float> ComputeAutoFitColumnWidths(TableElement table, float availableWidthTwips)
	{
		if (table.GridColumns.Count == 0)
		{
			return [];
		}

		// Resolve any percentage-based or explicit fixed column widths from cell definitions
		var colCount = table.GridColumns.Count;
		var fixedWidths = new float?[colCount];
		ResolveExplicitColumnWidths(table, availableWidthTwips, fixedWidths);

		var measurements = MeasureColumnWidths(table);

		// If there are any fixed/percentage columns, apply them and distribute remaining
		return DistributeWithFixedColumns(measurements, fixedWidths, availableWidthTwips);
	}

	/// <summary>
	/// Resolves explicit column widths from cell width specifications.
	/// For percentage-based widths (Pct), converts to absolute twips.
	/// For fixed widths (Dxa), uses the value directly.
	/// </summary>
	internal static void ResolveExplicitColumnWidths(
		TableElement table,
		float availableWidthTwips,
		float?[] fixedWidths)
	{
		foreach (var row in table.Rows)
		{
			var colIndex = 0;
			foreach (var cell in row.Cells)
			{
				if (colIndex >= fixedWidths.Length)
				{
					break;
				}

				if (cell.GridSpan == 1 && cell.VerticalMerge != VerticalMergeState.Continue)
				{
					if (cell.Width.Type == TableWidthUnit.Dxa && cell.Width.Value > 0f)
					{
						fixedWidths[colIndex] ??= cell.Width.Value;
					}
					else if (cell.Width.Type == TableWidthUnit.Pct && cell.Width.Value > 0f)
					{
						// Value is in fiftieths of a percent
						var pctWidth = availableWidthTwips * cell.Width.Value / 5000f;
						fixedWidths[colIndex] ??= pctWidth;
					}
				}

				colIndex += cell.GridSpan;
			}
		}
	}

	/// <summary>
	/// Distributes available width across columns, respecting any fixed column widths.
	/// Fixed columns keep their explicit width; auto columns share the remaining space
	/// proportionally to their preferred widths.
	/// </summary>
	internal static IReadOnlyList<float> DistributeWithFixedColumns(
		IReadOnlyList<ColumnMeasurement> measurements,
		float?[] fixedWidths,
		float availableWidthTwips)
	{
		if (measurements.Count == 0)
		{
			return [];
		}

		var hasAnyFixed = false;
		foreach (var fw in fixedWidths)
		{
			if (fw.HasValue)
			{
				hasAnyFixed = true;
				break;
			}
		}

		if (!hasAnyFixed)
		{
			return DistributeColumnWidths(measurements, availableWidthTwips);
		}

		// Separate fixed and auto columns
		var widths = new float[measurements.Count];
		var fixedTotal = 0f;
		var autoMeasurements = new List<(int Index, ColumnMeasurement Measurement)>();

		for (var i = 0; i < measurements.Count; i++)
		{
			if (fixedWidths[i].HasValue)
			{
				widths[i] = fixedWidths[i]!.Value;
				fixedTotal += widths[i];
			}
			else
			{
				autoMeasurements.Add((i, measurements[i]));
			}
		}

		if (autoMeasurements.Count == 0)
		{
			return widths;
		}

		// Distribute remaining space among auto columns
		var remainingWidth = Math.Max(0f, availableWidthTwips - fixedTotal);
		var autoColumnMeasurements = new ColumnMeasurement[autoMeasurements.Count];
		for (var i = 0; i < autoMeasurements.Count; i++)
		{
			autoColumnMeasurements[i] = autoMeasurements[i].Measurement;
		}

		var autoWidths = DistributeColumnWidths(autoColumnMeasurements, remainingWidth);

		for (var i = 0; i < autoMeasurements.Count; i++)
		{
			widths[autoMeasurements[i].Index] = autoWidths[i];
		}

		return widths;
	}

	/// <summary>
	/// Measures the preferred and minimum widths for each column in the table
	/// by examining cell content across all rows.
	/// </summary>
	internal static IReadOnlyList<ColumnMeasurement> MeasureColumnWidths(TableElement table)
	{
		var colCount = table.GridColumns.Count;
		var preferredWidths = new float[colCount];
		var minimumWidths = new float[colCount];

		foreach (var row in table.Rows)
		{
			var colIndex = 0;
			foreach (var cell in row.Cells)
			{
				if (colIndex >= colCount)
				{
					break;
				}

				if (cell.VerticalMerge == VerticalMergeState.Continue)
				{
					colIndex += cell.GridSpan;
					continue;
				}

				var preferredWidth = EstimateCellPreferredWidth(cell);
				var minimumWidth = EstimateCellMinimumWidth(cell);

				if (cell.GridSpan == 1)
				{
					if (preferredWidth > preferredWidths[colIndex])
					{
						preferredWidths[colIndex] = preferredWidth;
					}

					if (minimumWidth > minimumWidths[colIndex])
					{
						minimumWidths[colIndex] = minimumWidth;
					}
				}
				else
				{
					// For spanned cells, distribute evenly across spanned columns
					var spanEnd = Math.Min(colIndex + cell.GridSpan, colCount);
					var spanCount = spanEnd - colIndex;
					var perColPreferred = preferredWidth / spanCount;
					var perColMinimum = minimumWidth / spanCount;

					for (var s = colIndex; s < spanEnd; s++)
					{
						if (perColPreferred > preferredWidths[s])
						{
							preferredWidths[s] = perColPreferred;
						}

						if (perColMinimum > minimumWidths[s])
						{
							minimumWidths[s] = perColMinimum;
						}
					}
				}

				colIndex += cell.GridSpan;
			}
		}

		var result = new ColumnMeasurement[colCount];
		for (var i = 0; i < colCount; i++)
		{
			result[i] = new ColumnMeasurement(
				Math.Max(preferredWidths[i], MinimumColumnWidthTwips),
				Math.Max(minimumWidths[i], MinimumColumnWidthTwips));
		}

		return result;
	}

	/// <summary>
	/// Distributes available width across columns proportionally to preferred widths,
	/// respecting minimum widths.
	/// </summary>
	internal static IReadOnlyList<float> DistributeColumnWidths(
		IReadOnlyList<ColumnMeasurement> measurements,
		float availableWidthTwips)
	{
		if (measurements.Count == 0)
		{
			return [];
		}

		var widths = new float[measurements.Count];

		// Start with minimum widths
		var totalMinimum = 0f;
		var totalPreferred = 0f;
		for (var i = 0; i < measurements.Count; i++)
		{
			widths[i] = measurements[i].MinimumWidthTwips;
			totalMinimum += measurements[i].MinimumWidthTwips;
			totalPreferred += measurements[i].PreferredWidthTwips;
		}

		// If minimums exceed available width, use minimums as-is
		if (totalMinimum >= availableWidthTwips)
		{
			return widths;
		}

		// If preferred fits within available, use preferred widths
		if (totalPreferred <= availableWidthTwips)
		{
			// Distribute the remaining space proportionally to preferred widths
			var remaining = availableWidthTwips - totalPreferred;
			for (var i = 0; i < measurements.Count; i++)
			{
				widths[i] = measurements[i].PreferredWidthTwips + (remaining * measurements[i].PreferredWidthTwips / totalPreferred);
			}

			return widths;
		}

		// Preferred exceeds available: distribute proportionally between minimum and preferred
		var excessOverMinimum = availableWidthTwips - totalMinimum;
		var totalStretch = totalPreferred - totalMinimum;

		for (var i = 0; i < measurements.Count; i++)
		{
			var stretch = measurements[i].PreferredWidthTwips - measurements[i].MinimumWidthTwips;
			widths[i] = measurements[i].MinimumWidthTwips + (excessOverMinimum * stretch / totalStretch);
		}

		return widths;
	}

	/// <summary>
	/// The average estimated character width in twips for width estimation.
	/// Based on approximately 7pt per character in 12pt text (typical proportional font).
	/// </summary>
	internal const float AverageCharWidthTwips = 140f;

	/// <summary>
	/// Estimates the preferred (natural) width of a cell's content.
	/// Uses text content length when available, otherwise falls back to block count heuristic.
	/// </summary>
	internal static float EstimateCellPreferredWidth(TableCellElement cell)
	{
		if (cell.Blocks.Count == 0)
		{
			return MinimumColumnWidthTwips;
		}

		// If the cell has an explicit fixed width, use it as the preferred width
		if (cell.Width.Type == TableWidthUnit.Dxa && cell.Width.Value > 0f)
		{
			return cell.Width.Value;
		}

		// Measure based on text content: preferred is the width if each paragraph is one line
		var maxLineWidth = 0f;
		foreach (var block in cell.Blocks)
		{
			var lineWidth = EstimateBlockPreferredWidth(block);
			if (lineWidth > maxLineWidth)
			{
				maxLineWidth = lineWidth;
			}
		}

		var contentWidth = maxLineWidth > 0f ? maxLineWidth : DefaultBlockWidthTwips;
		return Math.Max(contentWidth + cell.Margins.Left + cell.Margins.Right, MinimumColumnWidthTwips);
	}

	/// <summary>
	/// Estimates the minimum width of a cell's content (widest non-breakable unit).
	/// Uses the longest word when text is available, otherwise uses a fixed minimum.
	/// </summary>
	internal static float EstimateCellMinimumWidth(TableCellElement cell)
	{
		var maxWordWidth = 0f;
		foreach (var block in cell.Blocks)
		{
			var wordWidth = EstimateBlockMinimumWidth(block);
			if (wordWidth > maxWordWidth)
			{
				maxWordWidth = wordWidth;
			}
		}

		var contentMinimum = maxWordWidth > 0f ? maxWordWidth : (cell.Blocks.Count > 0 ? MinimumColumnWidthTwips : 0f);
		return contentMinimum + cell.Margins.Left + cell.Margins.Right;
	}

	/// <summary>
	/// Estimates the preferred (single-line) width of a block.
	/// For paragraphs, uses the total text length × average character width.
	/// </summary>
	internal static float EstimateBlockPreferredWidth(DocumentBlock block)
	{
		if (block is ParagraphBlock paragraphBlock)
		{
			var text = paragraphBlock.SourceElement.InnerText;
			if (text.Length > 0)
			{
				return text.Length * AverageCharWidthTwips;
			}
		}

		return DefaultBlockWidthTwips;
	}

	/// <summary>
	/// Estimates the minimum width of a block (widest non-breakable word).
	/// For paragraphs, finds the longest whitespace-delimited word.
	/// </summary>
	internal static float EstimateBlockMinimumWidth(DocumentBlock block)
	{
		if (block is ParagraphBlock paragraphBlock)
		{
			var text = paragraphBlock.SourceElement.InnerText;
			if (text.Length > 0)
			{
				var maxWordLength = 0;
				var currentWordLength = 0;
				foreach (var ch in text)
				{
					if (char.IsWhiteSpace(ch))
					{
						if (currentWordLength > maxWordLength)
						{
							maxWordLength = currentWordLength;
						}

						currentWordLength = 0;
					}
					else
					{
						currentWordLength++;
					}
				}

				if (currentWordLength > maxWordLength)
				{
					maxWordLength = currentWordLength;
				}

				return maxWordLength * AverageCharWidthTwips;
			}
		}

		return MinimumColumnWidthTwips;
	}

	/// <summary>
	/// Computes the cumulative x-offset of each column (left edge position relative to the table's left edge).
	/// </summary>
	internal static IReadOnlyList<float> ComputeColumnOffsets(IReadOnlyList<float> columnWidths)
	{
		if (columnWidths.Count == 0)
		{
			return [];
		}

		var offsets = new float[columnWidths.Count];
		offsets[0] = 0f;
		for (var i = 1; i < columnWidths.Count; i++)
		{
			offsets[i] = offsets[i - 1] + columnWidths[i - 1];
		}

		return offsets;
	}

	/// <summary>
	/// Computes the table's x-offset from the left content edge based on alignment and indentation.
	/// </summary>
	internal static float ComputeTableXOffset(TableElement table, float tableWidth, float availableWidthTwips)
	{
		return table.Alignment switch
		{
			TableAlignment.Center => Math.Max(0f, (availableWidthTwips - tableWidth) / 2f),
			TableAlignment.Right => Math.Max(0f, availableWidthTwips - tableWidth),
			_ => table.IndentationTwips,
		};
	}

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

				var contentHeight = MeasureCellContentHeight(cell);
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

			var maxContentHeight = 0f;
			var colIndex = 0;
			foreach (var cell in row.Cells)
			{
				if (colIndex >= columnWidths.Count)
				{
					break;
				}

				var span = Math.Max(1, cell.GridSpan);
				var spanEnd = Math.Min(colIndex + span, columnWidths.Count);
				var cellWidth = 0f;
				for (var s = colIndex; s < spanEnd; s++)
				{
					cellWidth += columnWidths[s];
				}

				if (cell.VerticalMerge != VerticalMergeState.Continue)
				{
					var contentHeight = MeasureCellContentHeight(cell, cellWidth);
					if (contentHeight > maxContentHeight)
					{
						maxContentHeight = contentHeight;
					}
				}

				colIndex += span;
			}

			var contentBasedHeight = maxContentHeight > 0f ? maxContentHeight : DefaultRowHeightTwips;
			heights[i] = Math.Max(specifiedHeight, contentBasedHeight);
		}

		return heights;
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
	internal static (IReadOnlyList<LayoutBlock> Blocks, float TotalHeight) LayoutCellContent(TableCellElement cell)
	{
		ArgumentNullException.ThrowIfNull(cell);

		if (cell.Blocks.Count == 0)
		{
			var marginHeight = cell.Margins.Top + cell.Margins.Bottom;
			return ([], marginHeight);
		}

		var layoutBlocks = new List<LayoutBlock>();
		var totalHeight = cell.Margins.Top;

		foreach (var block in cell.Blocks)
		{
			var height = EstimateBlockHeight(block);
			layoutBlocks.Add(new LayoutBlock(block, height));
			totalHeight += height;
		}

		totalHeight += cell.Margins.Bottom;

		return (layoutBlocks, totalHeight);
	}

	/// <summary>
	/// Computes the effective content width of a cell, accounting for left and right margins.
	/// </summary>
	/// <param name="cellWidth">The total cell width in twips.</param>
	/// <param name="margins">The cell margins.</param>
	/// <returns>The content area width in twips (never less than zero).</returns>
	internal static float ComputeContentWidth(float cellWidth, CellMargins margins)
		=> Math.Max(0f, cellWidth - margins.Left - margins.Right);

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

			var preferredWidth = text.Length * AverageCharWidthTwips;
			var lineCount = Math.Max(1, (int)MathF.Ceiling(preferredWidth / contentWidthTwips));
			return lineCount * DefaultRowHeightTwips;
		}

		return DefaultRowHeightTwips;
	}

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

		// Compute row y-offsets (cumulative)
		var rowOffsets = new float[rowCount];
		if (rowCount > 0)
		{
			rowOffsets[0] = 0f;
			for (var r = 1; r < rowCount; r++)
			{
				rowOffsets[r] = rowOffsets[r - 1] + layout.RowHeights[r - 1];
			}
		}

		// Track visited owner cells to emit each once
		var visited = new HashSet<(int Row, int Col)>();
		var positions = new List<CellPosition>();

		for (var r = 0; r < rowCount; r++)
		{
			for (var c = 0; c < colCount; c++)
			{
				if (grid[r, c] is not { } resolved)
				{
					continue;
				}

				var ownerKey = (resolved.OwnerRowIndex, resolved.OwnerColumnIndex);
				if (!visited.Add(ownerKey))
				{
					continue;
				}

				// Compute width: sum of all columns this cell spans
				var cellWidth = 0f;
				for (var s = 0; s < resolved.Cell.GridSpan && resolved.OwnerColumnIndex + s < colCount; s++)
				{
					cellWidth += layout.ColumnWidths[resolved.OwnerColumnIndex + s];
				}

				// Compute height: sum of all rows this cell spans (via vertical merge)
				var spanRows = CountVerticalSpan(grid, resolved.OwnerRowIndex, resolved.OwnerColumnIndex, rowCount);
				var cellHeight = 0f;
				for (var sr = 0; sr < spanRows; sr++)
				{
					cellHeight += layout.RowHeights[resolved.OwnerRowIndex + sr];
				}

				positions.Add(new CellPosition(
					resolved.OwnerRowIndex,
					resolved.OwnerColumnIndex,
					layout.ColumnOffsets[resolved.OwnerColumnIndex],
					rowOffsets[resolved.OwnerRowIndex],
					cellWidth,
					cellHeight,
					resolved.Cell));
			}
		}

		return positions;
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
	/// Counts how many consecutive rows the cell at (<paramref name="ownerRow"/>, <paramref name="ownerCol"/>)
	/// spans by checking how many rows in the resolved grid reference the same owner.
	/// </summary>
	private static int CountVerticalSpan(ResolvedGridCell?[,] grid, int ownerRow, int ownerCol, int rowCount)
	{
		var span = 1;
		for (var r = ownerRow + 1; r < rowCount; r++)
		{
			if (grid[r, ownerCol] is { } cell
				&& cell.OwnerRowIndex == ownerRow
				&& cell.OwnerColumnIndex == ownerCol)
			{
				span++;
			}
			else
			{
				break;
			}
		}

		return span;
	}
}
