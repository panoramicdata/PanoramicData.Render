namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;

/// <summary>
/// Converts parsed <see cref="DocumentBlock"/> instances into measured <see cref="LayoutBlock"/>
/// instances suitable for pagination by <see cref="PageBuilder"/>.
/// </summary>
internal static partial class DocumentLayoutEngine
{
	/// <summary>
	/// The default natural line height in twips, consistent with the header/footer engines.
	/// </summary>
	private const float DefaultNaturalLineHeightTwips = 360f;

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
		=> MeasureBlocksCore(blocks, null, naturalLineHeight, null);

	/// <summary>
	/// Measures all body document blocks using section-aware content widths.
	/// </summary>
	/// <param name="blocks">The parsed document blocks.</param>
	/// <param name="bodySectionInfo">The final body section info used for the trailing section.</param>
	/// <param name="styles">Optional styles document used to resolve table style defaults such as cell margins.</param>
	/// <param name="naturalLineHeight">The natural line height in twips. Uses <see cref="DefaultNaturalLineHeightTwips"/> when zero or negative.</param>
	/// <returns>The measured layout blocks.</returns>
	public static IReadOnlyList<LayoutBlock> MeasureBlocks(
		IReadOnlyList<DocumentBlock> blocks,
		SectionInfo bodySectionInfo,
		Styles? styles = null,
		float naturalLineHeight = 0f)
		=> MeasureBlocksCore(blocks, bodySectionInfo, naturalLineHeight, styles);

	private static IReadOnlyList<LayoutBlock> MeasureBlocksCore(
		IReadOnlyList<DocumentBlock> blocks,
		SectionInfo? bodySectionInfo,
		float naturalLineHeight,
		Styles? styles)
	{
		ArgumentNullException.ThrowIfNull(blocks);

		var effectiveLineHeight = naturalLineHeight > 0f
			? naturalLineHeight
			: DefaultNaturalLineHeightTwips;

		var docDefaultSpacing = ExtractDocDefaultSpacing(styles);

		if (bodySectionInfo is null)
		{
			var fallbackBlocks = new List<LayoutBlock>(blocks.Count);
			var previousParagraphAfter = 0f;
			foreach (var block in blocks)
			{
				var measuredBlock = MeasureBlock(block, effectiveLineHeight, null, styles, docDefaultSpacing);
				if (block is ParagraphBlock)
				{
					measuredBlock = CollapseParagraphSpacing(measuredBlock, previousParagraphAfter);
					previousParagraphAfter = measuredBlock.SpaceAfter;
				}
				else
				{
					previousParagraphAfter = 0f;
				}

				fallbackBlocks.Add(measuredBlock);
			}

			return fallbackBlocks;
		}

		var layoutBlocks = new List<LayoutBlock>(blocks.Count);
		var pendingSectionBlocks = new List<DocumentBlock>();

		foreach (var block in blocks)
		{
			if (block is SectionBreakBlock sectionBreak)
			{
				MeasureSectionBlocks(layoutBlocks, pendingSectionBlocks, sectionBreak.SectionInfo, effectiveLineHeight, styles, docDefaultSpacing);
				pendingSectionBlocks.Clear();
				layoutBlocks.Add(new LayoutBlock(sectionBreak, 0f));
				continue;
			}

			pendingSectionBlocks.Add(block);
		}

		MeasureSectionBlocks(layoutBlocks, pendingSectionBlocks, bodySectionInfo, effectiveLineHeight, styles, docDefaultSpacing);

		return layoutBlocks;
	}

