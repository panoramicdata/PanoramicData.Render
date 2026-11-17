namespace PanoramicData.Render;

/// <summary>
/// Splits a stream of measured layout blocks into pages based on available height.
/// When blocks carry <see cref="LayoutBlock.LineHeights"/>, they can be split at line boundaries.
/// </summary>
internal static partial class PageBuilder
{
	/// <summary>
	/// Paginates a full document block stream that may contain <see cref="SectionBreakBlock"/> markers.
	/// Splits the stream into sections, then paginates each section with its own dimensions.
	/// </summary>
	/// <param name="blocks">The measured blocks (may include SectionBreakBlock wrappers with zero height).</param>
	/// <param name="bodySectionInfo">The section properties for the final (body-level) section.</param>
	/// <returns>An ordered list of pages across all sections.</returns>
	public static IReadOnlyList<LayoutPage> PaginateDocument(
		IReadOnlyList<LayoutBlock> blocks,
		SectionInfo bodySectionInfo)
	{
		ArgumentNullException.ThrowIfNull(blocks);
		ArgumentNullException.ThrowIfNull(bodySectionInfo);

		var sections = IdentifySections(blocks, bodySectionInfo);
		var pages = new List<LayoutPage>();
		var pageNumber = 1;

		foreach (var section in sections)
		{
			// Handle break type: determine starting page number.
			if (pages.Count > 0)
			{
				pageNumber = ApplySectionBreak(section.BreakType, pageNumber, section.Info, pages);
			}

			var sectionPages = PaginateStartingAt(section.Blocks, section.Info, pageNumber);
			pages.AddRange(sectionPages);
			pageNumber += sectionPages.Count;
		}

		return pages;
	}

	/// <summary>
	/// Paginates a list of measured blocks into pages for the given section.
	/// </summary>
	/// <param name="blocks">The measured blocks to paginate.</param>
	/// <param name="section">The section properties defining page dimensions and margins.</param>
	/// <param name="headerHeight">The height in twips of the header content to reserve. Default: 0.</param>
	/// <param name="footerHeight">The height in twips of the footer content to reserve. Default: 0.</param>
	/// <param name="footnoteHeight">The height in twips of footnote content to reserve from the content area. Default: 0.</param>
	/// <returns>An ordered list of pages. Empty when <paramref name="blocks"/> is empty.</returns>
	public static IReadOnlyList<LayoutPage> Paginate(
		IReadOnlyList<LayoutBlock> blocks,
		SectionInfo section,
		float headerHeight = 0f,
		float footerHeight = 0f,
		float footnoteHeight = 0f)
	{
		ArgumentNullException.ThrowIfNull(blocks);
		ArgumentNullException.ThrowIfNull(section);

		return PaginateStartingAt(blocks, section, 1, headerHeight, footerHeight, footnoteHeight);
	}

	/// <summary>
	/// Paginates table rows and repeats header rows on each continuation page.
	/// </summary>
	/// <param name="rowBlocks">One layout block per table row, in table order.</param>
	/// <param name="section">The section properties defining page dimensions and margins.</param>
	/// <param name="headerRowCount">The number of leading rows to repeat on continuation pages.</param>
	/// <param name="headerHeight">Reserved page header height in twips.</param>
	/// <param name="footerHeight">Reserved page footer height in twips.</param>
	/// <param name="footnoteHeight">Reserved footnote height in twips.</param>
	/// <returns>Paginated pages containing row blocks, with repeated header rows where applicable.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="rowBlocks"/> or <paramref name="section"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="headerRowCount"/> is out of range.</exception>
	public static IReadOnlyList<LayoutPage> PaginateTableRows(
		IReadOnlyList<LayoutBlock> rowBlocks,
		SectionInfo section,
		int headerRowCount,
		float headerHeight = 0f,
		float footerHeight = 0f,
		float footnoteHeight = 0f)
	{
		ArgumentNullException.ThrowIfNull(rowBlocks);
		ArgumentNullException.ThrowIfNull(section);

		if (headerRowCount < 0 || headerRowCount > rowBlocks.Count)
		{
			throw new ArgumentOutOfRangeException(nameof(headerRowCount));
		}

		if (rowBlocks.Count == 0)
		{
			return [];
		}

		if (headerRowCount == 0)
		{
			return Paginate(rowBlocks, section, headerHeight, footerHeight, footnoteHeight);
		}

		var repeatedHeaders = rowBlocks.Take(headerRowCount).ToArray();
		var availableHeight = ComputeAvailableContentHeight(section, headerHeight, footerHeight, footnoteHeight);
		return new TableRowPaginator(section, repeatedHeaders, availableHeight).Paginate(rowBlocks);
	}

	/// <summary>
	/// Core pagination logic supporting a custom starting page number.
	/// </summary>
	private static IReadOnlyList<LayoutPage> PaginateStartingAt(
		IReadOnlyList<LayoutBlock> blocks,
		SectionInfo section,
		int startPageNumber,
		float headerHeight = 0f,
		float footerHeight = 0f,
		float footnoteHeight = 0f)
	{
		if (blocks.Count == 0)
		{
			return [];
		}

		if (section.ColumnCount > 1)
		{
			return PaginateAcrossColumnsStartingAt(blocks, section, startPageNumber, headerHeight, footerHeight, footnoteHeight);
		}

		return new BodyPaginator(section, startPageNumber, headerHeight, footerHeight, footnoteHeight).Paginate(blocks);
	}
}
