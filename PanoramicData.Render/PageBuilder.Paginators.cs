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

	private sealed class TableRowPaginator
	{
		private readonly SectionInfo _section;
		private readonly LayoutBlock[] _repeatedHeaders;
		private readonly float _availableHeight;
		private readonly List<LayoutPage> _pages = [];
		private List<LayoutBlock> _pageBlocks = [];
		private float _currentHeight;
		private int _pageNumber = 1;
		private int _repeatedHeaderCountOnPage;

		public TableRowPaginator(SectionInfo section, LayoutBlock[] repeatedHeaders, float availableHeight)
		{
			_section = section;
			_repeatedHeaders = repeatedHeaders;
			_availableHeight = availableHeight;
		}

		public IReadOnlyList<LayoutPage> Paginate(IReadOnlyList<LayoutBlock> rowBlocks)
		{
			var index = 0;
			LayoutBlock? pending = null;

			while (index < rowBlocks.Count || pending is not null)
			{
				if (_pageBlocks.Count == 0 && _pageNumber > 1)
				{
					AddRepeatedHeaders();
				}

				var block = pending ?? rowBlocks[index];
				if (pending is null)
				{
					index++;
				}

				pending = Place(block);
			}

			if (_pageBlocks.Count > 0)
			{
				_pages.Add(CreatePage(_section, _pageNumber, _pageBlocks));
			}

			return _pages;
		}

		private void AddRepeatedHeaders()
		{
			_repeatedHeaderCountOnPage = _repeatedHeaders.Length;
			foreach (var header in _repeatedHeaders)
			{
				AddBlock(header);
			}
		}

		// Places the row and returns any row that must be placed next (a deferred or split remainder).
		private LayoutBlock? Place(LayoutBlock block)
		{
			if (_currentHeight + block.HeightTwips <= _availableHeight)
			{
				AddBlock(block);
				return null;
			}

			var remainingSpace = _pageBlocks.Count > 0 ? _availableHeight - _currentHeight : _availableHeight;
			var split = TrySplitBlock(block, remainingSpace);
			if (split is not null)
			{
				_pageBlocks.Add(split.Value.First);
				FinalizePage();
				return split.Value.Second;
			}

			if (_pageBlocks.Count > _repeatedHeaderCountOnPage)
			{
				FinalizePage();
				return block;
			}

			// Prevent retry loops when a row still cannot fit with repeated headers.
			AddBlock(block);
			return null;
		}

		private void AddBlock(LayoutBlock block)
		{
			_pageBlocks.Add(block);
			_currentHeight += block.HeightTwips;
		}

		private void FinalizePage()
		{
			_pages.Add(CreatePage(_section, _pageNumber, _pageBlocks));
			_pageNumber++;
			_pageBlocks = [];
			_currentHeight = 0f;
			_repeatedHeaderCountOnPage = 0;
		}
	}
}