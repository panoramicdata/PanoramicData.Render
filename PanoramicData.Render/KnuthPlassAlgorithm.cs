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

		return items.Count == 0 ? [] : new BreakFinder(items, lineWidthSelector).Find();
	}

	private sealed class BreakFinder
	{
		private readonly IReadOnlyList<KnuthPlassItem> _items;
		private readonly Func<int, float> _lineWidthSelector;
		private readonly float[] _cumWidth;
		private readonly float[] _cumStretch;
		private readonly float[] _cumShrink;

		// Active nodes: each represents a feasible breakpoint.
		private readonly List<BreakNode> _activeNodes =
		[
			new(position: -1, line: 0, fitnessClass: 1, totalWidth: 0, totalStretch: 0, totalShrink: 0, totalDemerits: 0, previous: null)
		];

		// Best-found node at a forced break (for fallback chaining).
		private BreakNode? _bestFinal;

		public BreakFinder(IReadOnlyList<KnuthPlassItem> items, Func<int, float> lineWidthSelector)
		{
			_items = items;
			_lineWidthSelector = lineWidthSelector;

			// Cumulative width/stretch/shrink sums for O(1) range queries.
			_cumWidth = new float[items.Count + 1];
			_cumStretch = new float[items.Count + 1];
			_cumShrink = new float[items.Count + 1];
			for (var i = 0; i < items.Count; i++)
			{
				_cumWidth[i + 1] = _cumWidth[i] + items[i].Width;
				var glue = items[i] as KnuthPlassGlue;
				_cumStretch[i + 1] = _cumStretch[i] + (glue?.Stretch ?? 0f);
				_cumShrink[i + 1] = _cumShrink[i] + (glue?.Shrink ?? 0f);
			}
		}

		public IReadOnlyList<KnuthPlassLine> Find()
		{
			for (var i = 0; i < _items.Count; i++)
			{
				if (IsLegalBreakpoint(i))
				{
					ProcessBreakpoint(i);
				}
			}

			ChooseBestRemainingNode();
			return _bestFinal is null ? [] : BuildLines(CollectBreakNodes(_bestFinal));
		}

		// Legal breakpoints: a penalty below infinity, or a glue preceded by a box.
		private bool IsLegalBreakpoint(int i)
		{
			var item = _items[i];
			if (item is KnuthPlassPenalty p && !float.IsPositiveInfinity(p.Penalty))
			{
				return true;
			}

			return item is KnuthPlassGlue && i > 0 && _items[i - 1] is KnuthPlassBox;
		}

		private void ProcessBreakpoint(int i)
		{
			var item = _items[i];
			var isForcedBreakHere = IsForcedBreak(item);

			var toDeactivate = new List<int>();
			var toActivate = new List<BreakNode>();
			for (var a = 0; a < _activeNodes.Count; a++)
			{
				var active = _activeNodes[a];
				var ratio = ComputeAdjustmentRatio(active, i, _items, _cumWidth, _cumStretch, _cumShrink, _lineWidthSelector(active.Line));

				// Too long even with maximum shrink: deactivate unless a break is forced.
				if (ratio < -1f && !isForcedBreakHere)
				{
					toDeactivate.Add(a);
					continue;
				}

				toActivate.Add(CreateNode(active, i, item, ratio));
			}

			// Deactivate nodes (in reverse order to preserve indices)
			for (var d = toDeactivate.Count - 1; d >= 0; d--)
			{
				_activeNodes.RemoveAt(toDeactivate[d]);
			}

			AddBestNodesPerLineAndFitness(toActivate);

			if (isForcedBreakHere)
			{
				HandleForcedBreak(i);
			}
			else if (_activeNodes.Count == 0)
			{
				AddFallbackNode(i);
			}
		}

		private BreakNode CreateNode(BreakNode active, int i, KnuthPlassItem item, float ratio)
		{
			// Clamp ratio for demerits calculation when it is out of feasible range.
			var effectiveRatio = Math.Clamp(ratio, -1f, _tolerance);
			var fitnessClass = GetFitnessClass(effectiveRatio);
			var demerits = ComputeDemerits(item, effectiveRatio)
				+ ExtraDemerits(ratio)
				+ (Math.Abs(fitnessClass - active.FitnessClass) > 1 ? _fitnessDemerit : 0f)
				+ (IsConsecutiveFlaggedBreak(item, active) ? _flaggedDemerit : 0f);
			demerits += active.TotalDemerits;

			var (postWidth, postStretch, postShrink) = ComputePostBreakSums(i, _items, _cumWidth, _cumStretch, _cumShrink);
			return new BreakNode(
				position: i,
				line: active.Line + 1,
				fitnessClass: fitnessClass,
				totalWidth: postWidth,
				totalStretch: postStretch,
				totalShrink: postShrink,
				totalDemerits: demerits,
				previous: active);
		}

		// Very loose lines (ratio above tolerance) and overfull lines (only accepted because of a forced break) cost extra.
		private static float ExtraDemerits(float ratio)
		{
			var extra = ratio > _tolerance ? 10000f : 0f;
			return ratio < -1f ? extra + 100_000_000f : extra;
		}

		private bool IsConsecutiveFlaggedBreak(KnuthPlassItem item, BreakNode active)
			=> item is KnuthPlassPenalty { IsFlagged: true }
				&& active.Position >= 0 && active.Position < _items.Count
				&& _items[active.Position] is KnuthPlassPenalty { IsFlagged: true };

		// Keeps only the best per (line, fitness class); on a tie the later candidate wins.
		private void AddBestNodesPerLineAndFitness(List<BreakNode> candidates)
		{
			var bestByLineAndFitness = new Dictionary<(int Line, int FitnessClass), BreakNode>();
			foreach (var node in candidates)
			{
				var key = (node.Line, node.FitnessClass);
				if (!bestByLineAndFitness.TryGetValue(key, out var existing) || node.TotalDemerits <= existing.TotalDemerits)
				{
					bestByLineAndFitness[key] = node;
				}
			}

			_activeNodes.AddRange(bestByLineAndFitness.Values);
		}

		// A forced break ends the paragraph segment: its best node becomes a final endpoint.
		private void HandleForcedBreak(int i)
		{
			BreakNode? best = null;
			for (var a = _activeNodes.Count - 1; a >= 0; a--)
			{
				if (_activeNodes[a].Position == i && (best is null || _activeNodes[a].TotalDemerits < best.TotalDemerits))
				{
					best = _activeNodes[a];
				}
			}

			if (best is not null)
			{
				_bestFinal = best;
			}

			_activeNodes.Clear();

			// If there is more content after this forced break, start a new active node.
			if (i < _items.Count - 1)
			{
				var (postWidth, postStretch, postShrink) = ComputePostBreakSums(i, _items, _cumWidth, _cumStretch, _cumShrink);
				_activeNodes.Add(new BreakNode(
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

		// Emergency fallback when no active nodes remain: a greedy break chained to the best final node.
		private void AddFallbackNode(int i)
		{
			var (postWidth, postStretch, postShrink) = ComputePostBreakSums(i, _items, _cumWidth, _cumStretch, _cumShrink);
			_activeNodes.Add(new BreakNode(
				position: i,
				line: (_bestFinal?.Line ?? 0) + 1,
				fitnessClass: 1,
				totalWidth: postWidth,
				totalStretch: postStretch,
				totalShrink: postShrink,
				totalDemerits: float.MaxValue / 2,
				previous: _bestFinal));
		}

		// Chooses the best remaining active node if we did not end on a forced break.
		private void ChooseBestRemainingNode()
		{
			foreach (var node in _activeNodes)
			{
				// Only consider actual breakpoints (position >= 0), not the initial sentinel.
				if (node.Position >= 0 && (_bestFinal is null || node.TotalDemerits < _bestFinal.TotalDemerits))
				{
					_bestFinal = node;
				}
			}
		}

		// Walks back to reconstruct break positions, skipping restart anchors that share a position with their previous node.
		private static List<BreakNode> CollectBreakNodes(BreakNode bestFinal)
		{
			var breakPositions = new List<BreakNode>();
			for (var node = bestFinal; node is not null && node.Position >= 0; node = node.Previous)
			{
				if (node.Previous is null || node.Position != node.Previous.Position)
				{
					breakPositions.Add(node);
				}
			}

			breakPositions.Reverse();
			return breakPositions;
		}

		private List<KnuthPlassLine> BuildLines(List<BreakNode> breakPositions)
		{
			var lines = new List<KnuthPlassLine>();
			var lineStart = 0;

			for (var b = 0; b < breakPositions.Count; b++)
			{
				var bp = breakPositions[b];
				var ratio = ComputeAdjustmentRatioForLine(lineStart, bp.Position, _items, _cumWidth, _cumStretch, _cumShrink, _lineWidthSelector(b));

				// Last line: clamp ratio to 0 (don't stretch the last line).
				if (b == breakPositions.Count - 1 && IsForcedBreak(_items[bp.Position]))
				{
					ratio = Math.Min(ratio, 0f);
				}

				lines.Add(new KnuthPlassLine(lineStart, bp.Position, ratio));

				// Next line starts after the break (skip glue after break).
				lineStart = bp.Position + 1;
				while (lineStart < _items.Count && _items[lineStart] is KnuthPlassGlue)
				{
					lineStart++;
				}
			}

			return lines;
		}
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
