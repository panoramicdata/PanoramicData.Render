namespace PanoramicData.Render;

/// <summary>
/// Represents a floating (anchored) image within a run.
/// </summary>
internal sealed class AnchorImageRunElement : RunElement
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
	/// Gets the source crop from the left edge, in 1/1000 percent units (OOXML <c>ST_Percentage</c>), or 0 when not specified.
	/// </summary>
	public int CropLeft { get; init; }

	/// <summary>
	/// Gets the source crop from the top edge, in 1/1000 percent units (OOXML <c>ST_Percentage</c>), or 0 when not specified.
	/// </summary>
	public int CropTop { get; init; }

	/// <summary>
	/// Gets the source crop from the right edge, in 1/1000 percent units (OOXML <c>ST_Percentage</c>), or 0 when not specified.
	/// </summary>
	public int CropRight { get; init; }

	/// <summary>
	/// Gets the source crop from the bottom edge, in 1/1000 percent units (OOXML <c>ST_Percentage</c>), or 0 when not specified.
	/// </summary>
	public int CropBottom { get; init; }

	/// <summary>
	/// Gets the horizontal position reference frame.
	/// </summary>
	public AnchorRelativeFrom HorizontalRelativeFrom { get; init; }

	/// <summary>
	/// Gets the vertical position reference frame.
	/// </summary>
	public AnchorRelativeFrom VerticalRelativeFrom { get; init; }

	/// <summary>
	/// Gets the horizontal offset in EMUs when specified by <c>wp:posOffset</c>.
	/// </summary>
	public long HorizontalOffsetEmu { get; init; }

	/// <summary>
	/// Gets the vertical offset in EMUs when specified by <c>wp:posOffset</c>.
	/// </summary>
	public long VerticalOffsetEmu { get; init; }

	/// <summary>
	/// Gets the horizontal alignment keyword when provided.
	/// </summary>
	public AnchorAlignment HorizontalAlignment { get; init; }

	/// <summary>
	/// Gets the vertical alignment keyword when provided.
	/// </summary>
	public AnchorAlignment VerticalAlignment { get; init; }
}
