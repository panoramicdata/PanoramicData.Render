namespace PanoramicData.Render;

/// <summary>
/// Identifies the coordinate reference frame used by an anchored object's position.
/// </summary>
internal enum AnchorRelativeFrom
{
	Unknown,
	Page,
	Margin,
	Column,
	Character,
	Paragraph,
	Line,
	LeftMargin,
	RightMargin,
	InsideMargin,
	OutsideMargin,
	TopMargin,
	BottomMargin
}

/// <summary>
/// Identifies optional alignment keywords used by anchored object positioning.
/// </summary>
internal enum AnchorAlignment
{
	None,
	Left,
	Center,
	Right,
	Inside,
	Outside,
	Top,
	Bottom
}