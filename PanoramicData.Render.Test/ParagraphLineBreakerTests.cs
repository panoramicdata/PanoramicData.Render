using AwesomeAssertions;
using SkiaSharp;
using Xunit;

namespace PanoramicData.Render.Test;

public sealed class ParagraphLineBreakerTests
{
	private const string LongText = "The quick brown fox jumps over the lazy dog";

	private readonly MeasurementEngine _engine = new();

	// The shipped test font, not a system font, so the results are the same on every machine.
	private static SKTypeface GetTypeface() => TestFonts.Sans;

	private ParagraphLineBreaker Breaker() => new(_engine);

	private static List<RunElement> ParseElements(string text)
	{
		List<RunElement> elements = [];
		var lines = text.Split('\n');
		for (var i = 0; i < lines.Length; i++)
		{
			if (i > 0)
			{
				elements.Add(new BreakRunElement { BreakType = RunBreakType.Line });
			}

			var parts = lines[i].Split('\t');
			for (var j = 0; j < parts.Length; j++)
			{
				if (j > 0)
				{
					elements.Add(InlineImage());
				}

				elements.Add(new TextRunElement { Text = parts[j] });
			}
		}

		return elements;
	}

	private static InlineImageRunElement InlineImage() => new()
	{
		RelationshipId = "rId1",
		WidthEmu = 914400,
		HeightEmu = 914400
	};

	/// <summary>Runs separated by |; within a run \n is a line break, \t an inline image and ~ an empty text run.</summary>
	private static ParsedRun[] Runs(string spec) =>
		spec.Length == 0
			? []
			: [.. spec.Split('|').Select(run => new ParsedRun { Elements = run == "~" ? [new TextRunElement { Text = string.Empty }] : [.. ParseElements(run)] })];

