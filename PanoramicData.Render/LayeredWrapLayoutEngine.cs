namespace PanoramicData.Render;

/// <summary>
/// Computes available text segments for layered floating objects (behind/in-front of text).
/// </summary>
internal static class LayeredWrapLayoutEngine
{
	/// <summary>
	/// Computes available horizontal text segments for a line where floating objects do not displace text.
	/// </summary>
	/// <param name="contentLeftTwips">Left edge of the text content area in twips.</param>
	/// <param name="contentWidthTwips">Width of the text content area in twips.</param>
	/// <param name="regions">Layered wrap regions (ignored for displacement).</param>
	/// <returns>A single full-width line segment.</returns>
	public static IReadOnlyList<WrapLineSegment> ComputeAvailableSegments(
		float contentLeftTwips,
		float contentWidthTwips,
		IReadOnlyList<FloatingLayeredRegion> regions)
	{
		ArgumentNullException.ThrowIfNull(regions);
		if (contentWidthTwips <= 0f)
		{
			throw new ArgumentOutOfRangeException(nameof(contentWidthTwips));
		}

		return [new WrapLineSegment(contentLeftTwips, contentWidthTwips)];
	}
}

/// <summary>
/// Represents a layered floating object region that does not affect text flow.
/// </summary>
/// <param name="XTwips">Object left X coordinate in twips.</param>
/// <param name="YTwips">Object top Y coordinate in twips.</param>
/// <param name="WidthTwips">Object width in twips.</param>
/// <param name="HeightTwips">Object height in twips.</param>
/// <param name="BehindDocument">True for behind-text placement, false for in-front placement.</param>
internal readonly record struct FloatingLayeredRegion(
	float XTwips,
	float YTwips,
	float WidthTwips,
	float HeightTwips,
	bool BehindDocument);
