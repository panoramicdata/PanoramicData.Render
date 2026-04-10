namespace PanoramicData.Render;

/// <summary>
/// Represents a single section column definition parsed from <c>w:cols</c>.
/// All dimensional values are in twips.
/// </summary>
/// <param name="WidthTwips">The explicit width of the column in twips, or 0 when unspecified.</param>
/// <param name="SpaceAfterTwips">The spacing after the column in twips, or 0 when unspecified.</param>
internal readonly record struct SectionColumnDefinition(int WidthTwips, int SpaceAfterTwips);