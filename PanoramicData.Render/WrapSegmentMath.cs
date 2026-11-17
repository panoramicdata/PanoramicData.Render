namespace PanoramicData.Render;

/// <summary>
/// Shared segment arithmetic for the text wrap layout engines.
/// </summary>
internal static class WrapSegmentMath
{
	internal static void SubtractRange(List<WrapLineSegment> segments, float subtractLeft, float subtractRight)
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
