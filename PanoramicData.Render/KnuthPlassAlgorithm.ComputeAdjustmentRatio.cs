namespace PanoramicData.Render;

internal static partial class KnuthPlassAlgorithm
{
	/// <summary>
	/// Computes the adjustment ratio for breaking at position <paramref name="breakIndex"/>
	/// from the given active node.
	/// </summary>
	private static float ComputeAdjustmentRatio(
		BreakNode active,
		int breakIndex,
		IReadOnlyList<KnuthPlassItem> items,
		float[] cumWidth,
		float[] cumStretch,
		float[] cumShrink,
		float lineWidth)
		=> ComputeRatio(
			items,
			breakIndex,
			(cumWidth[breakIndex] - active.TotalWidth, cumStretch[breakIndex] - active.TotalStretch, cumShrink[breakIndex] - active.TotalShrink),
			lineWidth,
			isFinalLine: false);

	/// <summary>
	/// Computes the adjustment ratio for a line from startIndex to breakIndex, for final output.
	/// </summary>
	private static float ComputeAdjustmentRatioForLine(
		int startIndex,
		int breakIndex,
		IReadOnlyList<KnuthPlassItem> items,
		float[] cumWidth,
		float[] cumStretch,
		float[] cumShrink,
		float lineWidth)
		=> ComputeRatio(
			items,
			breakIndex,
			(cumWidth[breakIndex] - cumWidth[startIndex], cumStretch[breakIndex] - cumStretch[startIndex], cumShrink[breakIndex] - cumShrink[startIndex]),
			lineWidth,
			isFinalLine: true);

	private static float ComputeRatio(
		IReadOnlyList<KnuthPlassItem> items,
		int breakIndex,
		(float Width, float Stretch, float Shrink) line,
		float lineWidth,
		bool isFinalLine)
	{
		var contentWidth = line.Width;
		if (items[breakIndex] is KnuthPlassPenalty penalty)
		{
			contentWidth += penalty.Width;
		}

		var diff = lineWidth - contentWidth;
		if (Math.Abs(diff) < 0.001f)
		{
			return 0f;
		}

		if (diff > 0)
		{
			return line.Stretch > 0.001f ? diff / line.Stretch : (isFinalLine ? 0f : _tolerance + 1f);
		}

		return line.Shrink > 0.001f ? diff / line.Shrink : (isFinalLine ? -1f : -(_tolerance + 1f));
	}
	/// <summary>
	/// Computes demerits for a potential breakpoint.
	/// </summary>
	private static float ComputeDemerits(KnuthPlassItem item, float ratio)
	{
		var badness = 100f * MathF.Pow(MathF.Abs(ratio), 3);

		if (item is KnuthPlassPenalty { Penalty: >= 0 } p)
		{
			return MathF.Pow(1f + badness + p.Penalty, 2);
		}

		if (item is KnuthPlassPenalty { Penalty: < 0 } pNeg && !float.IsNegativeInfinity(pNeg.Penalty))
		{
			return MathF.Pow(1f + badness, 2) - MathF.Pow(pNeg.Penalty, 2);
		}

		// Forced break or glue break
		return MathF.Pow(1f + badness, 2);
	}

	/// <summary>
	/// Determines the fitness class based on the adjustment ratio.
	/// 0 = tight, 1 = normal, 2 = loose, 3 = very loose.
	/// </summary>
	private static int GetFitnessClass(float ratio)
	{
		if (ratio < -0.5f)
		{
			return 0; // tight
		}

		if (ratio <= 0.5f)
		{
			return 1; // normal
		}

		if (ratio <= 1.0f)
		{
			return 2; // loose
		}

		return 3; // very loose
	}

	/// <summary>
	/// Returns cumulative width/stretch/shrink sums for items following the break at position i.
	/// This skips any glue immediately after the break point.
	/// </summary>
	private static (float Width, float Stretch, float Shrink) ComputePostBreakSums(
		int breakIndex,
		IReadOnlyList<KnuthPlassItem> items,
		float[] cumWidth,
		float[] cumStretch,
		float[] cumShrink)
	{
		var j = breakIndex + 1;
		while (j < items.Count && items[j] is KnuthPlassGlue)
		{
			j++;
		}

		return (cumWidth[j], cumStretch[j], cumShrink[j]);
	}
}
