namespace PanoramicData.Render;

/// <summary>
/// Computes available text segments for tight wrapping around polygonal floating objects.
/// </summary>
internal static partial class TightWrapLayoutEngine
{
	/// <summary>
	/// Computes available horizontal text segments for a line after applying tight-wrap polygon exclusions.
	/// </summary>
	/// <param name="contentLeftTwips">Left edge of the text content area in twips.</param>
	/// <param name="contentWidthTwips">Width of the text content area in twips.</param>
	/// <param name="lineTopTwips">Top Y coordinate of the line box in twips.</param>
	/// <param name="lineHeightTwips">Line height in twips.</param>
	/// <param name="regions">Tight-wrap polygon regions that exclude text.</param>
	/// <returns>Ordered available line segments in twips.</returns>
	public static IReadOnlyList<WrapLineSegment> ComputeAvailableSegments(
		float contentLeftTwips,
		float contentWidthTwips,
		float lineTopTwips,
		float lineHeightTwips,
		IReadOnlyList<FloatingTightWrapRegion> regions)
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

		var segments = new List<WrapLineSegment>
		{
			new(contentLeftTwips, contentWidthTwips)
		};
		var scan = new LineScan(
			contentLeftTwips,
			contentLeftTwips + contentWidthTwips,
			lineTopTwips,
			lineTopTwips + lineHeightTwips,
			lineTopTwips + (lineHeightTwips / 2f));

		foreach (var region in regions)
		{
			if (!SubtractRegion(segments, region, scan))
			{
				return [];
			}
		}

		return segments;
	}

	private readonly record struct LineScan(float ContentLeft, float ContentRight, float Top, float Bottom, float ScanY);

	// Returns false when the region leaves no available segments.
	private static bool SubtractRegion(List<WrapLineSegment> segments, FloatingTightWrapRegion region, LineScan scan)
	{
		if (region.Points.Count < 3 || !OverlapsLine(region, scan))
		{
			return true;
		}

		var intersections = ComputeIntersections(region.Points, scan.ScanY);
		if (intersections.Count < 2)
		{
			return true;
		}

		intersections.Sort();
		for (var i = 0; i + 1 < intersections.Count; i += 2)
		{
			var clipLeft = Math.Max(intersections[i] - region.DistanceLeftTwips, scan.ContentLeft);
			var clipRight = Math.Min(intersections[i + 1] + region.DistanceRightTwips, scan.ContentRight);
			if (clipRight <= clipLeft)
			{
				continue;
			}

			WrapSegmentMath.SubtractRange(segments, clipLeft, clipRight);
			if (segments.Count == 0)
			{
				return false;
			}
		}

		return true;
	}

	private static bool OverlapsLine(FloatingTightWrapRegion region, LineScan scan)
	{
		var minY = float.PositiveInfinity;
		var maxY = float.NegativeInfinity;
		for (var i = 0; i < region.Points.Count; i++)
		{
			minY = Math.Min(minY, region.Points[i].YTwips);
			maxY = Math.Max(maxY, region.Points[i].YTwips);
		}

		var exclusionTop = minY - region.DistanceTopTwips;
		var exclusionBottom = maxY + region.DistanceBottomTwips;
		return scan.Bottom > exclusionTop && scan.Top < exclusionBottom;
	}
	private static List<float> ComputeIntersections(IReadOnlyList<TightWrapPoint> points, float scanY)
	{
		var intersections = new List<float>();
		for (var i = 0; i < points.Count; i++)
		{
			var p1 = points[i];
			var p2 = points[(i + 1) % points.Count];
			if (Math.Abs(p1.YTwips - p2.YTwips) < 0.0001f)
			{
				continue;
			}

			var crosses = (p1.YTwips <= scanY && p2.YTwips > scanY)
				|| (p2.YTwips <= scanY && p1.YTwips > scanY);
			if (!crosses)
			{
				continue;
			}

			var t = (scanY - p1.YTwips) / (p2.YTwips - p1.YTwips);
			intersections.Add(p1.XTwips + (t * (p2.XTwips - p1.XTwips)));
		}

		return intersections;
	}
}

/// <summary>
/// Represents a point in a tight-wrap polygon.
/// </summary>
/// <param name="XTwips">X coordinate in twips.</param>
/// <param name="YTwips">Y coordinate in twips.</param>
internal readonly record struct TightWrapPoint(float XTwips, float YTwips);

/// <summary>
/// Represents a tight-wrap polygon exclusion region.
/// </summary>
/// <param name="Points">Polygon points in page coordinates.</param>
/// <param name="DistanceTopTwips">Top wrap distance in twips.</param>
/// <param name="DistanceBottomTwips">Bottom wrap distance in twips.</param>
/// <param name="DistanceLeftTwips">Left wrap distance in twips.</param>
/// <param name="DistanceRightTwips">Right wrap distance in twips.</param>
internal readonly record struct FloatingTightWrapRegion(
	IReadOnlyList<TightWrapPoint> Points,
	float DistanceTopTwips = 0f,
	float DistanceBottomTwips = 0f,
	float DistanceLeftTwips = 0f,
	float DistanceRightTwips = 0f);
