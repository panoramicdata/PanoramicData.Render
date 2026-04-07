using AwesomeAssertions;
using Xunit;

using PanoramicData.Render;

namespace PanoramicData.Render.Test;

public class ParagraphShadingTests
{
	// ===================================================================
	// Defaults
	// ===================================================================

	[Fact]
	public void None_AllDefaults()
	{
		var shading = ParagraphShading.None;

		shading.Pattern.Should().Be(ShadingPattern.Clear);
		shading.PatternColor.Should().BeNull();
		shading.FillColor.Should().BeNull();
		shading.HasVisibleShading.Should().BeFalse();
	}

	[Fact]
	public void Default_MatchesNone()
	{
		var shading = new ParagraphShading();
		shading.Should().Be(ParagraphShading.None);
	}

	// ===================================================================
	// HasVisibleShading
	// ===================================================================

	[Fact]
	public void HasVisibleShading_ClearNoFill_ReturnsFalse()
	{
		var shading = new ParagraphShading(ShadingPattern.Clear);
		shading.HasVisibleShading.Should().BeFalse();
	}

	[Fact]
	public void HasVisibleShading_ClearWithFill_ReturnsTrue()
	{
		var shading = new ParagraphShading(ShadingPattern.Clear, FillColor: "FFFF00");
		shading.HasVisibleShading.Should().BeTrue();
	}

	[Fact]
	public void HasVisibleShading_Solid_ReturnsTrue()
	{
		var shading = new ParagraphShading(ShadingPattern.Solid, PatternColor: "FF0000");
		shading.HasVisibleShading.Should().BeTrue();
	}

	[Fact]
	public void HasVisibleShading_PatternWithoutFill_ReturnsTrue()
	{
		var shading = new ParagraphShading(ShadingPattern.HorizontalStripe);
		shading.HasVisibleShading.Should().BeTrue();
	}

	// ===================================================================
	// GetEffectiveBackgroundColor
	// ===================================================================

	[Fact]
	public void GetEffectiveBackgroundColor_Clear_ReturnsFillColor()
	{
		var shading = new ParagraphShading(ShadingPattern.Clear, FillColor: "FFFF00");
		shading.GetEffectiveBackgroundColor().Should().Be("FFFF00");
	}

	[Fact]
	public void GetEffectiveBackgroundColor_ClearNoFill_ReturnsNull()
	{
		var shading = new ParagraphShading(ShadingPattern.Clear);
		shading.GetEffectiveBackgroundColor().Should().BeNull();
	}

	[Fact]
	public void GetEffectiveBackgroundColor_Solid_ReturnsPatternColor()
	{
		var shading = new ParagraphShading(ShadingPattern.Solid, PatternColor: "FF0000", FillColor: "00FF00");
		shading.GetEffectiveBackgroundColor().Should().Be("FF0000");
	}

	[Fact]
	public void GetEffectiveBackgroundColor_Solid_NoPatternColor_FallsBackToFill()
	{
		var shading = new ParagraphShading(ShadingPattern.Solid, FillColor: "00FF00");
		shading.GetEffectiveBackgroundColor().Should().Be("00FF00");
	}

	[Fact]
	public void GetEffectiveBackgroundColor_Solid_NeitherColor_ReturnsNull()
	{
		var shading = new ParagraphShading(ShadingPattern.Solid);
		shading.GetEffectiveBackgroundColor().Should().BeNull();
	}

	[Fact]
	public void GetEffectiveBackgroundColor_Pattern_ReturnsFillColor()
	{
		var shading = new ParagraphShading(ShadingPattern.Percent25, PatternColor: "000000", FillColor: "FFFFFF");
		shading.GetEffectiveBackgroundColor().Should().Be("FFFFFF");
	}

	[Fact]
	public void GetEffectiveBackgroundColor_Pattern_NoFill_ReturnsNull()
	{
		var shading = new ParagraphShading(ShadingPattern.HorizontalStripe, PatternColor: "000000");
		shading.GetEffectiveBackgroundColor().Should().BeNull();
	}

	// ===================================================================
	// Record equality
	// ===================================================================

	[Fact]
	public void Equality_SameValues_AreEqual()
	{
		var a = new ParagraphShading(ShadingPattern.Solid, "FF0000", "00FF00");
		var b = new ParagraphShading(ShadingPattern.Solid, "FF0000", "00FF00");
		a.Should().Be(b);
	}

	[Fact]
	public void Equality_DifferentValues_NotEqual()
	{
		var a = new ParagraphShading(ShadingPattern.Solid, "FF0000");
		var b = new ParagraphShading(ShadingPattern.Clear, "FF0000");
		a.Should().NotBe(b);
	}
}
