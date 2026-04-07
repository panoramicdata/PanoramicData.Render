using AwesomeAssertions;
using Xunit;

namespace PanoramicData.Render.Test;

public class TextDecorationTests
{
	[Fact]
	public void Default_HasNoDecorations()
	{
		var decoration = new TextDecoration();

		decoration.Underline.Should().Be(UnderlineStyle.None);
		decoration.UnderlineColor.Should().BeNull();
		decoration.Strikethrough.Should().BeFalse();
		decoration.DoubleStrikethrough.Should().BeFalse();
	}

	[Fact]
	public void None_Static_MatchesDefault()
	{
		TextDecoration.None.Should().Be(new TextDecoration());
		TextDecoration.None.HasAnyDecoration.Should().BeFalse();
	}

	// --- HasUnderline ---

	[Fact]
	public void HasUnderline_WhenNone_ReturnsFalse()
	{
		new TextDecoration(UnderlineStyle.None).HasUnderline.Should().BeFalse();
	}

	[Theory]
	[InlineData((int)UnderlineStyle.Single)]
	[InlineData((int)UnderlineStyle.Double)]
	[InlineData((int)UnderlineStyle.Thick)]
	[InlineData((int)UnderlineStyle.Dotted)]
	[InlineData((int)UnderlineStyle.DottedHeavy)]
	[InlineData((int)UnderlineStyle.Dash)]
	[InlineData((int)UnderlineStyle.DashedHeavy)]
	[InlineData((int)UnderlineStyle.DashLong)]
	[InlineData((int)UnderlineStyle.DashLongHeavy)]
	[InlineData((int)UnderlineStyle.DotDash)]
	[InlineData((int)UnderlineStyle.DashDotHeavy)]
	[InlineData((int)UnderlineStyle.DotDotDash)]
	[InlineData((int)UnderlineStyle.DashDotDotHeavy)]
	[InlineData((int)UnderlineStyle.Wave)]
	[InlineData((int)UnderlineStyle.WavyDouble)]
	[InlineData((int)UnderlineStyle.WavyHeavy)]
	[InlineData((int)UnderlineStyle.Words)]
	public void HasUnderline_WhenNonNone_ReturnsTrue(int underline)
	{
		new TextDecoration((UnderlineStyle)underline).HasUnderline.Should().BeTrue();
	}

	// --- HasStrikethrough ---

	[Fact]
	public void HasStrikethrough_WhenNeither_ReturnsFalse()
	{
		new TextDecoration(Strikethrough: false, DoubleStrikethrough: false)
			.HasStrikethrough.Should().BeFalse();
	}

	[Fact]
	public void HasStrikethrough_WhenSingleStrike_ReturnsTrue()
	{
		new TextDecoration(Strikethrough: true).HasStrikethrough.Should().BeTrue();
	}

	[Fact]
	public void HasStrikethrough_WhenDoubleStrike_ReturnsTrue()
	{
		new TextDecoration(DoubleStrikethrough: true).HasStrikethrough.Should().BeTrue();
	}

	[Fact]
	public void HasStrikethrough_WhenBothStrike_ReturnsTrue()
	{
		new TextDecoration(Strikethrough: true, DoubleStrikethrough: true)
			.HasStrikethrough.Should().BeTrue();
	}

	// --- HasAnyDecoration ---

	[Fact]
	public void HasAnyDecoration_WhenNoDecorations_ReturnsFalse()
	{
		new TextDecoration().HasAnyDecoration.Should().BeFalse();
	}

	[Fact]
	public void HasAnyDecoration_WhenOnlyUnderline_ReturnsTrue()
	{
		new TextDecoration(UnderlineStyle.Single).HasAnyDecoration.Should().BeTrue();
	}

	[Fact]
	public void HasAnyDecoration_WhenOnlyStrikethrough_ReturnsTrue()
	{
		new TextDecoration(Strikethrough: true).HasAnyDecoration.Should().BeTrue();
	}

	[Fact]
	public void HasAnyDecoration_WhenOnlyDoubleStrikethrough_ReturnsTrue()
	{
		new TextDecoration(DoubleStrikethrough: true).HasAnyDecoration.Should().BeTrue();
	}

	[Fact]
	public void HasAnyDecoration_WhenAllDecorations_ReturnsTrue()
	{
		new TextDecoration(UnderlineStyle.Wave, "FF0000", true, true)
			.HasAnyDecoration.Should().BeTrue();
	}

	// --- UnderlineColor ---

	[Fact]
	public void UnderlineColor_WhenSet_IsPreserved()
	{
		var decoration = new TextDecoration(UnderlineStyle.Single, "0000FF");

		decoration.UnderlineColor.Should().Be("0000FF");
	}

	[Fact]
	public void UnderlineColor_WhenNull_InheritsTextColor()
	{
		var decoration = new TextDecoration(UnderlineStyle.Single);

		decoration.UnderlineColor.Should().BeNull();
	}

	// --- Equality ---

	[Fact]
	public void Equality_SameValues_AreEqual()
	{
		var a = new TextDecoration(UnderlineStyle.Dash, "AABB00", true, false);
		var b = new TextDecoration(UnderlineStyle.Dash, "AABB00", true, false);

		a.Should().Be(b);
		(a == b).Should().BeTrue();
	}

	[Fact]
	public void Equality_DifferentUnderline_AreNotEqual()
	{
		var a = new TextDecoration(UnderlineStyle.Single);
		var b = new TextDecoration(UnderlineStyle.Double);

		a.Should().NotBe(b);
	}

	[Fact]
	public void Equality_DifferentColor_AreNotEqual()
	{
		var a = new TextDecoration(UnderlineStyle.Single, "FF0000");
		var b = new TextDecoration(UnderlineStyle.Single, "00FF00");

		a.Should().NotBe(b);
	}

	[Fact]
	public void Equality_DifferentStrikethrough_AreNotEqual()
	{
		var a = new TextDecoration(Strikethrough: true);
		var b = new TextDecoration(Strikethrough: false);

		a.Should().NotBe(b);
	}

	[Fact]
	public void Equality_DifferentDoubleStrikethrough_AreNotEqual()
	{
		var a = new TextDecoration(DoubleStrikethrough: true);
		var b = new TextDecoration(DoubleStrikethrough: false);

		a.Should().NotBe(b);
	}

	// --- Combined scenarios ---

	[Fact]
	public void FullDecoration_AllPropertiesSet()
	{
		var decoration = new TextDecoration(
			Underline: UnderlineStyle.WavyDouble,
			UnderlineColor: "CC0000",
			Strikethrough: true,
			DoubleStrikethrough: false);

		decoration.Underline.Should().Be(UnderlineStyle.WavyDouble);
		decoration.UnderlineColor.Should().Be("CC0000");
		decoration.Strikethrough.Should().BeTrue();
		decoration.DoubleStrikethrough.Should().BeFalse();
		decoration.HasUnderline.Should().BeTrue();
		decoration.HasStrikethrough.Should().BeTrue();
		decoration.HasAnyDecoration.Should().BeTrue();
	}
}
