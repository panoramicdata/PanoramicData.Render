namespace PanoramicData.Render;

/// <summary>
/// Represents a line produced by the Knuth-Plass algorithm.
/// </summary>
/// <param name="StartIndex">The index of the first item on this line (inclusive).</param>
/// <param name="EndIndex">The index of the last item on this line (inclusive).</param>
/// <param name="AdjustmentRatio">The adjustment ratio for this line: 0 = natural width, positive = stretched, negative = shrunk.</param>
internal readonly record struct KnuthPlassLine(
	int StartIndex,
	int EndIndex,
	float AdjustmentRatio);
