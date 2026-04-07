using AwesomeAssertions;
using Xunit;

namespace PanoramicData.Render.Test;

/// <summary>
/// Tests for <see cref="ParagraphAligner"/> — computes X offsets for content
/// boxes on a line based on paragraph alignment mode.
/// </summary>
public sealed class ParagraphAlignerTests
{
	// Convenience helpers
	private static KnuthPlassBox Box(float w) => new(w);
	private static KnuthPlassGlue Glue(float w, float stretch, float shrink) => new(w, stretch, shrink);
	private static KnuthPlassPenalty Penalty(float w, float penalty, bool flagged) => new(w, penalty, flagged);

	// ===================================================================
	// Guard tests
	// ===================================================================

	[Fact]
	public void NullItems_ThrowsArgumentNullException()
	{
		var act = () => ParagraphAligner.ComputeBoxPositions(
			null!, new KnuthPlassLine(0, 0, 0f), 1000f, ParagraphAlignment.Left);

		act.Should().Throw<ArgumentNullException>();
	}

	[Theory]
	[InlineData(0f)]
	[InlineData(-100f)]
	public void NonPositiveLineWidth_ThrowsArgumentOutOfRangeException(float lineWidth)
	{
		var items = new KnuthPlassItem[] { Box(100) };

		var act = () => ParagraphAligner.ComputeBoxPositions(
			items, new KnuthPlassLine(0, 0, 0f), lineWidth, ParagraphAlignment.Left);

		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	// ===================================================================
	// Left alignment
	// ===================================================================

	[Fact]
	public void Left_SingleBox_StartsAtZero()
	{
		// Items: Box(100) then break at Penalty
		var items = new KnuthPlassItem[] { Box(100), Penalty(0, float.NegativeInfinity, false) };
		var line = new KnuthPlassLine(0, 1, 0f);

		var result = ParagraphAligner.ComputeBoxPositions(items, line, 500f, ParagraphAlignment.Left);

		result.Should().ContainSingle();
		result[0].ItemIndex.Should().Be(0);
		result[0].XOffset.Should().Be(0f);
		result[0].Width.Should().Be(100f);
	}

	[Fact]
	public void Left_TwoBoxesWithGlue_SequentialPositions()
	{
		// Items: Box(100) Glue(20,10,5) Box(80) — break at glue after
		var items = new KnuthPlassItem[]
		{
			Box(100), Glue(20, 10, 5), Box(80),
			Glue(0, float.MaxValue, 0), Penalty(0, float.NegativeInfinity, false)
		};
		var line = new KnuthPlassLine(0, 4, 0f);

		var result = ParagraphAligner.ComputeBoxPositions(items, line, 500f, ParagraphAlignment.Left);

		result.Should().HaveCount(2);
		result[0].ItemIndex.Should().Be(0);
		result[0].XOffset.Should().Be(0f);
		result[0].Width.Should().Be(100f);
		result[1].ItemIndex.Should().Be(2);
		result[1].XOffset.Should().Be(120f); // 100 + 20
		result[1].Width.Should().Be(80f);
	}

	[Fact]
	public void Left_ThreeBoxes_CorrectPositions()
	{
		var items = new KnuthPlassItem[]
		{
			Box(100), Glue(20, 10, 5), Box(80), Glue(30, 15, 8), Box(60),
			Glue(0, float.MaxValue, 0), Penalty(0, float.NegativeInfinity, false)
		};
		var line = new KnuthPlassLine(0, 6, 0f);

		var result = ParagraphAligner.ComputeBoxPositions(items, line, 1000f, ParagraphAlignment.Left);

		result.Should().HaveCount(3);
		result[0].XOffset.Should().Be(0f);
		result[1].XOffset.Should().Be(120f); // 100 + 20
		result[2].XOffset.Should().Be(230f); // 100 + 20 + 80 + 30
	}

	// ===================================================================
	// Center alignment
	// ===================================================================

	[Fact]
	public void Center_SingleBox_Centered()
	{
		var items = new KnuthPlassItem[] { Box(100), Penalty(0, float.NegativeInfinity, false) };
		var line = new KnuthPlassLine(0, 1, 0f);

		var result = ParagraphAligner.ComputeBoxPositions(items, line, 500f, ParagraphAlignment.Center);

		result.Should().ContainSingle();
		// Content = 100, lineWidth = 500, offset = (500-100)/2 = 200
		result[0].XOffset.Should().Be(200f);
	}

	[Fact]
	public void Center_TwoBoxesWithGlue_Centered()
	{
		var items = new KnuthPlassItem[]
		{
			Box(100), Glue(20, 10, 5), Box(80),
			Glue(0, float.MaxValue, 0), Penalty(0, float.NegativeInfinity, false)
		};
		var line = new KnuthPlassLine(0, 4, 0f);

		var result = ParagraphAligner.ComputeBoxPositions(items, line, 500f, ParagraphAlignment.Center);

		// Natural content = 100 + 20 + 80 + 0 = 200, offset = (500-200)/2 = 150
		result.Should().HaveCount(2);
		result[0].XOffset.Should().Be(150f);
		result[1].XOffset.Should().Be(270f); // 150 + 100 + 20
	}

	// ===================================================================
	// Right alignment
	// ===================================================================

	[Fact]
	public void Right_SingleBox_RightAligned()
	{
		var items = new KnuthPlassItem[] { Box(100), Penalty(0, float.NegativeInfinity, false) };
		var line = new KnuthPlassLine(0, 1, 0f);

		var result = ParagraphAligner.ComputeBoxPositions(items, line, 500f, ParagraphAlignment.Right);

		result.Should().ContainSingle();
		// Content = 100, lineWidth = 500, offset = 500-100 = 400
		result[0].XOffset.Should().Be(400f);
	}

	[Fact]
	public void Right_TwoBoxesWithGlue_RightAligned()
	{
		var items = new KnuthPlassItem[]
		{
			Box(100), Glue(20, 10, 5), Box(80),
			Glue(0, float.MaxValue, 0), Penalty(0, float.NegativeInfinity, false)
		};
		var line = new KnuthPlassLine(0, 4, 0f);

		var result = ParagraphAligner.ComputeBoxPositions(items, line, 500f, ParagraphAlignment.Right);

		// Natural content = 200, offset = 500-200 = 300
		result.Should().HaveCount(2);
		result[0].XOffset.Should().Be(300f);
		result[1].XOffset.Should().Be(420f); // 300 + 100 + 20
	}

	// ===================================================================
	// Justified alignment
	// ===================================================================

	[Fact]
	public void Justified_PositiveRatio_GlueStretched()
	{
		// Two boxes with one glue, ratio=1.5 (stretch)
		// Glue: natural=20, stretch=10, shrink=5
		// Adjusted glue = 20 + 1.5*10 = 35
		var items = new KnuthPlassItem[]
		{
			Box(100), Glue(20, 10, 5), Box(80),
			Glue(0, float.MaxValue, 0), Penalty(0, float.NegativeInfinity, false)
		};
		var line = new KnuthPlassLine(0, 4, 1.5f);

		var result = ParagraphAligner.ComputeBoxPositions(items, line, 500f, ParagraphAlignment.Justified);

		result.Should().HaveCount(2);
		result[0].XOffset.Should().Be(0f); // Justified starts at 0
		result[1].XOffset.Should().Be(135f); // 100 + 35 (adjusted glue)
	}

	[Fact]
	public void Justified_NegativeRatio_GlueShrunk()
	{
		// Glue: natural=20, stretch=10, shrink=5
		// ratio=-0.5 → adjusted = 20 + (-0.5)*5 = 17.5
		var items = new KnuthPlassItem[]
		{
			Box(100), Glue(20, 10, 5), Box(80),
			Glue(0, float.MaxValue, 0), Penalty(0, float.NegativeInfinity, false)
		};
		var line = new KnuthPlassLine(0, 4, -0.5f);

		var result = ParagraphAligner.ComputeBoxPositions(items, line, 500f, ParagraphAlignment.Justified);

		result.Should().HaveCount(2);
		result[0].XOffset.Should().Be(0f);
		result[1].XOffset.Should().Be(117.5f); // 100 + 17.5
	}

	[Fact]
	public void Justified_ZeroRatio_NaturalWidths()
	{
		var items = new KnuthPlassItem[]
		{
			Box(100), Glue(20, 10, 5), Box(80),
			Glue(0, float.MaxValue, 0), Penalty(0, float.NegativeInfinity, false)
		};
		var line = new KnuthPlassLine(0, 4, 0f);

		var result = ParagraphAligner.ComputeBoxPositions(items, line, 500f, ParagraphAlignment.Justified);

		result.Should().HaveCount(2);
		result[0].XOffset.Should().Be(0f);
		result[1].XOffset.Should().Be(120f); // 100 + 20 (natural)
	}

	[Fact]
	public void Justified_MultipleGlue_AllAdjusted()
	{
		// Three boxes, two glue items, ratio=1.0
		// Glue1: natural=20, stretch=10, shrink=5 → adjusted=30
		// Glue2: natural=30, stretch=15, shrink=8 → adjusted=45
		var items = new KnuthPlassItem[]
		{
			Box(100), Glue(20, 10, 5), Box(80), Glue(30, 15, 8), Box(60),
			Glue(0, float.MaxValue, 0), Penalty(0, float.NegativeInfinity, false)
		};
		var line = new KnuthPlassLine(0, 6, 1.0f);

		var result = ParagraphAligner.ComputeBoxPositions(items, line, 1000f, ParagraphAlignment.Justified);

		result.Should().HaveCount(3);
		result[0].XOffset.Should().Be(0f);
		result[1].XOffset.Should().Be(130f); // 100 + 30
		result[2].XOffset.Should().Be(255f); // 100 + 30 + 80 + 45
	}

	// ===================================================================
	// Flagged penalty (hyphen) at break
	// ===================================================================

	[Fact]
	public void FlaggedPenaltyBreak_HyphenBoxAdded()
	{
		// Box("well") then flagged penalty (hyphen width=15)
		var items = new KnuthPlassItem[]
		{
			Box(100), Penalty(15, 100, true), Box(80)
		};
		// Line breaks at the flagged penalty (index 1)
		var line = new KnuthPlassLine(0, 1, 0f);

		var result = ParagraphAligner.ComputeBoxPositions(items, line, 500f, ParagraphAlignment.Left);

		result.Should().HaveCount(2);
		result[0].ItemIndex.Should().Be(0);
		result[0].XOffset.Should().Be(0f);
		result[0].Width.Should().Be(100f);
		// Hyphen box added at break
		result[1].ItemIndex.Should().Be(1);
		result[1].XOffset.Should().Be(100f);
		result[1].Width.Should().Be(15f);
	}

	[Fact]
	public void NonFlaggedPenalty_NoExtraBox()
	{
		// Forced-break penalty (not flagged, 0 width)
		var items = new KnuthPlassItem[]
		{
			Box(100), Penalty(0, float.NegativeInfinity, false)
		};
		var line = new KnuthPlassLine(0, 1, 0f);

		var result = ParagraphAligner.ComputeBoxPositions(items, line, 500f, ParagraphAlignment.Left);

		// Only the box, no penalty box
		result.Should().ContainSingle();
		result[0].ItemIndex.Should().Be(0);
	}

	// ===================================================================
	// Edge cases
	// ===================================================================

	[Fact]
	public void EmptyLine_NoItems_ReturnsEmpty()
	{
		var items = new KnuthPlassItem[] { Penalty(0, float.NegativeInfinity, false) };
		// Line from 0 to 0 — no content items before the break
		var line = new KnuthPlassLine(0, 0, 0f);

		var result = ParagraphAligner.ComputeBoxPositions(items, line, 500f, ParagraphAlignment.Left);

		result.Should().BeEmpty();
	}

	[Fact]
	public void ContentWiderThanLine_CenterClampsToZero()
	{
		// Content = 100, lineWidth = 50 → offset would be (50-100)/2 = -25, clamped to 0
		var items = new KnuthPlassItem[] { Box(100), Penalty(0, float.NegativeInfinity, false) };
		var line = new KnuthPlassLine(0, 1, -1f);

		var result = ParagraphAligner.ComputeBoxPositions(items, line, 50f, ParagraphAlignment.Center);

		result.Should().ContainSingle();
		result[0].XOffset.Should().Be(0f);
	}

	[Fact]
	public void ContentWiderThanLine_RightClampsToZero()
	{
		var items = new KnuthPlassItem[] { Box(100), Penalty(0, float.NegativeInfinity, false) };
		var line = new KnuthPlassLine(0, 1, -1f);

		var result = ParagraphAligner.ComputeBoxPositions(items, line, 50f, ParagraphAlignment.Right);

		result.Should().ContainSingle();
		result[0].XOffset.Should().Be(0f);
	}

	[Fact]
	public void LineStartsAtNonZeroIndex()
	{
		// Simulates a second line starting partway through items
		var items = new KnuthPlassItem[]
		{
			Box(100), Glue(20, 10, 5), // First line items (not part of this line)
			Box(80), Glue(25, 12, 6), Box(60), // Second line items
			Glue(0, float.MaxValue, 0), Penalty(0, float.NegativeInfinity, false)
		};
		// Second line: items 2..5 (break at finishing glue index 5)
		var line = new KnuthPlassLine(2, 6, 0f);

		var result = ParagraphAligner.ComputeBoxPositions(items, line, 1000f, ParagraphAlignment.Left);

		result.Should().HaveCount(2);
		result[0].ItemIndex.Should().Be(2);
		result[0].XOffset.Should().Be(0f);
		result[0].Width.Should().Be(80f);
		result[1].ItemIndex.Should().Be(4);
		result[1].XOffset.Should().Be(105f); // 80 + 25
		result[1].Width.Should().Be(60f);
	}

	[Fact]
	public void PenaltyWithinLine_ContributesZeroWidth()
	{
		// Penalty in the middle of a line (not at break point) — should not produce a box
		var items = new KnuthPlassItem[]
		{
			Box(100), Penalty(0, 50, false), Box(80),
			Glue(0, float.MaxValue, 0), Penalty(0, float.NegativeInfinity, false)
		};
		var line = new KnuthPlassLine(0, 4, 0f);

		var result = ParagraphAligner.ComputeBoxPositions(items, line, 500f, ParagraphAlignment.Left);

		result.Should().HaveCount(2);
		result[0].XOffset.Should().Be(0f);
		result[0].Width.Should().Be(100f);
		// Penalty at index 1 contributes 0 width
		result[1].XOffset.Should().Be(100f);
		result[1].Width.Should().Be(80f);
	}

	// ===================================================================
	// Alignment enum coverage
	// ===================================================================

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
}
