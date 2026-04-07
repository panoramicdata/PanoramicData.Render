namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using SkiaSharp;
using Xunit;

public sealed class ParagraphLineBreakerTests
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

	// --- Guard tests ---

	[Fact]
	public void ComputeLineBreaks_NullRuns_ThrowsArgumentNullException()
	{
		var breaker = new ParagraphLineBreaker(_engine);
		var typeface = SKTypeface.Default;

		var act = () => breaker.ComputeLineBreaks(null!, typeface, 12f, 5000f);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void ComputeLineBreaks_NullTypeface_ThrowsArgumentNullException()
	{
		var breaker = new ParagraphLineBreaker(_engine);

		var act = () => breaker.ComputeLineBreaks([], null!, 12f, 5000f);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void ComputeLineBreaks_ZeroFontSize_ThrowsArgumentOutOfRangeException()
	{
		var breaker = new ParagraphLineBreaker(_engine);
		var typeface = SKTypeface.Default;

		var act = () => breaker.ComputeLineBreaks([], typeface, 0f, 5000f);

		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void ComputeLineBreaks_ZeroLineWidth_ThrowsArgumentOutOfRangeException()
	{
		var breaker = new ParagraphLineBreaker(_engine);
		var typeface = SKTypeface.Default;

		var act = () => breaker.ComputeLineBreaks([], typeface, 12f, 0f);

		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	// --- Empty input ---

	[Fact]
	public void ComputeLineBreaks_EmptyRuns_ReturnsEmpty()
	{
		var breaker = new ParagraphLineBreaker(_engine);
		var typeface = SKTypeface.Default;

		var result = breaker.ComputeLineBreaks([], typeface, 12f, 5000f);

		result.Should().BeEmpty();
	}

	[Fact]
	public void ComputeLineBreaks_RunWithEmptyText_ReturnsEmpty()
	{
		var breaker = new ParagraphLineBreaker(_engine);
		var typeface = SKTypeface.Default;
		var runs = new[]
		{
			new ParsedRun { Elements = [new TextRunElement { Text = "" }] }
		};

		var result = breaker.ComputeLineBreaks(runs, typeface, 12f, 5000f);

		result.Should().BeEmpty();
	}

	// --- Single line ---

	[Fact]
	public void ComputeLineBreaks_ShortText_FitsOnOneLine()
	{
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var runs = new[]
		{
			new ParsedRun { Elements = [new TextRunElement { Text = "Hello world" }] }
		};

		// Use a very wide line so everything fits
		var result = breaker.ComputeLineBreaks(runs, typeface, 12f, 50000f);

		result.Should().ContainSingle();
		result[0].StartIndex.Should().Be(0);
	}

	// --- Multiple lines ---

	[Fact]
	public void ComputeLineBreaks_LongText_BreaksIntoMultipleLines()
	{
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var runs = new[]
		{
			new ParsedRun
			{
				Elements = [new TextRunElement
				{
					Text = "The quick brown fox jumps over the lazy dog and continues running across the field"
				}]
			}
		};

		// Use a narrow line width to force multiple breaks
		var result = breaker.ComputeLineBreaks(runs, typeface, 12f, 3000f);

		result.Count.Should().BeGreaterThan(1);
	}

	[Fact]
	public void ComputeLineBreaks_MultipleLines_IndicesAreContiguous()
	{
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var runs = new[]
		{
			new ParsedRun
			{
				Elements = [new TextRunElement
				{
					Text = "The quick brown fox jumps over the lazy dog"
				}]
			}
		};

		var result = breaker.ComputeLineBreaks(runs, typeface, 12f, 3000f);

		// First line starts at 0
		result[0].StartIndex.Should().Be(0);

		// Each subsequent line should start after the previous line's end
		for (var i = 1; i < result.Count; i++)
		{
			result[i].StartIndex.Should().BeGreaterThan(result[i - 1].EndIndex);
		}
	}

	// --- Forced breaks ---

	[Fact]
	public void ComputeLineBreaks_ForcedLineBreak_BreaksAtBreakElement()
	{
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var runs = new[]
		{
			new ParsedRun
			{
				Elements = new RunElement[]
				{
					new TextRunElement { Text = "First" },
					new BreakRunElement { BreakType = RunBreakType.Line },
					new TextRunElement { Text = "Second" }
				}
			}
		};

		// Wide enough for either part to fit on a single line
		var result = breaker.ComputeLineBreaks(runs, typeface, 12f, 50000f);

		result.Count.Should().BeGreaterThanOrEqualTo(2);
	}

	// --- Multiple runs ---

	[Fact]
	public void ComputeLineBreaks_MultipleRuns_CombinesElements()
	{
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var runs = new[]
		{
			new ParsedRun { Elements = [new TextRunElement { Text = "Hello " }] },
			new ParsedRun { Elements = [new TextRunElement { Text = "world" }] }
		};

		var result = breaker.ComputeLineBreaks(runs, typeface, 12f, 50000f);

		// "Hello world" should fit on one line
		result.Should().ContainSingle();
	}

	// --- Adjustment ratio ---

	[Fact]
	public void ComputeLineBreaks_SingleLine_AdjustmentRatioIsNonPositive()
	{
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var runs = new[]
		{
			new ParsedRun { Elements = [new TextRunElement { Text = "Hello" }] }
		};

		var result = breaker.ComputeLineBreaks(runs, typeface, 12f, 50000f);

		// Last line should not be stretched
		result.Should().ContainSingle();
		result[0].AdjustmentRatio.Should().BeLessThanOrEqualTo(0f);
	}

	// --- With hyphenation ---

	[Fact]
	public void ComputeLineBreaks_WithHyphenation_CanBreakAtHyphenationPoints()
	{
		var typeface = GetTypeface();
		var dict = new HyphenationDictionary();
		dict.AddPattern("om1p");
		dict.AddPattern("pu1t");
		var breaker = new ParagraphLineBreaker(_engine, dict);

		var runs = new[]
		{
			new ParsedRun { Elements = [new TextRunElement { Text = "computer" }] }
		};

		// Use a width that is narrower than the full word but wide enough
		// to fit "com-" (so it can hyphenate)
		var mapper = new TextRunToItemMapper(_engine);
		var fullWidth = mapper.MapTextRun("computer", typeface, 12f).Sum(i => i.Width);

		// Set line width to about 60% of the word — should force a hyphenation break
		var result = breaker.ComputeLineBreaks(runs, typeface, 12f, fullWidth * 0.6f);

		result.Count.Should().BeGreaterThanOrEqualTo(2);
	}

	// --- Items accessor ---

	[Fact]
	public void ComputeLineBreaksWithItems_ReturnsItemsAndLines()
	{
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var runs = new[]
		{
			new ParsedRun { Elements = [new TextRunElement { Text = "Hello world" }] }
		};

		var (lines, items) = breaker.ComputeLineBreaksWithItems(runs, typeface, 12f, 50000f);

		lines.Should().ContainSingle();
		items.Should().NotBeEmpty();
		items.Count.Should().BeGreaterThanOrEqualTo(3); // Box + Glue + Box minimum
	}

	[Fact]
	public void ComputeLineBreaksWithItems_ItemIndicesReferToReturnedItems()
	{
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var runs = new[]
		{
			new ParsedRun
			{
				Elements = [new TextRunElement
				{
					Text = "The quick brown fox jumps over the lazy dog"
				}]
			}
		};

		var (lines, items) = breaker.ComputeLineBreaksWithItems(runs, typeface, 12f, 3000f);

		foreach (var line in lines)
		{
			line.StartIndex.Should().BeGreaterThanOrEqualTo(0);
			line.EndIndex.Should().BeLessThan(items.Count);
			line.StartIndex.Should().BeLessThanOrEqualTo(line.EndIndex);
		}
	}

	// --- Paragraph finalizer (trailing glue + penalty) ---

	[Fact]
	public void ComputeLineBreaks_AlwaysTerminatesWithForcedBreak()
	{
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var runs = new[]
		{
			new ParsedRun { Elements = [new TextRunElement { Text = "Hello world" }] }
		};

		var (lines, items) = breaker.ComputeLineBreaksWithItems(runs, typeface, 12f, 50000f);

		// The last item should be a forced-break penalty (paragraph terminator)
		items[^1].Should().BeOfType<KnuthPlassPenalty>();
		var lastPenalty = (KnuthPlassPenalty)items[^1];
		lastPenalty.Penalty.Should().Be(float.NegativeInfinity);
	}

	[Fact]
	public void ComputeLineBreaks_FinalGlueBeforePenalty_HasInfiniteStretch()
	{
		var typeface = GetTypeface();
		var breaker = new ParagraphLineBreaker(_engine);
		var runs = new[]
		{
			new ParsedRun { Elements = [new TextRunElement { Text = "Hello" }] }
		};

		var (_, items) = breaker.ComputeLineBreaksWithItems(runs, typeface, 12f, 50000f);

		// Second-to-last item should be finishing glue with infinite stretch
		items[^2].Should().BeOfType<KnuthPlassGlue>();
		var finishGlue = (KnuthPlassGlue)items[^2];
		finishGlue.Width.Should().Be(0f);
		finishGlue.Stretch.Should().Be(float.PositiveInfinity);
		finishGlue.Shrink.Should().Be(0f);
	}
}
