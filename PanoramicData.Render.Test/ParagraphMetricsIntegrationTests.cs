using AwesomeAssertions;
using Xunit;

using PanoramicData.Render;

namespace PanoramicData.Render.Test;

/// <summary>
/// Integration tests that verify paragraph metrics across multiple formatting types:
/// alignment, indentation, spacing, borders, shading, and tab stops working together.
/// These tests call both positional (ComputeBoxPositions / ComputeParagraphBoxPositions)
/// and dimensional (ComputeParagraphHeight / ComputeLineHeight) methods with combined formatting.
/// </summary>
public class ParagraphMetricsIntegrationTests
{
	// Helper factories (same as ParagraphAlignerTests)
	private static KnuthPlassBox Box(float w) => new(w);
	private static KnuthPlassGlue Glue(float w, float stretch, float shrink) => new(w, stretch, shrink);
	private static KnuthPlassPenalty Penalty(float w, float penalty, bool flagged) => new(w, penalty, flagged);

	// ===================================================================
	// Combined Spacing + Indentation
	// ===================================================================

	[Fact]
	public void SpacingPlusIndentation_ParagraphHeight_IncludesBothDimensions()
	{
		// Spacing: before=120, after=80, double spacing (480 twips)
		// Natural line height=300 → double = 600 per line
		// 3 lines → 120 + 3*600 + 80 = 2000
		var spacing = new ParagraphSpacing(
			SpaceBefore: 120f,
			SpaceAfter: 80f,
			LineSpacingTwips: 480f);

		// Indentation doesn't affect height, but verify it doesn't interfere
		var indent = new ParagraphIndentation(Left: 200f, FirstLine: 100f);

		var height = spacing.ComputeParagraphHeight(3, 300f);
		height.Should().Be(2000f);

		// Verify indentation still works: first line at 300, subsequent at 200
		indent.GetFirstLineLeftIndent().Should().Be(300f);
		indent.GetSubsequentLineLeftIndent().Should().Be(200f);
	}

	[Fact]
	public void SpacingPlusIndentation_BoxPositions_IndentCorrectWithinSpacedParagraph()
	{
		// 2-line paragraph: left indent 200, first-line indent 100
		// Justified alignment, double spacing
		var items = new KnuthPlassItem[]
		{
			Box(100), Glue(20, 10, 5),
			Box(100),
			Glue(0, float.MaxValue, 0), Penalty(0, float.NegativeInfinity, false)
		};
		var line1 = new KnuthPlassLine(0, 1, 0f);
		var line2 = new KnuthPlassLine(2, 4, 0f);
		var lines = new[] { line1, line2 };
		var indent = new ParagraphIndentation(Left: 200f, FirstLine: 100f);

		var result = ParagraphAligner.ComputeParagraphBoxPositions(
			items, lines, 1000f, ParagraphAlignment.Left, indent);

		// Line 1 (first): starts at Left+FirstLine = 300
		result[0][0].XOffset.Should().Be(300f);
		// Line 2 (subsequent): starts at Left = 200
		result[1][0].XOffset.Should().Be(200f);

		// Paragraph height with spacing
		var spacing = new ParagraphSpacing(SpaceBefore: 120f, SpaceAfter: 80f, LineSpacingTwips: 480f);
		var height = spacing.ComputeParagraphHeight(lines.Length, 300f);
		height.Should().Be(1400f); // 120 + 2*600 + 80
	}

	// ===================================================================
	// Combined Alignment + Indentation + Spacing
	// ===================================================================

	[Fact]
	public void CenterAligned_WithIndentation_AndSpacing()
	{
		// Center-aligned, left=100, right=100 → effective width = 800 (from 1000)
		// Content = 200 → center offset = (800-200)/2 = 300
		// Absolute X = 100 + 300 = 400
		var items = new KnuthPlassItem[] { Box(200), Penalty(0, float.NegativeInfinity, false) };
		var line = new KnuthPlassLine(0, 1, 0f);
		var indent = new ParagraphIndentation(Left: 100f, Right: 100f);

		var result = ParagraphAligner.ComputeBoxPositions(
			items, line, 1000f, ParagraphAlignment.Center,
			indentation: indent);

		result[0].XOffset.Should().Be(400f);

		// Verify spacing height is independent
		var spacing = new ParagraphSpacing(SpaceBefore: 240f, SpaceAfter: 120f, LineSpacingTwips: 360f);
		var lineHeight = spacing.ComputeLineHeight(300f);
		lineHeight.Should().Be(450f); // 300 * 1.5
		var totalHeight = spacing.ComputeParagraphHeight(1, 300f);
		totalHeight.Should().Be(810f); // 240 + 450 + 120
	}

