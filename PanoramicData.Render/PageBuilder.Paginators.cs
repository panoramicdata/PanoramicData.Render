namespace PanoramicData.Render;

internal static partial class PageBuilder
{
	private sealed class BodyPaginator
	{
		private readonly SectionInfo _section;
		private readonly float _headerHeight;
		private readonly float _footerHeight;
		private readonly float _footnoteHeight;
		private readonly float _availableHeight;
		private readonly List<LayoutPage> _pages = [];
		private List<LayoutBlock> _pageBlocks = [];
		private float _currentHeight;
		private int _pageNumber;

		public BodyPaginator(SectionInfo section, int startPageNumber, float headerHeight, float footerHeight, float footnoteHeight)
		{
			_section = section;
			_pageNumber = startPageNumber;
			_headerHeight = headerHeight;
			_footerHeight = footerHeight;
			_footnoteHeight = footnoteHeight;
			_availableHeight = ComputeAvailableContentHeight(section, headerHeight, footerHeight, footnoteHeight);
		}

		public IReadOnlyList<LayoutPage> Paginate(IReadOnlyList<LayoutBlock> blocks)
		{
			var index = 0;
			LayoutBlock? pending = null;

			while (index < blocks.Count || pending is not null)
			{
				var block = pending ?? blocks[index];
				if (pending is null)
				{
					index++;
				}

				pending = Place(block);
			}

			if (_pageBlocks.Count > 0)
			{
				FinalizePage(applyReservedHeights: true);
			}

			return _pages;
		}

		// Places the block and returns any block that must be placed next (a deferred or split remainder).
		private LayoutBlock? Place(LayoutBlock block)
		{
			BreakBeforeIfForced(block);

			if (_currentHeight + block.HeightTwips <= _availableHeight)
			{
				AddBlock(block);
				return null;
			}

			if (TryPullBackKeepWithNextChain())
			{
				return block;
			}

			var remainingSpace = _pageBlocks.Count > 0 ? _availableHeight - _currentHeight : _availableHeight;
			var split = TrySplitBlock(block, remainingSpace);
			if (split is not null)
			{
				AddBlock(split.Value.First);
				FinalizePage(applyReservedHeights: true);
				return split.Value.Second;
			}

			if (_pageBlocks.Count > 0)
			{
				FinalizePage(applyReservedHeights: true);
				return block;
			}

			AddBlock(block);
			return null;
		}

		private void BreakBeforeIfForced(LayoutBlock block)
		{
			if ((block.ForcePageBreakBefore || block.ForceColumnBreakBefore) && _pageBlocks.Count > 0)
			{
				FinalizePage(applyReservedHeights: false);
			}
		}

		// Pulls back a keepNext chain at the tail of the current page so it moves with the next block.
		private bool TryPullBackKeepWithNextChain()
		{
			var pullBackCount = CountKeepWithNextTail(_pageBlocks);
			if (pullBackCount <= 0 || pullBackCount >= _pageBlocks.Count)
			{
				return false;
			}

			var keepStart = _pageBlocks.Count - pullBackCount;
			var pulledBack = _pageBlocks.GetRange(keepStart, pullBackCount);
			_pageBlocks.RemoveRange(keepStart, pullBackCount);

			FinalizePage(applyReservedHeights: true);
			_pageBlocks = new List<LayoutBlock>(pulledBack);
			_currentHeight = pulledBack.Sum(b => b.HeightTwips);
			return true;
		}

		private void AddBlock(LayoutBlock block)
		{
			_pageBlocks.Add(block);
			_currentHeight += block.HeightTwips;
		}

		private void FinalizePage(bool applyReservedHeights)
		{
			_pages.Add(applyReservedHeights
				? CreatePage(_section, _pageNumber, _pageBlocks, headerHeight: _headerHeight, footerHeight: _footerHeight, footnoteHeight: _footnoteHeight)
				: CreatePage(_section, _pageNumber, _pageBlocks));
			_pageNumber++;
			_pageBlocks = [];
			_currentHeight = 0f;
		}
	}
}
