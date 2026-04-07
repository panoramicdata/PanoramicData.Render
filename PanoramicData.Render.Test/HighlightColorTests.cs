using AwesomeAssertions;
using Xunit;

namespace PanoramicData.Render.Test;

public class HighlightColorTests
{
	[Fact]
	public void None_HasValue_Zero()
	{
		((int)HighlightColor.None).Should().Be(0);
	}

	[Fact]
	public void EnumCount_Is17()
	{
		// 16 named colors + None
		Enum.GetValues<HighlightColor>().Should().HaveCount(17);
	}

	[Theory]
	[InlineData((int)HighlightColor.None)]
	[InlineData((int)HighlightColor.Black)]
	[InlineData((int)HighlightColor.Blue)]
	[InlineData((int)HighlightColor.Cyan)]
	[InlineData((int)HighlightColor.DarkBlue)]
	[InlineData((int)HighlightColor.DarkCyan)]
	[InlineData((int)HighlightColor.DarkGray)]
	[InlineData((int)HighlightColor.DarkGreen)]
	[InlineData((int)HighlightColor.DarkMagenta)]
	[InlineData((int)HighlightColor.DarkRed)]
	[InlineData((int)HighlightColor.DarkYellow)]
	[InlineData((int)HighlightColor.Green)]
	[InlineData((int)HighlightColor.LightGray)]
	[InlineData((int)HighlightColor.Magenta)]
	[InlineData((int)HighlightColor.Red)]
	[InlineData((int)HighlightColor.White)]
	[InlineData((int)HighlightColor.Yellow)]
	public void AllValues_AreDefined(int value)
	{
		Enum.IsDefined((HighlightColor)value).Should().BeTrue();
	}
}

public class HighlightColorMapTests
{
	[Fact]
	public void None_ReturnsNull()
	{
		HighlightColorMap.ToHexRgb(HighlightColor.None).Should().BeNull();
	}

	[Theory]
	[InlineData((int)HighlightColor.Black, "000000")]
	[InlineData((int)HighlightColor.Blue, "0000FF")]
	[InlineData((int)HighlightColor.Cyan, "00FFFF")]
	[InlineData((int)HighlightColor.DarkBlue, "000080")]
	[InlineData((int)HighlightColor.DarkCyan, "008080")]
	[InlineData((int)HighlightColor.DarkGray, "808080")]
	[InlineData((int)HighlightColor.DarkGreen, "008000")]
	[InlineData((int)HighlightColor.DarkMagenta, "800080")]
	[InlineData((int)HighlightColor.DarkRed, "800000")]
	[InlineData((int)HighlightColor.DarkYellow, "808000")]
	[InlineData((int)HighlightColor.Green, "00FF00")]
	[InlineData((int)HighlightColor.LightGray, "C0C0C0")]
	[InlineData((int)HighlightColor.Magenta, "FF00FF")]
	[InlineData((int)HighlightColor.Red, "FF0000")]
	[InlineData((int)HighlightColor.White, "FFFFFF")]
	[InlineData((int)HighlightColor.Yellow, "FFFF00")]
	public void KnownColor_ReturnCorrectHexRgb(int color, string expectedHex)
	{
		HighlightColorMap.ToHexRgb((HighlightColor)color).Should().Be(expectedHex);
	}

	[Fact]
	public void UnknownValue_ReturnsNull()
	{
		HighlightColorMap.ToHexRgb((HighlightColor)999).Should().BeNull();
	}

	[Fact]
	public void AllNamedColors_HaveNonNullMapping()
	{
		foreach (var color in Enum.GetValues<HighlightColor>())
		{
			if (color == HighlightColor.None)
			{
				continue;
			}

			HighlightColorMap.ToHexRgb(color).Should().NotBeNull(
				$"HighlightColor.{color} should have a hex mapping");
		}
	}

	[Fact]
	public void AllHexValues_AreSixCharacters()
	{
		foreach (var color in Enum.GetValues<HighlightColor>())
		{
			var hex = HighlightColorMap.ToHexRgb(color);
			if (hex is not null)
			{
				hex.Should().HaveLength(6,
					$"HighlightColor.{color} hex should be 6 characters");
			}
		}
	}
}
