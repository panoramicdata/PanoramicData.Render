namespace PanoramicData.Render;

/// <summary>
/// Associates a <see cref="DocumentBlock"/> with its computed height for pagination.
/// When <see cref="LineHeights"/> is provided, the block can be split at line boundaries.
/// </summary>
/// <param name="Block">The document block.</param>
/// <param name="HeightTwips">The total computed height of the block in twips.</param>
/// <param name="SpaceBefore">Paragraph spacing before in twips (included in <paramref name="HeightTwips"/>).</param>
/// <param name="SpaceAfter">Paragraph spacing after in twips (included in <paramref name="HeightTwips"/>).</param>
/// <param name="LineHeights">Per-line heights in twips, excluding <paramref name="SpaceBefore"/> and <paramref name="SpaceAfter"/>. When present, enables line-level splitting.</param>
/// <param name="ForcePageBreakBefore">When <see langword="true"/>, forces a page break before this block (e.g. from <c>w:br w:type="page"</c>).</param>
/// <param name="WidowOrphanControl">When <see langword="true"/> (default), widow/orphan rules are enforced during splitting.</param>
internal readonly record struct LayoutBlock(
	DocumentBlock Block,
	float HeightTwips,
	float SpaceBefore = 0f,
	float SpaceAfter = 0f,
	IReadOnlyList<float>? LineHeights = null,
	bool ForcePageBreakBefore = false,
	bool WidowOrphanControl = true);
