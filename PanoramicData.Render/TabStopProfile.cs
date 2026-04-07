namespace PanoramicData.Render;

/// <summary>
/// Represents the complete tab stop configuration for a paragraph, including
/// explicitly defined tab stops and the default tab stop interval.
/// </summary>
/// <param name="ExplicitStops">
/// The explicitly defined tab stops, which must be sorted by <see cref="TabStop.PositionTwips"/> ascending.
/// </param>
/// <param name="DefaultIntervalTwips">
/// The default tab stop interval in twips (from document settings).
/// Used to generate implicit tab stops beyond the last explicit one.
/// A value of zero or negative disables default tab stops.
/// </param>
internal readonly record struct TabStopProfile(
	IReadOnlyList<TabStop> ExplicitStops,
	float DefaultIntervalTwips = 720f)
{
	/// <summary>
	/// Default tab stop interval in Word: 0.5 inches = 720 twips.
	/// </summary>
	public const float DefaultInterval = 720f;

	/// <summary>
	/// An empty profile with no explicit stops and the standard default interval.
	/// </summary>
	public static readonly TabStopProfile Default = new(Array.Empty<TabStop>());

	/// <summary>
	/// Resolves the next tab stop at or beyond the given X position.
	/// First checks explicit stops, then falls back to generated default stops.
	/// </summary>
	/// <param name="currentX">The current X position in twips (relative to left margin).</param>
	/// <returns>The resolved tab stop to advance to.</returns>
	public TabStop ResolveNextTabStop(float currentX)
	{
		// First, try to find the next explicit tab stop beyond currentX.
		// We use a small epsilon to avoid landing exactly on a stop and re-selecting it.
		const float epsilon = 0.01f;
		foreach (var stop in ExplicitStops)
		{
			if (stop.PositionTwips > currentX + epsilon)
			{
				return stop;
			}
		}

		// No explicit stop found — generate from default interval
		if (DefaultIntervalTwips <= 0f)
		{
			// Default tabs disabled; advance by one twip as a minimum
			return new TabStop(currentX + 1f);
		}

		// Find the next default tab position beyond all explicit stops and currentX
		var nextDefault = DefaultIntervalTwips;
		while (nextDefault <= currentX + epsilon)
		{
			nextDefault += DefaultIntervalTwips;
		}

		return new TabStop(nextDefault);
	}
}
