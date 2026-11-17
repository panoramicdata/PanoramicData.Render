namespace PanoramicData.Render;

/// <summary>
/// Represents a DrawingML inline or anchored shape with custom geometry commands.
/// </summary>
internal sealed class DrawingCustomGeometryRunElement : ShapeRunElement
{
	/// <summary>
	/// Gets the parsed custom geometry commands in path order.
	/// </summary>
	public required IReadOnlyList<CustomGeometryCommand> Commands { get; init; }
}
