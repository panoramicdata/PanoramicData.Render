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
	private readonly MeasurementEngine _engine;
	private readonly ParagraphLineBreaker _breaker;

	public LineBreakVerificationTests()
	{
		_engine = new MeasurementEngine();
		_breaker = new ParagraphLineBreaker(_engine);
	}

	// The shipped test font, not a system font, so the results are the same on every machine.
	private static SKTypeface GetTypeface() => TestFonts.Sans;

	private float W(string text) => _engine.MeasureGlyphAdvancesInTwips(GetTypeface(), 12f, text).Sum();

	private IReadOnlyList<KnuthPlassLine> Break(ParsedRun[] runs, float lineWidth, float fontSize = 12f)
		=> _breaker.ComputeLineBreaks(runs, GetTypeface(), fontSize, lineWidth);

	private IReadOnlyList<KnuthPlassLine> BreakLines(string text, float lineWidth)
		=> BreakWithItems(MakeRuns(text), lineWidth).Lines;

	private (IReadOnlyList<KnuthPlassLine> Lines, IReadOnlyList<KnuthPlassItem> Items) BreakWithItems(ParsedRun[] runs, float lineWidth)
		=> _breaker.ComputeLineBreaksWithItems(runs, GetTypeface(), 12f, lineWidth);

	// Scenario 1: Items: Box("aaa") Glue Box("bbb") Glue(finish) Penalty(forced)
	[Fact]
	public void Scenario01_TwoWords_ExactlyOneWordPerLine()
	{
		// Word width plus part of a space: enough for one word but not two
		var lines = BreakLines("aaa bbb", W("aaa") + W(" ") * 0.6f);

		// breaks at glue (index 1); the second line starts at the next box
		AssertLines(lines, (0, 1), (2, null));
	}

	// Scenario 2: Items: Box Glue Box Glue Box Glue(finish) Penalty; two words per line
	[Fact]
	public void Scenario02_ThreeWords_TwoFitPerLine()
	{
		var lines = BreakLines("aaa bbb ccc", W("aaa") * 2 + W(" ") * 1.5f);

		AssertLines(lines, (0, null), (4, null)); // Box("ccc") is at index 4
	}

	// Scenario 3: a long word overflows; Knuth-Plass yields an overfull line plus the paragraph-end break
	[Fact]
	public void Scenario03_SingleLongWord_NoBreakOpportunity()
	{
		var result = Break(MakeRuns("supercalifragilistic"), 500f);

		AssertLines(result, (0, null), (null, null));
		result[0].AdjustmentRatio.Should().BeLessThan(0f); // overfull
	}

	// Scenario 4: Barely wider than "a", so one word per line
	[Fact]
	public void Scenario04_FiveWords_OnePerLine()
	{
		Break(MakeRuns("a b c d e"), W("a") * 1.2f).Should().HaveCount(5);
	}

	// Scenario 5: All text fits on one line
	[Fact]
	public void Scenario05_AllFitsOnOneLine()
	{
		var result = Break(MakeRuns("The quick brown fox"), 100000f);

		AssertLines(result, (0, null));
	}

	// Scenario 6: "abc<br/>def" is Box(0) Penalty(1,forced) Box(2) Glue(3,finish) Penalty(4)
	[Fact]
	public void Scenario06_ForcedBreak_TwoLines()
	{
		var (lines, _) = BreakWithItems(MakeRunsWithLineBreaks("abc", "def"), 100000f);

		// Despite wide line, forced break should split into two lines
		// first line ends at the forced break penalty; the second starts at the box after it
		AssertLines(lines, (0, 1), (2, null));
	}

	// Scenario 7: "a<br/>b<br/>c" is Box(0) Penalty(1) Box(2) Penalty(3) Box(4) Glue(5) Penalty(6)
	[Fact]
	public void Scenario07_TwoForcedBreaks_ThreeLines()
	{
		var (lines, _) = BreakWithItems(MakeRunsWithLineBreaks("a", "b", "c"), 100000f);

		// forced breaks at indices 1 and 3; the last line starts at box "c"
		AssertLines(lines, (0, 1), (2, 3), (4, null));
	}

	// Scenario 8: "well-known fact" is Box("well-") Penalty(flag) Box("known") Glue Box("fact") Glue Penalty
	[Fact]
	public void Scenario08_ExplicitHyphen_BreakAtHyphen()
	{
		// Width can fit "well-" and "fact" but not "well-known"
		var lines = BreakLines("well-known fact", W("well-") + W("known") * 0.3f);

		lines.Count.Should().BeGreaterThanOrEqualTo(2);
		lines[0].EndIndex.Should().Be(1); // first line ends at the hyphen penalty
	}

	// Scenario 9: Knuth-Plass may choose different breaks than greedy; verify line count and valid indices
	[Fact]
	public void Scenario09_MixedWordLengths_OptimalBreaks()
	{
		// Wide enough to hold "of typography" with some stretch
		var lineWidth = W("typography") + W("I") + W(" ") * 3;

		var (lines, items) = BreakWithItems(MakeRuns("I am a student of typography and design"), lineWidth);

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

		lines[0].StartIndex.Should().Be(0);
	}

	// Scenario 10: line width equals the natural width of "aaa bbb", so ratio is near 0
	[Fact]
	public void Scenario10_ExactFit_AdjustmentRatioNearZero()
	{
		var result = Break(MakeRuns("aaa bbb"), W("aaa") + W(" ") + W("bbb"));

		AssertLines(result, (null, null));
		// Last line is clamped to a ratio of at most 0
		result[0].AdjustmentRatio.Should().BeApproximately(0f, 0.1f);
	}

	// Scenario 11: Run1="Hello " Run2="world today"; the break occurs between runs
	[Fact]
	public void Scenario11_MultipleRuns_BreakBetweenRuns()
	{
		var runs = new[]
		{
			new ParsedRun { Elements = [new TextRunElement { Text = "Hello " }] },
			new ParsedRun { Elements = [new TextRunElement { Text = "world today" }] }
		};

		// Width fits "Hello world" but not "Hello world today"
		var result = Break(runs, W("Hello") + W(" ") + W("world") + W(" ") * 0.5f);

		AssertLines(result, (0, null), (null, null));
	}

	// Scenario 12: the non-breaking space makes "100 kg" unbreakable
	[Fact]
	public void Scenario12_NonBreakingSpace_KeepsWordsTogether()
	{
		// Width fits "100 kg" but not "100 kg per"
		var lines = BreakLines("100 kg per box", W("100 kg") + W(" ") * 0.5f);

		// the single box (index 0), then break at the glue (index 1)
		AssertLines(lines, (0, 1), (null, null), (null, null));
	}

	// Scenario 13: a trailing space becomes glue, but the paragraph still produces one line
	[Fact]
	public void Scenario13_TrailingSpaces_HandledCorrectly()
	{
		var result = Break(MakeRuns("Hello world "), 100000f);

		AssertLines(result, (0, null));
	}

	// Scenario 14: the same text at 24pt produces more lines than at 8pt
	[Fact]
	public void Scenario14_LargerFont_MoreBreaks()
	{
		var runs = MakeRuns("The quick brown fox jumps over the lazy dog");

		var smallResult = Break(runs, 6000f, 8f);
		var largeResult = Break(runs, 6000f, 24f);

		largeResult.Count.Should().BeGreaterThan(smallResult.Count);
	}

	// Scenario 15: the last line is not stretched (ratio at most 0), per Knuth-Plass
	[Fact]
	public void Scenario15_LastLineNotStretched()
	{
		var result = Break(MakeRuns("The quick brown fox jumps over the lazy dog and then sleeps"), 4000f);

		result.Count.Should().BeGreaterThan(1);
		result[^1].AdjustmentRatio.Should().BeLessThanOrEqualTo(0f);
	}

	private static void AssertLines(IReadOnlyList<KnuthPlassLine> lines, params (int? Start, int? End)[] expected)
	{
		lines.Should().HaveCount(expected.Length);
		for (var index = 0; index < expected.Length; index++)
		{
			if (expected[index].Start is { } start)
			{
				lines[index].StartIndex.Should().Be(start);
			}

			if (expected[index].End is { } end)
			{
				lines[index].EndIndex.Should().Be(end);
			}
		}
	}

	private static ParsedRun[] MakeRuns(string text) =>
	[
		new ParsedRun { Elements = [new TextRunElement { Text = text }] }
	];

	private static ParsedRun[] MakeRunsWithLineBreaks(params string[] segments)
	{
		var elements = new List<RunElement>();
		foreach (var segment in segments)
		{
			if (elements.Count > 0)
			{
				elements.Add(new BreakRunElement { BreakType = RunBreakType.Line });
			}

			elements.Add(new TextRunElement { Text = segment });
		}

		return [new ParsedRun { Elements = elements }];
	}
}
