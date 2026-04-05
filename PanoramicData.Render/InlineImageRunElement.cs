namespace PanoramicData.Render;

/// <summary>
/// Represents an inline image within a run.
/// </summary>
internal sealed class InlineImageRunElement : RunElement
{
	/// <summary>
	/// Gets the relationship ID referencing the image part, or empty if no embedded image.
	/// </summary>
	public required string RelationshipId { get; init; }

	/// <summary>
	/// Gets the image width in English Metric Units (EMU). 1 inch = 914400 EMU.
	/// </summary>
	public required long WidthEmu { get; init; }

	/// <summary>
	/// Gets the image height in English Metric Units (EMU). 1 inch = 914400 EMU.
	/// </summary>
	public required long HeightEmu { get; init; }
}
