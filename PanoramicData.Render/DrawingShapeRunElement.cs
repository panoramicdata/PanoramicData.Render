namespace PanoramicData.Render;

/// <summary>
/// Represents a DrawingML inline or anchored shape with a preset geometry.
/// </summary>
internal sealed class DrawingShapeRunElement : RunElement
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
	/// Gets the recognised preset geometry kind, or <see cref="PresetShapeKind.Unknown"/>
	/// for shapes not in the known set.
	/// </summary>
	public PresetShapeKind PresetKind { get; init; }

	/// <summary>
	/// Gets the raw OOXML preset name string (e.g. <c>"rect"</c>, <c>"ellipse"</c>).
	/// Preserved for diagnostic purposes and for future geometry resolution.
	/// </summary>
	public string RawPresetName { get; init; } = string.Empty;

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
}
