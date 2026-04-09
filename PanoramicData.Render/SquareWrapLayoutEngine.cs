namespace PanoramicData.Render;

/// <summary>
/// Computes available text segments for square wrapping around floating objects.
/// </summary>
internal static class SquareWrapLayoutEngine
{
	/// <summary>
	/// Computes available horizontal text segments for a line after applying square-wrap exclusions.
	/// </summary>
	/// <param name="contentLeftTwips">Left edge of the text content area in twips.</param>
	/// <param name="contentWidthTwips">Width of the text content area in twips.</param>
	/// <param name="lineTopTwips">Top Y coordinate of the line box in twips.</param>
	/// <param name="lineHeightTwips">Line height in twips.</param>
	/// <param name="regions">Square-wrap regions that exclude text.</param>
	/// <returns>Ordered available line segments in twips.</returns>
	public static IReadOnlyList<WrapLineSegment> ComputeAvailableSegments(
		float contentLeftTwips,
		float contentWidthTwips,
		float lineTopTwips,
		float lineHeightTwips,
		IReadOnlyList<FloatingSquareWrapRegion> regions)
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

		var contentRightTwips = contentLeftTwips + contentWidthTwips;
		var segments = new List<WrapLineSegment>
		{
			new(contentLeftTwips, contentWidthTwips)
		};

		var lineBottomTwips = lineTopTwips + lineHeightTwips;
		foreach (var region in regions)
		{
			if (region.WidthTwips <= 0f || region.HeightTwips <= 0f)
			{
				continue;
			}

			var exclusionLeft = region.XTwips - region.DistanceLeftTwips;
			var exclusionTop = region.YTwips - region.DistanceTopTwips;
			var exclusionRight = region.XTwips + region.WidthTwips + region.DistanceRightTwips;
			var exclusionBottom = region.YTwips + region.HeightTwips + region.DistanceBottomTwips;

			// Half-open overlap check: [lineTop, lineBottom) intersects [exclusionTop, exclusionBottom)
			if (lineBottomTwips <= exclusionTop || lineTopTwips >= exclusionBottom)
			{
				continue;
			}

			var clipLeft = Math.Max(exclusionLeft, contentLeftTwips);
			var clipRight = Math.Min(exclusionRight, contentRightTwips);
			if (clipRight <= clipLeft)
			{
				continue;
			}

			SubtractRange(segments, clipLeft, clipRight);
			if (segments.Count == 0)
			{
				return [];
			}
		}

		return segments;
	}

	private static void SubtractRange(List<WrapLineSegment> segments, float subtractLeft, float subtractRight)
	{
		var output = new List<WrapLineSegment>(segments.Count + 1);

		for (var i = 0; i < segments.Count; i++)
		{
			var segment = segments[i];
			var segmentLeft = segment.XTwips;
			var segmentRight = segment.XTwips + segment.WidthTwips;

			if (subtractRight <= segmentLeft || subtractLeft >= segmentRight)
			{
				output.Add(segment);
				continue;
			}

			if (subtractLeft > segmentLeft)
			{
				output.Add(new WrapLineSegment(segmentLeft, subtractLeft - segmentLeft));
			}

			if (subtractRight < segmentRight)
			{
				output.Add(new WrapLineSegment(subtractRight, segmentRight - subtractRight));
			}
		}

		segments.Clear();
		segments.AddRange(output.Where(s => s.WidthTwips > 0f));
	}
}

/// <summary>
/// Represents a floating object square-wrap exclusion region.
/// </summary>
/// <param name="XTwips">Object left X coordinate in twips.</param>
/// <param name="YTwips">Object top Y coordinate in twips.</param>
/// <param name="WidthTwips">Object width in twips.</param>
/// <param name="HeightTwips">Object height in twips.</param>
/// <param name="DistanceTopTwips">Top wrap distance in twips.</param>
/// <param name="DistanceBottomTwips">Bottom wrap distance in twips.</param>
/// <param name="DistanceLeftTwips">Left wrap distance in twips.</param>
/// <param name="DistanceRightTwips">Right wrap distance in twips.</param>
internal readonly record struct FloatingSquareWrapRegion(
	float XTwips,
	float YTwips,
	float WidthTwips,
	float HeightTwips,
	float DistanceTopTwips = 0f,
	float DistanceBottomTwips = 0f,
	float DistanceLeftTwips = 0f,
	float DistanceRightTwips = 0f);

/// <summary>
/// Represents an available horizontal segment on a wrapped line.
/// </summary>
/// <param name="XTwips">Segment start X coordinate in twips.</param>
/// <param name="WidthTwips">Segment width in twips.</param>
internal readonly record struct WrapLineSegment(float XTwips, float WidthTwips);
