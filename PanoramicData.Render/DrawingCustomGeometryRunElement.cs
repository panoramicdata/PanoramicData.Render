namespace PanoramicData.Render;

/// <summary>
/// Represents a DrawingML inline or anchored shape with custom geometry commands.
/// </summary>
internal sealed class DrawingCustomGeometryRunElement : RunElement
{
	/// <summary>
	/// Gets the shape width in English Metric Units (EMU).
	/// </summary>
	public long WidthEmu { get; init; }

	/// <summary>
	/// Gets the shape height in English Metric Units (EMU).
	/// </summary>
	public long HeightEmu { get; init; }

	/// <summary>
	/// Gets the parsed custom geometry commands in path order.
	/// </summary>
	public required IReadOnlyList<CustomGeometryCommand> Commands { get; init; }

	/// <summary>
	/// Gets the parsed shape fill information.
	/// </summary>
	public ShapeFillInfo Fill { get; init; } = ShapeFillInfo.None;

	/// <summary>
	/// Gets the parsed shape outline information.
	/// </summary>
	public ShapeOutlineInfo Outline { get; init; } = ShapeOutlineInfo.None;

	/// <summary>
	/// Gets parsed shape text frame information.
	/// </summary>
	public ShapeTextFrameInfo TextFrame { get; init; } = ShapeTextFrameInfo.None;

	/// <summary>
	/// Gets parsed shape transform information.
	/// </summary>
	public ShapeTransformInfo Transform { get; init; } = ShapeTransformInfo.None;

	/// <summary>
	/// Gets parsed floating anchor placement information when this shape is anchored.
	/// </summary>
	public AnchorPlacementInfo AnchorPlacement { get; init; } = AnchorPlacementInfo.None;
}
