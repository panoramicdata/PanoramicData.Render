using AwesomeAssertions;
using Xunit;

namespace PanoramicData.Render.Test;

public sealed partial class ParagraphAlignerTests
{
	[Theory]
	[InlineData(0, 0f)]   // Left
	[InlineData(1, 150f)] // Center
	[InlineData(2, 300f)] // Right
	[InlineData(3, 0f)]   // Justified
	public void AllAlignments_SingleBox_CorrectOffset(int alignmentValue, float expectedX)
	{
		// Content=200 (box), lineWidth=500
		var alignment = (ParagraphAlignment)alignmentValue;
		var items = new KnuthPlassItem[] { Box(200), Penalty(0, float.NegativeInfinity, false) };
		var line = new KnuthPlassLine(0, 1, 0f);

		var result = ParagraphAligner.ComputeBoxPositions(items, line, 500f, alignment);

		result.Should().ContainSingle();
		result[0].XOffset.Should().Be(expectedX);
	}

	// ===================================================================
	// Step 2.3.2: Last-line justification behavior
	// ===================================================================

	[Fact]
	public void Justified_LastLine_UsesNaturalGlueWidth()
	{
		// Justified + isLastLine=true → use natural glue, not adjusted
		// Glue: natural=20, stretch=10, shrink=5, ratio=1.5
		// Without last-line: adjusted = 20 + 1.5*10 = 35
		// With last-line: natural = 20
		var items = new KnuthPlassItem[]
		{
			Box(100), Glue(20, 10, 5), Box(80),
			Glue(0, float.MaxValue, 0), Penalty(0, float.NegativeInfinity, false)
		};
		var line = new KnuthPlassLine(0, 4, 1.5f);

		var result = ParagraphAligner.ComputeBoxPositions(
			items, line, 500f, ParagraphAlignment.Justified, isLastLine: true);

		result.Should().HaveCount(2);
		AssertOffsets(result, 0f, 120f);
	}

	[Fact]
	public void Justified_NotLastLine_UsesAdjustedGlueWidth()
	{
		// Same items, not last line → glue is adjusted
		var items = new KnuthPlassItem[]
		{
			Box(100), Glue(20, 10, 5), Box(80),
			Glue(0, float.MaxValue, 0), Penalty(0, float.NegativeInfinity, false)
		};
		var line = new KnuthPlassLine(0, 4, 1.5f);

		var result = ParagraphAligner.ComputeBoxPositions(
			items, line, 500f, ParagraphAlignment.Justified, isLastLine: false);

		result.Should().HaveCount(2);
		AssertOffsets(result, 0f, 135f);
	}

	[Fact]
	public void Left_LastLine_NoEffect()
	{
		// isLastLine on non-justified alignment has no effect
		var items = new KnuthPlassItem[] { Box(100), Penalty(0, float.NegativeInfinity, false) };
		var line = new KnuthPlassLine(0, 1, 0f);

		var result = ParagraphAligner.ComputeBoxPositions(
			items, line, 500f, ParagraphAlignment.Left, isLastLine: true);

		result.Should().ContainSingle();
		result[0].XOffset.Should().Be(0f);
	}

	[Fact]
	public void Center_LastLine_NoEffect()
	{
		var items = new KnuthPlassItem[] { Box(100), Penalty(0, float.NegativeInfinity, false) };
		var line = new KnuthPlassLine(0, 1, 0f);

		var result = ParagraphAligner.ComputeBoxPositions(
			items, line, 500f, ParagraphAlignment.Center, isLastLine: true);

		result.Should().ContainSingle();
		result[0].XOffset.Should().Be(200f); // Centered as normal
	}

	[Fact]
	public void Right_LastLine_NoEffect()
	{
		var items = new KnuthPlassItem[] { Box(100), Penalty(0, float.NegativeInfinity, false) };
		var line = new KnuthPlassLine(0, 1, 0f);

		var result = ParagraphAligner.ComputeBoxPositions(
			items, line, 500f, ParagraphAlignment.Right, isLastLine: true);

		result.Should().ContainSingle();
		result[0].XOffset.Should().Be(400f); // Right-aligned as normal
	}

	// ===================================================================
	// ComputeParagraphBoxPositions — paragraph-level convenience
	// ===================================================================

