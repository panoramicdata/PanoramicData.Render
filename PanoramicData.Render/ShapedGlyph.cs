namespace PanoramicData.Render;

/// <summary>
/// Represents a single shaped glyph with its position and cluster information.
/// </summary>
/// <param name="Codepoint">The glyph codepoint (glyph ID) from the shaper.</param>
/// <param name="AdvanceWidth">The advance width of this glyph in font units.</param>
/// <param name="OffsetX">The X offset for rendering this glyph.</param>
/// <param name="OffsetY">The Y offset for rendering this glyph.</param>
/// <param name="Cluster">The cluster index mapping this glyph back to the source text.</param>
internal readonly record struct ShapedGlyph(
	uint Codepoint,
	float AdvanceWidth,
	float OffsetX,
	float OffsetY,
	uint Cluster);
