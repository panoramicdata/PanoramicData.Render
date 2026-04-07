using AwesomeAssertions;
using Xunit;

namespace PanoramicData.Render.Test;

public class RunColorTests
{
	// --- Constants ---

	[Fact]
	public void AutoHexValue_IsBlack()
	{
		RunColor.AutoHexValue.Should().Be("000000");
	}

	[Fact]
	public void DefaultHexValue_IsBlack()
	{
		RunColor.DefaultHexValue.Should().Be("000000");
	}

	// --- Static instances ---

	[Fact]
	public void Auto_IsBlackAndMarkedAuto()
	{
		RunColor.Auto.HexRgb.Should().Be("000000");
		RunColor.Auto.IsAuto.Should().BeTrue();
	}

	[Fact]
	public void Default_IsBlackAndNotAuto()
	{
		RunColor.Default.HexRgb.Should().Be("000000");
		RunColor.Default.IsAuto.Should().BeFalse();
	}

	// --- Constructor ---

	[Fact]
	public void Constructor_SetsHexRgbAndIsAuto()
	{
		var color = new RunColor("FF0000", IsAuto: false);

		color.HexRgb.Should().Be("FF0000");
		color.IsAuto.Should().BeFalse();
	}

	[Fact]
	public void Constructor_IsAutoDefaultsFalse()
	{
		var color = new RunColor("00FF00");

		color.IsAuto.Should().BeFalse();
	}

	// --- FromResolvedColor ---

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void FromResolvedColor_NullOrWhitespace_ReturnsAuto(string? input)
	{
		var color = RunColor.FromResolvedColor(input);

		color.Should().Be(RunColor.Auto);
		color.IsAuto.Should().BeTrue();
	}

	[Theory]
	[InlineData("auto")]
	[InlineData("Auto")]
	[InlineData("AUTO")]
	public void FromResolvedColor_AutoString_ReturnsAuto(string input)
	{
		var color = RunColor.FromResolvedColor(input);

		color.Should().Be(RunColor.Auto);
		color.IsAuto.Should().BeTrue();
	}

	[Fact]
	public void FromResolvedColor_HexColor_ReturnsUppercased()
	{
		var color = RunColor.FromResolvedColor("ff0000");

		color.HexRgb.Should().Be("FF0000");
		color.IsAuto.Should().BeFalse();
	}

	[Fact]
	public void FromResolvedColor_AlreadyUppercase_PreservesValue()
	{
		var color = RunColor.FromResolvedColor("00AABB");

		color.HexRgb.Should().Be("00AABB");
	}

	[Fact]
	public void FromResolvedColor_MixedCase_NormalizesToUpper()
	{
		var color = RunColor.FromResolvedColor("aAbBcC");

		color.HexRgb.Should().Be("AABBCC");
	}

	// --- Channel extraction ---

	[Fact]
	public void Red_ParsesCorrectly()
	{
		var color = new RunColor("FF8040");

		color.Red.Should().Be(0xFF);
	}

	[Fact]
	public void Green_ParsesCorrectly()
	{
		var color = new RunColor("FF8040");

		color.Green.Should().Be(0x80);
	}

	[Fact]
	public void Blue_ParsesCorrectly()
	{
		var color = new RunColor("FF8040");

		color.Blue.Should().Be(0x40);
	}

	[Theory]
	[InlineData("000000", 0, 0, 0)]
	[InlineData("FFFFFF", 255, 255, 255)]
	[InlineData("FF0000", 255, 0, 0)]
	[InlineData("00FF00", 0, 255, 0)]
	[InlineData("0000FF", 0, 0, 255)]
	public void Channels_ExtractCorrectValues(string hex, int r, int g, int b)
	{
		var color = new RunColor(hex);

		color.Red.Should().Be((byte)r);
		color.Green.Should().Be((byte)g);
		color.Blue.Should().Be((byte)b);
	}

	// --- Edge cases for channel parsing ---

	[Fact]
	public void Channels_WithNullHex_ReturnZero()
	{
		var color = new RunColor(null!);

		color.Red.Should().Be(0);
		color.Green.Should().Be(0);
		color.Blue.Should().Be(0);
	}

	[Fact]
	public void Channels_WithShortHex_ReturnZeroForMissingChannels()
	{
		var color = new RunColor("FF");

		color.Red.Should().Be(0xFF);
		color.Green.Should().Be(0);
		color.Blue.Should().Be(0);
	}

	[Fact]
	public void Channels_WithInvalidHex_ReturnZero()
	{
		var color = new RunColor("ZZZZZZ");

		color.Red.Should().Be(0);
		color.Green.Should().Be(0);
		color.Blue.Should().Be(0);
	}

	// --- Equality ---

	[Fact]
	public void Equality_SameValues_AreEqual()
	{
		var a = new RunColor("FF0000");
		var b = new RunColor("FF0000");

		a.Should().Be(b);
		(a == b).Should().BeTrue();
	}

	[Fact]
	public void Equality_DifferentHex_AreNotEqual()
	{
		var a = new RunColor("FF0000");
		var b = new RunColor("00FF00");

		a.Should().NotBe(b);
	}

	[Fact]
	public void Equality_SameHex_DifferentIsAuto_AreNotEqual()
	{
		var a = new RunColor("000000", IsAuto: true);
		var b = new RunColor("000000", IsAuto: false);

		a.Should().NotBe(b);
	}
}
