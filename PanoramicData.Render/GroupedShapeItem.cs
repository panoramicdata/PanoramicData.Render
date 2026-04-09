namespace PanoramicData.Render;

/// <summary>
/// Represents a single child shape within a DrawingML grouped shape.
/// </summary>
internal sealed record GroupedShapeItem
{
	/// <summary>
	/// Gets the horizontal offset within the group coordinate space, in EMUs.
	/// </summary>
	public long OffsetXEmu { get; init; }

	/// <summary>
	/// Gets the vertical offset within the group coordinate space, in EMUs.
	/// </summary>
	public long OffsetYEmu { get; init; }

	/// <summary>
	/// Gets the width in EMUs.
	/// </summary>
	public long WidthEmu { get; init; }

	/// <summary>
	/// Gets the height in EMUs.
	/// </summary>
	public long HeightEmu { get; init; }

	/// <summary>
	/// Gets the shape run element for this item.
	/// It may be a <see cref="DrawingShapeRunElement"/>, <see cref="DrawingCustomGeometryRunElement"/>,
	/// or a nested <see cref="DrawingGroupRunElement"/>.
	/// </summary>
	public required RunElement Shape { get; init; }
}
