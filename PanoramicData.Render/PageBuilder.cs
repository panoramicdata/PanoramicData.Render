namespace PanoramicData.Render;

/// <summary>
/// Splits a stream of measured layout blocks into pages based on available height.
/// When blocks carry <see cref="LayoutBlock.LineHeights"/>, they can be split at line boundaries.
/// </summary>
internal static class PageBuilder
{
	/// <summary>
	/// Paginates a list of measured blocks into pages for the given section.
	/// </summary>
	/// <param name="blocks">The measured blocks to paginate.</param>
	/// <param name="section">The section properties defining page dimensions and margins.</param>
	/// <returns>An ordered list of pages. Empty when <paramref name="blocks"/> is empty.</returns>
	public static IReadOnlyList<LayoutPage> Paginate(
		IReadOnlyList<LayoutBlock> blocks,
		SectionInfo section)
	{
		ArgumentNullException.ThrowIfNull(blocks);
		ArgumentNullException.ThrowIfNull(section);

		if (blocks.Count == 0)
		{
			return [];
		}

		var availableHeight = (float)(section.PageHeight - section.MarginTop - section.MarginBottom);
		var pages = new List<LayoutPage>();
		var currentPageBlocks = new List<LayoutBlock>();
		var currentHeight = 0f;
		var pageNumber = 1;

		var index = 0;
		LayoutBlock? pending = null;

		while (index < blocks.Count || pending is not null)
		{
			var block = pending ?? blocks[index];
			if (pending is null)
			{
				index++;
			}

			pending = null;

			// Handle forced page break before this block.
			if (block.ForcePageBreakBefore && currentPageBlocks.Count > 0)
			{
				pages.Add(CreatePage(section, pageNumber, currentPageBlocks));
				pageNumber++;
				currentPageBlocks = [];
				currentHeight = 0f;
			}

			if (currentHeight + block.HeightTwips <= availableHeight)
			{
				// Block fits on the current page.
				currentPageBlocks.Add(block);
				currentHeight += block.HeightTwips;
			}
			else
			{
				// Block doesn't fit. Try to split at a line boundary.
				var remainingSpace = currentPageBlocks.Count > 0
					? availableHeight - currentHeight
					: availableHeight;

				var split = TrySplitBlock(block, remainingSpace);

				if (split is not null)
				{
					// Place the first part on the current page, queue the second part.
					currentPageBlocks.Add(split.Value.First);
					pages.Add(CreatePage(section, pageNumber, currentPageBlocks));
					pageNumber++;
					currentPageBlocks = [];
					currentHeight = 0f;
					pending = split.Value.Second;
				}
				else if (currentPageBlocks.Count > 0)
				{
					// Cannot split and page has content. Finalize page and retry on a fresh page.
					pages.Add(CreatePage(section, pageNumber, currentPageBlocks));
					pageNumber++;
					currentPageBlocks = [];
					currentHeight = 0f;
					pending = block;
				}
				else
				{
					// Cannot split and page is empty. Place oversized block.
					currentPageBlocks.Add(block);
					currentHeight += block.HeightTwips;
				}
			}
		}

		// Finalize the last page.
		if (currentPageBlocks.Count > 0)
		{
			pages.Add(CreatePage(section, pageNumber, currentPageBlocks));
		}

		return pages;
	}

	/// <summary>
	/// Attempts to split a block at a line boundary to fit within the available space.
	/// Returns <see langword="null"/> when the block cannot be split (no <see cref="LayoutBlock.LineHeights"/>,
	/// fewer than 2 lines, or not even the first line fits).
	/// </summary>
	internal static (LayoutBlock First, LayoutBlock Second)? TrySplitBlock(
		LayoutBlock block,
		float availableSpace)
	{
		if (block.LineHeights is null || block.LineHeights.Count < 2)
		{
			return null;
		}

		// Accumulate lines until we exceed the available space.
		var heightAccumulated = block.SpaceBefore;
		var linesFitting = 0;

		for (var i = 0; i < block.LineHeights.Count; i++)
		{
			var nextHeight = heightAccumulated + block.LineHeights[i];
			if (nextHeight > availableSpace && linesFitting > 0)
			{
				break;
			}

			heightAccumulated = nextHeight;
			linesFitting++;
		}

		// Cannot split if no lines fit or all lines already fit.
		if (linesFitting == 0 || linesFitting >= block.LineHeights.Count)
		{
			return null;
		}

		var firstLineHeights = block.LineHeights.Take(linesFitting).ToArray();
		var secondLineHeights = block.LineHeights.Skip(linesFitting).ToArray();

		var firstHeight = block.SpaceBefore + Sum(firstLineHeights);
		var secondHeight = Sum(secondLineHeights) + block.SpaceAfter;

		var first = new LayoutBlock(block.Block, firstHeight, block.SpaceBefore, 0f, firstLineHeights);
		var second = new LayoutBlock(block.Block, secondHeight, 0f, block.SpaceAfter, secondLineHeights);

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

	private static LayoutPage CreatePage(
		SectionInfo section,
		int pageNumber,
		List<LayoutBlock> blocks) => new()
		{
			Section = section,
			PageNumber = pageNumber,
			Blocks = blocks.ToArray()
		};
}
