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

	private sealed class ColumnPaginator
	{
		private readonly SectionInfo _section;
		private readonly float _headerHeight;
		private readonly float _footerHeight;
		private readonly float _footnoteHeight;
		private readonly float _availableHeight;
		private readonly float _contentTop;
		private readonly IReadOnlyList<ColumnRegion> _columns;
		private readonly List<LayoutPage> _pages = [];
		private List<LayoutBlock> _pageBlocks = [];
		private List<LayoutBlockPlacement> _placements = [];
		private float _currentHeight;
		private int _columnIndex;
		private bool _columnHasBlocks;
		private int _pageNumber;

		public ColumnPaginator(SectionInfo section, int startPageNumber, float headerHeight, float footerHeight, float footnoteHeight)
		{
			_section = section;
			_pageNumber = startPageNumber;
			_headerHeight = headerHeight;
			_footerHeight = footerHeight;
			_footnoteHeight = footnoteHeight;
			_availableHeight = ComputeAvailableContentHeight(section, headerHeight, footerHeight, footnoteHeight);
			_contentTop = ComputeContentTop(section, headerHeight);
			_columns = ComputeColumnRegions(section);
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
				FinalizePage();
			}

			if (_pages.Count > 0)
			{
				_pages[^1] = BalanceLastPageColumns(_pages[^1], _section, _headerHeight, _footerHeight, _footnoteHeight);
			}

			return _pages;
		}

		// Places the block and returns any block that must be placed next (a deferred or split remainder).
		private LayoutBlock? Place(LayoutBlock block)
		{
			if (block.ForcePageBreakBefore && _pageBlocks.Count > 0)
			{
				FinalizePage();
				return block;
			}

			if (block.ForceColumnBreakBefore && _columnHasBlocks)
			{
				AdvanceColumnOrPage();
				return block;
			}

			if (_currentHeight + block.HeightTwips <= _availableHeight)
			{
				AddBlock(block);
				return null;
			}

			var remainingSpace = _columnHasBlocks ? _availableHeight - _currentHeight : _availableHeight;
			var split = TrySplitBlock(block, remainingSpace);
			if (split is not null)
			{
				AddBlock(split.Value.First);
				AdvanceColumnOrPage();
				return split.Value.Second;
			}

			if (_columnHasBlocks)
			{
				AdvanceColumnOrPage();
				return block;
			}

			AddBlock(block);
			return null;
		}

		private void AddBlock(LayoutBlock block)
		{
			var column = _columns[_columnIndex];
			_pageBlocks.Add(block);
			_placements.Add(new LayoutBlockPlacement(
				block,
				column.XTwips,
				_contentTop + _currentHeight,
				column.WidthTwips,
				_columnIndex));
			_currentHeight += block.HeightTwips;
			_columnHasBlocks = true;
		}

		private void AdvanceColumnOrPage()
		{
			if (_columnIndex + 1 < _columns.Count)
			{
				_columnIndex++;
				_currentHeight = 0f;
				_columnHasBlocks = false;
				return;
			}

			FinalizePage();
		}

		private void FinalizePage()
		{
			_pages.Add(CreatePage(
				_section,
				_pageNumber,
				_pageBlocks,
				_placements,
				_headerHeight,
				_footerHeight,
				_footnoteHeight));
			_pageNumber++;
			_pageBlocks = [];
			_placements = [];
			_columnIndex = 0;
			_currentHeight = 0f;
			_columnHasBlocks = false;
		}
	}

	private sealed class ColumnBalancer
	{
		private readonly SectionInfo _section;
		private readonly IReadOnlyList<ColumnRegion> _columns;
		private readonly float _targetHeight;
		private readonly float _headerHeight;
		private readonly float _footerHeight;
		private readonly float _footnoteHeight;
		private readonly float _contentTop;
		private readonly float _availableHeight;
		private readonly List<LayoutBlock> _balancedBlocks = [];
		private readonly List<LayoutBlockPlacement> _placements = [];
		private int _columnIndex;
		private float _currentHeight;

		public ColumnBalancer(SectionInfo section, IReadOnlyList<ColumnRegion> columns, float targetHeight, float headerHeight, float footerHeight, float footnoteHeight)
		{
			_section = section;
			_columns = columns;
			_targetHeight = targetHeight;
			_headerHeight = headerHeight;
			_footerHeight = footerHeight;
			_footnoteHeight = footnoteHeight;
			_contentTop = ComputeContentTop(section, headerHeight);
			_availableHeight = ComputeAvailableContentHeight(section, headerHeight, footerHeight, footnoteHeight);
		}

		public LayoutPage? TryBalance(IReadOnlyList<LayoutBlock> blocks, int pageNumber)
		{
			var index = 0;
			LayoutBlock? pending = null;

			while (index < blocks.Count || pending is not null)
			{
				if (_columnIndex >= _columns.Count)
				{
					return null;
				}

				var block = pending ?? blocks[index];
				if (pending is null)
				{
					index++;
				}

				var placement = Place(block);
				if (placement.Failed)
				{
					return null;
				}

				pending = placement.Pending;
			}

			return CreatePage(
				_section,
				pageNumber,
				_balancedBlocks,
				_placements,
				_headerHeight,
				_footerHeight,
				_footnoteHeight);
		}

		private (bool Failed, LayoutBlock? Pending) Place(LayoutBlock block)
		{
			var isLastColumn = _columnIndex == _columns.Count - 1;
			var maxHeight = isLastColumn ? _availableHeight : _targetHeight;

			if (_currentHeight + block.HeightTwips <= maxHeight)
			{
				AddBlock(block);
				return (false, null);
			}

			var remainingSpace = _currentHeight > 0f ? maxHeight - _currentHeight : maxHeight;
			var split = TrySplitBlock(block, remainingSpace);
			if (split is not null)
			{
				AddBlock(split.Value.First);
				return isLastColumn ? (true, null) : AdvanceWith(split.Value.Second);
			}

			if (_currentHeight > 0f)
			{
				return isLastColumn ? (true, null) : AdvanceWith(block);
			}

			AddBlock(block);
			return (false, null);
		}

		private (bool Failed, LayoutBlock? Pending) AdvanceWith(LayoutBlock pending)
		{
			_columnIndex++;
			_currentHeight = 0f;
			return (false, pending);
		}

		private void AddBlock(LayoutBlock block)
		{
			var column = _columns[_columnIndex];
			_balancedBlocks.Add(block);
			_placements.Add(new LayoutBlockPlacement(
				block,
				column.XTwips,
				_contentTop + _currentHeight,
				column.WidthTwips,
				_columnIndex));
			_currentHeight += block.HeightTwips;
		}
	}

	/// <summary>
	/// The default minimum number of lines for widow/orphan control.
	/// </summary>
	internal const int DefaultWidowOrphanMinLines = 2;
}
