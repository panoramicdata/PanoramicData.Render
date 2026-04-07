using AwesomeAssertions;
using Xunit;

namespace PanoramicData.Render.Test;

public class VerticalTextAlignmentTests
{
	[Fact]
	public void Baseline_HasValue_Zero()
	{
		((int)VerticalTextAlignment.Baseline).Should().Be(0);
	}

	[Fact]
	public void EnumCount_Is3()
	{
		Enum.GetValues<VerticalTextAlignment>().Should().HaveCount(3);
	}

	[Theory]
	[InlineData((int)VerticalTextAlignment.Baseline)]
	[InlineData((int)VerticalTextAlignment.Superscript)]
	[InlineData((int)VerticalTextAlignment.Subscript)]
	public void AllValues_AreDefined(int value)
	{
		Enum.IsDefined((VerticalTextAlignment)value).Should().BeTrue();
	}
}

public class SuperSubScriptCalculatorTests
{
	// --- Constants ---

	[Fact]
	public void DefaultSizeScale_IsApproximatelyTwoThirds()
	{
		SuperSubScriptCalculator.DefaultSizeScale.Should().BeApproximately(2f / 3f, 0.001f);
	}

	[Fact]
	public void DefaultOffsetFraction_IsApproximatelyOneThird()
	{
		SuperSubScriptCalculator.DefaultOffsetFraction.Should().BeApproximately(1f / 3f, 0.001f);
	}

	// --- ComputeFontSize ---

	[Theory]
	[InlineData(12f)]
	[InlineData(24f)]
	[InlineData(11f)]
	public void ComputeFontSize_Baseline_ReturnsParentSize(float parentSize)
	{
		SuperSubScriptCalculator.ComputeFontSize(parentSize, VerticalTextAlignment.Baseline)
			.Should().Be(parentSize);
	}

	[Fact]
	public void ComputeFontSize_Superscript_ReturnsTwoThirds()
	{
		var result = SuperSubScriptCalculator.ComputeFontSize(12f, VerticalTextAlignment.Superscript);

		result.Should().BeApproximately(8f, 0.01f);
	}

	[Fact]
	public void ComputeFontSize_Subscript_ReturnsTwoThirds()
	{
		var result = SuperSubScriptCalculator.ComputeFontSize(12f, VerticalTextAlignment.Subscript);

		result.Should().BeApproximately(8f, 0.01f);
	}

	[Fact]
	public void ComputeFontSize_CustomScale_AppliesCorrectly()
	{
		var result = SuperSubScriptCalculator.ComputeFontSize(12f, VerticalTextAlignment.Superscript, sizeScale: 0.5f);

		result.Should().Be(6f);
	}

	[Fact]
	public void ComputeFontSize_Baseline_IgnoresScale()
	{
		var result = SuperSubScriptCalculator.ComputeFontSize(12f, VerticalTextAlignment.Baseline, sizeScale: 0.5f);

		result.Should().Be(12f);
	}

	// --- ComputeBaselineOffset ---

	[Fact]
	public void ComputeBaselineOffset_Baseline_ReturnsZero()
	{
		SuperSubScriptCalculator.ComputeBaselineOffset(12f, VerticalTextAlignment.Baseline)
			.Should().Be(0f);
	}

	[Fact]
	public void ComputeBaselineOffset_Superscript_ReturnsPositive()
	{
		var result = SuperSubScriptCalculator.ComputeBaselineOffset(12f, VerticalTextAlignment.Superscript);

		result.Should().BeApproximately(4f, 0.01f);
		result.Should().BePositive();
	}

	[Fact]
	public void ComputeBaselineOffset_Subscript_ReturnsNegative()
	{
		var result = SuperSubScriptCalculator.ComputeBaselineOffset(12f, VerticalTextAlignment.Subscript);

		result.Should().BeApproximately(-4f, 0.01f);
		result.Should().BeNegative();
	}

	[Fact]
	public void ComputeBaselineOffset_CustomFraction_AppliesCorrectly()
	{
		var result = SuperSubScriptCalculator.ComputeBaselineOffset(
			12f, VerticalTextAlignment.Superscript, offsetFraction: 0.25f);

		result.Should().Be(3f);
	}

	[Fact]
	public void ComputeBaselineOffset_Baseline_IgnoresFraction()
	{
		var result = SuperSubScriptCalculator.ComputeBaselineOffset(
			12f, VerticalTextAlignment.Baseline, offsetFraction: 0.5f);

		result.Should().Be(0f);
	}

	// --- Symmetry ---

	[Theory]
	[InlineData(10f)]
	[InlineData(12f)]
	[InlineData(24f)]
	public void Offsets_AreSymmetric(float parentSize)
	{
		var superOffset = SuperSubScriptCalculator.ComputeBaselineOffset(
			parentSize, VerticalTextAlignment.Superscript);
		var subOffset = SuperSubScriptCalculator.ComputeBaselineOffset(
			parentSize, VerticalTextAlignment.Subscript);

		superOffset.Should().Be(-subOffset);
	}

	[Theory]
	[InlineData(10f)]
	[InlineData(12f)]
	[InlineData(24f)]
	public void FontSizes_AreSameForSuperAndSub(float parentSize)
	{
		var superSize = SuperSubScriptCalculator.ComputeFontSize(
			parentSize, VerticalTextAlignment.Superscript);
		var subSize = SuperSubScriptCalculator.ComputeFontSize(
			parentSize, VerticalTextAlignment.Subscript);

		superSize.Should().Be(subSize);
	}

	// --- Unknown alignment (default branch) ---

	[Fact]
	public void ComputeBaselineOffset_UnknownAlignment_ReturnsZero()
	{
		SuperSubScriptCalculator.ComputeBaselineOffset(12f, (VerticalTextAlignment)999)
			.Should().Be(0f);
	}

	// --- Edge cases ---

	[Fact]
	public void ComputeFontSize_ZeroParent_ReturnsZero()
	{
		SuperSubScriptCalculator.ComputeFontSize(0f, VerticalTextAlignment.Superscript)
			.Should().Be(0f);
	}

	[Fact]
	public void ComputeBaselineOffset_ZeroParent_ReturnsZero()
	{
		SuperSubScriptCalculator.ComputeBaselineOffset(0f, VerticalTextAlignment.Superscript)
			.Should().Be(0f);
	}
}
