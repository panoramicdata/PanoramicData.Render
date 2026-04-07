using AwesomeAssertions;
using Xunit;

using PanoramicData.Render;

namespace PanoramicData.Render.Test;

public class TabStopResolverTests
{
	// ===================================================================
	// Left tab — content starts at tab position
	// ===================================================================

	[Fact]
	public void Left_ContentStartsAtPosition()
	{
		var stop = new TabStop(1440f, TabStopType.Left);

		var x = TabStopResolver.ComputeContentStart(stop, contentWidthAfterTab: 500f);

		x.Should().Be(1440f);
	}

	[Fact]
	public void Left_IgnoresContentWidth()
	{
		var stop = new TabStop(720f, TabStopType.Left);

		var x = TabStopResolver.ComputeContentStart(stop, contentWidthAfterTab: 9999f);

		x.Should().Be(720f);
	}

	// ===================================================================
	// Center tab — content centers on tab position
	// ===================================================================

	[Fact]
	public void Center_ContentCenteredOnPosition()
	{
		var stop = new TabStop(2880f, TabStopType.Center);

		// Content of 1000 twips → starts at 2880 - 500 = 2380
		var x = TabStopResolver.ComputeContentStart(stop, contentWidthAfterTab: 1000f);

		x.Should().Be(2380f);
	}

	[Fact]
	public void Center_WidthExceedsDoublePosition_ClampsToZero()
	{
		var stop = new TabStop(200f, TabStopType.Center);

		// Content 600 → 200 - 300 = -100 → clamp to 0
		var x = TabStopResolver.ComputeContentStart(stop, contentWidthAfterTab: 600f);

		x.Should().Be(0f);
	}

	[Fact]
	public void Center_ZeroContentWidth_AtPosition()
	{
		var stop = new TabStop(1440f, TabStopType.Center);

		var x = TabStopResolver.ComputeContentStart(stop, contentWidthAfterTab: 0f);

		x.Should().Be(1440f);
	}

	// ===================================================================
	// Right tab — content ends at tab position
	// ===================================================================

	[Fact]
	public void Right_ContentEndsAtPosition()
	{
		var stop = new TabStop(2880f, TabStopType.Right);

		// Content of 800 → starts at 2880 - 800 = 2080
		var x = TabStopResolver.ComputeContentStart(stop, contentWidthAfterTab: 800f);

		x.Should().Be(2080f);
	}

	[Fact]
	public void Right_WidthExceedsPosition_ClampsToZero()
	{
		var stop = new TabStop(500f, TabStopType.Right);

		// Content 700 → 500 - 700 = -200 → clamp to 0
		var x = TabStopResolver.ComputeContentStart(stop, contentWidthAfterTab: 700f);

		x.Should().Be(0f);
	}

	[Fact]
	public void Right_ZeroContentWidth_AtPosition()
	{
		var stop = new TabStop(2880f, TabStopType.Right);

		var x = TabStopResolver.ComputeContentStart(stop, contentWidthAfterTab: 0f);

		x.Should().Be(2880f);
	}

	// ===================================================================
	// Decimal tab — decimal point aligns at tab position
	// ===================================================================

	[Fact]
	public void Decimal_AlignsByWidthBeforeDecimal()
	{
		var stop = new TabStop(2880f, TabStopType.Decimal);

		// "123.45": widthBeforeDecimal=300 → start at 2880 - 300 = 2580
		var x = TabStopResolver.ComputeContentStart(stop,
			contentWidthAfterTab: 500f,
			widthBeforeDecimal: 300f);

		x.Should().Be(2580f);
	}

	[Fact]
	public void Decimal_NoDecimalPoint_WidthBeforeIsTotal()
	{
		var stop = new TabStop(2880f, TabStopType.Decimal);

		// "12345" (no decimal): widthBeforeDecimal = totalWidth = 500
		var x = TabStopResolver.ComputeContentStart(stop,
			contentWidthAfterTab: 500f,
			widthBeforeDecimal: 500f);

		x.Should().Be(2380f); // 2880 - 500
	}

	[Fact]
	public void Decimal_WidthExceedsPosition_ClampsToZero()
	{
		var stop = new TabStop(200f, TabStopType.Decimal);

		var x = TabStopResolver.ComputeContentStart(stop,
			contentWidthAfterTab: 1000f,
			widthBeforeDecimal: 500f);

		x.Should().Be(0f); // 200 - 500 = -300 → 0
	}

	[Fact]
	public void Decimal_ZeroWidthBefore_AtPosition()
	{
		var stop = new TabStop(1440f, TabStopType.Decimal);

		var x = TabStopResolver.ComputeContentStart(stop,
			contentWidthAfterTab: 300f,
			widthBeforeDecimal: 0f);

		x.Should().Be(1440f);
	}

	// ===================================================================
	// Bar tab — behaves like Left for content
	// ===================================================================

	[Fact]
	public void Bar_ContentStartsAtPosition()
	{
		var stop = new TabStop(1440f, TabStopType.Bar);

		var x = TabStopResolver.ComputeContentStart(stop, contentWidthAfterTab: 500f);

		x.Should().Be(1440f);
	}

	// ===================================================================
	// Leader characters — no effect on position computation
	// ===================================================================

	[Theory]
	[InlineData(0)] // None
	[InlineData(1)] // Dot
	[InlineData(2)] // Hyphen
	[InlineData(3)] // Heavy
	[InlineData(4)] // MiddleDot
	[InlineData(5)] // Underscore
	public void Leader_DoesNotAffectPosition(int leaderValue)
	{
		var leader = (TabStopLeader)leaderValue;
		var stop = new TabStop(1440f, TabStopType.Left, leader);

		var x = TabStopResolver.ComputeContentStart(stop, contentWidthAfterTab: 500f);

		x.Should().Be(1440f); // Leader is a rendering concern, not a positioning one
	}

	// ===================================================================
	// Unknown tab type — falls back to position (like Left)
	// ===================================================================

	[Fact]
	public void UnknownType_FallsBackToPosition()
	{
		var stop = new TabStop(1440f, (TabStopType)999);

		var x = TabStopResolver.ComputeContentStart(stop, contentWidthAfterTab: 500f);

		x.Should().Be(1440f);
	}
}
