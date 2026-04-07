namespace PanoramicData.Render;

/// <summary>
/// Represents a single tab stop definition with its position, alignment type, and leader character.
/// </summary>
/// <param name="PositionTwips">The tab stop position from the left margin, in twips.</param>
/// <param name="Type">The alignment type for content at this tab stop.</param>
/// <param name="Leader">The leader fill character before this tab stop.</param>
internal readonly record struct TabStop(
	float PositionTwips,
	TabStopType Type = TabStopType.Left,
	TabStopLeader Leader = TabStopLeader.None);
