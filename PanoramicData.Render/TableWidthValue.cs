namespace PanoramicData.Render;

/// <summary>
/// Represents a width value with its type (auto, fixed, percentage, nil).
/// </summary>
/// <param name="Value">The numeric width value. In twips for <see cref="TableWidthUnit.Dxa"/>,
/// fiftieths of a percent for <see cref="TableWidthUnit.Pct"/>, zero otherwise.</param>
/// <param name="Type">How to interpret the <paramref name="Value"/>.</param>
internal readonly record struct TableWidthValue(float Value, TableWidthUnit Type)
{
	/// <summary>
	/// An auto-width value (no explicit width specified).
	/// </summary>
	public static readonly TableWidthValue Auto = new(0f, TableWidthUnit.Auto);
}
