namespace PanoramicData.Render;

/// <summary>
/// Identifies a parsed DrawingML custom geometry path command.
/// </summary>
internal enum CustomGeometryCommandKind
{
	MoveTo,
	LineTo,
	CubicBezierTo,
	ArcTo,
	Close
}
