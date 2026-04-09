namespace PanoramicData.Render;

/// <summary>
/// Represents a parsed DrawingML custom geometry command.
/// </summary>
/// <param name="Kind">The command kind.</param>
/// <param name="Points">Control points associated with the command, in EMU coordinates.</param>
/// <param name="ArcWidthRadius">Arc width radius (EMU) for <see cref="CustomGeometryCommandKind.ArcTo"/>.</param>
/// <param name="ArcHeightRadius">Arc height radius (EMU) for <see cref="CustomGeometryCommandKind.ArcTo"/>.</param>
/// <param name="ArcStartAngle">Arc start angle for <see cref="CustomGeometryCommandKind.ArcTo"/>.</param>
/// <param name="ArcSweepAngle">Arc sweep angle for <see cref="CustomGeometryCommandKind.ArcTo"/>.</param>
internal readonly record struct CustomGeometryCommand(
	CustomGeometryCommandKind Kind,
	IReadOnlyList<CustomGeometryPoint> Points,
	long ArcWidthRadius = 0,
	long ArcHeightRadius = 0,
	int ArcStartAngle = 0,
	int ArcSweepAngle = 0);

/// <summary>
/// Represents a custom geometry point in EMUs.
/// </summary>
/// <param name="XEmu">X coordinate in EMUs.</param>
/// <param name="YEmu">Y coordinate in EMUs.</param>
internal readonly record struct CustomGeometryPoint(long XEmu, long YEmu);
