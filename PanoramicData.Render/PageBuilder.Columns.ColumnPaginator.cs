namespace PanoramicData.Render;

internal static partial class PageBuilder
{
	private sealed class ColumnPaginator : PaginatorBase
	{
		private readonly float _contentTop;
		private readonly IReadOnlyList<ColumnRegion> _columns;
		private List<LayoutBlockPlacement> _placements = [];
		private int _columnIndex;
		private bool _columnHasBlocks;

		public ColumnPaginator(SectionInfo section, int startPageNumber, float headerHeight, float footerHeight, float footnoteHeight)
			: base(section, startPageNumber, headerHeight, footerHeight, footnoteHeight)
		{
			_contentTop = ComputeContentTop(section, headerHeight);
			_columns = ComputeColumnRegions(section);
		}

		protected override bool HasBlocksInRegion => _columnHasBlocks;

		protected override void Complete()
		{
			if (PageBlocks.Count > 0)
			{
				FinalizePage();
			}

			if (Pages.Count > 0)
			{
				Pages[^1] = BalanceLastPageColumns(Pages[^1], Section, HeaderHeight, FooterHeight, FootnoteHeight);
			}
		}

		protected override bool TryBreakBefore(LayoutBlock block)
		{
			if (block.ForcePageBreakBefore && PageBlocks.Count > 0)
			{
				FinalizePage();
				return true;
			}

			if (block.ForceColumnBreakBefore && _columnHasBlocks)
			{
				AdvanceColumnOrPage();
				return true;
			}

			return false;
		}

		protected override void AddBlock(LayoutBlock block)
		{
			var column = _columns[_columnIndex];
			PageBlocks.Add(block);
			_placements.Add(new LayoutBlockPlacement(
				block,
				column.XTwips,
				_contentTop + CurrentHeight,
				column.WidthTwips,
				_columnIndex));
			CurrentHeight += block.HeightTwips;
			_columnHasBlocks = true;
		}

		protected override void EndRegion() => AdvanceColumnOrPage();

		private void AdvanceColumnOrPage()
		{
			if (_columnIndex + 1 < _columns.Count)
			{
				_columnIndex++;
				CurrentHeight = 0f;
				_columnHasBlocks = false;
				return;
			}

			FinalizePage();
		}

		private void FinalizePage()
		{
			Pages.Add(CreatePage(
				Section,
				PageNumber,
				PageBlocks,
				_placements,
				HeaderHeight,
				FooterHeight,
				FootnoteHeight));
			PageNumber++;
			PageBlocks = [];
			_placements = [];
			_columnIndex = 0;
			CurrentHeight = 0f;
			_columnHasBlocks = false;
		}
	}
}