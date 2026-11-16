namespace PanoramicData.Render;

internal static partial class PageBuilder
{
	private static IReadOnlyList<LayoutPage> PaginateAcrossColumnsStartingAt(
		IReadOnlyList<LayoutBlock> blocks,
		SectionInfo section,
		int startPageNumber,
		float headerHeight = 0f,
		float footerHeight = 0f,
		float footnoteHeight = 0f)
	{
		var availableHeight = ComputeAvailableContentHeight(section, headerHeight, footerHeight, footnoteHeight);
		var contentTop = ComputeContentTop(section, headerHeight);
		var columns = ComputeColumnRegions(section);
		var pages = new List<LayoutPage>();
		var currentPageBlocks = new List<LayoutBlock>();
		var currentPlacements = new List<LayoutBlockPlacement>();
		var currentHeight = 0f;
		var currentColumnIndex = 0;
		var currentColumnHasBlocks = false;
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

			if (block.ForcePageBreakBefore && currentPageBlocks.Count > 0)
			{
				FinalizeCurrentPage();
				pending = block;
				continue;
			}

			if (block.ForceColumnBreakBefore && currentColumnHasBlocks)
			{
				pending = block;
				AdvanceColumnOrPage();
				continue;
			}

			if (currentHeight + block.HeightTwips <= availableHeight)
			{
				AddCurrentBlock(block);
				continue;
			}

			var remainingSpace = currentColumnHasBlocks
				? availableHeight - currentHeight
				: availableHeight;

			var split = TrySplitBlock(block, remainingSpace);

			if (split is not null)
			{
				AddCurrentBlock(split.Value.First);
				pending = split.Value.Second;
				AdvanceColumnOrPage();
				continue;
			}

			if (currentColumnHasBlocks)
			{
				pending = block;
				AdvanceColumnOrPage();
				continue;
			}

			AddCurrentBlock(block);
		}

		if (currentPageBlocks.Count > 0)
		{
			FinalizeCurrentPage();
		}

		if (pages.Count > 0)
		{
			pages[^1] = BalanceLastPageColumns(pages[^1], section, headerHeight, footerHeight, footnoteHeight);
		}

		return pages;

		void AddCurrentBlock(LayoutBlock block)
		{
			var column = columns[currentColumnIndex];
			currentPageBlocks.Add(block);
			currentPlacements.Add(new LayoutBlockPlacement(
				block,
				column.XTwips,
				contentTop + currentHeight,
				column.WidthTwips,
				currentColumnIndex));
			currentHeight += block.HeightTwips;
			currentColumnHasBlocks = true;
		}

		void AdvanceColumnOrPage()
		{
			if (currentColumnIndex + 1 < columns.Count)
			{
				currentColumnIndex++;
				currentHeight = 0f;
				currentColumnHasBlocks = false;
				return;
			}

			FinalizeCurrentPage();
		}

		void FinalizeCurrentPage()
		{
			pages.Add(CreatePage(
				section,
				pageNumber,
				currentPageBlocks,
				currentPlacements,
				headerHeight,
				footerHeight,
				footnoteHeight));
			pageNumber++;
			currentPageBlocks = [];
			currentPlacements = [];
			currentColumnIndex = 0;
			currentHeight = 0f;
			currentColumnHasBlocks = false;
		}
	}

	private static LayoutPage BalanceLastPageColumns(
		LayoutPage page,
		SectionInfo section,
		float headerHeight = 0f,
		float footerHeight = 0f,
		float footnoteHeight = 0f)
	{
		if (section.ColumnCount <= 1 || page.Blocks.Count == 0)
		{
			return page;
		}

		if (page.Blocks.Any(block => block.ForceColumnBreakBefore || block.ForcePageBreakBefore))
		{
			return page;
		}

		var availableHeight = ComputeAvailableContentHeight(section, headerHeight, footerHeight, footnoteHeight);
		var totalHeight = page.Blocks.Sum(block => block.HeightTwips);
		if (totalHeight <= 0f)
		{
			return page;
		}

		var columns = ComputeColumnRegions(section);
		var targetHeight = MathF.Min(availableHeight, totalHeight / columns.Count);
		if (targetHeight <= 0f || targetHeight >= availableHeight)
		{
			return page;
		}

		var balanced = TryBalanceBlocksAcrossColumns(page.Blocks, page.PageNumber, section, columns, targetHeight, headerHeight, footerHeight, footnoteHeight);
		return balanced ?? page;
	}

	private static LayoutPage? TryBalanceBlocksAcrossColumns(
		IReadOnlyList<LayoutBlock> blocks,
		int pageNumber,
		SectionInfo section,
		IReadOnlyList<ColumnRegion> columns,
		float targetHeight,
		float headerHeight,
		float footerHeight,
		float footnoteHeight)
	{
		var contentTop = ComputeContentTop(section, headerHeight);
		var availableHeight = ComputeAvailableContentHeight(section, headerHeight, footerHeight, footnoteHeight);
		var balancedBlocks = new List<LayoutBlock>();
		var placements = new List<LayoutBlockPlacement>();
		var currentColumnIndex = 0;
		var currentHeight = 0f;
		var index = 0;
		LayoutBlock? pending = null;

		while (index < blocks.Count || pending is not null)
		{
			if (currentColumnIndex >= columns.Count)
			{
				return null;
			}

			var block = pending ?? blocks[index];
			if (pending is null)
			{
				index++;
			}

			pending = null;

			var maxHeight = currentColumnIndex == columns.Count - 1
				? availableHeight
				: targetHeight;

			if (currentHeight + block.HeightTwips <= maxHeight)
			{
				AddBlock(block);
				continue;
			}

			var remainingSpace = currentHeight > 0f ? maxHeight - currentHeight : maxHeight;
			var split = TrySplitBlock(block, remainingSpace);
			if (split is not null)
			{
				AddBlock(split.Value.First);
				if (currentColumnIndex == columns.Count - 1)
				{
					return null;
				}

				pending = split.Value.Second;
				AdvanceColumn();
				continue;
			}

			if (currentHeight > 0f)
			{
				if (currentColumnIndex == columns.Count - 1)
				{
					return null;
				}

				pending = block;
				AdvanceColumn();
				continue;
			}

			AddBlock(block);
		}

		return CreatePage(
			section,
			pageNumber,
			balancedBlocks,
			placements,
			headerHeight,
			footerHeight,
			footnoteHeight);

		void AddBlock(LayoutBlock block)
		{
			var column = columns[currentColumnIndex];
			balancedBlocks.Add(block);
			placements.Add(new LayoutBlockPlacement(
				block,
				column.XTwips,
				contentTop + currentHeight,
				column.WidthTwips,
				currentColumnIndex));
			currentHeight += block.HeightTwips;
		}

		void AdvanceColumn()
		{
			currentColumnIndex++;
			currentHeight = 0f;
		}
	}

	/// <summary>
	/// The default minimum number of lines for widow/orphan control.
	/// </summary>
	internal const int DefaultWidowOrphanMinLines = 2;
}