	private static void MeasureSectionBlocks(
		List<LayoutBlock> layoutBlocks,
		List<DocumentBlock> sectionBlocks,
		SectionInfo sectionInfo,
		float naturalLineHeight,
		Styles? styles,
		ParagraphSpacing docDefaultSpacing = default)
	{
		ArgumentNullException.ThrowIfNull(layoutBlocks);
		ArgumentNullException.ThrowIfNull(sectionBlocks);
		ArgumentNullException.ThrowIfNull(sectionInfo);

		if (sectionBlocks.Count == 0)
		{
			return;
		}

		var columnRegions = PageBuilder.ComputeColumnRegions(sectionInfo);
		var availableWidthTwips = columnRegions.Count > 0
			? columnRegions[0].WidthTwips
			: MathF.Max(0f, sectionInfo.PageWidth - sectionInfo.MarginLeft - sectionInfo.MarginRight);

		var previousParagraphAfter = 0f;

		foreach (var sectionBlock in sectionBlocks)
		{
			var measuredBlock = MeasureBlock(sectionBlock, naturalLineHeight, availableWidthTwips, styles, docDefaultSpacing);
			if (sectionBlock is ParagraphBlock)
			{
				measuredBlock = CollapseParagraphSpacing(measuredBlock, previousParagraphAfter);
				previousParagraphAfter = measuredBlock.SpaceAfter;
			}
			else
			{
				previousParagraphAfter = 0f;
			}

			layoutBlocks.Add(measuredBlock);
		}
	}

	private static LayoutBlock CollapseParagraphSpacing(LayoutBlock paragraphBlock, float previousParagraphAfter)
	{
		if (previousParagraphAfter <= 0f || paragraphBlock.SpaceBefore <= 0f)
		{
			return paragraphBlock;
		}

		var collapsedBefore = MathF.Max(0f, paragraphBlock.SpaceBefore - previousParagraphAfter);
		if (collapsedBefore >= paragraphBlock.SpaceBefore)
		{
			return paragraphBlock;
		}

		var collapsedHeight = MathF.Max(0f, paragraphBlock.HeightTwips - (paragraphBlock.SpaceBefore - collapsedBefore));
		return paragraphBlock with
		{
			HeightTwips = collapsedHeight,
			SpaceBefore = collapsedBefore
		};
	}

	private static LayoutBlock MeasureBlock(DocumentBlock block, float naturalLineHeight, float? availableWidthTwips, Styles? styles, ParagraphSpacing docDefaultSpacing = default)
		=> block switch
		{
			ParagraphBlock para => MeasureParagraph(para, naturalLineHeight, availableWidthTwips, docDefaultSpacing),
			TablePlaceholderBlock table => MeasureTable(table, availableWidthTwips, styles),
			SectionBreakBlock => new LayoutBlock(block, 0f),
			FootnoteSeparatorBlock => new LayoutBlock(block, naturalLineHeight),
			_ => new LayoutBlock(block, naturalLineHeight),
		};

	private static LayoutBlock MeasureParagraph(ParagraphBlock para, float naturalLineHeight, float? availableWidthTwips, ParagraphSpacing docDefaultSpacing = default)
	{
		var spacing = ResolveParagraphSpacing(para, docDefaultSpacing);

		// Derive the natural line height from the paragraph's font size so that body text
		// (e.g. 11 pt) is measured correctly rather than using the heading-sized fallback.
		// Fall back to the caller-supplied naturalLineHeight when no font size can be resolved.
		var fontBasedLineHeight = GetNaturalLineHeightFromParagraph(para, naturalLineHeight);

		// Inflate the effective line height when inline images are taller than the text line.
		var effectiveLineHeight = Math.Max(fontBasedLineHeight, ComputeMaxInlineImageHeight(para));

		if (availableWidthTwips is > 0f)
		{
			var lineCount = RenderCommandEmitter.EstimateWrappedLineCount(para, availableWidthTwips.Value);
			if (lineCount > 1)
			{
				var lineHeights = Enumerable.Repeat(spacing.ComputeLineHeight(effectiveLineHeight), lineCount).ToArray();
				var wrappedHeight = spacing.ComputeParagraphHeight(lineCount, effectiveLineHeight);
				return new LayoutBlock(
					para,
					wrappedHeight,
					SpaceBefore: spacing.SpaceBefore,
					SpaceAfter: spacing.SpaceAfter,
					LineHeights: lineHeights,
					ForcePageBreakBefore: para.PageBreakBefore);
			}
		}

		var height = spacing.ComputeParagraphHeight(1, effectiveLineHeight);
		return new LayoutBlock(
			para,
			height,
			SpaceBefore: spacing.SpaceBefore,
			SpaceAfter: spacing.SpaceAfter,
			ForcePageBreakBefore: para.PageBreakBefore);
	}
}
