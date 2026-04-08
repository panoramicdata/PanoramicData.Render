namespace PanoramicData.Render;

/// <summary>
/// Groups a contiguous sequence of layout blocks belonging to the same document section.
/// </summary>
/// <param name="Info">The section properties (page dimensions, margins, etc.).</param>
/// <param name="Blocks">The layout blocks in this section (excluding the SectionBreakBlock itself).</param>
/// <param name="BreakType">How this section's content should begin relative to the previous section.</param>
internal readonly record struct DocumentSection(
	SectionInfo Info,
	IReadOnlyList<LayoutBlock> Blocks,
	SectionBreakType BreakType);
