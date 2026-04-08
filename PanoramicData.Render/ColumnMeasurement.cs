namespace PanoramicData.Render;

/// <summary>
/// Stores measured width information for a single table column, used in auto-fit layout.
/// </summary>
/// <param name="PreferredWidthTwips">The natural (preferred) width of the column content in twips.</param>
/// <param name="MinimumWidthTwips">The minimum width the column can accept without breaking content.</param>
internal readonly record struct ColumnMeasurement(float PreferredWidthTwips, float MinimumWidthTwips);
