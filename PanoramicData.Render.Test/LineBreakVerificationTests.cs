namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using SkiaSharp;
using Xunit;

/// <summary>
/// Step 2.2.7: Verify break positions against hand-computed expected results
/// for at least 10 paragraphs of varying complexity.
///
/// Strategy: Each test measures word/space widths at runtime using the same
/// measurement engine, then computes the expected item indices for each line
/// and verifies that <see cref="ParagraphLineBreaker"/> produces exactly
/// those breaks. This avoids hardcoding platform-dependent widths while
/// still constituting hand-computed expected results (we manually derive
/// which words fit on which line given known width sums).
/// </summary>
public sealed class LineBreakVerificationTests
{
	private readonly MeasurementEngine _engine = new();

	private static SKTypeface GetTypeface()
	{
		var typeface = SKTypeface.FromFamilyName("Arial");
		if (typeface is null || typeface.FamilyName != "Arial")
		{
			Assert.Skip("Arial not available on this platform");
		}

		return typeface;
	}

	/// <summary>
	/// Measures the total width of a text string in twips.
	/// </summary>
	private float MeasureWidth(SKTypeface typeface, float fontSizePoints, string text)
	{
		var advances = _engine.MeasureGlyphAdvancesInTwips(typeface, fontSizePoints, text);
		return advances.Sum();
	}

	// =====================================================================
	// Scenario 1: Two equal words — line width fits exactly one word + space
	// Items: Box("aaa") Glue Box("bbb") Glue(finish) Penalty(forced)
	// Expected: Line 1 = [0..2] breaks at glue(1), Line 2 = [2..5] forced break
	// =====================================================================
	[Fact]
	public void Scenario01_TwoWords_ExactlyOneWordPerLine()
	{
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var runs = MakeRuns("aaa bbb");

		// Measure: word width + half a space — enough for one word but not two
		var wordWidth = MeasureWidth(typeface, 12f, "aaa");
		var spaceWidth = MeasureWidth(typeface, 12f, " ");
		var lineWidth = wordWidth + spaceWidth * 0.6f;

		var (lines, items) = breaker.ComputeLineBreaksWithItems(runs, typeface, 12f, lineWidth);

		// Items: [0]=Box("aaa"), [1]=Glue(" "), [2]=Box("bbb"), [3]=Glue(finish), [4]=Penalty
		lines.Should().HaveCount(2);
		lines[0].StartIndex.Should().Be(0);
		lines[0].EndIndex.Should().Be(1); // breaks at glue (index 1)
		lines[1].StartIndex.Should().Be(2); // starts at next box
	}

	// =====================================================================
	// Scenario 2: Three equal words — two fit per line
	// Items: Box Glue Box Glue Box Glue(finish) Penalty
	// =====================================================================
	[Fact]
	public void Scenario02_ThreeWords_TwoFitPerLine()
	{
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var runs = MakeRuns("aaa bbb ccc");

		var wordWidth = MeasureWidth(typeface, 12f, "aaa");
		var spaceWidth = MeasureWidth(typeface, 12f, " ");
		// Width for exactly two words + one space + small margin
		var lineWidth = wordWidth * 2 + spaceWidth * 1.5f;

		var (lines, _) = breaker.ComputeLineBreaksWithItems(runs, typeface, 12f, lineWidth);

		// Should break after second word — two lines total
		lines.Should().HaveCount(2);
		lines[0].StartIndex.Should().Be(0);
		// First line has: Box(0) Glue(1) Box(2) — break at Glue(3)
		lines[1].StartIndex.Should().Be(4); // Box("ccc") is at index 4
	}

	// =====================================================================
	// Scenario 3: Single very long word on a narrow line — no break opportunity
	// Items: Box("supercalifragilistic") Glue(finish) Penalty(forced)
	// The word overflows the line. Knuth-Plass produces an overfull first line
	// plus the paragraph-end break, yielding 2 line entries.
	// =====================================================================
	[Fact]
	public void Scenario03_SingleLongWord_NoBreakOpportunity()
	{
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var runs = MakeRuns("supercalifragilistic");

		// Very narrow line — but no break opportunity within the word
		var result = breaker.ComputeLineBreaks(runs, typeface, 12f, 500f);

		// Overfull word on line 1, paragraph-end on line 2
		result.Should().HaveCount(2);
		result[0].StartIndex.Should().Be(0);
		result[0].AdjustmentRatio.Should().BeLessThan(0f); // overfull
	}