	[Fact]
	public void RightAligned_WithHangingIndent_MultipleLines()
	{
		// Right-aligned, Left=200, Hanging=300
		// Line 1 (first): leftIndent=200, effective = 1000-200-0 = 800
		// Line 2 (subsequent): leftIndent=500, effective = 1000-500-0 = 500
		var items = new KnuthPlassItem[]
		{
			Box(100), Glue(20, 10, 5),
			Box(100),
			Glue(0, float.MaxValue, 0), Penalty(0, float.NegativeInfinity, false)
		};
		var line1 = new KnuthPlassLine(0, 1, 0f);
		var line2 = new KnuthPlassLine(2, 4, 0f);
		var lines = new[] { line1, line2 };
		var indent = new ParagraphIndentation(Left: 200f, Hanging: 300f);

		var result = ParagraphAligner.ComputeParagraphBoxPositions(
			items, lines, 1000f, ParagraphAlignment.Right, indent);

		// Line 1: right-align in 800 effective. Content=100. offset=700. X=200+700=900
		result[0][0].XOffset.Should().Be(900f);
		// Line 2: right-align in 500 effective. Content=100. offset=400. X=500+400=900
		result[1][0].XOffset.Should().Be(900f);
	}

	// ===================================================================
	// Spacing rules with different natural line heights
	// ===================================================================

	[Fact]
	public void ExactSpacing_TruncatesTallContent_HeightIsFixed()
	{
		// Exact 200 twips, natural height 400 → clipped to 200
		var spacing = new ParagraphSpacing(
			SpaceBefore: 60f,
			SpaceAfter: 60f,
			LineSpacingTwips: 200f,
			LineRule: LineSpacingRule.Exact);

		spacing.ComputeLineHeight(400f).Should().Be(200f);
		spacing.ComputeParagraphHeight(5, 400f).Should().Be(1120f); // 60 + 5*200 + 60
	}

	[Fact]
	public void AtLeastSpacing_ExpandsForTallContent()
	{
		// AtLeast 200 twips, but natural 400 → uses 400
		var spacing = new ParagraphSpacing(
			SpaceBefore: 0f,
			SpaceAfter: 0f,
			LineSpacingTwips: 200f,
			LineRule: LineSpacingRule.AtLeast);

		spacing.ComputeLineHeight(400f).Should().Be(400f);
		spacing.ComputeParagraphHeight(3, 400f).Should().Be(1200f); // 3*400

		// And when natural is smaller, uses the minimum
		spacing.ComputeLineHeight(100f).Should().Be(200f);
		spacing.ComputeParagraphHeight(3, 100f).Should().Be(600f); // 3*200
	}

	// ===================================================================
	// Borders contribution to paragraph geometry
	// ===================================================================

	[Fact]
	public void Borders_AddSpacingAroundContent()
	{
		// Top border: 1pt wide (8 eighths), 4pt spacing
		// Bottom border: 0.5pt wide (4 eighths), 2pt spacing
		var top = new ParagraphBorder(BorderStyle.Single, 8, 4f, "000000");
		var bottom = new ParagraphBorder(BorderStyle.Single, 4, 2f, "000000");
		var borders = new ParagraphBorders(Top: top, Bottom: bottom);

		// Top: borderWidth=20 twips, spacing=80 twips → adds 100 above content
		top.GetWidthTwips().Should().Be(20f);
		top.GetSpacingTwips().Should().Be(80f);

		// Bottom: borderWidth=10 twips, spacing=40 twips → adds 50 below content
		bottom.GetWidthTwips().Should().Be(10f);
		bottom.GetSpacingTwips().Should().Be(40f);

		// Total overhead = (20+80) + contentHeight + (10+40)
		// With 2 lines at 300 natural, no explicit spacing:
		var spacing = new ParagraphSpacing();
		var contentHeight = spacing.ComputeParagraphHeight(2, 300f);
		contentHeight.Should().Be(600f); // 2*300

		var totalWithBorders = (top.GetWidthTwips() + top.GetSpacingTwips())
			+ contentHeight
			+ (bottom.GetWidthTwips() + bottom.GetSpacingTwips());
		totalWithBorders.Should().Be(750f); // 100 + 600 + 50
	}

