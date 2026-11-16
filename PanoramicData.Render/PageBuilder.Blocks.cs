namespace PanoramicData.Render;

internal static partial class PageBuilder
{
	/// <summary>
	/// Attempts to split a block at a line boundary to fit within the available space.
	/// When <see cref="LayoutBlock.KeepLinesTogether"/> is enabled, the block cannot be split.
	/// When <see cref="LayoutBlock.WidowOrphanControl"/> is enabled, ensures at least
	/// <see cref="DefaultWidowOrphanMinLines"/> lines remain on each side of the split.
	/// Returns <see langword="null"/> when the block cannot be split.
	/// </summary>
	internal static (LayoutBlock First, LayoutBlock Second)? TrySplitBlock(
		LayoutBlock block,
		float availableSpace)
	{
		if (block.KeepLinesTogether || block.LineHeights is null || block.LineHeights.Count < 2)
		{
			return null;
		}

		var linesFitting = CountFittingLines(block.LineHeights, block.SpaceBefore, availableSpace);

		// Cannot split if no lines fit or all lines already fit.
		if (linesFitting == 0 || linesFitting >= block.LineHeights.Count)
		{
			return null;
		}

		if (block.WidowOrphanControl)
		{
			var adjusted = ApplyWidowOrphanControl(block.LineHeights.Count, linesFitting);
			if (adjusted is null)
			{
				return null;
			}

			linesFitting = adjusted.Value;
		}

		return SplitAtLine(block, block.LineHeights, linesFitting);
	}

	// Accumulates lines until the available space is exceeded.
	private static int CountFittingLines(IReadOnlyList<float> lineHeights, float spaceBefore, float availableSpace)
	{
		var heightAccumulated = spaceBefore;
		var linesFitting = 0;

		for (var i = 0; i < lineHeights.Count; i++)
		{
			var nextHeight = heightAccumulated + lineHeights[i];
			if (nextHeight > availableSpace && linesFitting > 0)
			{
				break;
			}

			heightAccumulated = nextHeight;
			linesFitting++;
		}

		return linesFitting;
	}

	// Returns the adjusted line count, or null when the orphan or widow rule forbids splitting.
	private static int? ApplyWidowOrphanControl(int totalLines, int linesFitting)
	{
		// Orphan check: must leave at least minLines on the current page.
		if (linesFitting < DefaultWidowOrphanMinLines)
		{
			return null;
		}

		// Widow check: must send at least minLines to the next page.
		if (totalLines - linesFitting >= DefaultWidowOrphanMinLines)
		{
			return linesFitting;
		}

		// Re-check orphan: if pulling back lines violates the orphan rule, can't split.
		var pulledBack = totalLines - DefaultWidowOrphanMinLines;
		return pulledBack < DefaultWidowOrphanMinLines ? null : pulledBack;
	}

	private static (LayoutBlock First, LayoutBlock Second) SplitAtLine(LayoutBlock block, IReadOnlyList<float> lineHeights, int linesFitting)
	{
		var firstLineHeights = lineHeights.Take(linesFitting).ToArray();
		var secondLineHeights = lineHeights.Skip(linesFitting).ToArray();

		var firstHeight = block.SpaceBefore + Sum(firstLineHeights);
		var secondHeight = Sum(secondLineHeights) + block.SpaceAfter;

		var first = new LayoutBlock(block.Block, firstHeight, block.SpaceBefore, 0f, firstLineHeights,
			WidowOrphanControl: block.WidowOrphanControl,
			LineStartIndex: block.LineStartIndex);
		var second = new LayoutBlock(block.Block, secondHeight, 0f, block.SpaceAfter, secondLineHeights,
			WidowOrphanControl: block.WidowOrphanControl,
			LineStartIndex: block.LineStartIndex + linesFitting);

		return (first, second);
	}
	private static float Sum(float[] values)
	{
		var sum = 0f;
		foreach (var v in values)
		{
			sum += v;
		}

		return sum;
	}

