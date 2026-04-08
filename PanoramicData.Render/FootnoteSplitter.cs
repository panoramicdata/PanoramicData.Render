namespace PanoramicData.Render;

/// <summary>
/// Splits footnote content across page boundaries when a footnote's content
/// exceeds the remaining space on the current page.
/// </summary>
internal static class FootnoteSplitter
{
	/// <summary>
	/// Splits a list of footnote layout blocks into two parts: those that fit on the current
	/// page (within the given available height) and those that must continue on the next page.
	/// </summary>
	/// <param name="blocks">The footnote layout blocks to split.</param>
	/// <param name="availableHeight">The available height in twips on the current page for footnotes.</param>
	/// <returns>
	/// A tuple of (currentPage, overflow). If all blocks fit, overflow is empty.
	/// If no blocks fit, currentPage is empty and overflow contains all blocks.
	/// </returns>
	public static (IReadOnlyList<LayoutBlock> CurrentPage, IReadOnlyList<LayoutBlock> Overflow) Split(
		IReadOnlyList<LayoutBlock> blocks,
		float availableHeight)
	{
		ArgumentNullException.ThrowIfNull(blocks);

		if (blocks.Count == 0 || availableHeight <= 0f)
		{
			return ([], blocks);
		}

		var accumulatedHeight = 0f;
		var splitIndex = 0;

		for (var i = 0; i < blocks.Count; i++)
		{
			var blockHeight = blocks[i].HeightTwips;
			if (accumulatedHeight + blockHeight > availableHeight)
			{
				break;
			}

			accumulatedHeight += blockHeight;
			splitIndex = i + 1;
		}

		if (splitIndex == 0)
		{
			// Not even the first block fits. All overflow.
			return ([], blocks);
		}

		if (splitIndex >= blocks.Count)
		{
			// All blocks fit.
			return (blocks, []);
		}

		var currentPage = blocks.Take(splitIndex).ToArray();
		var overflow = blocks.Skip(splitIndex).ToArray();
		return (currentPage, overflow);
	}
}
