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
	{
		// Width of content from after the active break to just before this break
		var contentWidth = cumWidth[breakIndex] - active.TotalWidth;

		// Subtract glue widths that come right after the active break (they don't appear on the line)
		// This is handled by the active node's TotalWidth being set to post-break position

		// Add penalty width if breaking at a penalty
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
			// Need to stretch
			var stretch = cumStretch[breakIndex] - active.TotalStretch;
			return stretch > 0.001f ? diff / stretch : _tolerance + 1f;
		}
		else
		{
			// Need to shrink
			var shrink = cumShrink[breakIndex] - active.TotalShrink;
			return shrink > 0.001f ? diff / shrink : -(_tolerance + 1f);
		}
	}

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
	{
		var contentWidth = cumWidth[breakIndex] - cumWidth[startIndex];

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
			var stretch = cumStretch[breakIndex] - cumStretch[startIndex];
			return stretch > 0.001f ? diff / stretch : 0f;
		}
		else
		{
			var shrink = cumShrink[breakIndex] - cumShrink[startIndex];
			return shrink > 0.001f ? diff / shrink : -1f;
		}
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
