namespace PanoramicData.Render;

/// <summary>
/// Computes available text segments for top-and-bottom wrapping around floating objects.
/// </summary>
internal static class TopBottomWrapLayoutEngine
{
	/// <summary>
	/// Computes available horizontal text segments for a line after applying top-and-bottom exclusions.
	/// </summary>
	/// <param name="contentLeftTwips">Left edge of the text content area in twips.</param>
	/// <param name="contentWidthTwips">Width of the text content area in twips.</param>
	/// <param name="lineTopTwips">Top Y coordinate of the line box in twips.</param>
	/// <param name="lineHeightTwips">Line height in twips.</param>
	/// <param name="regions">Top-and-bottom wrap regions that block full line width within their vertical span.</param>
	/// <returns>Available line segments in twips.</returns>
	public static IReadOnlyList<WrapLineSegment> ComputeAvailableSegments(
		float contentLeftTwips,
		float contentWidthTwips,
		float lineTopTwips,
		float lineHeightTwips,
		IReadOnlyList<FloatingTopBottomWrapRegion> regions)
	{
		ArgumentNullException.ThrowIfNull(regions);
		if (contentWidthTwips <= 0f)
		{
			throw new ArgumentOutOfRangeException(nameof(contentWidthTwips));
		}

		if (lineHeightTwips <= 0f)
		{
			throw new ArgumentOutOfRangeException(nameof(lineHeightTwips));
		}

		var lineBottomTwips = lineTopTwips + lineHeightTwips;
		foreach (var region in regions)
		{
			if (region.HeightTwips <= 0f)
			{
				continue;
			}

			var exclusionTop = region.YTwips - region.DistanceTopTwips;
			var exclusionBottom = region.YTwips + region.HeightTwips + region.DistanceBottomTwips;
			if (lineBottomTwips <= exclusionTop || lineTopTwips >= exclusionBottom)
			{
				continue;
			}

			return [];
		}

		return [new WrapLineSegment(contentLeftTwips, contentWidthTwips)];
	}
}

/// <summary>
/// Represents a top-and-bottom wrap exclusion region.
/// </summary>
/// <param name="YTwips">Object top Y coordinate in twips.</param>
/// <param name="HeightTwips">Object height in twips.</param>
/// <param name="DistanceTopTwips">Top wrap distance in twips.</param>
/// <param name="DistanceBottomTwips">Bottom wrap distance in twips.</param>
internal readonly record struct FloatingTopBottomWrapRegion(
	float YTwips,
	float HeightTwips,
	float DistanceTopTwips = 0f,
	float DistanceBottomTwips = 0f);
