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
		var contentLeft = section.MarginLeft;
		var contentWidth = MathF.Max(0f, section.PageWidth - section.MarginLeft - section.MarginRight);
		var columnCount = Math.Max(1, Math.Max(section.ColumnCount, section.Columns.Count));
		if (columnCount == 1)
		{
			return [new ColumnRegion(contentLeft, contentWidth)];
		}

		if (!section.ColumnsEqualWidth && section.Columns.Count > 0)
		{
			var totalFixedWidth = 0f;
			var totalExplicitSpacing = 0f;
			var unresolvedColumnCount = 0;

			for (var i = 0; i < columnCount; i++)
			{
				var definition = i < section.Columns.Count ? section.Columns[i] : default;
				if (definition.WidthTwips > 0)
				{
					totalFixedWidth += definition.WidthTwips;
				}
				else
				{
					unresolvedColumnCount++;
				}

				if (i < columnCount - 1)
				{
					totalExplicitSpacing += i < section.Columns.Count
						? MathF.Max(0f, definition.SpaceAfterTwips)
						: MathF.Max(0f, section.ColumnSpacingTwips);
				}
			}

			var remainingWidth = MathF.Max(0f, contentWidth - totalFixedWidth - totalExplicitSpacing);
			var fallbackWidth = unresolvedColumnCount > 0 ? remainingWidth / unresolvedColumnCount : 0f;
			var explicitResult = new ColumnRegion[columnCount];
			var explicitX = (float)contentLeft;

			for (var i = 0; i < columnCount; i++)
			{
				var definition = i < section.Columns.Count ? section.Columns[i] : default;
				var width = definition.WidthTwips > 0 ? definition.WidthTwips : fallbackWidth;
				explicitResult[i] = new ColumnRegion(explicitX, MathF.Max(0f, width));
				if (i < columnCount - 1)
				{
					var spacingAfter = i < section.Columns.Count
						? MathF.Max(0f, definition.SpaceAfterTwips)
						: MathF.Max(0f, section.ColumnSpacingTwips);
					explicitX += MathF.Max(0f, width) + spacingAfter;
				}
			}

			return explicitResult;
		}

		var spacing = MathF.Max(0f, section.ColumnSpacingTwips);
		var totalSpacing = spacing * (columnCount - 1);
		var columnWidth = MathF.Max(0f, (contentWidth - totalSpacing) / columnCount);
		var result = new ColumnRegion[columnCount];
		var currentX = (float)contentLeft;
		for (var i = 0; i < columnCount; i++)
		{
			result[i] = new ColumnRegion(currentX, columnWidth);
			currentX += columnWidth + spacing;
		}

		return result;
	}

	/// <summary>
	/// Computes the available content height for body text, accounting for page dimensions,
	/// margins, header/footer content heights, and footnote space.
	/// </summary>
	/// <remarks>
	/// In the OOXML model, <see cref="SectionInfo.MarginHeader"/> is the distance from the page
	/// top edge to the header start, and <see cref="SectionInfo.MarginTop"/> is the distance from
	/// the page top edge to the body start. If the header content is taller than the space between
	/// <c>MarginHeader</c> and <c>MarginTop</c>, the body area shrinks. The same logic applies
	/// symmetrically to the footer and bottom margin. Footnote height is subtracted directly
	/// from the remaining content area.
	/// </remarks>
	/// <param name="section">The section properties defining page dimensions and margins.</param>
	/// <param name="headerHeight">The height in twips of the header content. Default: 0.</param>
	/// <param name="footerHeight">The height in twips of the footer content. Default: 0.</param>
	/// <param name="footnoteHeight">The height in twips of footnote content to reserve. Default: 0.</param>
	/// <returns>The available height in twips for body content. Never negative.</returns>
	internal static float ComputeAvailableContentHeight(
		SectionInfo section,
		float headerHeight = 0f,
		float footerHeight = 0f,
		float footnoteHeight = 0f)
	{
		var effectiveTop = ComputeContentTop(section, headerHeight);
		var effectiveBottom = ComputeEffectiveBottomMargin(section, footerHeight);
		return Math.Max(0f, section.PageHeight - effectiveTop - effectiveBottom - footnoteHeight);
	}

	/// <summary>
	/// Computes the Y position (in twips from the page top) where header content starts.
	/// This is simply <see cref="SectionInfo.MarginHeader"/>.
	/// </summary>
	/// <param name="section">The section properties.</param>
	/// <returns>The Y offset in twips for the header.</returns>
	internal static float ComputeHeaderTop(SectionInfo section) => section.MarginHeader;

	/// <summary>
	/// Computes the Y position (in twips from the page top) where body content starts.
	/// If the header overflows its allocated space, the body is pushed down.
	/// </summary>
	/// <param name="section">The section properties.</param>
	/// <param name="headerHeight">The height in twips of the header content.</param>
	/// <returns>The Y offset in twips for the body content.</returns>
	internal static float ComputeContentTop(SectionInfo section, float headerHeight = 0f)
		=> Math.Max(section.MarginTop, section.MarginHeader + headerHeight);

	/// <summary>
	/// Computes the Y position (in twips from the page top) where footer content starts.
	/// The footer sits at <c>PageHeight - effectiveBottom</c>, where <c>effectiveBottom</c>
	/// is the greater of the bottom margin or the footer margin plus footer content height.
	/// </summary>
	/// <param name="section">The section properties.</param>
	/// <param name="footerHeight">The height in twips of the footer content.</param>
	/// <returns>The Y offset in twips for the footer.</returns>
	internal static float ComputeFooterTop(SectionInfo section, float footerHeight = 0f)
		=> section.PageHeight - ComputeEffectiveBottomMargin(section, footerHeight);

	/// <summary>
	/// Computes the Y position (in twips from the page top) where footnote content starts.
	/// Footnotes are placed above the footer, at the bottom of the content area.
	/// </summary>
	/// <param name="section">The section properties.</param>
	/// <param name="footerHeight">The height in twips of the footer content.</param>
	/// <param name="footnoteHeight">The height in twips of the footnote content.</param>
	/// <returns>The Y offset in twips for the footnote area.</returns>
	internal static float ComputeFootnoteTop(SectionInfo section, float footerHeight = 0f, float footnoteHeight = 0f)
		=> ComputeFooterTop(section, footerHeight) - footnoteHeight;

	private static float ComputeEffectiveBottomMargin(SectionInfo section, float footerHeight)
		=> Math.Max(section.MarginBottom, section.MarginFooter + footerHeight);

	internal readonly record struct ColumnRegion(float XTwips, float WidthTwips);
}
