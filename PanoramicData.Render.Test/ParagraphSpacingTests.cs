using AwesomeAssertions;
using Xunit;

using PanoramicData.Render;

namespace PanoramicData.Render.Test;

public class ParagraphSpacingTests
{
	// ===================================================================
	// Defaults and None
	// ===================================================================

	[Fact]
	public void None_AllZeroDefaults()
	{
		var spacing = ParagraphSpacing.None;

		spacing.SpaceBefore.Should().Be(0f);
		spacing.SpaceAfter.Should().Be(0f);
		spacing.LineSpacingTwips.Should().Be(0f);
		spacing.LineRule.Should().BeNull();
		spacing.EffectiveLineRule.Should().Be(LineSpacingRule.Auto);
	}

	[Fact]
	public void Default_AllZero()
	{
		var spacing = new ParagraphSpacing();

		spacing.SpaceBefore.Should().Be(0f);
		spacing.SpaceAfter.Should().Be(0f);
		spacing.LineSpacingTwips.Should().Be(0f);
		spacing.LineRule.Should().BeNull();
	}

	// ===================================================================
	// EffectiveLineRule
	// ===================================================================

	[Fact]
	public void EffectiveLineRule_NullDefaultsToAuto()
	{
		var spacing = new ParagraphSpacing();
		spacing.EffectiveLineRule.Should().Be(LineSpacingRule.Auto);
	}

	[Fact]
	public void EffectiveLineRule_ExplicitExact()
	{
		var spacing = new ParagraphSpacing(LineRule: LineSpacingRule.Exact);
		spacing.EffectiveLineRule.Should().Be(LineSpacingRule.Exact);
	}

	[Fact]
	public void EffectiveLineRule_ExplicitAtLeast()
	{
		var spacing = new ParagraphSpacing(LineRule: LineSpacingRule.AtLeast);
		spacing.EffectiveLineRule.Should().Be(LineSpacingRule.AtLeast);
	}

	// ===================================================================
	// GetLineSpacingMultiplier
	// ===================================================================

	[Fact]
	public void GetLineSpacingMultiplier_Zero_Returns1()
	{
		var spacing = new ParagraphSpacing(LineSpacingTwips: 0f);
		spacing.GetLineSpacingMultiplier().Should().Be(1f);
	}

	[Fact]
	public void GetLineSpacingMultiplier_Negative_Returns1()
	{
		var spacing = new ParagraphSpacing(LineSpacingTwips: -100f);
		spacing.GetLineSpacingMultiplier().Should().Be(1f);
	}

	[Fact]
	public void GetLineSpacingMultiplier_SingleSpacing240_Returns1()
	{
		var spacing = new ParagraphSpacing(LineSpacingTwips: 240f);
		spacing.GetLineSpacingMultiplier().Should().Be(1f);
	}

	[Fact]
	public void GetLineSpacingMultiplier_OneAndHalf360_Returns1Point5()
	{
		var spacing = new ParagraphSpacing(LineSpacingTwips: 360f);
		spacing.GetLineSpacingMultiplier().Should().Be(1.5f);
	}

	[Fact]
	public void GetLineSpacingMultiplier_Double480_Returns2()
	{
		var spacing = new ParagraphSpacing(LineSpacingTwips: 480f);
		spacing.GetLineSpacingMultiplier().Should().Be(2f);
	}

	[Fact]
	public void GetLineSpacingMultiplier_Custom276_ReturnsCorrectValue()
	{
		// Word uses 276 twips for slightly-more-than-single spacing
		var spacing = new ParagraphSpacing(LineSpacingTwips: 276f);
		spacing.GetLineSpacingMultiplier().Should().BeApproximately(1.15f, 0.001f);
	}

	// ===================================================================
	// ComputeLineHeight — Auto rule
	// ===================================================================

	[Fact]
	public void ComputeLineHeight_Auto_ZeroLineSpacing_ReturnsNatural()
	{
		var spacing = new ParagraphSpacing(); // Auto, LineSpacingTwips=0
		spacing.ComputeLineHeight(300f).Should().Be(300f); // 300 * 1.0
	}

	[Fact]
	public void ComputeLineHeight_Auto_SingleSpacing_ReturnsNatural()
	{
		var spacing = new ParagraphSpacing(LineSpacingTwips: 240f);
		spacing.ComputeLineHeight(300f).Should().Be(300f); // 300 * 1.0
	}

	[Fact]
	public void ComputeLineHeight_Auto_DoubleSpacing_ReturnsDouble()
	{
		var spacing = new ParagraphSpacing(LineSpacingTwips: 480f);
		spacing.ComputeLineHeight(300f).Should().Be(600f); // 300 * 2.0
	}

	[Fact]
	public void ComputeLineHeight_Auto_OneAndHalf_ReturnsCorrect()
	{
		var spacing = new ParagraphSpacing(LineSpacingTwips: 360f);
		spacing.ComputeLineHeight(300f).Should().Be(450f); // 300 * 1.5
	}

	// ===================================================================
	// ComputeLineHeight — Exact rule
	// ===================================================================

	[Fact]
	public void ComputeLineHeight_Exact_ReturnsSpecifiedValue()
	{
		var spacing = new ParagraphSpacing(LineSpacingTwips: 200f, LineRule: LineSpacingRule.Exact);
		spacing.ComputeLineHeight(300f).Should().Be(200f); // Exactly 200
	}

