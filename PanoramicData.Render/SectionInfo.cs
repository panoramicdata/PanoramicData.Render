namespace PanoramicData.Render;

/// <summary>
/// Parsed section properties representing page layout for a document section.
/// All dimensional values are in twips (1/1440 inch).
/// </summary>
internal sealed class SectionInfo
{
	/// <summary>
	/// Gets the page width in twips. Default: 12240 (8.5 inches, US Letter).
	/// </summary>
	public int PageWidth { get; init; } = 12240;

	/// <summary>
	/// Gets the page height in twips. Default: 15840 (11 inches, US Letter).
	/// </summary>
	public int PageHeight { get; init; } = 15840;

	/// <summary>
	/// Gets the page orientation.
	/// </summary>
	public PageOrientation Orientation { get; init; } = PageOrientation.Portrait;

	/// <summary>
	/// Gets the top margin in twips. Default: 1440 (1 inch).
	/// </summary>
	public int MarginTop { get; init; } = 1440;

	/// <summary>
	/// Gets the right margin in twips. Default: 1440 (1 inch).
	/// </summary>
	public int MarginRight { get; init; } = 1440;

	/// <summary>
	/// Gets the bottom margin in twips. Default: 1440 (1 inch).
	/// </summary>
	public int MarginBottom { get; init; } = 1440;

	/// <summary>
	/// Gets the left margin in twips. Default: 1440 (1 inch).
	/// </summary>
	public int MarginLeft { get; init; } = 1440;

	/// <summary>
	/// Gets the header margin in twips. Default: 720 (0.5 inch).
	/// </summary>
	public int MarginHeader { get; init; } = 720;

	/// <summary>
	/// Gets the footer margin in twips. Default: 720 (0.5 inch).
	/// </summary>
	public int MarginFooter { get; init; } = 720;

	/// <summary>
	/// Gets the gutter margin in twips. Default: 0.
	/// </summary>
	public int MarginGutter { get; init; }

	/// <summary>
	/// Gets the section break type. Default: <see cref="SectionBreakType.NextPage"/>.
	/// </summary>
	public SectionBreakType BreakType { get; init; } = SectionBreakType.NextPage;

	/// <summary>
	/// Gets the number of text columns in this section. Default: 1.
	/// Column layout rendering is deferred to Phase 7; this value tracks the section metadata.
	/// </summary>
	public int ColumnCount { get; init; } = 1;

	/// <summary>
	/// Gets the line numbering properties for this section, or <see langword="null"/> if line numbering is not enabled.
	/// </summary>
	public LineNumberingInfo? LineNumbering { get; init; }

	/// <summary>
	/// Gets the header references for this section.
	/// </summary>
	public IReadOnlyList<HeaderFooterReference> HeaderReferences { get; init; } = [];

	/// <summary>
	/// Gets the footer references for this section.
	/// </summary>
	public IReadOnlyList<HeaderFooterReference> FooterReferences { get; init; } = [];
}
