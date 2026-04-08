namespace PanoramicData.Render;

/// <summary>
/// Represents the margins (padding) inside a table cell in twips.
/// </summary>
/// <param name="Top">Top margin in twips.</param>
/// <param name="Right">Right margin in twips.</param>
/// <param name="Bottom">Bottom margin in twips.</param>
/// <param name="Left">Left margin in twips.</param>
internal readonly record struct CellMargins(float Top, float Right, float Bottom, float Left)
{
	/// <summary>
	/// Default cell margins: no padding.
	/// </summary>
	public static readonly CellMargins None = new(0f, 0f, 0f, 0f);
}