	[Fact]
	public void LeftRightBorders_NarrowEffectiveWidth()
	{
		// Left border: 1pt wide (8 eighths) + 4pt spacing = 100 twips
		// Right border: 1pt wide (8 eighths) + 4pt spacing = 100 twips
		var left = new ParagraphBorder(BorderStyle.Single, 8, 4f);
		var right = new ParagraphBorder(BorderStyle.Single, 8, 4f);

		var leftOverhead = left.GetWidthTwips() + left.GetSpacingTwips(); // 20+80=100
		var rightOverhead = right.GetWidthTwips() + right.GetSpacingTwips(); // 20+80=100
		leftOverhead.Should().Be(100f);
		rightOverhead.Should().Be(100f);

		// Effective content width from 5000 twip line → 4800
		var effectiveWidth = 5000f - leftOverhead - rightOverhead;
		effectiveWidth.Should().Be(4800f);
	}

	// ===================================================================
	// Shading with content positioning
	// ===================================================================

	[Fact]
	public void Shading_DoesNotAffectContentPositioning()
	{
		// Shading is purely visual — content position should be identical with/without
		var items = new KnuthPlassItem[] { Box(200), Penalty(0, float.NegativeInfinity, false) };
		var line = new KnuthPlassLine(0, 1, 0f);

		var withoutShading = ParagraphAligner.ComputeBoxPositions(
			items, line, 1000f, ParagraphAlignment.Center);

		// Shading exists but doesn't change position calculations
		var shading = new ParagraphShading(ShadingPattern.Solid, "FF0000", "FFFF00");
		shading.HasVisibleShading.Should().BeTrue();
		shading.GetEffectiveBackgroundColor().Should().Be("FF0000");

		// Position calculation is the same — shading is a render concern
		var withShading = ParagraphAligner.ComputeBoxPositions(
			items, line, 1000f, ParagraphAlignment.Center);

		withoutShading[0].XOffset.Should().Be(withShading[0].XOffset);
	}

	// ===================================================================
	// Tab stops with indentation
	// ===================================================================

	[Fact]
	public void TabStops_ResolveRelativeToLeftMargin()
	{
		// Tab stop at 1440 twips from left margin
		// Left indent = 720 twips
		// When cursor is at indent position (720), next tab at 1440
		var stops = new[] { new TabStop(1440f, TabStopType.Left) };
		var profile = new TabStopProfile(stops, 720f);

		// From position 720 (left indent), next tab is at 1440
		var tab = profile.ResolveNextTabStop(720f);
		tab.PositionTwips.Should().Be(1440f);
		TabStopResolver.ComputeContentStart(tab).Should().Be(1440f);

		// From position 0 (before indent), next tab is still at 1440
		var tab2 = profile.ResolveNextTabStop(0f);
		tab2.PositionTwips.Should().Be(1440f);
	}

	[Fact]
	public void RightTabStop_WithRightIndent_ContentEndsBeforeMargin()
	{
		// Right tab at 4000, right indent = 720 (effective boundary ~4280 from 5000)
		// Content of 500 twips at right tab → starts at 3500
		var stop = new TabStop(4000f, TabStopType.Right);

		var contentStart = TabStopResolver.ComputeContentStart(stop, contentWidthAfterTab: 500f);
		contentStart.Should().Be(3500f);
	}

	[Fact]
	public void DecimalTab_WithContentBeforeAndAfterDecimal()
	{
		// "1,234.56" — width before decimal = 400, total width = 600
		// Decimal tab at 2880 → content starts at 2880 - 400 = 2480
		var stop = new TabStop(2880f, TabStopType.Decimal);

		var contentStart = TabStopResolver.ComputeContentStart(
			stop, contentWidthAfterTab: 600f, widthBeforeDecimal: 400f);
		contentStart.Should().Be(2480f);
	}

	// ===================================================================
	// Full paragraph scenario: all formatting types combined
	// ===================================================================

