namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class TableLayoutEngine
{
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

		if (block is TablePlaceholderBlock tableBlock)
		{
			return EstimateNestedTablePreferredWidth(tableBlock);
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

		if (block is TablePlaceholderBlock tableBlock)
		{
			return EstimateNestedTableMinimumWidth(tableBlock);
		}

		return MinimumColumnWidthTwips;
	}

	private static float EstimateNestedTablePreferredWidth(TablePlaceholderBlock tableBlock)
	{
		var nestedTable = TableParser.Parse(tableBlock.TableElement);
		var measurements = MeasureColumnWidths(nestedTable);
		if (measurements.Count == 0)
		{
			return DefaultBlockWidthTwips;
		}

		var preferred = 0f;
		foreach (var measurement in measurements)
		{
			preferred += measurement.PreferredWidthTwips;
		}

		return preferred > 0f ? preferred : DefaultBlockWidthTwips;
	}

	private static float EstimateNestedTableMinimumWidth(TablePlaceholderBlock tableBlock)
	{
		var nestedTable = TableParser.Parse(tableBlock.TableElement);
		var measurements = MeasureColumnWidths(nestedTable);
		if (measurements.Count == 0)
		{
			return MinimumColumnWidthTwips;
		}

		var minimum = 0f;
		foreach (var measurement in measurements)
		{
			minimum += measurement.MinimumWidthTwips;
		}

		return minimum > 0f ? minimum : MinimumColumnWidthTwips;
	}

	/// <summary>
	/// Computes the cumulative x-offset of each column (left edge position relative to the table's left edge).
	/// When <paramref name="isBiDi"/> is <see langword="true"/>, columns are mirrored right-to-left.
	/// </summary>
	internal static IReadOnlyList<float> ComputeColumnOffsets(IReadOnlyList<float> columnWidths, bool isBiDi = false)
	{
		if (columnWidths.Count == 0)
		{
			return [];
		}

		// Always compute LTR offsets first
		var offsets = new float[columnWidths.Count];
		offsets[0] = 0f;
		for (var i = 1; i < columnWidths.Count; i++)
		{
			offsets[i] = offsets[i - 1] + columnWidths[i - 1];
		}

		if (isBiDi)
		{
			// Mirror: column i's left edge is at totalWidth - ltrOffset[i] - columnWidth[i]
			var totalWidth = offsets[^1] + columnWidths[^1];
			for (var i = 0; i < offsets.Length; i++)
			{
				offsets[i] = totalWidth - offsets[i] - columnWidths[i];
			}
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

	private static float SumColumnWidths(IReadOnlyList<float> columnWidths)
	{
		var total = 0f;
		for (var i = 0; i < columnWidths.Count; i++)
		{
			total += columnWidths[i];
		}

		return total;
	}
}
