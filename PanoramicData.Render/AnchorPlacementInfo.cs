namespace PanoramicData.Render;

/// <summary>
/// Captures floating anchor positioning and wrapping metadata for drawings.
/// </summary>
internal sealed record AnchorPlacementInfo
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
	/// Gets a value indicating whether the anchor is behind document text.
	/// </summary>
	public bool BehindDocument { get; init; }

	/// <summary>
	/// Gets the wrapping style requested by the anchor.
	/// </summary>
	public AnchorWrapStyle WrapStyle { get; init; }

	/// <summary>
	/// Gets the top wrap distance in EMUs.
	/// </summary>
	public long DistanceTopEmu { get; init; }

	/// <summary>
	/// Gets the bottom wrap distance in EMUs.
	/// </summary>
	public long DistanceBottomEmu { get; init; }

	/// <summary>
	/// Gets the left wrap distance in EMUs.
	/// </summary>
	public long DistanceLeftEmu { get; init; }

	/// <summary>
	/// Gets the right wrap distance in EMUs.
	/// </summary>
	public long DistanceRightEmu { get; init; }

	/// <summary>
	/// Gets an empty placement descriptor for inline drawings.
	/// </summary>
	public static AnchorPlacementInfo None { get; } = new();
}

/// <summary>
/// Identifies the wrapping behavior requested by an anchored drawing.
/// </summary>
internal enum AnchorWrapStyle
{
	None,
	Square,
	Tight,
	TopAndBottom
}