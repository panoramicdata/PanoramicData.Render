namespace PanoramicData.Render;

/// <summary>
/// Represents a DrawingML inline or anchored shape with a preset geometry.
/// </summary>
internal sealed class DrawingShapeRunElement : ShapeRunElement
{
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
}
