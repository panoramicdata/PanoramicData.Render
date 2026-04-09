namespace PanoramicData.Render;

/// <summary>
/// Implements the Knuth-Plass optimal paragraph line-breaking algorithm.
/// Given a sequence of boxes, glue, and penalties, finds the set of breakpoints
/// that minimizes the total demerits (a measure of how "bad" the paragraph looks).
/// </summary>
internal static class KnuthPlassAlgorithm
{
	/// <summary>
	/// Default tolerance for the adjustment ratio. Lines with |ratio| above this
	/// are considered infeasible unless no better option exists.
	/// </summary>
	private const float _tolerance = 2f;

	/// <summary>
	/// Demerit weight for flagged (hyphenated) consecutive breaks.
	/// </summary>
	private const float _flaggedDemerit = 3000f;

	/// <summary>
	/// Fitness class demerit for adjacent lines with fitness classes differing by more than 1.
	/// </summary>
	private const float _fitnessDemerit = 100f;

	/// <summary>
	/// Finds the optimal set of line breaks for the given items and a uniform line width.
	/// </summary>
	/// <param name="items">The sequence of boxes, glue, and penalties.</param>
	/// <param name="lineWidth">The target line width in twips (applied to every line).</param>
	/// <returns>A list of <see cref="KnuthPlassLine"/> describing each line.</returns>
	public static IReadOnlyList<KnuthPlassLine> FindBreaks(IReadOnlyList<KnuthPlassItem> items, float lineWidth)
	{
		ArgumentNullException.ThrowIfNull(items);

		if (lineWidth <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(lineWidth));
		}

