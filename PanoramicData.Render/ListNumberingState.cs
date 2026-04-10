namespace PanoramicData.Render;

/// <summary>
/// Tracks list numbering counters per numbering instance and level.
/// </summary>
internal sealed class ListNumberingState
{
	private readonly Dictionary<int, NumberingInstanceState> _instances = [];

	/// <summary>
	/// Advances numbering state for a level and returns the current label and counter snapshot.
	/// </summary>
	/// <param name="numberingId">The numbering instance ID.</param>
	/// <param name="style">The numbering style for the emitted level.</param>
	/// <returns>The formatted label and a snapshot of counters by level.</returns>
	public ListLabelResult Advance(int numberingId, NumberingLevelStyle style)
	{
		ArgumentNullException.ThrowIfNull(style);

		if (style.LevelIndex < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(style.LevelIndex));
		}

		if (!_instances.TryGetValue(numberingId, out var instanceState))
		{
			instanceState = new NumberingInstanceState();
			_instances[numberingId] = instanceState;
		}

		var currentLevel = style.LevelIndex;
		foreach (var level in instanceState.Counters.Keys.Where(level => level > currentLevel).ToArray())
		{
			instanceState.Counters.Remove(level);
			instanceState.RestartAnchors.Remove(level);
		}

		var shouldRestart = ShouldRestart(style, instanceState, out var restartAnchorValue);
		if (!instanceState.Counters.ContainsKey(currentLevel) || shouldRestart)
		{
			instanceState.Counters[currentLevel] = style.Start;
			if (restartAnchorValue is not null)
			{
				instanceState.RestartAnchors[currentLevel] = restartAnchorValue.Value;
			}
		}
		else
		{
			instanceState.Counters[currentLevel]++;
		}

		var snapshot = new Dictionary<int, int>(instanceState.Counters);
		var label = ListNumberingFormatter.FormatLabel(style, snapshot);
		return new ListLabelResult(label, snapshot);
	}

	private static bool ShouldRestart(NumberingLevelStyle style, NumberingInstanceState instanceState, out int? restartAnchorValue)
	{
		restartAnchorValue = null;
		if (style.RestartAfterLevel is null)
		{
			return false;
		}

		var restartLevelIndex = style.RestartAfterLevel.Value - 1;
		if (restartLevelIndex < 0)
		{
			return false;
		}

		if (!instanceState.Counters.TryGetValue(restartLevelIndex, out var higherLevelCounter))
		{
			return false;
		}

		restartAnchorValue = higherLevelCounter;
		if (!instanceState.RestartAnchors.TryGetValue(style.LevelIndex, out var lastAnchor))
		{
			return false;
		}

		return lastAnchor != higherLevelCounter;
	}

	private sealed class NumberingInstanceState
	{
		public Dictionary<int, int> Counters { get; } = [];

		public Dictionary<int, int> RestartAnchors { get; } = [];
	}
}

/// <summary>
/// Result from advancing list numbering for a paragraph.
/// </summary>
/// <param name="Label">The formatted list label text.</param>
/// <param name="CountersByLevel">A snapshot of active counters by level.</param>
internal readonly record struct ListLabelResult(string Label, IReadOnlyDictionary<int, int> CountersByLevel);
