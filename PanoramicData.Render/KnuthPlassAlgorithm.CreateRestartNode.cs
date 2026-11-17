namespace PanoramicData.Render;

internal static partial class KnuthPlassAlgorithm
{
	private sealed partial class BreakFinder
	{
		private BreakNode CreateRestartNode(int i, BreakNode? best)
		{
			var (postWidth, postStretch, postShrink) = ComputePostBreakSums(i, _items, _cumWidth, _cumStretch, _cumShrink);
			return new BreakNode(
				position: i,
				line: best?.Line ?? 1,
				fitnessClass: 1,
				totalWidth: postWidth,
				totalStretch: postStretch,
				totalShrink: postShrink,
				totalDemerits: best?.TotalDemerits ?? 0,
				previous: best);
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
					RecordBestFinal(node);
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
}
