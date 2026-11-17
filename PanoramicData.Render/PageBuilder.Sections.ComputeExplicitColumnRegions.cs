namespace PanoramicData.Render;

internal static partial class PageBuilder
{
	private static ColumnRegion[] ComputeExplicitColumnRegions(SectionInfo section, int columnCount, float contentLeft, float contentWidth)
	{
		var totalFixedWidth = 0f;
		var totalExplicitSpacing = 0f;
		var unresolvedColumnCount = 0;

		for (var i = 0; i < columnCount; i++)
		{
			var definition = GetColumnDefinition(section, i);
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
				totalExplicitSpacing += GetSpacingAfter(section, i, definition);
			}
		}

		var remainingWidth = MathF.Max(0f, contentWidth - totalFixedWidth - totalExplicitSpacing);
		var fallbackWidth = unresolvedColumnCount > 0 ? remainingWidth / unresolvedColumnCount : 0f;
		var result = new ColumnRegion[columnCount];
		var x = contentLeft;

		for (var i = 0; i < columnCount; i++)
		{
			var definition = GetColumnDefinition(section, i);
			var width = definition.WidthTwips > 0 ? definition.WidthTwips : fallbackWidth;
			result[i] = new ColumnRegion(x, MathF.Max(0f, width));
			if (i < columnCount - 1)
			{
				x += MathF.Max(0f, width) + GetSpacingAfter(section, i, definition);
			}
		}

		return result;
	}

	private static SectionColumnDefinition GetColumnDefinition(SectionInfo section, int index)
		=> index < section.Columns.Count ? section.Columns[index] : default;

	private static float GetSpacingAfter(SectionInfo section, int index, SectionColumnDefinition definition)
		=> index < section.Columns.Count
			? MathF.Max(0f, definition.SpaceAfterTwips)
			: MathF.Max(0f, section.ColumnSpacingTwips);

	private static ColumnRegion[] ComputeEqualColumnRegions(SectionInfo section, int columnCount, float contentLeft, float contentWidth)
	{
		var spacing = MathF.Max(0f, section.ColumnSpacingTwips);
		var totalSpacing = spacing * (columnCount - 1);
		var columnWidth = MathF.Max(0f, (contentWidth - totalSpacing) / columnCount);
		var result = new ColumnRegion[columnCount];
		var currentX = contentLeft;
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
