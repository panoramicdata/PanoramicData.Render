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
