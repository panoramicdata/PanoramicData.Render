using AwesomeAssertions;
using Xunit;

namespace PanoramicData.Render.Test;

/// <summary>
/// Integration tests verifying that character formatting types produce correct
/// combined render instructions. Tests cross-cutting interactions between
/// FontProperties, TextDecoration, RunColor, HighlightColor, VerticalTextAlignment,
/// CapsMode, CharacterSpacing, and RunVisibility.
/// </summary>
public class CharacterFormattingIntegrationTests
{
	// --- Font + SuperScript combined ---

	[Fact]
	public void Superscript_ReducesFontSize_And_RaisesBaseline()
	{
		var font = new FontProperties("Calibri", 12f, Bold: false, Italic: false);
		var alignment = VerticalTextAlignment.Superscript;

		var adjustedSize = SuperSubScriptCalculator.ComputeFontSize(font.SizePoints, alignment);
		var offset = SuperSubScriptCalculator.ComputeBaselineOffset(font.SizePoints, alignment);

		adjustedSize.Should().BeApproximately(8f, 0.01f);
		offset.Should().BePositive();
		adjustedSize.Should().BeLessThan(font.SizePoints);
	}

	[Fact]
	public void Subscript_ReducesFontSize_And_LowersBaseline()
	{
		var font = new FontProperties("Arial", 24f, Bold: true, Italic: false);
		var alignment = VerticalTextAlignment.Subscript;

		var adjustedSize = SuperSubScriptCalculator.ComputeFontSize(font.SizePoints, alignment);
		var offset = SuperSubScriptCalculator.ComputeBaselineOffset(font.SizePoints, alignment);

		adjustedSize.Should().BeApproximately(16f, 0.01f);
		offset.Should().BeNegative();
	}

	// --- Font + SmallCaps combined ---

	[Fact]
	public void SmallCaps_LowercaseChar_GetsReducedFontSize()
	{
		var font = FontProperties.FromHalfPoints("Times New Roman", 24, bold: false, italic: true);
		var mode = CapsTransform.Resolve(caps: false, smallCaps: true);

		var text = "Hello";
		var transformed = CapsTransform.TransformText(text, mode);
		var hSize = CapsTransform.ComputeCharacterFontSize('H', font.SizePoints, mode);
		var eSize = CapsTransform.ComputeCharacterFontSize('e', font.SizePoints, mode);

		transformed.Should().Be("HELLO");
		hSize.Should().Be(12f); // uppercase H keeps full size
		eSize.Should().BeApproximately(9.6f, 0.01f); // lowercase e gets 80%
	}

	// --- Color + Highlight combined ---

	[Fact]
	public void RedText_OnYellowHighlight()
	{
		var color = RunColor.FromResolvedColor("FF0000");
		var highlight = HighlightColor.Yellow;
		var highlightHex = HighlightColorMap.ToHexRgb(highlight);

		color.HexRgb.Should().Be("FF0000");
		color.Red.Should().Be(255);
		color.Green.Should().Be(0);
		color.Blue.Should().Be(0);
		highlightHex.Should().Be("FFFF00");
	}

	[Fact]
	public void AutoColor_OnNoHighlight()
	{
		var color = RunColor.FromResolvedColor(null);
		var highlightHex = HighlightColorMap.ToHexRgb(HighlightColor.None);

		color.Should().Be(RunColor.Auto);
		color.HexRgb.Should().Be("000000");
		highlightHex.Should().BeNull();
	}

	// --- Decoration + Bold + Italic ---

	[Fact]
	public void Bold_Italic_WithWavyUnderline_And_Strikethrough()
	{
		var font = new FontProperties("Calibri", 11f, Bold: true, Italic: true);
		var decoration = new TextDecoration(
			Underline: UnderlineStyle.Wave,
			UnderlineColor: "0000FF",
			Strikethrough: true,
			DoubleStrikethrough: false);

		font.Bold.Should().BeTrue();
		font.Italic.Should().BeTrue();
		decoration.HasUnderline.Should().BeTrue();
		decoration.HasStrikethrough.Should().BeTrue();
		decoration.HasAnyDecoration.Should().BeTrue();
		decoration.UnderlineColor.Should().Be("0000FF");
	}

	// --- CharacterSpacing + SmallCaps ---

	[Fact]
	public void ExpandedSpacing_WithSmallCaps_CompoundsEffects()
	{
		var spacing = new CharacterSpacing(40f); // +2pt expanded
		var mode = CapsMode.SmallCaps;
		var text = "abc";
		var transformed = CapsTransform.TransformText(text, mode);

		transformed.Should().Be("ABC");
		spacing.ComputeTotalAdjustment(text.Length).Should().Be(80f); // 2 gaps × 40 twips
		spacing.IsExpanded.Should().BeTrue();
	}

	// --- Vanish hides everything ---

