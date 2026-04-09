namespace PanoramicData.Render;

/// <summary>
/// Aggregates floating wrap regions of all types and provides combined
/// available-segment queries for the line-breaking engine.
/// </summary>
internal sealed class WrapRegionRegistry
{
	private readonly List<FloatingSquareWrapRegion> _squareRegions = [];
	private readonly List<FloatingTightWrapRegion> _tightRegions = [];
	private readonly List<FloatingTopBottomWrapRegion> _topBottomRegions = [];

	/// <summary>
	/// Gets a value indicating whether no wrap regions have been registered.
	/// </summary>
	public bool IsEmpty =>
		_squareRegions.Count == 0 &&
		_tightRegions.Count == 0 &&
		_topBottomRegions.Count == 0;

	/// <summary>
	/// Registers a square-wrap floating region.
	/// </summary>
	/// <param name="region">The square-wrap region to add.</param>
	public void AddSquareRegion(FloatingSquareWrapRegion region) =>
		_squareRegions.Add(region);

	/// <summary>
	/// Registers a tight-wrap floating region.
	/// </summary>
	/// <param name="region">The tight-wrap region to add.</param>
	public void AddTightRegion(FloatingTightWrapRegion region) =>
		_tightRegions.Add(region);

	/// <summary>
	/// Registers a top-and-bottom wrap floating region.
	/// </summary>
	/// <param name="region">The top-and-bottom region to add.</param>
	public void AddTopBottomRegion(FloatingTopBottomWrapRegion region) =>
		_topBottomRegions.Add(region);

	/// <summary>
	/// Computes the combined available horizontal text segments for a line
	/// after applying all registered wrap exclusions.
	/// </summary>
	/// <param name="contentLeftTwips">Left edge of the text content area in twips (absolute page coordinates).</param>
	/// <param name="contentWidthTwips">Width of the text content area in twips.</param>
	/// <param name="lineTopTwips">Top Y coordinate of the line box in twips (absolute page coordinates).</param>
	/// <param name="lineHeightTwips">Line height in twips.</param>
	/// <returns>Ordered available line segments in twips, or the full content segment if no regions affect this line.</returns>
	public IReadOnlyList<WrapLineSegment> GetAvailableSegments(
		float contentLeftTwips,
		float contentWidthTwips,
		float lineTopTwips,
		float lineHeightTwips)
	{
		// Start with the full content band.
		var segments = new List<WrapLineSegment>
		{
			new(contentLeftTwips, contentWidthTwips)
		};

		// Intersect with square-wrap available segments.
		if (_squareRegions.Count > 0)
		{
			var squareResult = SquareWrapLayoutEngine.ComputeAvailableSegments(
				contentLeftTwips, contentWidthTwips, lineTopTwips, lineHeightTwips, _squareRegions);
			segments = IntersectSegments(segments, squareResult);
		}

		// Intersect with tight-wrap available segments.
		if (_tightRegions.Count > 0 && segments.Count > 0)
		{
			var tightResult = TightWrapLayoutEngine.ComputeAvailableSegments(
				contentLeftTwips, contentWidthTwips, lineTopTwips, lineHeightTwips, _tightRegions);
			segments = IntersectSegments(segments, tightResult);
		}

		// Intersect with top-and-bottom available segments.
		if (_topBottomRegions.Count > 0 && segments.Count > 0)
		{
			var tbResult = TopBottomWrapLayoutEngine.ComputeAvailableSegments(
				contentLeftTwips, contentWidthTwips, lineTopTwips, lineHeightTwips, _topBottomRegions);
			segments = IntersectSegments(segments, tbResult);
		}

		return segments;
	}

	/// <summary>
	/// Returns the width of the widest available text segment at the given line position.
	/// Returns <paramref name="contentWidthTwips"/> if no regions affect this line.
	/// Returns zero if all text is blocked (e.g. by a top-and-bottom region).
	/// </summary>
	/// <param name="contentLeftTwips">Left edge of the text content area in twips (absolute page coordinates).</param>
	/// <param name="contentWidthTwips">Width of the text content area in twips.</param>
	/// <param name="lineTopTwips">Top Y coordinate of the line box in twips (absolute page coordinates).</param>
	/// <param name="lineHeightTwips">Line height in twips.</param>
	/// <returns>Available line width in twips for Knuth-Plass input.</returns>
	public float GetPrimaryLineWidth(
		float contentLeftTwips,
		float contentWidthTwips,
		float lineTopTwips,
		float lineHeightTwips)
	{
		if (IsEmpty)
		{
			return contentWidthTwips;
		}

		var segments = GetAvailableSegments(contentLeftTwips, contentWidthTwips, lineTopTwips, lineHeightTwips);

		if (segments.Count == 0)
		{
			return 0f;
		}

		var maxWidth = 0f;
		foreach (var segment in segments)
		{
			if (segment.WidthTwips > maxWidth)
			{
				maxWidth = segment.WidthTwips;
			}
		}

		return maxWidth;
	}

	/// <summary>
	/// Computes the intersection of two ordered segment lists,
	/// keeping only the sub-intervals that appear in both lists.
	/// </summary>
	private static List<WrapLineSegment> IntersectSegments(
		List<WrapLineSegment> a,
		IReadOnlyList<WrapLineSegment> b)
	{
		var result = new List<WrapLineSegment>(a.Count);

		foreach (var sa in a)
		{
			var saLeft = sa.XTwips;
			var saRight = sa.XTwips + sa.WidthTwips;

			foreach (var sb in b)
			{
				var sbLeft = sb.XTwips;
				var sbRight = sb.XTwips + sb.WidthTwips;

				var left = MathF.Max(saLeft, sbLeft);
				var right = MathF.Min(saRight, sbRight);

				if (right > left)
				{
					result.Add(new WrapLineSegment(left, right - left));
				}
			}
		}

		return result;
	}
}
