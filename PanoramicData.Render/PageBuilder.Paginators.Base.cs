namespace PanoramicData.Render;

internal static partial class PageBuilder
{
	private abstract class PaginatorBase
	{
		protected PaginatorBase(SectionInfo section, int startPageNumber, float headerHeight, float footerHeight, float footnoteHeight)
		{
			Section = section;
			PageNumber = startPageNumber;
			HeaderHeight = headerHeight;
			FooterHeight = footerHeight;
			FootnoteHeight = footnoteHeight;
			AvailableHeight = ComputeAvailableContentHeight(section, headerHeight, footerHeight, footnoteHeight);
		}

		protected SectionInfo Section { get; }

		protected float HeaderHeight { get; }

		protected float FooterHeight { get; }

		protected float FootnoteHeight { get; }

		protected float AvailableHeight { get; }

		protected List<LayoutPage> Pages { get; } = [];

		protected List<LayoutBlock> PageBlocks { get; set; } = [];

		protected float CurrentHeight { get; set; }

		protected int PageNumber { get; set; }

		protected abstract bool HasBlocksInRegion { get; }

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

			Complete();
			return Pages;
		}

		protected abstract void Complete();

		protected abstract bool TryBreakBefore(LayoutBlock block);

		protected abstract void AddBlock(LayoutBlock block);

		protected abstract void EndRegion();

		protected virtual bool TryRelieveOverflow() => false;

		// Places the block and returns any block that must be placed next (a deferred or split remainder).
		private LayoutBlock? Place(LayoutBlock block)
		{
			if (TryBreakBefore(block))
			{
				return block;
			}

			if (CurrentHeight + block.HeightTwips <= AvailableHeight)
			{
				AddBlock(block);
				return null;
			}

			if (TryRelieveOverflow())
			{
				return block;
			}

			var remainingSpace = HasBlocksInRegion ? AvailableHeight - CurrentHeight : AvailableHeight;
			var split = TrySplitBlock(block, remainingSpace);
			if (split is not null)
			{
				AddBlock(split.Value.First);
				EndRegion();
				return split.Value.Second;
			}

			if (HasBlocksInRegion)
			{
				EndRegion();
				return block;
			}

			AddBlock(block);
			return null;
		}
	}
}