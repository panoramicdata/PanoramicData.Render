namespace PanoramicData.Render;

internal static partial class PageBuilder
{
	/// <summary>
	/// Splits the block stream into sections based on <see cref="SectionBreakBlock"/> markers.
	/// The last section uses <paramref name="bodySectionInfo"/>.
	/// </summary>
	internal static IReadOnlyList<DocumentSection> IdentifySections(
		IReadOnlyList<LayoutBlock> blocks,
		SectionInfo bodySectionInfo)
	{
		var sections = new List<DocumentSection>();
		var currentBlocks = new List<LayoutBlock>();

		foreach (var layoutBlock in blocks)
		{
			if (layoutBlock.Block is SectionBreakBlock sectionBreak)
			{
				// The first section always starts with NextPage (it begins the document).
				// Subsequent sections use their own SectionInfo.BreakType, which declares
				// how that section itself begins (e.g. EvenPage, OddPage, Continuous).
				var breakType = sections.Count == 0
					? SectionBreakType.NextPage
					: sectionBreak.SectionInfo.BreakType;
				sections.Add(new DocumentSection(sectionBreak.SectionInfo, currentBlocks.ToArray(), breakType));
				currentBlocks = [];
			}
			else
			{
				currentBlocks.Add(layoutBlock);
			}
		}

		// Remaining blocks belong to the body (final) section.
		if (currentBlocks.Count > 0 || sections.Count == 0)
		{
			// Body section uses its own BreakType unless there are no breaks.
			var bodyBreakType = sections.Count == 0
				? SectionBreakType.NextPage
				: bodySectionInfo.BreakType;
			sections.Add(new DocumentSection(bodySectionInfo, currentBlocks.ToArray(), bodyBreakType));
		}

		return sections;
	}

	/// <summary>
	/// Applies a section break type, potentially inserting blank pages.
	/// Returns the page number at which the new section should start.
	/// </summary>
	private static int ApplySectionBreak(
		SectionBreakType breakType,
		int nextPageNumber,
		SectionInfo newSection,
		List<LayoutPage> pages)
	{
		switch (breakType)
		{
			case SectionBreakType.Continuous:
				// Continuous break: no new page (but we still start a fresh section).
				// For simplicity, start on a new page since page dimensions may change.
				return nextPageNumber;

			case SectionBreakType.EvenPage:
				// Must start on an even page number.
				if (nextPageNumber % 2 != 0)
				{
					// Insert a blank odd page.
					pages.Add(CreateBlankPage(newSection, nextPageNumber));
					return nextPageNumber + 1;
				}

				return nextPageNumber;

			case SectionBreakType.OddPage:
				// Must start on an odd page number.
				if (nextPageNumber % 2 == 0)
				{
					// Insert a blank even page.
					pages.Add(CreateBlankPage(newSection, nextPageNumber));
					return nextPageNumber + 1;
				}

				return nextPageNumber;

			case SectionBreakType.NextPage:
			default:
				return nextPageNumber;
		}
	}

	private static LayoutPage CreateBlankPage(SectionInfo section, int pageNumber) => new()
	{
		Section = section,
		PageNumber = pageNumber,
		Blocks = [],
		HeaderTopTwips = ComputeHeaderTop(section),
		ContentTopTwips = ComputeContentTop(section),
		FooterTopTwips = ComputeFooterTop(section),
		FootnoteTopTwips = ComputeFootnoteTop(section)
	};

	internal static IReadOnlyList<ColumnRegion> ComputeColumnRegions(SectionInfo section)
	{
		var contentLeft = (float)section.MarginLeft;
		var contentWidth = MathF.Max(0f, section.PageWidth - section.MarginLeft - section.MarginRight);
		var columnCount = Math.Max(1, Math.Max(section.ColumnCount, section.Columns.Count));
		if (columnCount == 1)
		{
			return [new ColumnRegion(contentLeft, contentWidth)];
		}

		return !section.ColumnsEqualWidth && section.Columns.Count > 0
			? ComputeExplicitColumnRegions(section, columnCount, contentLeft, contentWidth)
			: ComputeEqualColumnRegions(section, columnCount, contentLeft, contentWidth);
	}
}
