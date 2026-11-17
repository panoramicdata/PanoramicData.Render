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
}
