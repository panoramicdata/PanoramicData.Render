namespace PanoramicData.Render;

/// <summary>
/// Implements the Knuth-Plass optimal paragraph line-breaking algorithm.
/// Given a sequence of boxes, glue, and penalties, finds the set of breakpoints
/// that minimizes the total demerits (a measure of how "bad" the paragraph looks).
/// </summary>
internal static partial class KnuthPlassAlgorithm
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

	private sealed partial class BreakFinder
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

		private void RecordBestFinal(BreakNode node) => _bestFinal = node;

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
	}
}
