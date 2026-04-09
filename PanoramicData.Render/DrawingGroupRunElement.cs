namespace PanoramicData.Render;

/// <summary>
/// Represents a group of DrawingML shapes from a <c>wpg:wgp</c> element.
/// </summary>
internal sealed class DrawingGroupRunElement : RunElement
{
	/// <summary>
	/// Gets the group bounding-box width in EMUs.
	/// </summary>
	public long WidthEmu { get; init; }

	/// <summary>
	/// Gets the group bounding-box height in EMUs.
	/// </summary>
	public long HeightEmu { get; init; }

	/// <summary>
	/// Gets the group-level transform (rotation/flip from <c>wpg:grpSpPr/a:xfrm</c>).
	/// </summary>
	public ShapeTransformInfo GroupTransform { get; init; } = ShapeTransformInfo.None;

	/// <summary>
	/// Gets the child shapes in document order.
	/// </summary>
	public required IReadOnlyList<GroupedShapeItem> Children { get; init; }
}
