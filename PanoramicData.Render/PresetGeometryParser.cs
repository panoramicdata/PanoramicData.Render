namespace PanoramicData.Render;

/// <summary>
/// Maps OOXML preset geometry name strings (e.g. <c>"rect"</c>, <c>"ellipse"</c>)
/// to <see cref="PresetShapeKind"/> values.
/// </summary>
internal static class PresetGeometryParser
{
	private static readonly IReadOnlyDictionary<string, PresetShapeKind> _map =
		new Dictionary<string, PresetShapeKind>(StringComparer.Ordinal)
		{
			// Basic shapes
			["rect"] = PresetShapeKind.Rectangle,
			["roundRect"] = PresetShapeKind.RoundedRectangle,
			["ellipse"] = PresetShapeKind.Ellipse,
			["triangle"] = PresetShapeKind.Triangle,
			["rtTriangle"] = PresetShapeKind.RightTriangle,
			["diamond"] = PresetShapeKind.Diamond,
			["parallelogram"] = PresetShapeKind.Parallelogram,
			["trapezoid"] = PresetShapeKind.Trapezoid,
			["pentagon"] = PresetShapeKind.Pentagon,
			["hexagon"] = PresetShapeKind.Hexagon,
			["octagon"] = PresetShapeKind.Octagon,
			["cross"] = PresetShapeKind.Cross,
			["foldedCorner"] = PresetShapeKind.FoldedCorner,

			// Stars
			["star4"] = PresetShapeKind.Star4,
			["star5"] = PresetShapeKind.Star5,
			["star6"] = PresetShapeKind.Star6,
			["star8"] = PresetShapeKind.Star8,

			// Arrows
			["rightArrow"] = PresetShapeKind.RightArrow,
			["leftArrow"] = PresetShapeKind.LeftArrow,
			["upArrow"] = PresetShapeKind.UpArrow,
			["downArrow"] = PresetShapeKind.DownArrow,
			["leftRightArrow"] = PresetShapeKind.LeftRightArrow,

			// Callouts
			["wedgeRectCallout"] = PresetShapeKind.WedgeRectCallout,
			["wedgeRoundRectCallout"] = PresetShapeKind.WedgeRoundRectCallout,
			["wedgeEllipseCallout"] = PresetShapeKind.WedgeEllipseCallout,
			["cloudCallout"] = PresetShapeKind.CloudCallout,
			["callout1"] = PresetShapeKind.Callout1,
			["callout2"] = PresetShapeKind.Callout2,
			["callout3"] = PresetShapeKind.Callout3,

			// Lines and snips
			["line"] = PresetShapeKind.Line,
			["snip1Rect"] = PresetShapeKind.Snip1Rect,
			["snip2SameRect"] = PresetShapeKind.Snip2SameRect,
			["snip2DiagRect"] = PresetShapeKind.Snip2DiagRect,
			["snipRoundRect"] = PresetShapeKind.SnipRoundRect,
		};

	/// <summary>
	/// Maps a raw OOXML preset geometry name string to a <see cref="PresetShapeKind"/>.
	/// Returns <see cref="PresetShapeKind.Unknown"/> for unrecognised names.
	/// </summary>
	/// <param name="rawName">The OOXML preset name, e.g. <c>"rect"</c>.</param>
	/// <returns>The corresponding <see cref="PresetShapeKind"/>.</returns>
	public static PresetShapeKind Parse(string? rawName)
	{
		if (string.IsNullOrEmpty(rawName))
		{
			return PresetShapeKind.Unknown;
		}

		return _map.TryGetValue(rawName, out var kind) ? kind : PresetShapeKind.Unknown;
	}
}
