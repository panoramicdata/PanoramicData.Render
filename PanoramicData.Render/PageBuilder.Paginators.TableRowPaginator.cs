namespace PanoramicData.Render;

internal static partial class PageBuilder
{
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
				AddRepeatedHeadersOnNewPage();

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

		private void AddRepeatedHeadersOnNewPage()
		{
			if (_pageBlocks.Count != 0 || _pageNumber <= 1)
			{
				return;
			}

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