	/// <summary>
	/// Creates a pagination block for a table using per-row heights as line boundaries.
	/// This enables table row splitting at page boundaries when row splitting is allowed.
	/// </summary>
	/// <param name="tableBlock">The source table placeholder block.</param>
	/// <param name="rowHeights">Per-row heights in twips.</param>
	/// <returns>A table <see cref="LayoutBlock"/> that can be split at row boundaries.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="tableBlock"/> or <paramref name="rowHeights"/> is <see langword="null"/>.</exception>
	internal static LayoutBlock CreateTableLayoutBlock(
		TablePlaceholderBlock tableBlock,
		IReadOnlyList<float> rowHeights)
	{
		ArgumentNullException.ThrowIfNull(tableBlock);
		ArgumentNullException.ThrowIfNull(rowHeights);

		var heights = rowHeights.ToArray();
		var totalHeight = 0f;
		foreach (var height in heights)
		{
			totalHeight += Math.Max(0f, height);
		}

		return new LayoutBlock(
			tableBlock,
			totalHeight,
			LineHeights: heights,
			WidowOrphanControl: false,
			KeepLinesTogether: false);
	}

	/// <summary>
	/// Creates row-level pagination blocks for a table.
	/// Each row can be marked as non-splittable via <paramref name="cantSplitRows"/>.
	/// </summary>
	/// <param name="tableBlock">The source table placeholder block.</param>
	/// <param name="rowLineHeights">Per-row line heights (each row can contain multiple line heights).</param>
	/// <param name="cantSplitRows">Optional flags indicating rows that cannot split across pages.</param>
	/// <returns>One <see cref="LayoutBlock"/> per table row.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="tableBlock"/> or <paramref name="rowLineHeights"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException"><paramref name="cantSplitRows"/> count does not match row count.</exception>
	internal static IReadOnlyList<LayoutBlock> CreateTableRowLayoutBlocks(
		TablePlaceholderBlock tableBlock,
		IReadOnlyList<IReadOnlyList<float>> rowLineHeights,
		IReadOnlyList<bool>? cantSplitRows = null)
	{
		ArgumentNullException.ThrowIfNull(tableBlock);
		ArgumentNullException.ThrowIfNull(rowLineHeights);

		if (cantSplitRows is not null && cantSplitRows.Count != rowLineHeights.Count)
		{
			throw new ArgumentException("cantSplitRows count must match rowLineHeights count.", nameof(cantSplitRows));
		}

		var blocks = new LayoutBlock[rowLineHeights.Count];
		for (var i = 0; i < rowLineHeights.Count; i++)
		{
			var lines = rowLineHeights[i]?.ToArray() ?? [];
			var rowHeight = 0f;
			foreach (var line in lines)
			{
				rowHeight += Math.Max(0f, line);
			}

			blocks[i] = new LayoutBlock(
				tableBlock,
				rowHeight,
				LineHeights: lines,
				WidowOrphanControl: false,
				KeepLinesTogether: cantSplitRows?[i] ?? false);
		}

		return blocks;
	}

	private static LayoutPage CreatePage(
		SectionInfo section,
		int pageNumber,
		List<LayoutBlock> blocks,
		IReadOnlyList<LayoutBlockPlacement>? blockPlacements = null,
		float headerHeight = 0f,
		float footerHeight = 0f,
		float footnoteHeight = 0f) => new()
		{
			Section = section,
			PageNumber = pageNumber,
			Blocks = blocks.ToArray(),
			BlockPlacements = blockPlacements?.ToArray() ?? [],
			HeaderTopTwips = ComputeHeaderTop(section),
			ContentTopTwips = ComputeContentTop(section, headerHeight),
			FooterTopTwips = ComputeFooterTop(section, footerHeight),
			FootnoteTopTwips = ComputeFootnoteTop(section, footerHeight, footnoteHeight)
		};

	/// <summary>
	/// Counts the number of consecutive blocks at the tail of the list that have <see cref="LayoutBlock.KeepWithNext"/> set.
	/// </summary>
	private static int CountKeepWithNextTail(List<LayoutBlock> blocks)
	{
		var count = 0;
		for (var i = blocks.Count - 1; i >= 0; i--)
		{
			if (!blocks[i].KeepWithNext)
			{
				break;
			}

			count++;
		}

		return count;
	}
}