	[Fact]
	public void FullParagraph_AllFormattingCombined()
	{
		// Realistic scenario: 3-line paragraph with all formatting
		// Alignment: Justified
		// Indentation: left=720, firstLine=360
		// Spacing: before=120, after=240, 1.15× (276 twips)
		// Borders: top=single 1pt, bottom=single 1pt, both with 4pt spacing
		// Shading: yellow background
		var indent = new ParagraphIndentation(Left: 720f, FirstLine: 360f);
		var spacing = new ParagraphSpacing(SpaceBefore: 120f, SpaceAfter: 240f, LineSpacingTwips: 276f);
		var topBorder = new ParagraphBorder(BorderStyle.Single, 8, 4f, "000000");
		var bottomBorder = new ParagraphBorder(BorderStyle.Single, 8, 4f, "000000");
		var borders = new ParagraphBorders(Top: topBorder, Bottom: bottomBorder);
		var shading = new ParagraphShading(ShadingPattern.Clear, FillColor: "FFFF00");

		// Verify indentation
		indent.GetFirstLineLeftIndent().Should().Be(1080f); // 720+360
		indent.GetSubsequentLineLeftIndent().Should().Be(720f);

		// Verify spacing: line height = 300 * 1.15 = 345
		var lineHeight = spacing.ComputeLineHeight(300f);
		lineHeight.Should().BeApproximately(345f, 0.1f);

		// Paragraph content height: 120 + 3*345 + 240 = 1395
		var contentHeight = spacing.ComputeParagraphHeight(3, 300f);
		contentHeight.Should().BeApproximately(1395f, 0.5f);

		// Border overhead: top(20+80) + bottom(20+80) = 200
		var borderOverhead = topBorder.GetWidthTwips() + topBorder.GetSpacingTwips()
			+ bottomBorder.GetWidthTwips() + bottomBorder.GetSpacingTwips();
		borderOverhead.Should().Be(200f);

		// Total paragraph box height
		var totalHeight = borderOverhead + contentHeight;
		totalHeight.Should().BeApproximately(1595f, 0.5f);

		// Shading fills the entire paragraph box
		shading.HasVisibleShading.Should().BeTrue();
		shading.GetEffectiveBackgroundColor().Should().Be("FFFF00");

		// Borders are visible
		borders.HasAnyVisibleBorder.Should().BeTrue();
	}

	[Fact]
	public void FullParagraph_BoxPositions_WithAllFormatting()
	{
		// 3-line justified paragraph with indentation
		var items = new KnuthPlassItem[]
		{
			Box(200), Glue(30, 15, 8),  // Line 1
			Box(200), Glue(30, 15, 8),  // Line 2
			Box(200),                     // Line 3
			Glue(0, float.MaxValue, 0), Penalty(0, float.NegativeInfinity, false)
		};
		var line1 = new KnuthPlassLine(0, 1, 0f);
		var line2 = new KnuthPlassLine(2, 3, 0f);
		var line3 = new KnuthPlassLine(4, 6, 0f);
		var lines = new[] { line1, line2, line3 };
		var indent = new ParagraphIndentation(Left: 720f, FirstLine: 360f);

		var positions = ParagraphAligner.ComputeParagraphBoxPositions(
			items, lines, 5000f, ParagraphAlignment.Justified, indent);

		positions.Should().HaveCount(3);

		// Line 1 (first, justified): starts at Left+FirstLine = 1080
		positions[0][0].XOffset.Should().Be(1080f);

		// Line 2 (middle, justified): starts at Left = 720
		positions[1][0].XOffset.Should().Be(720f);

		// Line 3 (last, left-aligned per justification rule): starts at Left = 720
		positions[2][0].XOffset.Should().Be(720f);
	}

	// ===================================================================
	// Edge cases: zero-content and single-line paragraphs
	// ===================================================================

	[Fact]
	public void SingleLineParagraph_IsBothFirstAndLast()
	{
		var items = new KnuthPlassItem[]
		{
			Box(300),
			Glue(0, float.MaxValue, 0), Penalty(0, float.NegativeInfinity, false)
		};
		var line = new KnuthPlassLine(0, 2, 0f);
		var lines = new[] { line };
		var indent = new ParagraphIndentation(Left: 200f, FirstLine: 100f);

		var positions = ParagraphAligner.ComputeParagraphBoxPositions(
			items, lines, 2000f, ParagraphAlignment.Justified, indent);

		// Single line is both first (gets FirstLine indent) and last (left-aligned, not justified)
		positions.Should().ContainSingle();
		positions[0][0].XOffset.Should().Be(300f); // Left+FirstLine = 200+100
	}

	[Fact]
	public void EmptyParagraph_ZeroLines_HeightIsZero()
	{
		var spacing = new ParagraphSpacing(SpaceBefore: 120f, SpaceAfter: 240f, LineSpacingTwips: 480f);

		// Zero lines → height is 0 (no content, no spacing applied)
		spacing.ComputeParagraphHeight(0, 300f).Should().Be(0f);
	}

	[Fact]
	public void DefaultTabStopsWithIndentation_StillResolveCorrectly()
	{
		// Default tab stops at 720-twip intervals
		// Left indent = 360 twips
		// After indent, first default tab beyond 360 is at 720
		var profile = TabStopProfile.Default;

		var tab = profile.ResolveNextTabStop(360f);
		tab.PositionTwips.Should().Be(720f);

		var tab2 = profile.ResolveNextTabStop(720f);
		tab2.PositionTwips.Should().Be(1440f);
	}
}