	[Fact]
	public void ComputeLineBreaks_NullRuns_ThrowsArgumentNullException()
	{
		var act = () => Breaker().ComputeLineBreaks(null!, GetTypeface(), 12f, 5000f);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void ComputeLineBreaks_NullTypeface_ThrowsArgumentNullException()
	{
		var act = () => Breaker().ComputeLineBreaks([], null!, 12f, 5000f);

		act.Should().Throw<ArgumentNullException>();
	}

	[Theory]
	[InlineData(0f, 5000f)]
	[InlineData(12f, 0f)]
	public void ComputeLineBreaks_NonPositiveSize_ThrowsArgumentOutOfRangeException(float fontSize, float lineWidth)
	{
		var act = () => Breaker().ComputeLineBreaks([], GetTypeface(), fontSize, lineWidth);

		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Theory]
	[InlineData("", 5000f, 0, 0)]
	[InlineData("~", 5000f, 0, 0)]
	[InlineData("Hello world", 50000f, 1, 1)]
	[InlineData("The quick brown fox jumps over the lazy dog and continues running across the field", 3000f, 2, int.MaxValue)]
	[InlineData(LongText, 3000f, 1, int.MaxValue)]
	[InlineData("First\nSecond", 50000f, 2, int.MaxValue)]
	[InlineData("Hello |world", 50000f, 1, 1)]
	public void ComputeLineBreaks_ProducesExpectedLineCount(string spec, float lineWidth, int minLines, int maxLines)
	{
		var result = Breaker().ComputeLineBreaks(Runs(spec), GetTypeface(), 12f, lineWidth);

		result.Count.Should().BeInRange(minLines, maxLines);
		if (result.Count > 0)
		{
			result[0].StartIndex.Should().Be(0);
		}

		for (var i = 1; i < result.Count; i++)
		{
			result[i].StartIndex.Should().BeGreaterThan(result[i - 1].EndIndex);
		}
	}

	[Fact]
	public void ComputeLineBreaks_SingleLine_AdjustmentRatioIsNonPositive()
	{
		var result = Breaker().ComputeLineBreaks(Runs("Hello"), GetTypeface(), 12f, 50000f);

		result.Should().ContainSingle();
		result[0].AdjustmentRatio.Should().BeLessThanOrEqualTo(0f);
	}

	[Fact]
	public void ComputeLineBreaks_WithHyphenation_CanBreakAtHyphenationPoints()
	{
		var typeface = GetTypeface();
		var dict = new HyphenationDictionary();
		dict.AddPattern("om1p");
		dict.AddPattern("pu1t");
		var breaker = new ParagraphLineBreaker(_engine, dict);
		var fullWidth = new TextRunToItemMapper(_engine).MapTextRun("computer", typeface, 12f).Sum(i => i.Width);

		var result = breaker.ComputeLineBreaks(Runs("computer"), typeface, 12f, fullWidth * 0.6f);

		result.Count.Should().BeGreaterThanOrEqualTo(2);
	}

	[Fact]
	public void ComputeLineBreaksWithItems_ReturnsItemsAndLines()
	{
		var (lines, items) = Breaker().ComputeLineBreaksWithItems(Runs("Hello world"), GetTypeface(), 12f, 50000f);

		lines.Should().ContainSingle();
		items.Should().NotBeEmpty();
		items.Count.Should().BeGreaterThanOrEqualTo(3); // Box + Glue + Box minimum
	}

	[Fact]
	public void ComputeLineBreaksWithItems_ItemIndicesReferToReturnedItems()
	{
		var (lines, items) = Breaker().ComputeLineBreaksWithItems(Runs(LongText), GetTypeface(), 12f, 3000f);

		foreach (var line in lines)
		{
			line.StartIndex.Should().BeGreaterThanOrEqualTo(0);
			line.EndIndex.Should().BeLessThan(items.Count);
			line.StartIndex.Should().BeLessThanOrEqualTo(line.EndIndex);
		}
	}

	[Fact]
	public void ComputeLineBreaksWithItems_InlineImageWidthPreservedInItems()
	{
		var (_, items) = Breaker().ComputeLineBreaksWithItems(Runs("A \t B"), SKTypeface.Default, 12f, 50000f);

		items.OfType<KnuthPlassBox>().Should().Contain(i => Math.Abs(i.Width - 1440f) < 0.001f);
	}

	[Fact]
	public void ComputeLineBreaks_InlineImageInfluencesWrapDecision()
	{
		var typeface = GetTypeface();
		var breaker = Breaker();
		var (_, textOnlyItems) = breaker.ComputeLineBreaksWithItems(Runs("word tail"), typeface, 12f, 50000f);
		var constrainedWidth = textOnlyItems.Where(i => i is not KnuthPlassPenalty).Sum(i => i.Width) + 20f;

		var linesWithoutImage = breaker.ComputeLineBreaks(Runs("word tail"), typeface, 12f, constrainedWidth);
		var linesWithImage = breaker.ComputeLineBreaks(Runs("word \t tail"), typeface, 12f, constrainedWidth);

		linesWithoutImage.Should().ContainSingle();
		linesWithImage.Count.Should().BeGreaterThanOrEqualTo(2);
	}

	[Fact]
	public void ComputeLineBreaks_AlwaysTerminatesWithForcedBreak()
	{
		var (_, items) = Breaker().ComputeLineBreaksWithItems(Runs("Hello world"), GetTypeface(), 12f, 50000f);

		items[^1].Should().BeOfType<KnuthPlassPenalty>();
		((KnuthPlassPenalty)items[^1]).Penalty.Should().Be(float.NegativeInfinity);
	}

	[Fact]
	public void ComputeLineBreaks_FinalGlueBeforePenalty_HasInfiniteStretch()
	{
		var (_, items) = Breaker().ComputeLineBreaksWithItems(Runs("Hello"), GetTypeface(), 12f, 50000f);

		items[^2].Should().BeOfType<KnuthPlassGlue>();
		var finishGlue = (KnuthPlassGlue)items[^2];
		finishGlue.Width.Should().Be(0f);
		finishGlue.Stretch.Should().Be(float.PositiveInfinity);
		finishGlue.Shrink.Should().Be(0f);
	}
}
