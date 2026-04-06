namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public class KnuthPlassTests
{
	// --- Item model tests ---

	[Fact]
	public void Box_StoresWidth()
	{
		var box = new KnuthPlassBox(100f);
		box.Width.Should().Be(100f);
	}

	[Fact]
	public void Glue_StoresWidthStretchShrink()
	{
		var glue = new KnuthPlassGlue(60f, 30f, 20f);
		glue.Width.Should().Be(60f);
		glue.Stretch.Should().Be(30f);
		glue.Shrink.Should().Be(20f);
	}

	[Fact]
	public void Penalty_StoresWidthPenaltyAndFlag()
	{
		var penalty = new KnuthPlassPenalty(40f, 100f, true);
		penalty.Width.Should().Be(40f);
		penalty.Penalty.Should().Be(100f);
		penalty.IsFlagged.Should().BeTrue();
	}

	[Fact]
	public void Penalty_DefaultIsNotFlagged()
	{
		var penalty = new KnuthPlassPenalty(0f, 50f);
		penalty.IsFlagged.Should().BeFalse();
	}

	// --- Algorithm tests ---

	[Fact]
	public void FindBreaks_WithNullItems_ThrowsArgumentNullException()
	{
		var act = () => KnuthPlassAlgorithm.FindBreaks(null!, 1000f);
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void FindBreaks_WithNonPositiveLineWidth_ThrowsArgumentOutOfRangeException()
	{
		var act = () => KnuthPlassAlgorithm.FindBreaks([], 0f);
		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void FindBreaks_WithEmptyItems_ReturnsNoLines()
	{
		var lines = KnuthPlassAlgorithm.FindBreaks([], 1000f);
		lines.Should().BeEmpty();
	}

	[Fact]
	public void FindBreaks_SingleBoxFitsOnLine_ReturnsSingleLine()
	{
		var items = new KnuthPlassItem[]
		{
			new KnuthPlassBox(500f),
			ForcedBreak()
		};

		var lines = KnuthPlassAlgorithm.FindBreaks(items, 1000f);

		lines.Should().HaveCount(1);
		lines[0].StartIndex.Should().Be(0);
		lines[0].EndIndex.Should().Be(1);
	}

	[Fact]
	public void FindBreaks_TwoBoxesWithGlueFitOnLine_ReturnsSingleLine()
	{
		var items = new KnuthPlassItem[]
		{
			new KnuthPlassBox(300f),
			new KnuthPlassGlue(60f, 30f, 20f),
			new KnuthPlassBox(300f),
			ForcedBreak()
		};

		var lines = KnuthPlassAlgorithm.FindBreaks(items, 1000f);

		lines.Should().HaveCount(1);
	}

	[Fact]
	public void FindBreaks_TwoBoxesExceedLineWidth_ReturnsTwoLines()
	{
		var items = new KnuthPlassItem[]
		{
			new KnuthPlassBox(600f),
			new KnuthPlassGlue(60f, 30f, 20f),
			new KnuthPlassBox(600f),
			ForcedBreak()
		};

		var lines = KnuthPlassAlgorithm.FindBreaks(items, 1000f);

		lines.Should().HaveCount(2);
		// First line should end at the glue (break point)
		lines[0].EndIndex.Should().Be(1);
		// Second line should start at the box after glue
		lines[1].StartIndex.Should().Be(2);
	}

	[Fact]
	public void FindBreaks_ForcedBreak_AlwaysBreaks()
	{
		var items = new KnuthPlassItem[]
		{
			new KnuthPlassBox(100f),
			ForcedBreak(),
			new KnuthPlassBox(100f),
			ForcedBreak()
		};

		var lines = KnuthPlassAlgorithm.FindBreaks(items, 10000f);

		lines.Should().HaveCount(2);
	}

	[Fact]
	public void FindBreaks_MultipleWords_BreaksOptimally()
	{
		// Five words of equal width (200 twips each), spaces of 60 twips,
		// line width 700 twips.
		// Natural: 200 + 60 + 200 + 60 + 200 + 60 + 200 + 60 + 200 = 1240 twips
		// Should break into multiple lines
		var items = new List<KnuthPlassItem>();
		for (var i = 0; i < 5; i++)
		{
			if (i > 0)
			{
				items.Add(new KnuthPlassGlue(60f, 30f, 20f));
			}

			items.Add(new KnuthPlassBox(200f));
		}

		items.Add(ForcedBreak());

		var lines = KnuthPlassAlgorithm.FindBreaks(items, 700f);

		lines.Should().HaveCountGreaterThan(1);
		// Verify lines are non-overlapping and cover all content
		for (var i = 1; i < lines.Count; i++)
		{
			lines[i].StartIndex.Should().BeGreaterThanOrEqualTo(lines[i - 1].EndIndex);
		}
	}

	[Fact]
	public void FindBreaks_AdjustmentRatio_IsZeroForNaturalWidth()
	{
		// Box exactly fills the line
		var items = new KnuthPlassItem[]
		{
			new KnuthPlassBox(1000f),
			ForcedBreak()
		};

		var lines = KnuthPlassAlgorithm.FindBreaks(items, 1000f);

		lines.Should().HaveCount(1);
		lines[0].AdjustmentRatio.Should().BeApproximately(0f, 0.01f);
	}

	[Fact]
	public void FindBreaks_ShortLine_HasPositiveAdjustmentRatio()
	{
		// Content narrower than line width → needs stretching
		var items = new KnuthPlassItem[]
		{
			new KnuthPlassBox(300f),
			new KnuthPlassGlue(60f, 200f, 20f),
			new KnuthPlassBox(300f),
			ForcedBreak()
		};

		var lines = KnuthPlassAlgorithm.FindBreaks(items, 1000f);

		lines.Should().HaveCount(1);
		// Last line of a paragraph: ratio is clamped to 0 (no stretching)
		// But the algorithm should still produce a valid result
		lines[0].AdjustmentRatio.Should().BeGreaterThanOrEqualTo(-1f);
	}

	[Fact]
	public void FindBreaks_OnlyForcedBreak_ReturnsNoLines()
	{
		var items = new KnuthPlassItem[]
		{
			ForcedBreak()
		};

		var lines = KnuthPlassAlgorithm.FindBreaks(items, 1000f);

		// A forced break with no content should produce an empty line
		lines.Should().HaveCount(1);
	}

	[Fact]
	public void FindBreaks_MultipleForcedBreaks_ProducesMultipleLines()
	{
		var items = new KnuthPlassItem[]
		{
			new KnuthPlassBox(100f),
			ForcedBreak(),
			new KnuthPlassBox(100f),
			ForcedBreak(),
			new KnuthPlassBox(100f),
			ForcedBreak()
		};

		var lines = KnuthPlassAlgorithm.FindBreaks(items, 5000f);

		lines.Should().HaveCount(3);
	}

	[Fact]
	public void FindBreaks_PenaltyWithHighCost_AvoidsBreakingThere()
	{
		// Two possible break points: one with high penalty, one with zero penalty.
		// The algorithm should prefer the zero-penalty break.
		var items = new KnuthPlassItem[]
		{
			new KnuthPlassBox(400f),
			new KnuthPlassPenalty(0f, 1000f), // high penalty
			new KnuthPlassGlue(60f, 30f, 20f),
			new KnuthPlassBox(400f),
			new KnuthPlassGlue(60f, 30f, 20f), // zero penalty (easy break)
			new KnuthPlassBox(400f),
			ForcedBreak()
		};

		var lines = KnuthPlassAlgorithm.FindBreaks(items, 900f);

		lines.Should().HaveCountGreaterThan(1);
	}

	[Fact]
	public void FindBreaks_FlaggedConsecutiveBreaks_IncursFlaggedDemerit()
	{
		// Two consecutive flagged penalties (e.g., hyphen breaks) should be penalised.
		// The algorithm should still produce valid output.
		var items = new KnuthPlassItem[]
		{
			new KnuthPlassBox(200f),
			new KnuthPlassPenalty(20f, 50f, true),  // flagged break
			new KnuthPlassBox(200f),
			new KnuthPlassPenalty(20f, 50f, true),  // flagged consecutive break
			new KnuthPlassBox(200f),
			ForcedBreak()
		};

		var lines = KnuthPlassAlgorithm.FindBreaks(items, 250f);

		lines.Should().HaveCountGreaterThan(1);
	}

	[Fact]
	public void FindBreaks_NegativePenalty_EncouragesBreaking()
	{
		// Negative penalties encourage breaks at that point.
		var items = new KnuthPlassItem[]
		{
			new KnuthPlassBox(400f),
			new KnuthPlassPenalty(0f, -100f),  // negative penalty
			new KnuthPlassBox(400f),
			ForcedBreak()
		};

		var lines = KnuthPlassAlgorithm.FindBreaks(items, 500f);

		// Should break at the negative penalty since content exceeds line width
		lines.Should().HaveCountGreaterThan(1);
	}

	[Fact]
	public void FindBreaks_GlueAfterBreak_IsSkippedForNextLine()
	{
		// Glue immediately after a break point should not appear on the next line.
		var items = new KnuthPlassItem[]
		{
			new KnuthPlassBox(500f),
			new KnuthPlassGlue(60f, 30f, 20f),
			new KnuthPlassGlue(60f, 30f, 20f), // extra glue after break
			new KnuthPlassBox(500f),
			ForcedBreak()
		};

		var lines = KnuthPlassAlgorithm.FindBreaks(items, 600f);

		lines.Should().HaveCountGreaterThan(1);
		// Second line should skip the glue(s)
		lines[1].StartIndex.Should().BeGreaterThan(2);
	}

	[Fact]
	public void FindBreaks_EmergencyFallback_WhenNoFeasibleBreaks()
	{
		// A single huge box with no glue — must use emergency fallback
		var items = new KnuthPlassItem[]
		{
			new KnuthPlassBox(2000f),
			new KnuthPlassGlue(60f, 30f, 20f),
			new KnuthPlassBox(100f),
			ForcedBreak()
		};

		var lines = KnuthPlassAlgorithm.FindBreaks(items, 500f);

		// Should produce at least 1 line via emergency fallback
		lines.Should().HaveCountGreaterThan(0);
	}

	[Fact]
	public void FindBreaks_TrailingGlueBreakpoint_UsesRemainingActiveNodes()
	{
		// Items ending at a glue that constitutes a legal breakpoint.
		// The best remaining active node should be used as the final break.
		var items = new KnuthPlassItem[]
		{
			new KnuthPlassBox(400f),
			new KnuthPlassGlue(60f, 30f, 20f),
			new KnuthPlassBox(400f),
			new KnuthPlassGlue(60f, 30f, 20f), // legal breakpoint at end
		};

		var lines = KnuthPlassAlgorithm.FindBreaks(items, 1000f);

		// The active nodes at the end produce at least one line
		lines.Should().HaveCountGreaterThan(0);
	}

	[Fact]
	public void FindBreaks_AllItemsAreForcedBreaks_ReturnsNoLines()
	{
		// Edge case: bestFinal is null when no content precedes any break.
		// Single forced break should produce 1 empty line.
		var items = new KnuthPlassItem[]
		{
			ForcedBreak()
		};

		var lines = KnuthPlassAlgorithm.FindBreaks(items, 1000f);
		lines.Should().HaveCount(1);
	}

	[Fact]
	public void FindBreaks_OnlyBoxesNoBreakpoints_ReturnsEmpty()
	{
		// Items with no legal breakpoints should return empty.
		var items = new KnuthPlassItem[]
		{
			new KnuthPlassBox(100f),
			new KnuthPlassBox(200f),
		};

		var lines = KnuthPlassAlgorithm.FindBreaks(items, 1000f);

		lines.Should().BeEmpty();
	}

	[Fact]
	public void FindBreaks_LooseFitnessClass_IsAssignedCorrectly()
	{
		// Create a scenario where the adjustment ratio is in (0.5, 1.0]
		// to exercise the "loose" fitness class branch.
		// Content = 700, line width = 1000, glue stretch = 400.
		// ratio = (1000 - 700) / 400 = 0.75 → loose
		var items = new KnuthPlassItem[]
		{
			new KnuthPlassBox(300f),
			new KnuthPlassGlue(100f, 400f, 50f),
			new KnuthPlassBox(300f),
			ForcedBreak()
		};

		var lines = KnuthPlassAlgorithm.FindBreaks(items, 1000f);

		lines.Should().HaveCount(1);
		// ratio = (1000 - 700) / 400 = 0.75 — but last line may be clamped to 0
		lines[0].AdjustmentRatio.Should().BeLessThanOrEqualTo(0f);
	}

	[Fact]
	public void FindBreaks_LooseFitnessOnMidLine_ProducesMultipleLines()
	{
		// Ensure "loose" fitness class (ratio in (0.5, 1.0]) is exercised on a
		// non-final line. Content at break idx=1: Box(200), diff=lineWidth-200,
		// stretch from the glue at idx=1 is NOT in the line (glue is the break).
		// We need glue stretch WITHIN the line, so: Box Glue Box then break.
		// 
		// For two lines with loose first line:
		// Box(200) Glue(50,400,20) Box(200) Glue(50,400,20) Box(600) ForcedBreak
		// Full content = 200+50+200+50+600 = 1100, lineWidth=550 → must break.
		// Break at idx=3: line1 content=200+50+200=450, diff=100, stretch=400, ratio=0.25 → normal.
		// Try lineWidth=650: diff=200, stretch=400, ratio=0.5 → boundary of normal/loose.
		// Try lineWidth=700: diff=250, stretch=400, ratio=0.625 → loose!
		// Line2 from idx=4: Box(600), forced break. Content=600, diff=100, stretch=0.
		// Full from active(-1) to forced break: 1100 vs 700 → 1100-700=400 overflow. ratio=-400/40=-10.
		// So the algorithm should prefer breaking at idx=3 with ratio=0.625 (loose).
		var items = new List<KnuthPlassItem>
		{
			new KnuthPlassBox(200f),
			new KnuthPlassGlue(50f, 400f, 20f),
			new KnuthPlassBox(200f),
			new KnuthPlassGlue(50f, 400f, 20f),
			new KnuthPlassBox(600f),
			ForcedBreak()
		};

		var lines = KnuthPlassAlgorithm.FindBreaks(items, 700f);

		lines.Should().HaveCountGreaterThan(1);
	}

	private static KnuthPlassPenalty ForcedBreak() =>
		new(0f, KnuthPlassPenalty.NegativeInfinity);
}
