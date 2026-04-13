namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;

/// <summary>
/// Converts parsed <see cref="DocumentBlock"/> instances into measured <see cref="LayoutBlock"/>
/// instances suitable for pagination by <see cref="PageBuilder"/>.
/// </summary>
internal static class DocumentLayoutEngine
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
		=> MeasureBlocksCore(blocks, null, naturalLineHeight);

	/// <summary>
	/// Measures all body document blocks using section-aware content widths.
	/// </summary>
	/// <param name="blocks">The parsed document blocks.</param>
	/// <param name="bodySectionInfo">The final body section info used for the trailing section.</param>
	/// <param name="naturalLineHeight">The natural line height in twips. Uses <see cref="DefaultNaturalLineHeightTwips"/> when zero or negative.</param>
	/// <returns>The measured layout blocks.</returns>
	public static IReadOnlyList<LayoutBlock> MeasureBlocks(
		IReadOnlyList<DocumentBlock> blocks,
		SectionInfo bodySectionInfo,
		float naturalLineHeight = 0f)
		=> MeasureBlocksCore(blocks, bodySectionInfo, naturalLineHeight);

	private static IReadOnlyList<LayoutBlock> MeasureBlocksCore(
		IReadOnlyList<DocumentBlock> blocks,
		SectionInfo? bodySectionInfo,
		float naturalLineHeight)
	{
		ArgumentNullException.ThrowIfNull(blocks);

		var effectiveLineHeight = naturalLineHeight > 0f
			? naturalLineHeight
			: DefaultNaturalLineHeightTwips;

		if (bodySectionInfo is null)
		{
			var fallbackBlocks = new List<LayoutBlock>(blocks.Count);
			foreach (var block in blocks)
			{
				fallbackBlocks.Add(MeasureBlock(block, effectiveLineHeight, null));
			}

			return fallbackBlocks;
		}

		var layoutBlocks = new List<LayoutBlock>(blocks.Count);
		var pendingSectionBlocks = new List<DocumentBlock>();

		foreach (var block in blocks)
		{
			if (block is SectionBreakBlock sectionBreak)
			{
				MeasureSectionBlocks(layoutBlocks, pendingSectionBlocks, sectionBreak.SectionInfo, effectiveLineHeight);
				pendingSectionBlocks.Clear();
				layoutBlocks.Add(new LayoutBlock(sectionBreak, 0f));
				continue;
			}

			pendingSectionBlocks.Add(block);
		}

		MeasureSectionBlocks(layoutBlocks, pendingSectionBlocks, bodySectionInfo, effectiveLineHeight);

		return layoutBlocks;
	}

	private static void MeasureSectionBlocks(
		List<LayoutBlock> layoutBlocks,
		List<DocumentBlock> sectionBlocks,
		SectionInfo sectionInfo,
		float naturalLineHeight)
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

		foreach (var sectionBlock in sectionBlocks)
		{
			layoutBlocks.Add(MeasureBlock(sectionBlock, naturalLineHeight, availableWidthTwips));
		}
	}

	private static LayoutBlock MeasureBlock(DocumentBlock block, float naturalLineHeight, float? availableWidthTwips)
		=> block switch
		{
			ParagraphBlock para => MeasureParagraph(para, naturalLineHeight, availableWidthTwips),
			TablePlaceholderBlock table => MeasureTable(table, availableWidthTwips),
			SectionBreakBlock => new LayoutBlock(block, 0f),
			FootnoteSeparatorBlock => new LayoutBlock(block, naturalLineHeight),
			_ => new LayoutBlock(block, naturalLineHeight),
		};

	private static LayoutBlock MeasureParagraph(ParagraphBlock para, float naturalLineHeight, float? availableWidthTwips)
	{
		var spacing = ResolveParagraphSpacing(para);

		// Inflate the effective line height when inline images are taller than the text line.
		var effectiveLineHeight = Math.Max(naturalLineHeight, ComputeMaxInlineImageHeight(para));

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

	/// <summary>
	/// Scans a paragraph for inline images and returns the maximum image height in twips.
	/// Returns 0 when no inline images are present.
	/// </summary>
	private static float ComputeMaxInlineImageHeight(ParagraphBlock para)
	{
		var maxHeight = 0f;
		foreach (var drawing in para.SourceElement.Descendants<Drawing>())
		{
			var inline = drawing.GetFirstChild<DW.Inline>();
			if (inline?.Extent is { Cy: { } cy })
			{
				var heightTwips = TwipConverter.EmusToTwips(cy);
				if (heightTwips > maxHeight)
				{
					maxHeight = heightTwips;
				}
			}
		}

		return maxHeight;
	}

	/// <summary>
	/// Resolves the paragraph spacing from the materialized paragraph properties.
	/// </summary>
	private static ParagraphSpacing ResolveParagraphSpacing(ParagraphBlock para)
	{
		var spacingElement = para.SourceElement.ParagraphProperties?.SpacingBetweenLines;
		if (spacingElement is null)
		{
			return ParagraphSpacing.None;
		}

		var before = ParseTwips(spacingElement.Before?.Value);
		var after = ParseTwips(spacingElement.After?.Value);
		var line = ParseTwips(spacingElement.Line?.Value);
		var lineRule = spacingElement.LineRule?.Value switch
		{
			var v when v == LineSpacingRuleValues.Exact => LineSpacingRule.Exact,
			var v when v == LineSpacingRuleValues.AtLeast => LineSpacingRule.AtLeast,
			_ => (LineSpacingRule?)null // Auto is the default
		};

		return new ParagraphSpacing(before, after, line, lineRule);
	}

	/// <summary>
	/// Parses a twip string value, returning 0 if null or invalid.
	/// </summary>
	private static float ParseTwips(string? value)
		=> value is not null && float.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var result)
			? result
			: 0f;

	private static LayoutBlock MeasureTable(TablePlaceholderBlock table, float? availableWidthTwips)
	{
		// Parse the table and compute proper row heights for accurate pagination.
		var parsedTable = TableParser.Parse(table.TableElement);
		var rowHeights = TableLayoutEngine.ComputeRowHeights(parsedTable);
		var height = 0f;
		foreach (var rh in rowHeights)
		{
			height += rh;
		}

		return new LayoutBlock(table, height);
	}
}
