namespace PanoramicData.Render;

/// <summary>
/// Represents a chart object detected in a drawing element.
/// </summary>
internal sealed class ChartRunElement : ExtentRunElement
{
	/// <summary>
	/// Gets the relationship ID referencing the chart part.
	/// </summary>
	public required string RelationshipId { get; init; }

	/// <summary>
	/// Gets the relationship ID of a fallback image embedded in the chart part, or empty when none.
	/// </summary>
	public string FallbackImageRelationshipId { get; init; } = string.Empty;

	/// <summary>
	/// Gets a value indicating whether a fallback image is available.
	/// </summary>
	public bool HasFallbackImage => !string.IsNullOrEmpty(FallbackImageRelationshipId);
}
