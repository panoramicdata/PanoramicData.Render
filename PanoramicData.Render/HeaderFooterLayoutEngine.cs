namespace PanoramicData.Render;

/// <summary>
/// Lays out header and footer content blocks into <see cref="LayoutBlock"/> sequences
/// and computes total content height.
/// </summary>
/// <remarks>
/// Uses paragraph spacing properties from each block where available.
/// Line count is estimated as 1 per paragraph (accurate line breaking requires
/// font metrics and available width which are connected in the rendering phase).
/// </remarks>
internal static class HeaderFooterLayoutEngine
{
	/// <summary>
	/// The default natural line height in twips used when font metrics are not yet available.
	/// Corresponds to approximately 12pt single-spaced text (12pt × 20 twips/pt = 240 twips).
	/// </summary>
	internal const float DefaultNaturalLineHeightTwips = 240f;

	/// <summary>
	/// Computes <see cref="LayoutBlock"/> instances for the blocks within a header or footer,
	/// along with the total content height.
	/// </summary>
	/// <param name="content">The header or footer content to lay out.</param>
	/// <param name="naturalLineHeight">The natural line height in twips (from font metrics). Uses <see cref="DefaultNaturalLineHeightTwips"/> when zero or negative.</param>
	/// <returns>A tuple of the laid-out blocks and the total height in twips.</returns>
	public static (IReadOnlyList<LayoutBlock> Blocks, float TotalHeightTwips) Layout(
		HeaderFooterContent content,
		float naturalLineHeight = 0f)
	{
		ArgumentNullException.ThrowIfNull(content);

		if (content.Blocks.Count == 0)
		{
			return ([], 0f);
		}

		var effectiveLineHeight = naturalLineHeight > 0f
			? naturalLineHeight
			: DefaultNaturalLineHeightTwips;

		var layoutBlocks = new List<LayoutBlock>();
		var totalHeight = 0f;

		foreach (var block in content.Blocks)
		{
			var height = EstimateBlockHeight(block, effectiveLineHeight);
			layoutBlocks.Add(new LayoutBlock(block, height));
			totalHeight += height;
		}

		return (layoutBlocks, totalHeight);
	}

	private static float EstimateBlockHeight(DocumentBlock block, float naturalLineHeight) => block switch
	{
		ParagraphBlock para => EstimateParagraphHeight(para, naturalLineHeight),
		_ => naturalLineHeight, // Default estimate for other block types
	};

	private static float EstimateParagraphHeight(ParagraphBlock para, float naturalLineHeight)
	{
		// Estimate 1 line per paragraph (accurate count requires line breaking with font metrics).
		// ParagraphBlock.Spacing is not yet connected; use no extra spacing.
		_ = para; // Used for future paragraph-specific spacing.
		return ParagraphSpacing.None.ComputeParagraphHeight(1, naturalLineHeight);
	}
}