	[Fact]
	public void ParagraphPositions_NullItems_Throws()
	{
		var act = () => ParagraphAligner.ComputeParagraphBoxPositions(
			null!, [], 500f, ParagraphAlignment.Left);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void ParagraphPositions_NullLines_Throws()
	{
		var act = () => ParagraphAligner.ComputeParagraphBoxPositions(
			[], null!, 500f, ParagraphAlignment.Left);

		act.Should().Throw<ArgumentNullException>();
	}

	[Theory]
	[InlineData(0f)]
	[InlineData(-1f)]
	public void ParagraphPositions_NonPositiveLineWidth_Throws(float width)
	{
		var act = () => ParagraphAligner.ComputeParagraphBoxPositions(
			[], [], width, ParagraphAlignment.Left);

		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void ParagraphPositions_EmptyLines_ReturnsEmpty()
	{
		var result = ParagraphAligner.ComputeParagraphBoxPositions(
			Array.Empty<KnuthPlassItem>(), Array.Empty<KnuthPlassLine>(), 500f, ParagraphAlignment.Left);

		result.Should().BeEmpty();
	}

	[Fact]
	public void ParagraphPositions_SingleLine_IsLastLine()
	{
		// A single-line justified paragraph: the only line IS the last line → left-aligned
		var items = new KnuthPlassItem[]
		{
			Box(100), Glue(20, 10, 5), Box(80),
			Glue(0, float.MaxValue, 0), Penalty(0, float.NegativeInfinity, false)
		};
		var lines = new[] { new KnuthPlassLine(0, 4, 1.5f) };

		var result = ParagraphAligner.ComputeParagraphBoxPositions(
			items, lines, 500f, ParagraphAlignment.Justified);

		result.Should().ContainSingle();
		// Last line → natural glue (20), not adjusted (35)
		result[0].Should().HaveCount(2);
		result[0][1].XOffset.Should().Be(120f); // 100 + 20
	}

	[Fact]
	public void ParagraphPositions_TwoLines_OnlyLastIsLeftAligned()
	{
		// Two lines, justified. Line 1 gets adjusted glue, line 2 (last) gets natural.
		var items = new KnuthPlassItem[]
		{
			// Line 1: Box(100) Glue(20,10,5) Box(80) — break at glue(3)
			Box(100), Glue(20, 10, 5), Box(80), Glue(20, 10, 5),
			// Line 2: Box(60) — finisher
			Box(60),
			Glue(0, float.MaxValue, 0), Penalty(0, float.NegativeInfinity, false)
		};
		var line1 = new KnuthPlassLine(0, 3, 1.0f); // ratio=1.0 stretches to 30
		var line2 = new KnuthPlassLine(4, 6, 0f);
		var lines = new[] { line1, line2 };

		var result = ParagraphAligner.ComputeParagraphBoxPositions(
			items, lines, 500f, ParagraphAlignment.Justified);

		result.Should().HaveCount(2);

		// Line 1 (not last): adjusted glue = 20 + 1.0*10 = 30
		result[0][0].XOffset.Should().Be(0f);
		result[0][1].XOffset.Should().Be(130f); // 100 + 30

		// Line 2 (last): natural glue — but there's only one box, no glue to test
		result[1].Should().ContainSingle();
		result[1][0].XOffset.Should().Be(0f); // left-aligned
	}

	[Fact]
	public void ParagraphPositions_ThreeLines_MiddleIsJustified()
	{
		// Three lines, justified. Lines 1 and 2 get adjusted glue, line 3 (last) gets natural.
		var items = new KnuthPlassItem[]
		{
			// Line 1: indices 0-2, break at 3
			Box(100), Glue(20, 10, 5), Box(80), Glue(20, 10, 5),
			// Line 2: indices 4-6, break at 7
			Box(90), Glue(25, 12, 6), Box(70), Glue(25, 12, 6),
			// Line 3: indices 8-10
			Box(50), Glue(15, 8, 4), Box(40),
			Glue(0, float.MaxValue, 0), Penalty(0, float.NegativeInfinity, false)
		};
		var line1 = new KnuthPlassLine(0, 3, 0.5f);
		var line2 = new KnuthPlassLine(4, 7, 0.8f);
		var line3 = new KnuthPlassLine(8, 12, 0f);
		var lines = new[] { line1, line2, line3 };

		var result = ParagraphAligner.ComputeParagraphBoxPositions(
			items, lines, 500f, ParagraphAlignment.Justified);

		result.Should().HaveCount(3);

		// Line 1: adjusted glue = 20 + 0.5*10 = 25
		result[0][1].XOffset.Should().Be(125f); // 100 + 25

		// Line 2: adjusted glue = 25 + 0.8*12 = 34.6
		result[1][1].XOffset.Should().BeApproximately(124.6f, 0.01f); // 90 + 34.6

		// Line 3 (last): natural glue = 15, left-aligned
		result[2][1].XOffset.Should().Be(65f); // 50 + 15
	}

	[Fact]
	public void ParagraphPositions_CenterAlignment_AllLinesCentered()
	{
		// Center alignment — last-line logic should not affect center mode
		var items = new KnuthPlassItem[]
		{
			Box(100), Glue(20, 10, 5),
			Box(80),
			Glue(0, float.MaxValue, 0), Penalty(0, float.NegativeInfinity, false)
		};
		var lines = new[] { new KnuthPlassLine(0, 4, 0f) };

		var result = ParagraphAligner.ComputeParagraphBoxPositions(
			items, lines, 500f, ParagraphAlignment.Center);

		result.Should().ContainSingle();
		// Content = 100 + 20 + 80 = 200, offset = (500-200)/2 = 150
		result[0][0].XOffset.Should().Be(150f);
	}

	// ===================================================================
	// Step 2.3.3: Indentation
	// ===================================================================

	[Fact]
	public void LeftIndent_ShiftsContentRight()
	{
		var items = new KnuthPlassItem[] { Box(100), Penalty(0, float.NegativeInfinity, false) };
		var line = new KnuthPlassLine(0, 1, 0f);
		var indent = new ParagraphIndentation(Left: 200f);

		var result = ParagraphAligner.ComputeBoxPositions(
			items, line, 500f, ParagraphAlignment.Left,
			indentation: indent);

		result.Should().ContainSingle();
		result[0].XOffset.Should().Be(200f); // Shifted by left indent
	}

	[Fact]
	public void RightIndent_NarrowsEffectiveWidth_CenterAdjusted()
	{
		// Content=100, lineWidth=500, right=100 → effective=400
		// Center: offset = (400-100)/2 = 150 (relative to indent area start)
		// No left indent, so absolute X = 150
		var items = new KnuthPlassItem[] { Box(100), Penalty(0, float.NegativeInfinity, false) };
		var line = new KnuthPlassLine(0, 1, 0f);
		var indent = new ParagraphIndentation(Right: 100f);

		var result = ParagraphAligner.ComputeBoxPositions(
			items, line, 500f, ParagraphAlignment.Center,
			indentation: indent);

		result.Should().ContainSingle();
		result[0].XOffset.Should().Be(150f); // (400-100)/2
	}

	[Fact]
	public void RightIndent_RightAlignment_ShiftsInward()
	{
		// Content=100, lineWidth=500, right=100 → effective=400
		// Right: offset = 400-100 = 300, plus leftIndent=0 → X=300
		var items = new KnuthPlassItem[] { Box(100), Penalty(0, float.NegativeInfinity, false) };
		var line = new KnuthPlassLine(0, 1, 0f);
		var indent = new ParagraphIndentation(Right: 100f);

		var result = ParagraphAligner.ComputeBoxPositions(
			items, line, 500f, ParagraphAlignment.Right,
			indentation: indent);

		result.Should().ContainSingle();
		result[0].XOffset.Should().Be(300f); // 400-100
	}

	[Fact]
	public void FirstLineIndent_FirstLineShiftedFurther()
	{
		// Left=200, FirstLine=100 → first line at 300, subsequent at 200
		var items = new KnuthPlassItem[] { Box(100), Penalty(0, float.NegativeInfinity, false) };
		var line = new KnuthPlassLine(0, 1, 0f);
		var indent = new ParagraphIndentation(Left: 200f, FirstLine: 100f);

		var firstResult = ParagraphAligner.ComputeBoxPositions(
			items, line, 500f, ParagraphAlignment.Left,
			indentation: indent, isFirstLine: true);

		var subsequentResult = ParagraphAligner.ComputeBoxPositions(
			items, line, 500f, ParagraphAlignment.Left,
			indentation: indent, isFirstLine: false);

		firstResult[0].XOffset.Should().Be(300f); // Left+FirstLine
		subsequentResult[0].XOffset.Should().Be(200f); // Left only
	}

	[Fact]
	public void HangingIndent_SubsequentLinesShiftedFurther()
	{
		// Left=200, Hanging=300 → first line at 200, subsequent at 500
		var items = new KnuthPlassItem[] { Box(50), Penalty(0, float.NegativeInfinity, false) };
		var line = new KnuthPlassLine(0, 1, 0f);
		var indent = new ParagraphIndentation(Left: 200f, Hanging: 300f);

		var firstResult = ParagraphAligner.ComputeBoxPositions(
			items, line, 1000f, ParagraphAlignment.Left,
			indentation: indent, isFirstLine: true);

		var subsequentResult = ParagraphAligner.ComputeBoxPositions(
			items, line, 1000f, ParagraphAlignment.Left,
			indentation: indent, isFirstLine: false);

		firstResult[0].XOffset.Should().Be(200f); // Left only
		subsequentResult[0].XOffset.Should().Be(500f); // Left+Hanging
	}

	[Fact]
	public void BothLeftAndRight_CenterAlignment()
	{
		// Left=100, Right=100 → effective width = 500-100-100 = 300
		// Content=100 → center offset = (300-100)/2 = 100
		// Absolute X = Left(100) + 100 = 200
		var items = new KnuthPlassItem[] { Box(100), Penalty(0, float.NegativeInfinity, false) };
		var line = new KnuthPlassLine(0, 1, 0f);
		var indent = new ParagraphIndentation(Left: 100f, Right: 100f);

		var result = ParagraphAligner.ComputeBoxPositions(
			items, line, 500f, ParagraphAlignment.Center,
			indentation: indent);

		result.Should().ContainSingle();
		result[0].XOffset.Should().Be(200f); // 100 + (300-100)/2
	}

	[Fact]
	public void ParagraphPositions_WithIndentation_FirstLineDetected()
	{
		// Two lines with hanging indent: Left=200, Hanging=300
		// Line 1 (first): X starts at 200
		// Line 2 (not first): X starts at 500
		var items = new KnuthPlassItem[]
		{
			Box(50), Glue(20, 10, 5),
			Box(50),
			Glue(0, float.MaxValue, 0), Penalty(0, float.NegativeInfinity, false)
		};
		var line1 = new KnuthPlassLine(0, 1, 0f); // break at glue(1)
		var line2 = new KnuthPlassLine(2, 4, 0f);
		var lines = new[] { line1, line2 };
		var indent = new ParagraphIndentation(Left: 200f, Hanging: 300f);

		var result = ParagraphAligner.ComputeParagraphBoxPositions(
			items, lines, 1000f, ParagraphAlignment.Left, indent);

		result.Should().HaveCount(2);
		result[0][0].XOffset.Should().Be(200f); // First line: Left only
		result[1][0].XOffset.Should().Be(500f); // Subsequent: Left+Hanging
	}

	[Fact]
	public void ParagraphPositions_FirstLineIndent_OnlyAffectsFirstLine()
	{
		var items = new KnuthPlassItem[]
		{
			Box(50), Glue(20, 10, 5),
			Box(50),
			Glue(0, float.MaxValue, 0), Penalty(0, float.NegativeInfinity, false)
		};
		var line1 = new KnuthPlassLine(0, 1, 0f);
		var line2 = new KnuthPlassLine(2, 4, 0f);
		var lines = new[] { line1, line2 };
		var indent = new ParagraphIndentation(Left: 100f, FirstLine: 150f);

		var result = ParagraphAligner.ComputeParagraphBoxPositions(
			items, lines, 1000f, ParagraphAlignment.Left, indent);

		result.Should().HaveCount(2);
		result[0][0].XOffset.Should().Be(250f); // Left+FirstLine
		result[1][0].XOffset.Should().Be(100f); // Left only
	}
}
