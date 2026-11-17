namespace PanoramicData.Render;

internal static partial class PageBuilder
{
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
