namespace PanoramicData.Render;

/// <summary>
/// Splits a stream of measured layout blocks into pages based on available height.
/// When blocks carry <see cref="LayoutBlock.LineHeights"/>, they can be split at line boundaries.
/// </summary>
internal static class PageBuilder
{
	/// <summary>
	/// Paginates a full document block stream that may contain <see cref="SectionBreakBlock"/> markers.
	/// Splits the stream into sections, then paginates each section with its own dimensions.
	/// </summary>
	/// <param name="blocks">The measured blocks (may include SectionBreakBlock wrappers with zero height).</param>
	/// <param name="bodySectionInfo">The section properties for the final (body-level) section.</param>
	/// <returns>An ordered list of pages across all sections.</returns>
	public static IReadOnlyList<LayoutPage> PaginateDocument(
		IReadOnlyList<LayoutBlock> blocks,
		SectionInfo bodySectionInfo)
	{
		ArgumentNullException.ThrowIfNull(blocks);
		ArgumentNullException.ThrowIfNull(bodySectionInfo);

		var sections = IdentifySections(blocks, bodySectionInfo);
		var pages = new List<LayoutPage>();
		var pageNumber = 1;

		foreach (var section in sections)
		{
			// Handle break type: determine starting page number.
			if (pages.Count > 0)
			{
				pageNumber = ApplySectionBreak(section.BreakType, pageNumber, section.Info, pages);
			}

			var sectionPages = PaginateStartingAt(section.Blocks, section.Info, pageNumber);
			pages.AddRange(sectionPages);
			pageNumber += sectionPages.Count;
		}

		return pages;
	}

	/// <summary>
	/// Paginates a list of measured blocks into pages for the given section.
	/// </summary>
	/// <param name="blocks">The measured blocks to paginate.</param>
	/// <param name="section">The section properties defining page dimensions and margins.</param>
	/// <param name="headerHeight">The height in twips of the header content to reserve. Default: 0.</param>
	/// <param name="footerHeight">The height in twips of the footer content to reserve. Default: 0.</param>
	/// <returns>An ordered list of pages. Empty when <paramref name="blocks"/> is empty.</returns>
	public static IReadOnlyList<LayoutPage> Paginate(
		IReadOnlyList<LayoutBlock> blocks,
		SectionInfo section,
		float headerHeight = 0f,
		float footerHeight = 0f)
	{
		ArgumentNullException.ThrowIfNull(blocks);
		ArgumentNullException.ThrowIfNull(section);

		return PaginateStartingAt(blocks, section, 1, headerHeight, footerHeight);
	}

