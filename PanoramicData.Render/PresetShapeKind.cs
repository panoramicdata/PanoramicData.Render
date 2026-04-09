namespace PanoramicData.Render;

/// <summary>
/// Identifies the preset geometry kind of a DrawingML shape.
/// Covers the most common shapes used in Word documents.
/// </summary>
internal enum PresetShapeKind
{
	/// <summary>Unrecognised or unsupported preset geometry.</summary>
	Unknown = 0,

	// --- Basic shapes ---

	/// <summary>Rectangle (<c>rect</c>).</summary>
	Rectangle,

	/// <summary>Rounded rectangle (<c>roundRect</c>).</summary>
	RoundedRectangle,

	/// <summary>Ellipse / circle (<c>ellipse</c>).</summary>
	Ellipse,

	/// <summary>Isosceles triangle (<c>triangle</c>).</summary>
	Triangle,

	/// <summary>Right triangle (<c>rtTriangle</c>).</summary>
	RightTriangle,

	/// <summary>Diamond (<c>diamond</c>).</summary>
	Diamond,

	/// <summary>Parallelogram (<c>parallelogram</c>).</summary>
	Parallelogram,

	/// <summary>Trapezoid (<c>trapezoid</c>).</summary>
	Trapezoid,

	/// <summary>Pentagon (<c>pentagon</c>).</summary>
	Pentagon,

	/// <summary>Hexagon (<c>hexagon</c>).</summary>
	Hexagon,

	/// <summary>Octagon (<c>octagon</c>).</summary>
	Octagon,

	/// <summary>Cross (<c>cross</c>).</summary>
	Cross,

	/// <summary>Folded corner (<c>foldedCorner</c>).</summary>
	FoldedCorner,

	// --- Stars ---

	/// <summary>4-pointed star (<c>star4</c>).</summary>
	Star4,

	/// <summary>5-pointed star (<c>star5</c>).</summary>
	Star5,

	/// <summary>6-pointed star (<c>star6</c>).</summary>
	Star6,

	/// <summary>8-pointed star (<c>star8</c>).</summary>
	Star8,

	// --- Arrows ---

	/// <summary>Right arrow (<c>rightArrow</c>).</summary>
	RightArrow,

	/// <summary>Left arrow (<c>leftArrow</c>).</summary>
	LeftArrow,

	/// <summary>Up arrow (<c>upArrow</c>).</summary>
	UpArrow,

	/// <summary>Down arrow (<c>downArrow</c>).</summary>
	DownArrow,

	/// <summary>Left-right arrow (<c>leftRightArrow</c>).</summary>
	LeftRightArrow,

	// --- Callouts ---

	/// <summary>Rectangular callout (<c>wedgeRectCallout</c>).</summary>
	WedgeRectCallout,

	/// <summary>Rounded rectangle callout (<c>wedgeRoundRectCallout</c>).</summary>
	WedgeRoundRectCallout,

	/// <summary>Oval callout (<c>wedgeEllipseCallout</c>).</summary>
	WedgeEllipseCallout,

	/// <summary>Cloud callout (<c>cloudCallout</c>).</summary>
	CloudCallout,

	/// <summary>Callout 1 (<c>callout1</c>).</summary>
	Callout1,

	/// <summary>Callout 2 (<c>callout2</c>).</summary>
	Callout2,

	/// <summary>Callout 3 (<c>callout3</c>).</summary>
	Callout3,

	// --- Lines ---

	/// <summary>Straight line (<c>line</c>).</summary>
	Line,

	/// <summary>Snip single corner rectangle (<c>snip1Rect</c>).</summary>
	Snip1Rect,

	/// <summary>Snip same-side corner rectangle (<c>snip2SameRect</c>).</summary>
	Snip2SameRect,

	/// <summary>Snip diagonal corner rectangle (<c>snip2DiagRect</c>).</summary>
	Snip2DiagRect,

	/// <summary>Snip and round corner rectangle (<c>snipRoundRect</c>).</summary>
	SnipRoundRect,
}
