namespace PanoramicData.Render;

/// <summary>
/// Represents a rendered table border line segment.
/// </summary>
/// <param name="X1">Segment start x-coordinate in twips.</param>
/// <param name="Y1">Segment start y-coordinate in twips.</param>
/// <param name="X2">Segment end x-coordinate in twips.</param>
/// <param name="Y2">Segment end y-coordinate in twips.</param>
/// <param name="WidthTwips">Stroke width in twips.</param>
/// <param name="ColorHex">Stroke color as 6-digit uppercase hex RGB.</param>
/// <param name="Style">Resolved border style.</param>
/// <param name="DashPatternTwips">Dash pattern in twips; null for solid strokes.</param>
internal readonly record struct TableBorderSegment(
	float X1,
	float Y1,
	float X2,
	float Y2,
	float WidthTwips,
	string ColorHex,
	BorderStyle Style,
	IReadOnlyList<float>? DashPatternTwips = null);