	/// <summary>
	/// Core pagination logic supporting a custom starting page number.
	/// </summary>
	private static IReadOnlyList<LayoutPage> PaginateStartingAt(
		IReadOnlyList<LayoutBlock> blocks,
		SectionInfo section,
		int startPageNumber,
		float headerHeight = 0f,
		float footerHeight = 0f)
	{
		if (blocks.Count == 0)
		{
			return [];
		}

		var availableHeight = ComputeAvailableContentHeight(section, headerHeight, footerHeight);
		var pages = new List<LayoutPage>();
		var currentPageBlocks = new List<LayoutBlock>();
		var currentHeight = 0f;
		var pageNumber = startPageNumber;

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
				// Block doesn't fit. First check for keepNext chain at the tail of the current page.
				var pullBackCount = CountKeepWithNextTail(currentPageBlocks);
				if (pullBackCount > 0 && pullBackCount < currentPageBlocks.Count)
				{
					// Pull back the keepNext chain and re-process the current block.
					var keepStart = currentPageBlocks.Count - pullBackCount;
					var pulledBack = currentPageBlocks.GetRange(keepStart, pullBackCount);
					currentPageBlocks.RemoveRange(keepStart, pullBackCount);
					currentHeight -= pulledBack.Sum(b => b.HeightTwips);

					pages.Add(CreatePage(section, pageNumber, currentPageBlocks));
					pageNumber++;
					currentPageBlocks = new List<LayoutBlock>(pulledBack);
					currentHeight = pulledBack.Sum(b => b.HeightTwips);
					pending = block;
					continue;
				}

				// Try to split at a line boundary.
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
	/// The default minimum number of lines for widow/orphan control.
	/// </summary>
	internal const int DefaultWidowOrphanMinLines = 2;

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

		// Apply widow/orphan constraints when enabled.
		if (block.WidowOrphanControl)
		{
			var totalLines = block.LineHeights.Count;
			var linesRemaining = totalLines - linesFitting;

			// Orphan check: must leave at least minLines on the current page.
			if (linesFitting < DefaultWidowOrphanMinLines)
			{
				return null;
			}

			// Widow check: must send at least minLines to the next page.
			if (linesRemaining < DefaultWidowOrphanMinLines)
			{
				linesFitting = totalLines - DefaultWidowOrphanMinLines;

				// Re-check orphan: if pulling back lines violates orphan rule, can't split.
				if (linesFitting < DefaultWidowOrphanMinLines)
				{
					return null;
				}
			}
		}

		var firstLineHeights = block.LineHeights.Take(linesFitting).ToArray();
		var secondLineHeights = block.LineHeights.Skip(linesFitting).ToArray();

		var firstHeight = block.SpaceBefore + Sum(firstLineHeights);
		var secondHeight = Sum(secondLineHeights) + block.SpaceAfter;

		var first = new LayoutBlock(block.Block, firstHeight, block.SpaceBefore, 0f, firstLineHeights,
			WidowOrphanControl: block.WidowOrphanControl);
		var second = new LayoutBlock(block.Block, secondHeight, 0f, block.SpaceAfter, secondLineHeights,
			WidowOrphanControl: block.WidowOrphanControl);

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

	/// <summary>
	/// Splits the block stream into sections based on <see cref="SectionBreakBlock"/> markers.
	/// The last section uses <paramref name="bodySectionInfo"/>.
	/// </summary>
	internal static IReadOnlyList<DocumentSection> IdentifySections(
		IReadOnlyList<LayoutBlock> blocks,
		SectionInfo bodySectionInfo)
	{
		var sections = new List<DocumentSection>();
		var currentBlocks = new List<LayoutBlock>();
		var isFirst = true;

		foreach (var layoutBlock in blocks)
		{
			if (layoutBlock.Block is SectionBreakBlock sectionBreak)
			{
				// The break's SectionInfo describes the section that just ended.
				var breakType = isFirst ? SectionBreakType.NextPage : sectionBreak.SectionInfo.BreakType;
				sections.Add(new DocumentSection(sectionBreak.SectionInfo, currentBlocks.ToArray(), breakType));
				currentBlocks = [];
				isFirst = false;
			}
			else
			{
				currentBlocks.Add(layoutBlock);
			}
		}

		// Remaining blocks belong to the body (final) section.
		if (currentBlocks.Count > 0 || sections.Count == 0)
		{
			var breakType = isFirst ? SectionBreakType.NextPage : bodySectionInfo.BreakType;
			sections.Add(new DocumentSection(bodySectionInfo, currentBlocks.ToArray(), breakType));
		}

		return sections;
	}

	/// <summary>
	/// Applies a section break type, potentially inserting blank pages.
	/// Returns the page number at which the new section should start.
	/// </summary>
	private static int ApplySectionBreak(
		SectionBreakType breakType,
		int nextPageNumber,
		SectionInfo newSection,
		List<LayoutPage> pages)
	{
		switch (breakType)
		{
			case SectionBreakType.Continuous:
				// Continuous break: no new page (but we still start a fresh section).
				// For simplicity, start on a new page since page dimensions may change.
				return nextPageNumber;

			case SectionBreakType.EvenPage:
				// Must start on an even page number.
				if (nextPageNumber % 2 != 0)
				{
					// Insert a blank odd page.
					pages.Add(CreateBlankPage(newSection, nextPageNumber));
					return nextPageNumber + 1;
				}

				return nextPageNumber;

			case SectionBreakType.OddPage:
				// Must start on an odd page number.
				if (nextPageNumber % 2 == 0)
				{
					// Insert a blank even page.
					pages.Add(CreateBlankPage(newSection, nextPageNumber));
					return nextPageNumber + 1;
				}

				return nextPageNumber;

			case SectionBreakType.NextPage:
			default:
				return nextPageNumber;
		}
	}

	private static LayoutPage CreateBlankPage(SectionInfo section, int pageNumber) => new()
	{
		Section = section,
		PageNumber = pageNumber,
		Blocks = []
	};

	/// <summary>
	/// Computes the available content height for body text, accounting for page dimensions,
	/// margins, and header/footer content heights.
	/// </summary>
	/// <remarks>
	/// In the OOXML model, <see cref="SectionInfo.MarginHeader"/> is the distance from the page
	/// top edge to the header start, and <see cref="SectionInfo.MarginTop"/> is the distance from
	/// the page top edge to the body start. If the header content is taller than the space between
	/// <c>MarginHeader</c> and <c>MarginTop</c>, the body area shrinks. The same logic applies
	/// symmetrically to the footer and bottom margin.
	/// </remarks>
	/// <param name="section">The section properties defining page dimensions and margins.</param>
	/// <param name="headerHeight">The height in twips of the header content. Default: 0.</param>
	/// <param name="footerHeight">The height in twips of the footer content. Default: 0.</param>
	/// <returns>The available height in twips for body content. Never negative.</returns>
	internal static float ComputeAvailableContentHeight(
		SectionInfo section,
		float headerHeight = 0f,
		float footerHeight = 0f)
	{
		var effectiveTop = Math.Max(section.MarginTop, section.MarginHeader + headerHeight);
		var effectiveBottom = Math.Max(section.MarginBottom, section.MarginFooter + footerHeight);
		return Math.Max(0f, section.PageHeight - effectiveTop - effectiveBottom);
	}
}