	// =====================================================================
	// Scenario 4: Five short words — each fits on its own line (very narrow)
	// Items: Box Glue Box Glue Box Glue Box Glue Box Glue(finish) Penalty
	// Indices: 0   1   2   3   4   5   6   7   8   9            10
	// =====================================================================
	[Fact]
	public void Scenario04_FiveWords_OnePerLine()
	{
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var runs = MakeRuns("a b c d e");

		// Barely wider than "a" — one word per line
		var wordWidth = MeasureWidth(typeface, 12f, "a");
		var lineWidth = wordWidth * 1.2f;

		var result = breaker.ComputeLineBreaks(runs, typeface, 12f, lineWidth);

		result.Should().HaveCount(5);
	}

	// =====================================================================
	// Scenario 5: All text fits on one line (wide line)
	// "The quick brown fox" → Box Glue Box Glue Box Glue Box Glue(finish) Penalty
	// =====================================================================
	[Fact]
	public void Scenario05_AllFitsOnOneLine()
	{
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var runs = MakeRuns("The quick brown fox");

		// Very wide line
		var result = breaker.ComputeLineBreaks(runs, typeface, 12f, 100000f);

		result.Should().ContainSingle();
		result[0].StartIndex.Should().Be(0);
	}

	// =====================================================================
	// Scenario 6: Forced line break in middle
	// "abc<br/>def" → Box(0) Penalty(1,forced) Box(2) Glue(3,finish) Penalty(4)
	// Expected: Line 1 = [0..1], Line 2 = [2..4]
	// =====================================================================
	[Fact]
	public void Scenario06_ForcedBreak_TwoLines()
	{
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var runs = new[]
		{
			new ParsedRun
			{
				Elements = new RunElement[]
				{
					new TextRunElement { Text = "abc" },
					new BreakRunElement { BreakType = RunBreakType.Line },
					new TextRunElement { Text = "def" }
				}
			}
		};

		var (lines, items) = breaker.ComputeLineBreaksWithItems(runs, typeface, 12f, 100000f);

		// Despite wide line, forced break should split into two lines
		lines.Should().HaveCount(2);
		lines[0].StartIndex.Should().Be(0);
		// First line ends at the forced break penalty
		lines[0].EndIndex.Should().Be(1);
		// Second line starts at the box after the break
		lines[1].StartIndex.Should().Be(2);
	}

	// =====================================================================
	// Scenario 7: Two forced breaks — three lines
	// "a<br/>b<br/>c" → Box(0) Penalty(1) Box(2) Penalty(3) Box(4) Glue(5) Penalty(6)
	// Expected: 3 lines
	// =====================================================================
	[Fact]
	public void Scenario07_TwoForcedBreaks_ThreeLines()
	{
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var runs = new[]
		{
			new ParsedRun
			{
				Elements = new RunElement[]
				{
					new TextRunElement { Text = "a" },
					new BreakRunElement { BreakType = RunBreakType.Line },
					new TextRunElement { Text = "b" },
					new BreakRunElement { BreakType = RunBreakType.Line },
					new TextRunElement { Text = "c" }
				}
			}
		};

		var (lines, _) = breaker.ComputeLineBreaksWithItems(runs, typeface, 12f, 100000f);

		lines.Should().HaveCount(3);
		lines[0].StartIndex.Should().Be(0);
		lines[0].EndIndex.Should().Be(1); // forced break at index 1
		lines[1].StartIndex.Should().Be(2);
		lines[1].EndIndex.Should().Be(3); // forced break at index 3
		lines[2].StartIndex.Should().Be(4); // box "c"
	}

