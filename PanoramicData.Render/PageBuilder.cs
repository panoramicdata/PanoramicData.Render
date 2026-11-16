namespace PanoramicData.Render;

/// <summary>
/// Splits a stream of measured layout blocks into pages based on available height.
/// When blocks carry <see cref="LayoutBlock.LineHeights"/>, they can be split at line boundaries.
/// </summary>
internal static partial class PageBuilder
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
	/// <param name="footnoteHeight">The height in twips of footnote content to reserve from the content area. Default: 0.</param>
	/// <returns>An ordered list of pages. Empty when <paramref name="blocks"/> is empty.</returns>
	public static IReadOnlyList<LayoutPage> Paginate(
		IReadOnlyList<LayoutBlock> blocks,
		SectionInfo section,
		float headerHeight = 0f,
		float footerHeight = 0f,
		float footnoteHeight = 0f)
	{
		ArgumentNullException.ThrowIfNull(blocks);
		ArgumentNullException.ThrowIfNull(section);

		return PaginateStartingAt(blocks, section, 1, headerHeight, footerHeight, footnoteHeight);
	}

	/// <summary>
	/// Paginates table rows and repeats header rows on each continuation page.
	/// </summary>
	/// <param name="rowBlocks">One layout block per table row, in table order.</param>
	/// <param name="section">The section properties defining page dimensions and margins.</param>
	/// <param name="headerRowCount">The number of leading rows to repeat on continuation pages.</param>
	/// <param name="headerHeight">Reserved page header height in twips.</param>
	/// <param name="footerHeight">Reserved page footer height in twips.</param>
	/// <param name="footnoteHeight">Reserved footnote height in twips.</param>
	/// <returns>Paginated pages containing row blocks, with repeated header rows where applicable.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="rowBlocks"/> or <paramref name="section"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="headerRowCount"/> is out of range.</exception>
	public static IReadOnlyList<LayoutPage> PaginateTableRows(
		IReadOnlyList<LayoutBlock> rowBlocks,
		SectionInfo section,
		int headerRowCount,
		float headerHeight = 0f,
		float footerHeight = 0f,
		float footnoteHeight = 0f)
	{
		ArgumentNullException.ThrowIfNull(rowBlocks);
		ArgumentNullException.ThrowIfNull(section);

		if (headerRowCount < 0 || headerRowCount > rowBlocks.Count)
		{
			throw new ArgumentOutOfRangeException(nameof(headerRowCount));
		}

		if (rowBlocks.Count == 0)
		{
			return [];
		}

		if (headerRowCount == 0)
		{
			return Paginate(rowBlocks, section, headerHeight, footerHeight, footnoteHeight);
		}

		var repeatedHeaders = rowBlocks.Take(headerRowCount).ToArray();
		var availableHeight = ComputeAvailableContentHeight(section, headerHeight, footerHeight, footnoteHeight);
		var pages = new List<LayoutPage>();
		var currentPageBlocks = new List<LayoutBlock>();
		var currentHeight = 0f;
		var pageNumber = 1;
		var index = 0;
		LayoutBlock? pending = null;
		var repeatedHeaderCountOnCurrentPage = 0;

		while (index < rowBlocks.Count || pending is not null)
		{
			if (currentPageBlocks.Count == 0 && pageNumber > 1)
			{
				repeatedHeaderCountOnCurrentPage = repeatedHeaders.Length;
				foreach (var header in repeatedHeaders)
				{
					currentPageBlocks.Add(header);
					currentHeight += header.HeightTwips;
				}
			}

			var block = pending ?? rowBlocks[index];
			if (pending is null)
			{
				index++;
			}

			pending = null;

			if (currentHeight + block.HeightTwips <= availableHeight)
			{
				currentPageBlocks.Add(block);
				currentHeight += block.HeightTwips;
				continue;
			}

			var remainingSpace = currentPageBlocks.Count > 0
				? availableHeight - currentHeight
				: availableHeight;

			var split = TrySplitBlock(block, remainingSpace);
			if (split is not null)
			{
				currentPageBlocks.Add(split.Value.First);
				pages.Add(CreatePage(section, pageNumber, currentPageBlocks));
				pageNumber++;
				currentPageBlocks = [];
				currentHeight = 0f;
				repeatedHeaderCountOnCurrentPage = 0;
				pending = split.Value.Second;
				continue;
			}

			if (currentPageBlocks.Count > repeatedHeaderCountOnCurrentPage)
			{
				pages.Add(CreatePage(section, pageNumber, currentPageBlocks));
				pageNumber++;
				currentPageBlocks = [];
				currentHeight = 0f;
				repeatedHeaderCountOnCurrentPage = 0;
				pending = block;
			}
			else
			{
				// Prevent retry loops when a row still cannot fit with repeated headers.
				currentPageBlocks.Add(block);
				currentHeight += block.HeightTwips;
			}
		}

		if (currentPageBlocks.Count > 0)
		{
			pages.Add(CreatePage(section, pageNumber, currentPageBlocks));
		}

		return pages;
	}

	/// <summary>
	/// Core pagination logic supporting a custom starting page number.
	/// </summary>
	private static IReadOnlyList<LayoutPage> PaginateStartingAt(
		IReadOnlyList<LayoutBlock> blocks,
		SectionInfo section,
		int startPageNumber,
		float headerHeight = 0f,
		float footerHeight = 0f,
		float footnoteHeight = 0f)
	{
		if (blocks.Count == 0)
		{
			return [];
		}

		if (section.ColumnCount > 1)
		{
			return PaginateAcrossColumnsStartingAt(blocks, section, startPageNumber, headerHeight, footerHeight, footnoteHeight);
		}

		var availableHeight = ComputeAvailableContentHeight(section, headerHeight, footerHeight, footnoteHeight);
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

			// Handle forced page/column break before this block.
			if ((block.ForcePageBreakBefore || block.ForceColumnBreakBefore) && currentPageBlocks.Count > 0)
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

					pages.Add(CreatePage(section, pageNumber, currentPageBlocks, headerHeight: headerHeight, footerHeight: footerHeight, footnoteHeight: footnoteHeight));
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
					pages.Add(CreatePage(section, pageNumber, currentPageBlocks, headerHeight: headerHeight, footerHeight: footerHeight, footnoteHeight: footnoteHeight));
					pageNumber++;
					currentPageBlocks = [];
					currentHeight = 0f;
					pending = split.Value.Second;
				}
				else if (currentPageBlocks.Count > 0)
				{
					// Cannot split and page has content. Finalize page and retry on a fresh page.
					pages.Add(CreatePage(section, pageNumber, currentPageBlocks, headerHeight: headerHeight, footerHeight: footerHeight, footnoteHeight: footnoteHeight));
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
			pages.Add(CreatePage(section, pageNumber, currentPageBlocks, headerHeight: headerHeight, footerHeight: footerHeight, footnoteHeight: footnoteHeight));
		}

		return pages;
	}
}