	[Fact]
	public void ComputeLineHeight_Exact_SmallerThanNatural_StillReturnsExact()
	{
		var spacing = new ParagraphSpacing(LineSpacingTwips: 100f, LineRule: LineSpacingRule.Exact);
		spacing.ComputeLineHeight(300f).Should().Be(100f); // Content may be clipped
	}

	[Fact]
	public void ComputeLineHeight_Exact_LargerThanNatural_ReturnsExact()
	{
		var spacing = new ParagraphSpacing(LineSpacingTwips: 500f, LineRule: LineSpacingRule.Exact);
		spacing.ComputeLineHeight(300f).Should().Be(500f);
	}

	[Fact]
	public void ComputeLineHeight_Exact_ZeroLineSpacing_FallsBackToNatural()
	{
		var spacing = new ParagraphSpacing(LineSpacingTwips: 0f, LineRule: LineSpacingRule.Exact);
		spacing.ComputeLineHeight(300f).Should().Be(300f);
	}

	// ===================================================================
	// ComputeLineHeight — AtLeast rule
	// ===================================================================

	[Fact]
	public void ComputeLineHeight_AtLeast_NaturalSmaller_ReturnsMinimum()
	{
		var spacing = new ParagraphSpacing(LineSpacingTwips: 400f, LineRule: LineSpacingRule.AtLeast);
		spacing.ComputeLineHeight(300f).Should().Be(400f); // Max(300, 400) = 400
	}

	[Fact]
	public void ComputeLineHeight_AtLeast_NaturalLarger_ReturnsNatural()
	{
		var spacing = new ParagraphSpacing(LineSpacingTwips: 200f, LineRule: LineSpacingRule.AtLeast);
		spacing.ComputeLineHeight(300f).Should().Be(300f); // Max(300, 200) = 300
	}

	[Fact]
	public void ComputeLineHeight_AtLeast_Equal_ReturnsEither()
	{
		var spacing = new ParagraphSpacing(LineSpacingTwips: 300f, LineRule: LineSpacingRule.AtLeast);
		spacing.ComputeLineHeight(300f).Should().Be(300f); // Max(300, 300) = 300
	}

	[Fact]
	public void ComputeLineHeight_AtLeast_ZeroLineSpacing_ReturnsNatural()
	{
		var spacing = new ParagraphSpacing(LineSpacingTwips: 0f, LineRule: LineSpacingRule.AtLeast);
		spacing.ComputeLineHeight(300f).Should().Be(300f);
	}

	// ===================================================================
	// ComputeParagraphHeight
	// ===================================================================

	[Fact]
	public void ComputeParagraphHeight_ZeroLines_ReturnsZero()
	{
		var spacing = new ParagraphSpacing(SpaceBefore: 100f, SpaceAfter: 200f, LineSpacingTwips: 240f);
		spacing.ComputeParagraphHeight(0, 300f).Should().Be(0f);
	}

	[Fact]
	public void ComputeParagraphHeight_OneLine_IncludesBeforeAndAfter()
	{
		var spacing = new ParagraphSpacing(SpaceBefore: 100f, SpaceAfter: 200f);
		// 1 line * 300 natural + 100 before + 200 after = 600
		spacing.ComputeParagraphHeight(1, 300f).Should().Be(600f);
	}

	[Fact]
	public void ComputeParagraphHeight_ThreeLines_DoubleSpacing()
	{
		var spacing = new ParagraphSpacing(
			SpaceBefore: 120f,
			SpaceAfter: 120f,
			LineSpacingTwips: 480f); // Double
		// 3 lines * 600 (300*2.0) + 120 before + 120 after = 2040
		spacing.ComputeParagraphHeight(3, 300f).Should().Be(2040f);
	}

	[Fact]
	public void ComputeParagraphHeight_Exact_IgnoresNatural()
	{
		var spacing = new ParagraphSpacing(
			SpaceBefore: 50f,
			SpaceAfter: 50f,
			LineSpacingTwips: 200f,
			LineRule: LineSpacingRule.Exact);
		// 2 lines * 200 + 50 + 50 = 500
		spacing.ComputeParagraphHeight(2, 300f).Should().Be(500f);
	}

	[Fact]
	public void ComputeParagraphHeight_NegativeLines_ReturnsZero()
	{
		var spacing = new ParagraphSpacing(SpaceBefore: 100f, SpaceAfter: 200f);
		spacing.ComputeParagraphHeight(-1, 300f).Should().Be(0f);
	}

	[Fact]
	public void ComputeParagraphHeight_NoSpacing_JustLines()
	{
		var spacing = new ParagraphSpacing();
		// 4 lines * 300 natural + 0 + 0 = 1200
		spacing.ComputeParagraphHeight(4, 300f).Should().Be(1200f);
	}

	// ===================================================================
	// Record equality
	// ===================================================================

	[Fact]
	public void RecordEquality_SameValues_AreEqual()
	{
		var a = new ParagraphSpacing(100f, 200f, 360f, LineSpacingRule.AtLeast);
		var b = new ParagraphSpacing(100f, 200f, 360f, LineSpacingRule.AtLeast);
		a.Should().Be(b);
	}

	[Fact]
	public void RecordEquality_DifferentValues_AreNotEqual()
	{
		var a = new ParagraphSpacing(100f, 200f, 360f, LineSpacingRule.AtLeast);
		var b = new ParagraphSpacing(100f, 200f, 360f, LineSpacingRule.Exact);
		a.Should().NotBe(b);
	}
}
