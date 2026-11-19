namespace PanoramicData.Render;

internal static partial class PageBuilder
{
	private sealed class BodyPaginator(SectionInfo section, int startPageNumber, float headerHeight, float footerHeight, float footnoteHeight)
		: PaginatorBase(section, startPageNumber, headerHeight, footerHeight, footnoteHeight)
	{
		protected override bool HasBlocksInRegion => PageBlocks.Count > 0;

		protected override void Complete()
		{
			if (PageBlocks.Count > 0)
			{
				FinalizePage(applyReservedHeights: true);
			}
		}

		protected override bool TryBreakBefore(LayoutBlock block)
		{
			if ((block.ForcePageBreakBefore || block.ForceColumnBreakBefore) && PageBlocks.Count > 0)
			{
				FinalizePage(applyReservedHeights: false);
			}

			return false;
		}

		// Pulls back a keepNext chain at the tail of the current page so it moves with the next block.
		protected override bool TryRelieveOverflow()
		{
			var pullBackCount = CountKeepWithNextTail(PageBlocks);
			if (pullBackCount <= 0 || pullBackCount >= PageBlocks.Count)
			{
				return false;
			}

			var keepStart = PageBlocks.Count - pullBackCount;
			var pulledBack = PageBlocks.GetRange(keepStart, pullBackCount);
			PageBlocks.RemoveRange(keepStart, pullBackCount);

			FinalizePage(applyReservedHeights: true);
			PageBlocks = new List<LayoutBlock>(pulledBack);
			CurrentHeight = pulledBack.Sum(b => b.HeightTwips);
			return true;
		}

		protected override void AddBlock(LayoutBlock block)
		{
			PageBlocks.Add(block);
			CurrentHeight += block.HeightTwips;
		}

		protected override void EndRegion() => FinalizePage(applyReservedHeights: true);

		private void FinalizePage(bool applyReservedHeights)
		{
			Pages.Add(applyReservedHeights
				? CreatePage(Section, PageNumber, PageBlocks, headerHeight: HeaderHeight, footerHeight: FooterHeight, footnoteHeight: FootnoteHeight)
				: CreatePage(Section, PageNumber, PageBlocks));
			PageNumber++;
			PageBlocks = [];
			CurrentHeight = 0f;
		}
	}
}