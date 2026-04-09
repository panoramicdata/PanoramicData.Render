namespace PanoramicData.Render;

/// <summary>
/// Represents a SmartArt diagram object detected in a drawing element.
/// </summary>
internal sealed class SmartArtRunElement : RunElement
{
	/// <summary>
	/// Gets the relationship ID referencing the diagram data part.
	/// </summary>
	public required string RelationshipId { get; init; }

	/// <summary>
	/// Gets the width in English Metric Units (EMU).
	/// </summary>
	public long WidthEmu { get; init; }

	/// <summary>
	/// Gets the height in English Metric Units (EMU).
	/// </summary>
	public long HeightEmu { get; init; }

	/// <summary>
	/// Gets a value indicating whether a DrawingML fallback is available.
	/// </summary>
	public bool HasFallback { get; init; }
}