	[Fact]
	public void Vanished_Run_ExcludedFromLayout()
	{
		var font = new FontProperties("Arial", 24f, Bold: true, Italic: false);
		var decoration = new TextDecoration(UnderlineStyle.Thick, Strikethrough: true);
		var color = RunColor.FromResolvedColor("FF0000");

		// Even with all formatting, vanish=true hides it
		RunVisibility.IsVisible(vanish: true, showHiddenText: false).Should().BeFalse();

		// But ShowHiddenText=true reveals it
		RunVisibility.IsVisible(vanish: true, showHiddenText: true).Should().BeTrue();

		// Verify the formatting is still fully valid (not lost)
		font.SizePoints.Should().Be(24f);
		decoration.HasAnyDecoration.Should().BeTrue();
		color.HexRgb.Should().Be("FF0000");
	}

	// --- Full character formatting scenario ---

	[Fact]
	public void FullFormatting_AllPropertiesCombined()
	{
		// Simulate a complex run: bold+italic Calibri 11pt, red text, yellow highlight,
		// wavy underline, condensed spacing, small caps
		var font = FontProperties.FromHalfPoints("Calibri", 22, bold: true, italic: true);
		var color = RunColor.FromResolvedColor("FF0000");
		var highlight = HighlightColor.Yellow;
		var decoration = new TextDecoration(UnderlineStyle.Wave, "0000FF");
		var spacing = new CharacterSpacing(-10f); // condensed
		var capsMode = CapsTransform.Resolve(caps: false, smallCaps: true);
		var alignment = VerticalTextAlignment.Baseline;

		font.SizePoints.Should().Be(11f);
		font.SizeTwips.Should().Be(220f);
		font.Bold.Should().BeTrue();
		font.Italic.Should().BeTrue();

		color.HexRgb.Should().Be("FF0000");
		HighlightColorMap.ToHexRgb(highlight).Should().Be("FFFF00");

		decoration.HasUnderline.Should().BeTrue();
		decoration.Underline.Should().Be(UnderlineStyle.Wave);

		spacing.IsCondensed.Should().BeTrue();
		spacing.ValuePoints.Should().Be(-0.5f);

		capsMode.Should().Be(CapsMode.SmallCaps);
		CapsTransform.TransformText("hello", capsMode).Should().Be("HELLO");

		SuperSubScriptCalculator.ComputeFontSize(font.SizePoints, alignment)
			.Should().Be(11f); // baseline keeps original

		RunVisibility.IsVisible(vanish: false, showHiddenText: false).Should().BeTrue();
	}

	// --- Font size conversions through the pipeline ---

	[Fact]
	public void FontSize_Pipeline_HalfPoints_To_Points_To_Twips()
	{
		// OpenXML: sz="48" (= 24pt)
		var font = FontProperties.FromHalfPoints("Arial", 48);

		font.SizePoints.Should().Be(24f);
		font.SizeTwips.Should().Be(480f);
		font.SizeHalfPoints.Should().Be(48f);

		// Super/subscript reduces to 2/3
		var superSize = SuperSubScriptCalculator.ComputeFontSize(font.SizePoints, VerticalTextAlignment.Superscript);
		superSize.Should().BeApproximately(16f, 0.01f);
		TwipConverter.PointsToTwips(superSize).Should().BeApproximately(320f, 0.1f);
	}

	// --- Default formatting scenario ---

	[Fact]
	public void DefaultFormatting_ProducesMinimalOutput()
	{
		var font = FontProperties.Default;
		var color = RunColor.Auto;
		var highlight = HighlightColor.None;
		var decoration = TextDecoration.None;
		var spacing = CharacterSpacing.Normal;
		var capsMode = CapsMode.None;
		var alignment = VerticalTextAlignment.Baseline;

		font.FamilyName.Should().Be("Calibri");
		font.SizePoints.Should().Be(11f);
		font.Bold.Should().BeFalse();
		font.Italic.Should().BeFalse();

		color.IsAuto.Should().BeTrue();
		HighlightColorMap.ToHexRgb(highlight).Should().BeNull();

		decoration.HasAnyDecoration.Should().BeFalse();
		spacing.IsNormal.Should().BeTrue();
		capsMode.Should().Be(CapsMode.None);

		SuperSubScriptCalculator.ComputeBaselineOffset(font.SizePoints, alignment)
			.Should().Be(0f);
	}

	// --- Double-strikethrough vs single strikethrough ---

	[Fact]
	public void DoubleStrikethrough_Overrides_SingleConceptually()
	{
		var single = new TextDecoration(Strikethrough: true);
		var double_ = new TextDecoration(DoubleStrikethrough: true);
		var both = new TextDecoration(Strikethrough: true, DoubleStrikethrough: true);

		single.HasStrikethrough.Should().BeTrue();
		double_.HasStrikethrough.Should().BeTrue();
		both.HasStrikethrough.Should().BeTrue();

		single.Strikethrough.Should().BeTrue();
		single.DoubleStrikethrough.Should().BeFalse();
		double_.Strikethrough.Should().BeFalse();
		double_.DoubleStrikethrough.Should().BeTrue();
	}
}
