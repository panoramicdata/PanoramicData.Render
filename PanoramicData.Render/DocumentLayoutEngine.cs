namespace PanoramicData.Render;

/// <summary>
/// Converts parsed <see cref="DocumentBlock"/> instances into measured <see cref="LayoutBlock"/>
/// instances suitable for pagination by <see cref="PageBuilder"/>.
/// </summary>
internal static class DocumentLayoutEngine
{
	/// <summary>
	/// The default natural line height in twips, consistent with the header/footer engines.
	/// </summary>
	private const float DefaultNaturalLineHeightTwips = 240f;

	/// <summary>
	/// The default table row height in twips, consistent with <see cref="TableLayoutEngine"/>.
	/// </summary>
	private const float DefaultTableRowHeightTwips = 240f;

	/// <summary>
	/// Measures all body document blocks and wraps them as <see cref="LayoutBlock"/> instances.
	/// Section break blocks are preserved with zero height so that <see cref="PageBuilder.PaginateDocument"/>
	/// can split the stream into per-section page groups.
	/// </summary>
	/// <param name="blocks">The parsed document blocks.</param>
	/// <param name="naturalLineHeight">The natural line height in twips. Uses <see cref="DefaultNaturalLineHeightTwips"/> when zero or negative.</param>
	/// <returns>The measured layout blocks.</returns>
	public static IReadOnlyList<LayoutBlock> MeasureBlocks(
		IReadOnlyList<DocumentBlock> blocks,
		float naturalLineHeight = 0f)
	{
		ArgumentNullException.ThrowIfNull(blocks);

		var effectiveLineHeight = naturalLineHeight > 0f
			? naturalLineHeight
			: DefaultNaturalLineHeightTwips;

		var layoutBlocks = new List<LayoutBlock>(blocks.Count);

		foreach (var block in blocks)
		{
			layoutBlocks.Add(MeasureBlock(block, effectiveLineHeight));
		}

		return layoutBlocks;
	}

	private static LayoutBlock MeasureBlock(DocumentBlock block, float naturalLineHeight)
		=> block switch
		{
			ParagraphBlock para => MeasureParagraph(para, naturalLineHeight),
			TablePlaceholderBlock table => MeasureTable(table, naturalLineHeight),
			SectionBreakBlock => new LayoutBlock(block, 0f),
			FootnoteSeparatorBlock => new LayoutBlock(block, naturalLineHeight),
			_ => new LayoutBlock(block, naturalLineHeight),
		};

	private static LayoutBlock MeasureParagraph(ParagraphBlock para, float naturalLineHeight)
	{
		var height = ParagraphSpacing.None.ComputeParagraphHeight(1, naturalLineHeight);
		return new LayoutBlock(
			para,
			height,
			ForcePageBreakBefore: para.PageBreakBefore);
	}

	private static LayoutBlock MeasureTable(TablePlaceholderBlock table, float naturalLineHeight)
	{
		// Estimate: count rows × default row height.
		var rowCount = 0;
		foreach (var child in table.TableElement.ChildElements)
		{
			if (child is DocumentFormat.OpenXml.Wordprocessing.TableRow)
			{
				rowCount++;
			}
		}

		var height = Math.Max(1, rowCount) * DefaultTableRowHeightTwips;
		_ = naturalLineHeight; // Used in future for per-cell measurement.
		return new LayoutBlock(table, height);
	}
}
