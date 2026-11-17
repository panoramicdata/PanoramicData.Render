namespace PanoramicData.Render;

internal static partial class KnuthPlassAlgorithm
{
	private sealed partial class BreakFinder
	{
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
			var best = FindBestNodeAt(i);
			if (best is not null)
			{
				_bestFinal = best;
			}

			_activeNodes.Clear();

			// If there is more content after this forced break, start a new active node.
			if (i < _items.Count - 1)
			{
				_activeNodes.Add(CreateRestartNode(i, best));
			}
		}

		private BreakNode? FindBestNodeAt(int i)
		{
			BreakNode? best = null;
			for (var a = _activeNodes.Count - 1; a >= 0; a--)
			{
				if (_activeNodes[a].Position == i && (best is null || _activeNodes[a].TotalDemerits < best.TotalDemerits))
				{
					best = _activeNodes[a];
				}
			}

			return best;
		}
	}
}
