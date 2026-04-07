using AwesomeAssertions;
using Xunit;

using PanoramicData.Render;

namespace PanoramicData.Render.Test;

public class ParagraphBorderTests
{
	// ===================================================================
	// ParagraphBorder record defaults
	// ===================================================================

	[Fact]
	public void None_AllDefaults()
	{
		var border = ParagraphBorder.None;

		border.Style.Should().Be(BorderStyle.None);
		border.WidthEighthsOfPoint.Should().Be(0);
		border.SpacingPoints.Should().Be(0f);
		border.Color.Should().BeNull();
		border.IsVisible.Should().BeFalse();
	}

	[Fact]
	public void Default_Constructor_MatchesNone()
	{
		var border = new ParagraphBorder();

		border.Should().Be(ParagraphBorder.None);
	}

	[Fact]
	public void ExplicitValues_Stored()
	{
		var border = new ParagraphBorder(BorderStyle.Single, 8, 1f, "FF0000");

		border.Style.Should().Be(BorderStyle.Single);
		border.WidthEighthsOfPoint.Should().Be(8);
		border.SpacingPoints.Should().Be(1f);
		border.Color.Should().Be("FF0000");
	}

	// ===================================================================
	// IsVisible
	// ===================================================================

	[Fact]
	public void IsVisible_None_ReturnsFalse()
	{
		var border = new ParagraphBorder(BorderStyle.None);
		border.IsVisible.Should().BeFalse();
	}

	[Fact]
	public void IsVisible_Single_ReturnsTrue()
	{
		var border = new ParagraphBorder(BorderStyle.Single);
		border.IsVisible.Should().BeTrue();
	}

	[Theory]
	[InlineData(1)]  // Single
	[InlineData(2)]  // Double
	[InlineData(3)]  // Dotted
	[InlineData(4)]  // Dashed
	[InlineData(7)]  // Triple
	[InlineData(8)]  // Thick
	[InlineData(15)] // Shadow
	public void IsVisible_NonNoneStyle_ReturnsTrue(int styleValue)
	{
		var border = new ParagraphBorder((BorderStyle)styleValue);
		border.IsVisible.Should().BeTrue();
	}

	// ===================================================================
	// GetWidthTwips
	// ===================================================================

	[Fact]
	public void GetWidthTwips_Zero_ReturnsZero()
	{
		var border = new ParagraphBorder(WidthEighthsOfPoint: 0);
		border.GetWidthTwips().Should().Be(0f);
	}

	[Fact]
	public void GetWidthTwips_8_Returns20()
	{
		// 8/8 point = 1 point = 20 twips
		var border = new ParagraphBorder(WidthEighthsOfPoint: 8);
		border.GetWidthTwips().Should().Be(20f);
	}

	[Fact]
	public void GetWidthTwips_4_Returns10()
	{
		// 4/8 point = 0.5 point = 10 twips
		var border = new ParagraphBorder(WidthEighthsOfPoint: 4);
		border.GetWidthTwips().Should().Be(10f);
	}

	[Fact]
	public void GetWidthTwips_12_Returns30()
	{
		// 12/8 point = 1.5 point = 30 twips
		var border = new ParagraphBorder(WidthEighthsOfPoint: 12);
		border.GetWidthTwips().Should().Be(30f);
	}

	// ===================================================================
	// GetSpacingTwips
	// ===================================================================

	[Fact]
	public void GetSpacingTwips_Zero_ReturnsZero()
	{
		var border = new ParagraphBorder(SpacingPoints: 0f);
		border.GetSpacingTwips().Should().Be(0f);
	}

	[Fact]
	public void GetSpacingTwips_1Point_Returns20()
	{
		var border = new ParagraphBorder(SpacingPoints: 1f);
		border.GetSpacingTwips().Should().Be(20f);
	}

	[Fact]
	public void GetSpacingTwips_4Points_Returns80()
	{
		var border = new ParagraphBorder(SpacingPoints: 4f);
		border.GetSpacingTwips().Should().Be(80f);
	}

	// ===================================================================
	// Record equality
	// ===================================================================

	[Fact]
	public void Equality_SameValues_AreEqual()
	{
		var a = new ParagraphBorder(BorderStyle.Double, 8, 2f, "0000FF");
		var b = new ParagraphBorder(BorderStyle.Double, 8, 2f, "0000FF");
		a.Should().Be(b);
	}

	[Fact]
	public void Equality_DifferentStyle_NotEqual()
	{
		var a = new ParagraphBorder(BorderStyle.Single);
		var b = new ParagraphBorder(BorderStyle.Double);
		a.Should().NotBe(b);
	}

	[Fact]
	public void Equality_DifferentColor_NotEqual()
	{
		var a = new ParagraphBorder(Color: "FF0000");
		var b = new ParagraphBorder(Color: "0000FF");
		a.Should().NotBe(b);
	}
}
