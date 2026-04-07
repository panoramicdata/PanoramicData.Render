namespace PanoramicData.Render;

/// <summary>
/// Splits a stream of measured layout blocks into pages based on available height.
/// </summary>
internal static class PageBuilder
{
	/// <summary>
	/// Paginates a list of measured blocks into pages for the given section.
	/// </summary>
	/// <param name="blocks">The measured blocks to paginate.</param>
	/// <param name="section">The section properties defining page dimensions and margins.</param>
	/// <returns>An ordered list of pages. Empty when <paramref name="blocks"/> is empty.</returns>
	public static IReadOnlyList<LayoutPage> Paginate(
		IReadOnlyList<LayoutBlock> blocks,
		SectionInfo section)
	{
		ArgumentNullException.ThrowIfNull(blocks);
		ArgumentNullException.ThrowIfNull(section);

		if (blocks.Count == 0)
		{
			return [];
		}

		var availableHeight = (float)(section.PageHeight - section.MarginTop - section.MarginBottom);
		var pages = new List<LayoutPage>();
		var currentPageBlocks = new List<LayoutBlock>();
		var currentHeight = 0f;
		var pageNumber = 1;

		foreach (var block in blocks)
		{
			// If adding this block would exceed available height and the current page
			// already has content, finalize the current page and start a new one.
			if (currentPageBlocks.Count > 0 && currentHeight + block.HeightTwips > availableHeight)
			{
				pages.Add(CreatePage(section, pageNumber, currentPageBlocks));
				pageNumber++;
				currentPageBlocks = [];
				currentHeight = 0f;
			}

			currentPageBlocks.Add(block);
			currentHeight += block.HeightTwips;
		}

		// Finalize the last page.
		if (currentPageBlocks.Count > 0)
		{
			pages.Add(CreatePage(section, pageNumber, currentPageBlocks));
		}

		return pages;
	}

	private static LayoutPage CreatePage(
		SectionInfo section,
		int pageNumber,
		List<LayoutBlock> blocks) => new()
		{
			Section = section,
			PageNumber = pageNumber,
			Blocks = blocks.ToArray()
		};
}
