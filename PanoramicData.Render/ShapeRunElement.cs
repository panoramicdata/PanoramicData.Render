namespace PanoramicData.Render;

/// <summary>
/// Base class for DrawingML shapes (preset or custom geometry) within a run.
/// </summary>
internal abstract class ShapeRunElement : ExtentRunElement
{
	/// <summary>
	/// Gets the parsed fill information.
	/// </summary>
	public ShapeFillInfo Fill { get; init; } = ShapeFillInfo.None;

	/// <summary>
	/// Gets the parsed outline information.
	/// </summary>
	public ShapeOutlineInfo Outline { get; init; } = ShapeOutlineInfo.None;

	/// <summary>
	/// Gets the parsed text frame information.
	/// </summary>
	public ShapeTextFrameInfo TextFrame { get; init; } = ShapeTextFrameInfo.None;

	/// <summary>
	/// Gets the parsed shape transform.
	/// </summary>
	public ShapeTransformInfo Transform { get; init; } = ShapeTransformInfo.None;

	/// <summary>
	/// Gets the anchor placement metadata, or <see cref="AnchorPlacementInfo.None"/> for inline shapes.
	/// </summary>
	public AnchorPlacementInfo AnchorPlacement { get; init; } = AnchorPlacementInfo.None;
}