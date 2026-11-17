namespace PanoramicData.Render;

/// <summary>
/// Represents a floating (anchored) image within a run.
/// </summary>
internal sealed class AnchorImageRunElement : ImageRunElement
{
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

	/// <summary>
	/// Gets a value indicating whether the anchor is behind document text (<c>wp:anchor/@behindDoc</c>).
	/// </summary>
	public bool BehindDocument { get; init; }
}