		return FindBreaks(items, _ => lineWidth);
	}

	/// <summary>
	/// Finds the optimal set of line breaks for the given items using a per-line width selector.
	/// This overload supports variable line widths, e.g. when floating objects reduce available
	/// width on specific lines.
	/// </summary>
	/// <param name="items">The sequence of boxes, glue, and penalties.</param>
	/// <param name="lineWidthSelector">
	/// A delegate that returns the target line width in twips for a given 0-based line index.
	/// Called with the line index (0 = first line) for each line being evaluated.
	/// </param>
	/// <returns>A list of <see cref="KnuthPlassLine"/> describing each line.</returns>
	public static IReadOnlyList<KnuthPlassLine> FindBreaks(
		IReadOnlyList<KnuthPlassItem> items,
		Func<int, float> lineWidthSelector)
	{
		ArgumentNullException.ThrowIfNull(items);
		ArgumentNullException.ThrowIfNull(lineWidthSelector);

		if (items.Count == 0)
		{
			return [];
		}

		// Build cumulative width/stretch/shrink sums for O(1) range queries
		var cumWidth = new float[items.Count + 1];
		var cumStretch = new float[items.Count + 1];
		var cumShrink = new float[items.Count + 1];

		for (var i = 0; i < items.Count; i++)
		{
			cumWidth[i + 1] = cumWidth[i] + items[i].Width;
			if (items[i] is KnuthPlassGlue glue)
			{
				cumStretch[i + 1] = cumStretch[i] + glue.Stretch;
				cumShrink[i + 1] = cumShrink[i] + glue.Shrink;
			}
			else
			{
				cumStretch[i + 1] = cumStretch[i];
				cumShrink[i + 1] = cumShrink[i];
			}
		}

		// Active nodes: each represents a feasible breakpoint
		var activeNodes = new List<BreakNode>
		{
			new(position: -1, line: 0, fitnessClass: 1, totalWidth: 0, totalStretch: 0, totalShrink: 0, totalDemerits: 0, previous: null)
		};

		// Best-found nodes at each position (for fallback)
		BreakNode? bestFinal = null;

		for (var i = 0; i < items.Count; i++)
		{
			var item = items[i];

			// Only consider legal breakpoints:
			// - At a penalty (if penalty < PositiveInfinity)
			// - At a glue preceded by a box
			var isLegalBreakpoint = item is KnuthPlassPenalty p && !float.IsPositiveInfinity(p.Penalty);
			if (!isLegalBreakpoint && item is KnuthPlassGlue && i > 0 && items[i - 1] is KnuthPlassBox)
			{
				isLegalBreakpoint = true;
			}

			if (!isLegalBreakpoint)
			{
				continue;
			}

			var isForcedBreakHere = IsForcedBreak(item);

			// Evaluate this breakpoint against all active nodes
			var toDeactivate = new List<int>();
			var toActivate = new List<BreakNode>();

			for (var a = 0; a < activeNodes.Count; a++)
			{
				var active = activeNodes[a];
				var ratio = ComputeAdjustmentRatio(active, i, items, cumWidth, cumStretch, cumShrink, lineWidthSelector(active.Line));

				// If ratio is too negative, the line is too long even with max shrink — deactivate
				if (ratio < -1f && !isForcedBreakHere)
				{
					toDeactivate.Add(a);
					continue;
				}

				// Accept the breakpoint if:
				// - ratio is within [-1, tolerance] (normal feasible range), OR
				// - it is a forced break (must break regardless of ratio)
				// Note: ratio > tolerance means the line is too short and can't stretch to fill,
				// but the content still fits on the line. We accept it with high demerits.
				if (ratio >= -1f || isForcedBreakHere)
				{
					// Clamp ratio for demerits calculation when it's out of feasible range
					var effectiveRatio = Math.Clamp(ratio, -1f, _tolerance);

					var demerits = ComputeDemerits(item, effectiveRatio);

					// Extra penalty for very loose lines (ratio > tolerance)
					if (ratio > _tolerance)
					{
						demerits += 10000f;
					}

					// Extra massive penalty for overfull lines (ratio < -1).
					// These are only accepted because of a forced break — the content
					// physically overflows the line. Prefer breaking earlier if possible.
					if (ratio < -1f)
					{
						demerits += 100_000_000f;
					}

					// Fitness class penalty
					var fitnessClass = GetFitnessClass(effectiveRatio);
					if (Math.Abs(fitnessClass - active.FitnessClass) > 1)
					{
						demerits += _fitnessDemerit;
					}

					// Flagged consecutive break penalty
					if (item is KnuthPlassPenalty { IsFlagged: true } &&
						active.Position >= 0 && active.Position < items.Count &&
						items[active.Position] is KnuthPlassPenalty { IsFlagged: true })
					{
						demerits += _flaggedDemerit;
					}

					demerits += active.TotalDemerits;

					// Compute cumulative sums at the point after this break
					var (postWidth, postStretch, postShrink) = ComputePostBreakSums(i, items, cumWidth, cumStretch, cumShrink);

					var node = new BreakNode(
						position: i,
						line: active.Line + 1,
						fitnessClass: fitnessClass,
						totalWidth: postWidth,
						totalStretch: postStretch,
						totalShrink: postShrink,
						totalDemerits: demerits,
						previous: active);

					toActivate.Add(node);
				}
			}

			// Deactivate nodes (in reverse order to preserve indices)
			for (var d = toDeactivate.Count - 1; d >= 0; d--)
			{
				activeNodes.RemoveAt(toDeactivate[d]);
			}

			// Add new active nodes (keep only the best per fitness class for this position)
			foreach (var node in toActivate)
			{
				activeNodes.Add(node);
			}

			// If a forced break, deactivate all preceding active nodes
			if (isForcedBreakHere)
			{
				// Find the best node at this position
				BreakNode? best = null;
				for (var a = activeNodes.Count - 1; a >= 0; a--)
				{
					if (activeNodes[a].Position == i)
					{
						if (best is null || activeNodes[a].TotalDemerits < best.TotalDemerits)
						{
							best = activeNodes[a];
						}
					}
				}

				// For forced breaks, the best node becomes a final endpoint.
				// We need to chain it with any previously found bestFinal.
				if (best is not null)
				{
					bestFinal = best;
				}

				// Remove all active nodes — forced break ends the paragraph segment
				activeNodes.Clear();

				// If there's more content after this forced break, start a new active node
				if (i < items.Count - 1)
				{
					var (postWidth, postStretch, postShrink) = ComputePostBreakSums(i, items, cumWidth, cumStretch, cumShrink);
					activeNodes.Add(new BreakNode(
						position: i,
						line: best?.Line ?? 1,
						fitnessClass: 1,
						totalWidth: postWidth,
						totalStretch: postStretch,
						totalShrink: postShrink,
						totalDemerits: best?.TotalDemerits ?? 0,
						previous: best));
				}
			}

			// Emergency fallback: if no active nodes remain, use a greedy break
			if (activeNodes.Count == 0 && !isForcedBreakHere)
			{
				// Find the best previous node for chaining
				BreakNode? fallbackPrevious = bestFinal;
				var fallbackLine = (fallbackPrevious?.Line ?? 0) + 1;

				var (postWidth, postStretch, postShrink) = ComputePostBreakSums(i, items, cumWidth, cumStretch, cumShrink);
				activeNodes.Add(new BreakNode(
					position: i,
					line: fallbackLine,
					fitnessClass: 1,
					totalWidth: postWidth,
					totalStretch: postStretch,
					totalShrink: postShrink,
					totalDemerits: float.MaxValue / 2,
					previous: fallbackPrevious));
			}
		}

		// Choose the best remaining active node if we didn't end on a forced break
		if (activeNodes.Count > 0)
		{
			foreach (var node in activeNodes)
			{
				// Only consider actual breakpoints (position >= 0), not the initial sentinel
				if (node.Position >= 0 && (bestFinal is null || node.TotalDemerits < bestFinal.TotalDemerits))
				{
					bestFinal = node;
				}
			}
		}

		if (bestFinal is null)
		{
			return [];
		}

		// Walk back to reconstruct break positions, skipping restart anchors
		// (restart anchors share the same position as their previous node)
		var breakPositions = new List<BreakNode>();
		for (var node = bestFinal; node is not null && node.Position >= 0; node = node.Previous)
		{
			if (node.Previous is not null && node.Position == node.Previous.Position)
			{
				// This is a restart anchor after a forced break — skip it
				continue;
			}

			breakPositions.Add(node);
		}

		breakPositions.Reverse();

		// Build lines from break positions
		var lines = new List<KnuthPlassLine>();
		var lineStart = 0;

		for (var b = 0; b < breakPositions.Count; b++)
		{
			var bp = breakPositions[b];

			// Compute adjustment ratio for this line
			var startNode = b == 0 ? null : breakPositions[b - 1];
			var ratio = ComputeAdjustmentRatioForLine(lineStart, bp.Position, items, cumWidth, cumStretch, cumShrink, lineWidthSelector(b));

			// Last line: clamp ratio to 0 (don't stretch the last line)
			if (b == breakPositions.Count - 1 && IsForcedBreak(items[bp.Position]))
			{
				ratio = Math.Min(ratio, 0f);
			}

			lines.Add(new KnuthPlassLine(lineStart, bp.Position, ratio));

			// Next line starts after the break (skip glue after break)
			lineStart = bp.Position + 1;
			while (lineStart < items.Count && items[lineStart] is KnuthPlassGlue)
			{
				lineStart++;
			}
		}

		return lines;
	}

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

	/// <summary>
	/// Returns true if the item represents a forced break (negative infinity penalty).
	/// </summary>
	private static bool IsForcedBreak(KnuthPlassItem item) =>
		item is KnuthPlassPenalty p && float.IsNegativeInfinity(p.Penalty);

	/// <summary>
	/// Represents a node in the Knuth-Plass active node list.
	/// </summary>
	private sealed class BreakNode(
		int position,
		int line,
		int fitnessClass,
		float totalWidth,
		float totalStretch,
		float totalShrink,
		float totalDemerits,
		KnuthPlassAlgorithm.BreakNode? previous)
	{
		/// <summary>
		/// Gets the position in the item list where this break occurs.
		/// </summary>
		public int Position { get; } = position;

		/// <summary>
		/// Gets the line number (starting from 1) after this break.
		/// </summary>
		public int Line { get; } = line;

		/// <summary>
		/// Gets the fitness class (0-3) of the line ending at this break.
		/// </summary>
		public int FitnessClass { get; } = fitnessClass;

		/// <summary>
		/// Gets the cumulative width up to the content following this break.
		/// </summary>
		public float TotalWidth { get; } = totalWidth;

		/// <summary>
		/// Gets the cumulative stretch up to the content following this break.
		/// </summary>
		public float TotalStretch { get; } = totalStretch;

		/// <summary>
		/// Gets the cumulative shrink up to the content following this break.
		/// </summary>
		public float TotalShrink { get; } = totalShrink;

		/// <summary>
		/// Gets the total demerits accumulated up to this break.
		/// </summary>
		public float TotalDemerits { get; } = totalDemerits;

		/// <summary>
		/// Gets the previous break node in the optimal path.
		/// </summary>
		public BreakNode? Previous { get; } = previous;
	}
}
