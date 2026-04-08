namespace PanoramicData.Render;

/// <summary>
/// Lays out footnote content blocks into <see cref="LayoutBlock"/> sequences
/// and computes total content height, for placement at the bottom of the page
/// above the footer.
/// </summary>
internal static class FootnoteLayoutEngine
{
	/// <summary>
	/// The default natural line height in twips used when font metrics are not yet available.
	/// Corresponds to approximately 10pt single-spaced text (10pt × 20 twips/pt = 200 twips),
	/// which is the typical footnote font size.
	/// </summary>
	internal const float DefaultFootnoteLineHeightTwips = 200f;

	/// <summary>
	/// The default height of the footnote separator line in twips.
	/// In Word, this is approximately 12pt (240 twips) including spacing above and below.
	/// </summary>
	internal const float DefaultSeparatorHeightTwips = 240f;

	/// <summary>
	/// Computes <see cref="LayoutBlock"/> instances for a set of footnotes to be placed
	/// on a single page, along with the total height including separator.
	/// </summary>
	/// <param name="footnotes">The footnote definitions to lay out on this page.</param>
	/// <param name="naturalLineHeight">The natural line height in twips (from font metrics). Uses <see cref="DefaultFootnoteLineHeightTwips"/> when zero or negative.</param>
	/// <param name="includeSeparator">Whether to include the separator line height. Default: true.</param>
	/// <returns>A tuple of the laid-out blocks and the total height in twips (including separator if applicable).</returns>
	public static (IReadOnlyList<LayoutBlock> Blocks, float TotalHeightTwips) Layout(
		IReadOnlyList<NoteDefinition> footnotes,
		float naturalLineHeight = 0f,
		bool includeSeparator = true)
	{
		ArgumentNullException.ThrowIfNull(footnotes);

		if (footnotes.Count == 0)
		{
			return ([], 0f);
		}

		var effectiveLineHeight = naturalLineHeight > 0f
			? naturalLineHeight
			: DefaultFootnoteLineHeightTwips;

		var layoutBlocks = new List<LayoutBlock>();
		var totalHeight = 0f;

		if (includeSeparator)
		{
			var separatorBlock = new FootnoteSeparatorBlock();
			layoutBlocks.Add(new LayoutBlock(separatorBlock, DefaultSeparatorHeightTwips));
			totalHeight += DefaultSeparatorHeightTwips;
		}

		foreach (var footnote in footnotes)
		{
			foreach (var block in footnote.Blocks)
			{
				var height = EstimateBlockHeight(block, effectiveLineHeight);
				layoutBlocks.Add(new LayoutBlock(block, height));
				totalHeight += height;
			}
		}

		return (layoutBlocks, totalHeight);
	}

	private static float EstimateBlockHeight(DocumentBlock block, float naturalLineHeight) => block switch
	{
		ParagraphBlock => ParagraphSpacing.None.ComputeParagraphHeight(1, naturalLineHeight),
		_ => naturalLineHeight,
	};
}
