namespace PanoramicData.Render;

/// <summary>
/// Represents a complete set of table border definitions.
/// </summary>
/// <param name="Top">Top border.</param>
/// <param name="Bottom">Bottom border.</param>
/// <param name="Left">Left border.</param>
/// <param name="Right">Right border.</param>
/// <param name="InsideHorizontal">Inside horizontal border.</param>
/// <param name="InsideVertical">Inside vertical border.</param>
internal readonly record struct TableBorderSet(
	TableBorderDefinition? Top = null,
	TableBorderDefinition? Bottom = null,
	TableBorderDefinition? Left = null,
	TableBorderDefinition? Right = null,
	TableBorderDefinition? InsideHorizontal = null,
	TableBorderDefinition? InsideVertical = null)
{
	/// <summary>
	/// A border set with no borders defined.
	/// </summary>
	public static readonly TableBorderSet None = new();

	/// <summary>
	/// Gets whether any border in the set is visible.
	/// </summary>
	public bool HasAnyVisibleBorder =>
		(Top?.IsVisible ?? false)
		|| (Bottom?.IsVisible ?? false)
		|| (Left?.IsVisible ?? false)
		|| (Right?.IsVisible ?? false)
		|| (InsideHorizontal?.IsVisible ?? false)
		|| (InsideVertical?.IsVisible ?? false);
}
