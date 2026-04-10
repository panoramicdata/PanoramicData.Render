namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class ListNumberingStateTests
{
	[Fact]
	public void Advance_FirstItem_UsesStartValue()
	{
		var state = new ListNumberingState();
		var style = new NumberingLevelStyle { LevelIndex = 0, Start = 3, NumberFormat = "decimal", LevelText = "%1." };

		var result = state.Advance(1, style);

		result.Label.Should().Be("3.");
		result.CountersByLevel[0].Should().Be(3);
	}

	[Fact]
	public void Advance_SubsequentItem_IncrementsCounter()
	{
		var state = new ListNumberingState();
		var style = new NumberingLevelStyle { LevelIndex = 0, Start = 1, NumberFormat = "decimal", LevelText = "%1." };

		state.Advance(1, style);
		var result = state.Advance(1, style);

		result.Label.Should().Be("2.");
	}

	[Fact]
	public void Advance_NestedLevels_ResetsDeeperLevelsWhenReturningToHigherLevel()
	{
		var state = new ListNumberingState();
		var level0 = new NumberingLevelStyle { LevelIndex = 0, Start = 1, NumberFormat = "decimal", LevelText = "%1." };
		var level1 = new NumberingLevelStyle { LevelIndex = 1, Start = 1, NumberFormat = "decimal", LevelText = "%1.%2." };

		state.Advance(1, level0); // 1.
		state.Advance(1, level1); // 1.1.
		state.Advance(1, level1); // 1.2.
		state.Advance(1, level0); // 2.
		var result = state.Advance(1, level1); // 2.1.

		result.Label.Should().Be("2.1.");
	}

	[Fact]
	public void Advance_WithRestartAfterHigherLevel_RestartsWhenAnchorChanges()
	{
		var state = new ListNumberingState();
		var level0 = new NumberingLevelStyle { LevelIndex = 0, Start = 1, NumberFormat = "decimal", LevelText = "%1." };
		var level1 = new NumberingLevelStyle
		{
			LevelIndex = 1,
			Start = 1,
			NumberFormat = "decimal",
			LevelText = "%1.%2.",
			RestartAfterLevel = 1
		};

		state.Advance(1, level0); // 1.
		state.Advance(1, level1); // 1.1.
		state.Advance(1, level1); // 1.2.
		state.Advance(1, level0); // 2.
		var result = state.Advance(1, level1); // 2.1.

		result.Label.Should().Be("2.1.");
	}

	[Fact]
	public void Advance_DifferentNumberingIds_UseIndependentCounters()
	{
		var state = new ListNumberingState();
		var style = new NumberingLevelStyle { LevelIndex = 0, Start = 1, NumberFormat = "decimal", LevelText = "%1." };

		state.Advance(1, style);
		state.Advance(1, style);
		var result = state.Advance(2, style);

		result.Label.Should().Be("1.");
	}
}
