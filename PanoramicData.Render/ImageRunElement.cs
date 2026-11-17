namespace PanoramicData.Render;

/// <summary>
/// Base class for inline and anchored images within a run.
/// </summary>
internal abstract class ImageRunElement : RunElement
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

	/// <summary>
	/// Gets the left crop as a percentage in 1/1000ths of a percent.
	/// </summary>
	public int CropLeft { get; init; }

	/// <summary>
	/// Gets the top crop as a percentage in 1/1000ths of a percent.
	/// </summary>
	public int CropTop { get; init; }

	/// <summary>
	/// Gets the right crop as a percentage in 1/1000ths of a percent.
	/// </summary>
	public int CropRight { get; init; }

	/// <summary>
	/// Gets the bottom crop as a percentage in 1/1000ths of a percent.
	/// </summary>
	public int CropBottom { get; init; }
}