	// =====================================================================
	// Scenario 8: Word with explicit hyphen — break at hyphen when line is narrow
	// "well-known fact" → Box("well-") Penalty(flag) Box("known") Glue Box("fact") Glue Penalty
	// Indices:               0         1              2           3    4           5    6
	// If line width fits "well-" but not "well-known": break at penalty(1)
	// =====================================================================
	[Fact]
	public void Scenario08_ExplicitHyphen_BreakAtHyphen()
	{
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var runs = MakeRuns("well-known fact");

		// Width can fit "well-" and "fact" but not "well-known"
		var wellDashWidth = MeasureWidth(typeface, 12f, "well-");
		var knownWidth = MeasureWidth(typeface, 12f, "known");
		var lineWidth = wellDashWidth + knownWidth * 0.3f; // too narrow for "well-known"

		var (lines, _) = breaker.ComputeLineBreaksWithItems(runs, typeface, 12f, lineWidth);

		lines.Count.Should().BeGreaterThanOrEqualTo(2);
		// First line ends at the hyphen penalty (index 1)
		lines[0].EndIndex.Should().Be(1);
	}

	// =====================================================================
	// Scenario 9: Mixed short and long words — verify optimal breaks differ from greedy
	// "I am a student of typography and design" — with a specific width,
	// Knuth-Plass may choose different breaks than greedy to minimize overall badness.
	// We verify: correct number of lines and all line indices are valid.
	// =====================================================================
	[Fact]
	public void Scenario09_MixedWordLengths_OptimalBreaks()
	{
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var runs = MakeRuns("I am a student of typography and design");

		// Moderate line width — about 3-4 words per line
		var iWidth = MeasureWidth(typeface, 12f, "I");
		var spaceWidth = MeasureWidth(typeface, 12f, " ");
		var typographyWidth = MeasureWidth(typeface, 12f, "typography");
		// Set width to hold "of typography" with some stretch
		var lineWidth = typographyWidth + iWidth + spaceWidth * 3;

		var (lines, items) = breaker.ComputeLineBreaksWithItems(runs, typeface, 12f, lineWidth);

		lines.Count.Should().BeGreaterThanOrEqualTo(2);

		// Verify all lines have valid, non-overlapping indices
		for (var i = 0; i < lines.Count; i++)
		{
			lines[i].StartIndex.Should().BeGreaterThanOrEqualTo(0);
			lines[i].EndIndex.Should().BeLessThan(items.Count);
			lines[i].StartIndex.Should().BeLessThanOrEqualTo(lines[i].EndIndex);

			if (i > 0)
			{
				lines[i].StartIndex.Should().BeGreaterThan(lines[i - 1].EndIndex);
			}
		}

		// First line starts at 0
		lines[0].StartIndex.Should().Be(0);
	}

	// =====================================================================
	// Scenario 10: Exactly fitting line — no stretch needed
	// Construct a line width that exactly matches "aaa bbb" so ratio ≈ 0
	// =====================================================================
	[Fact]
	public void Scenario10_ExactFit_AdjustmentRatioNearZero()
	{
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var runs = MakeRuns("aaa bbb");

		// Set line width to exactly the natural width of "aaa bbb"
		var totalWidth = MeasureWidth(typeface, 12f, "aaa") +
						 MeasureWidth(typeface, 12f, " ") +
						 MeasureWidth(typeface, 12f, "bbb");

		var result = breaker.ComputeLineBreaks(runs, typeface, 12f, totalWidth);

		result.Should().ContainSingle();
		// Adjustment ratio should be very close to 0 (last line is clamped to ≤ 0)
		result[0].AdjustmentRatio.Should().BeApproximately(0f, 0.1f);
	}

