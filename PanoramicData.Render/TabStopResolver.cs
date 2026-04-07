namespace PanoramicData.Render;

/// <summary>
/// Computes the X position where content should start after a tab character,
/// based on the resolved tab stop type and the width of the content that follows.
/// </summary>
internal static class TabStopResolver
{
	/// <summary>
	/// The decimal separator character used for decimal tab alignment.
	/// </summary>
	public const char DecimalSeparator = '.';

	/// <summary>
	/// Computes the X offset where content should begin after a tab,
	/// given the resolved tab stop and the dimensions of the content that follows.
	/// </summary>
	/// <param name="tabStop">The resolved tab stop to align to.</param>
	/// <param name="contentWidthAfterTab">
	/// The total width (in twips) of content following the tab until the next tab or line end.
	/// Used for Center, Right, and Decimal alignment.
	/// </param>
	/// <param name="widthBeforeDecimal">
	/// For Decimal tabs: the width (in twips) of content before the decimal point.
	/// Ignored for other tab types.
	/// </param>
	/// <returns>The X position (in twips) where content should start.</returns>
	public static float ComputeContentStart(
		TabStop tabStop,
		float contentWidthAfterTab = 0f,
		float widthBeforeDecimal = 0f)
	{
		var pos = tabStop.PositionTwips;

		return tabStop.Type switch
		{
			TabStopType.Left => pos,
			TabStopType.Center => Math.Max(0f, pos - contentWidthAfterTab / 2f),
			TabStopType.Right => Math.Max(0f, pos - contentWidthAfterTab),
			TabStopType.Decimal => Math.Max(0f, pos - widthBeforeDecimal),
			TabStopType.Bar => pos, // Bar is a visual marker; content starts after it like Left
			_ => pos
		};
	}
}
