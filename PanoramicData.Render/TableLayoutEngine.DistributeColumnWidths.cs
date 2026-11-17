namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class TableLayoutEngine
{
	/// <summary>
	/// Distributes available width across columns proportionally to preferred widths,
	/// respecting minimum widths.
	/// </summary>
	internal static IReadOnlyList<float> DistributeColumnWidths(
		IReadOnlyList<ColumnMeasurement> measurements,
		float availableWidthTwips)
	{
		if (measurements.Count == 0)
		{
			return [];
		}

		var widths = new float[measurements.Count];

		// Start with minimum widths
		var totalMinimum = 0f;
		var totalPreferred = 0f;
		for (var i = 0; i < measurements.Count; i++)
		{
			widths[i] = measurements[i].MinimumWidthTwips;
			totalMinimum += measurements[i].MinimumWidthTwips;
			totalPreferred += measurements[i].PreferredWidthTwips;
		}

		// If minimums exceed available width, use minimums as-is
		if (totalMinimum >= availableWidthTwips)
		{
			return widths;
		}

		// If preferred fits within available, use preferred widths
		if (totalPreferred <= availableWidthTwips)
		{
			// Distribute the remaining space proportionally to preferred widths
			var remaining = availableWidthTwips - totalPreferred;
			for (var i = 0; i < measurements.Count; i++)
			{
				widths[i] = measurements[i].PreferredWidthTwips + (remaining * measurements[i].PreferredWidthTwips / totalPreferred);
			}

			return widths;
		}

		// Preferred exceeds available: distribute proportionally between minimum and preferred
		var excessOverMinimum = availableWidthTwips - totalMinimum;
		var totalStretch = totalPreferred - totalMinimum;

		for (var i = 0; i < measurements.Count; i++)
		{
			var stretch = measurements[i].PreferredWidthTwips - measurements[i].MinimumWidthTwips;
			widths[i] = measurements[i].MinimumWidthTwips + (excessOverMinimum * stretch / totalStretch);
		}

		return widths;
	}

	/// <summary>
	/// The average estimated character width in twips for width estimation.
	/// Based on approximately 7pt per character in 12pt text (typical proportional font).
	/// </summary>
	internal const float AverageCharWidthTwips = 140f;

	/// <summary>
	/// The average estimated character width in twips for height (line-wrapping) estimation.
	/// More conservative than <see cref="AverageCharWidthTwips"/> to avoid overestimating
	/// row heights that push content to extra pages.
	/// </summary>
	private const float AverageCharWidthForHeightTwips = 100f;
}
