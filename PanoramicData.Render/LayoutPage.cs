namespace PanoramicData.Render;

/// <summary>
/// Represents a single paginated page containing laid-out blocks.
/// </summary>
internal sealed class LayoutPage
{
	/// <summary>
	/// Gets the section properties that apply to this page.
	/// </summary>
	public required SectionInfo Section { get; init; }

	/// <summary>
	/// Gets the 1-based page number.
	/// </summary>
	public required int PageNumber { get; init; }

	/// <summary>
	/// Gets the blocks assigned to this page.
	/// </summary>
	public required IReadOnlyList<LayoutBlock> Blocks { get; init; }

	/// <summary>
	/// Gets the positioned block placements for this page.
	/// When empty, blocks are rendered as a single top-to-bottom stream.
	/// </summary>
	public IReadOnlyList<LayoutBlockPlacement> BlockPlacements { get; init; } = [];

	/// <summary>
	/// Gets the header layout blocks for this page, or <see langword="null"/> when no header applies.
	/// </summary>
	public IReadOnlyList<LayoutBlock>? HeaderBlocks { get; init; }

	/// <summary>
	/// Gets the footer layout blocks for this page, or <see langword="null"/> when no footer applies.
	/// </summary>
	public IReadOnlyList<LayoutBlock>? FooterBlocks { get; init; }

	/// <summary>
	/// Gets the Y position (in twips from page top) where the header content starts.
	/// Equal to <see cref="SectionInfo.MarginHeader"/>.
	/// </summary>
	public float HeaderTopTwips { get; init; }

	/// <summary>
	/// Gets the Y position (in twips from page top) where the body content starts.
	/// Accounts for header overflow using the Word margin model.
	/// </summary>
	public float ContentTopTwips { get; init; }

	/// <summary>
	/// Gets the Y position (in twips from page top) where the footer content starts.
	/// </summary>
	public float FooterTopTwips { get; init; }

	/// <summary>
	/// Gets the footnote layout blocks for this page, or <see langword="null"/> when no footnotes appear.
	/// Footnotes are positioned above the footer, at the bottom of the content area.
	/// </summary>
	public IReadOnlyList<LayoutBlock>? FootnoteBlocks { get; init; }

	/// <summary>
	/// Gets the Y position (in twips from page top) where the footnote area starts.
	/// This is below the body content and above the footer.
	/// </summary>
	public float FootnoteTopTwips { get; init; }
}

/// <summary>
/// Associates a layout block with its positioned content box on a page.
/// </summary>
/// <param name="Block">The block being placed.</param>
/// <param name="XTwips">The X origin of the block's content region in twips.</param>
/// <param name="YTwips">The Y origin of the block in twips.</param>
/// <param name="ContentWidthTwips">The available content width for the block in twips.</param>
/// <param name="ColumnIndex">The zero-based page column index containing the block.</param>
internal readonly record struct LayoutBlockPlacement(
	LayoutBlock Block,
	float XTwips,
	float YTwips,
	float ContentWidthTwips,
	int ColumnIndex);
