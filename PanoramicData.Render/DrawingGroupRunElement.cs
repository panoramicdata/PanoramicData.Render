namespace PanoramicData.Render;

/// <summary>
/// Represents a group of DrawingML shapes from a <c>wpg:wgp</c> element.
/// </summary>
internal sealed class DrawingGroupRunElement : ExtentRunElement
{
	/// <summary>
	/// Gets the group-level transform (rotation/flip from <c>wpg:grpSpPr/a:xfrm</c>).
	/// </summary>
	public ShapeTransformInfo GroupTransform { get; init; } = ShapeTransformInfo.None;

	/// <summary>
	/// Gets the child shapes in document order.
	/// </summary>
	public required IReadOnlyList<GroupedShapeItem> Children { get; init; }
}
