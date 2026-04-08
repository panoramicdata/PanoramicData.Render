namespace PanoramicData.Render;

/// <summary>
/// Represents a parsed table grid column with its width in twips.
/// Corresponds to a <c>w:gridCol</c> element in <c>w:tblGrid</c>.
/// </summary>
/// <param name="WidthTwips">The column width in twips, or zero if not specified.</param>
internal readonly record struct TableGridColumn(float WidthTwips);
