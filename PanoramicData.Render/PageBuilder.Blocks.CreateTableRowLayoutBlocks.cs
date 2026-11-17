namespace PanoramicData.Render;

internal static partial class PageBuilder
{
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
