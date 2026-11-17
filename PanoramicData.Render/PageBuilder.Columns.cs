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
		=> new ColumnPaginator(section, startPageNumber, headerHeight, footerHeight, footnoteHeight).Paginate(blocks);

	private static LayoutPage BalanceLastPageColumns(
		LayoutPage page,
		SectionInfo section,
		float headerHeight = 0f,
		float footerHeight = 0f,
		float footnoteHeight = 0f)
	{
		if (section.ColumnCount <= 1 || page.Blocks.Count == 0 || HasForcedBreak(page))
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

		var balancer = new ColumnBalancer(section, columns, targetHeight, headerHeight, footerHeight, footnoteHeight);
		return balancer.TryBalance(page.Blocks, page.PageNumber) ?? page;
	}

	private static bool HasForcedBreak(LayoutPage page)
		=> page.Blocks.Any(block => block.ForceColumnBreakBefore || block.ForcePageBreakBefore);
}
