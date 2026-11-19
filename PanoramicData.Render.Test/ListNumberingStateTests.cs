using AwesomeAssertions;
using Xunit;

namespace PanoramicData.Render.Test;

public sealed class ListNumberingStateTests
{
	private static NumberingLevelStyle Style(int level, int start, bool restart) => new()
	{
		LevelIndex = level,
		Start = start,
		NumberFormat = "decimal",
		LevelText = level == 0 ? "%1." : "%1.%2.",
		RestartAfterLevel = restart && level == 1 ? 1 : null
	};

	private static ListLabelResult Run(string steps, int start = 1, bool restart = false)
	{
		var state = new ListNumberingState();
		ListLabelResult result = default!;
		foreach (var step in steps.Split(' '))
		{
			var parts = step.Split(':');
			result = state.Advance(int.Parse(parts[0]), Style(int.Parse(parts[1]), start, restart));
		}

		return result;
	}

	[Fact]
	public void Advance_FirstItem_UsesStartValue()
	{
		var result = Run("1:0", start: 3);

		result.Label.Should().Be("3.");
		result.CountersByLevel[0].Should().Be(3);
	}

	[Theory]
	[InlineData("1:0 1:0", false, "2.")]
	[InlineData("1:0 1:1 1:1 1:0 1:1", false, "2.1.")]
	[InlineData("1:0 1:1 1:1 1:0 1:1", true, "2.1.")]
	[InlineData("1:0 1:0 2:0", false, "1.")]
	public void Advance_ProducesExpectedLabel(string steps, bool restartAfterHigherLevel, string expected)
	{
		Run(steps, restart: restartAfterHigherLevel).Label.Should().Be(expected);
	}
}
