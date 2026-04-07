using AwesomeAssertions;
using Xunit;

namespace PanoramicData.Render.Test;

public class CharacterSpacingTests
{
	// --- Default / Normal ---

	[Fact]
	public void Default_IsZero()
	{
		var spacing = new CharacterSpacing();

		spacing.ValueTwips.Should().Be(0f);
	}

	[Fact]
	public void Normal_IsZero()
	{
		CharacterSpacing.Normal.ValueTwips.Should().Be(0f);
		CharacterSpacing.Normal.IsNormal.Should().BeTrue();
	}

	[Fact]
	public void Normal_MatchesDefault()
	{
		CharacterSpacing.Normal.Should().Be(new CharacterSpacing());
	}

	// --- IsExpanded / IsCondensed / IsNormal ---

	[Theory]
	[InlineData(20f)]
	[InlineData(1f)]
	[InlineData(100f)]
	public void Positive_IsExpanded(float twips)
	{
		var spacing = new CharacterSpacing(twips);

		spacing.IsExpanded.Should().BeTrue();
		spacing.IsCondensed.Should().BeFalse();
		spacing.IsNormal.Should().BeFalse();
	}

	[Theory]
	[InlineData(-20f)]
	[InlineData(-1f)]
	[InlineData(-100f)]
	public void Negative_IsCondensed(float twips)
	{
		var spacing = new CharacterSpacing(twips);

		spacing.IsCondensed.Should().BeTrue();
		spacing.IsExpanded.Should().BeFalse();
		spacing.IsNormal.Should().BeFalse();
	}

	[Fact]
	public void Zero_IsNormal()
	{
		var spacing = new CharacterSpacing(0f);

		spacing.IsNormal.Should().BeTrue();
		spacing.IsExpanded.Should().BeFalse();
		spacing.IsCondensed.Should().BeFalse();
	}

	// --- ValuePoints ---

	[Fact]
	public void ValuePoints_ConvertsCorrectly()
	{
		// 20 twips = 1 point
		var spacing = new CharacterSpacing(20f);

		spacing.ValuePoints.Should().Be(1f);
	}

	[Fact]
	public void ValuePoints_NegativeConvertsCorrectly()
	{
		var spacing = new CharacterSpacing(-40f);

		spacing.ValuePoints.Should().Be(-2f);
	}

	// --- ComputeTotalAdjustment ---

	[Fact]
	public void ComputeTotalAdjustment_ZeroChars_ReturnsZero()
	{
		new CharacterSpacing(20f).ComputeTotalAdjustment(0).Should().Be(0f);
	}

	[Fact]
	public void ComputeTotalAdjustment_OneChar_ReturnsZero()
	{
		new CharacterSpacing(20f).ComputeTotalAdjustment(1).Should().Be(0f);
	}

	[Fact]
	public void ComputeTotalAdjustment_TwoChars_ReturnsOneGap()
	{
		new CharacterSpacing(20f).ComputeTotalAdjustment(2).Should().Be(20f);
	}

	[Fact]
	public void ComputeTotalAdjustment_FiveChars_ReturnsFourGaps()
	{
		new CharacterSpacing(10f).ComputeTotalAdjustment(5).Should().Be(40f);
	}

	[Fact]
	public void ComputeTotalAdjustment_NegativeSpacing_ReturnsNegativeTotal()
	{
		new CharacterSpacing(-10f).ComputeTotalAdjustment(3).Should().Be(-20f);
	}

	[Fact]
	public void ComputeTotalAdjustment_NormalSpacing_ReturnsZero()
	{
		CharacterSpacing.Normal.ComputeTotalAdjustment(10).Should().Be(0f);
	}

	// --- Equality ---

	[Fact]
	public void Equality_SameValue_AreEqual()
	{
		var a = new CharacterSpacing(20f);
		var b = new CharacterSpacing(20f);

		a.Should().Be(b);
		(a == b).Should().BeTrue();
	}

	[Fact]
	public void Equality_DifferentValue_AreNotEqual()
	{
		var a = new CharacterSpacing(20f);
		var b = new CharacterSpacing(-20f);

		a.Should().NotBe(b);
	}
}
