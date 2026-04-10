namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class ListNumberingFormatterTests
{
	[Fact]
	public void FormatLabel_DecimalPattern_FormatsCurrentLevelCounter()
	{
		var style = new NumberingLevelStyle { LevelIndex = 0, Start = 1, NumberFormat = "decimal", LevelText = "%1." };

		var label = ListNumberingFormatter.FormatLabel(style, new Dictionary<int, int> { [0] = 12 });

		label.Should().Be("12.");
	}

	[Fact]
	public void FormatLabel_LowerLetter_FormatsAlphabeticCounter()
	{
		var style = new NumberingLevelStyle { LevelIndex = 0, Start = 1, NumberFormat = "lowerLetter", LevelText = "%1)" };

		var label = ListNumberingFormatter.FormatLabel(style, new Dictionary<int, int> { [0] = 27 });

		label.Should().Be("aa)");
	}

	[Fact]
	public void FormatLabel_UpperRoman_FormatsRomanCounter()
	{
		var style = new NumberingLevelStyle { LevelIndex = 0, Start = 1, NumberFormat = "upperRoman", LevelText = "%1." };

		var label = ListNumberingFormatter.FormatLabel(style, new Dictionary<int, int> { [0] = 14 });

		label.Should().Be("XIV.");
	}

	[Fact]
	public void FormatLabel_MultiLevelPattern_ReplacesEachPlaceholder()
	{
		var style = new NumberingLevelStyle { LevelIndex = 1, Start = 1, NumberFormat = "decimal", LevelText = "%1.%2." };

		var label = ListNumberingFormatter.FormatLabel(style, new Dictionary<int, int> { [0] = 3, [1] = 7 });

		label.Should().Be("3.7.");
	}

	[Fact]
	public void FormatLabel_Bullet_ReturnsBulletText()
	{
		var style = new NumberingLevelStyle { LevelIndex = 0, Start = 1, NumberFormat = "bullet", LevelText = "•" };

		var label = ListNumberingFormatter.FormatLabel(style, new Dictionary<int, int> { [0] = 1 });

		label.Should().Be("•");
	}
}
