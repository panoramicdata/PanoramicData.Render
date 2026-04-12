namespace PanoramicData.Render;

/// <summary>
/// Diagnostic information about field updates applied during rendering.
/// </summary>
public sealed record FieldUpdateResult
{
	/// <summary>
	/// Gets or sets the number of layout/update iterations required.
	/// </summary>
	public int IterationsRequired { get; init; }

	/// <summary>
	/// Gets or sets the set of field types that were updated.
	/// </summary>
	public IReadOnlyList<string> UpdatedFields { get; init; } = Array.Empty<string>();
}