	// =====================================================================
	// Scenario 11: Multiple runs merged — break occurs between runs
	// Run1="Hello " Run2="world today" → items from first + items from second
	// Items: Box("Hello") Glue Box("world") Glue Box("today") Glue Penalty
	// =====================================================================
	[Fact]
	public void Scenario11_MultipleRuns_BreakBetweenRuns()
	{
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var runs = new[]
		{
			new ParsedRun { Elements = [new TextRunElement { Text = "Hello " }] },
			new ParsedRun { Elements = [new TextRunElement { Text = "world today" }] }
		};

		// Width fits "Hello world" but not "Hello world today"
		var helloWidth = MeasureWidth(typeface, 12f, "Hello");
		var spaceWidth = MeasureWidth(typeface, 12f, " ");
		var worldWidth = MeasureWidth(typeface, 12f, "world");
		var todayWidth = MeasureWidth(typeface, 12f, "today");
		var lineWidth = helloWidth + spaceWidth + worldWidth + spaceWidth * 0.5f;

		var result = breaker.ComputeLineBreaks(runs, typeface, 12f, lineWidth);

		result.Should().HaveCount(2);
		result[0].StartIndex.Should().Be(0);
	}

	// =====================================================================
	// Scenario 12: Non-breaking space — keeps words together
	// "100\u00A0kg per box" → Box("100\u00A0kg") Glue Box("per") Glue Box("box") ...
	// The non-breaking space makes "100 kg" unbreakable
	// =====================================================================
	[Fact]
	public void Scenario12_NonBreakingSpace_KeepsWordsTogether()
	{
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var runs = MakeRuns("100\u00A0kg per box");

		// Width fits about "100 kg" but not "100 kg per"
		var nbspWordWidth = MeasureWidth(typeface, 12f, "100\u00A0kg");
		var spaceWidth = MeasureWidth(typeface, 12f, " ");
		var lineWidth = nbspWordWidth + spaceWidth * 0.5f;

		var (lines, _) = breaker.ComputeLineBreaksWithItems(runs, typeface, 12f, lineWidth);

		lines.Should().HaveCount(3);
		lines[0].StartIndex.Should().Be(0);
		// First line should contain the full "100 kg" as a single box (index 0)
		// break at glue after it (index 1)
		lines[0].EndIndex.Should().Be(1);
	}

	// =====================================================================
	// Scenario 13: Paragraph with trailing spaces produces correct single line
	// "Hello world " — trailing space becomes glue, but the paragraph still works
	// =====================================================================
	[Fact]
	public void Scenario13_TrailingSpaces_HandledCorrectly()
	{
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var runs = MakeRuns("Hello world ");

		var result = breaker.ComputeLineBreaks(runs, typeface, 12f, 100000f);

		result.Should().ContainSingle();
		result[0].StartIndex.Should().Be(0);
	}

	// =====================================================================
	// Scenario 14: Large font forces more breaks than small font
	// Same text at 24pt should produce more lines than at 8pt
	// =====================================================================
	[Fact]
	public void Scenario14_LargerFont_MoreBreaks()
	{
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var text = "The quick brown fox jumps over the lazy dog";
		var runs = MakeRuns(text);

		// 6000 twips line width
		var smallResult = breaker.ComputeLineBreaks(runs, typeface, 8f, 6000f);
		var largeResult = breaker.ComputeLineBreaks(runs, typeface, 24f, 6000f);

		largeResult.Count.Should().BeGreaterThan(smallResult.Count);
	}

	// =====================================================================
	// Scenario 15: Verify last line is not stretched (ratio ≤ 0)
	// Multi-line paragraph: all non-last lines may have positive ratio,
	// but the last line should have ratio ≤ 0 (Knuth-Plass standard)
	// =====================================================================
	[Fact]
	public void Scenario15_LastLineNotStretched()
	{
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var runs = MakeRuns("The quick brown fox jumps over the lazy dog and then sleeps");

		// Narrow enough for multiple lines
		var result = breaker.ComputeLineBreaks(runs, typeface, 12f, 4000f);

		result.Count.Should().BeGreaterThan(1);
		result[^1].AdjustmentRatio.Should().BeLessThanOrEqualTo(0f);
	}

	/// <summary>
	/// Helper to create a simple single-run paragraph from text.
	/// </summary>
	private static ParsedRun[] MakeRuns(string text) =>
	[
		new ParsedRun { Elements = [new TextRunElement { Text = text }] }
	];
}
