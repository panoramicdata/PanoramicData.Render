using AwesomeAssertions;
using SkiaSharp;
using Xunit;

namespace PanoramicData.Render.Test;

public partial class TextRunToItemMapperTests
{
	[Fact]
	public void MapRunElements_ColumnBreak_ProducesForcedBreakWithColumnType()
	{
		var mapper = new TextRunToItemMapper(_engine);
		var typeface = SKTypeface.Default;
		var elements = new RunElement[]
		{
			new BreakRunElement { BreakType = RunBreakType.Column }
		};

		var items = mapper.MapRunElements(elements, typeface, 12f);

		AssertKinds(items, "P");

		var penalty = (KnuthPlassPenalty)items[0];
		penalty.Penalty.Should().Be(float.NegativeInfinity);
		penalty.BreakType.Should().Be(RunBreakType.Column);
	}

	[Fact]
	public void MapRunElements_TabElement_ProducesGlue()
	{
		var items = MapElements(
			new TextRunElement { Text = "a" },
			new TabRunElement(),
			new TextRunElement { Text = "b" }
		);

		// Box("a") + Glue(tab) + Box("b")
		AssertKinds(items, "BGB");
	}

	[Fact]
	public void MapRunElements_InlineImageElement_ProducesBoxWithImageWidth()
	{
		var mapper = new TextRunToItemMapper(_engine);
		var typeface = SKTypeface.Default;
		var elements = new RunElement[]
		{
			new InlineImageRunElement
			{
				RelationshipId = "rId1",
				WidthEmu = 914400,
				HeightEmu = 914400
			}
		};

		var items = mapper.MapRunElements(elements, typeface, 12f);

		AssertKinds(items, "B");
		items[0].Width.Should().BeApproximately(1440f, 0.001f);
	}

	[Fact]
	public void MapRunElements_InlineImageBetweenText_ProducesBoxSequence()
	{
		var mapper = new TextRunToItemMapper(_engine);
		var typeface = SKTypeface.Default;
		var elements = new RunElement[]
		{
			new TextRunElement { Text = "A" },
			new InlineImageRunElement
			{
				RelationshipId = "rId2",
				WidthEmu = 457200,
				HeightEmu = 228600
			},
			new TextRunElement { Text = "B" }
		};

		var items = mapper.MapRunElements(elements, typeface, 12f);

		AssertKinds(items, "BBB");
		items[1].Width.Should().BeApproximately(720f, 0.001f);
	}

	[Fact]
	public void MapRunElements_MultipleTextElements_ConcatenatesItems()
	{
		var items = MapElements(
			new TextRunElement { Text = "Hello" },
			new TextRunElement { Text = " world" }
		);

		// Box("Hello") from first, then Glue(" ") + Box("world") from second
		AssertKinds(items, "BGB");
	}

	[Fact]
	public void MapRunElements_BreakBetweenWords_ProducesCorrectSequence()
	{
		var items = MapElements(
			new TextRunElement { Text = "first line" },
			new BreakRunElement { BreakType = RunBreakType.Line },
			new TextRunElement { Text = "second line" }
		);

		// Box("first") Glue Box("line") ForcedBreak Box("second") Glue Box("line")
		AssertKinds(items, "BGBPBGB");

		var penalty = (KnuthPlassPenalty)items[3];
		penalty.Penalty.Should().Be(float.NegativeInfinity);
	}

	[Fact]
	public void MapRunElements_ForcedBreakPenalty_HasZeroWidth()
	{
		var mapper = new TextRunToItemMapper(_engine);
		var typeface = SKTypeface.Default;
		var elements = new RunElement[]
		{
			new BreakRunElement { BreakType = RunBreakType.Line }
		};

		var items = mapper.MapRunElements(elements, typeface, 12f);

		var penalty = (KnuthPlassPenalty)items[0];
		penalty.Width.Should().Be(0f);
		penalty.IsFlagged.Should().BeFalse();
	}

	// --- Non-breaking space tests (U+00A0) ---

	[Fact]
	public void MapTextRun_NonBreakingSpace_TreatedAsWordCharacter()
	{
		// "100\u00A0kg" should be a single box (no break opportunity)
		var items = MapText("100\u00A0kg");

		AssertKinds(items, "B");
	}

	[Fact]
	public void MapTextRun_NonBreakingSpaceBetweenWords_NoGlue()
	{
		// Words joined by non-breaking space should not produce glue
		var items = MapText("Mr.\u00A0Smith");

		// Should be treated as a single word (including the hyphen split logic doesn't affect it)
		AssertKinds(items, "B");
		items[0].Width.Should().BeGreaterThan(0f);
	}

	[Fact]
	public void MapTextRun_NonBreakingSpaceWithRegularSpaces_BreakOnlyAtRegularSpaces()
	{
		// "100\u00A0kg each" — non-breaking space joins "100" and "kg", but regular space after "kg" is a break
		var items = MapText("100\u00A0kg each");

		// Box("100\u00A0kg") + Glue + Box("each")
		AssertKinds(items, "BGB");
	}

	// --- Non-breaking hyphen tests (U+2011) ---

	[Fact]
	public void MapTextRun_NonBreakingHyphen_DoesNotCreatePenalty()
	{
		// Non-breaking hyphen (U+2011) should NOT create a break opportunity
		var items = MapText("well\u2011known");

		// Should be a single box — no penalty break at non-breaking hyphen
		AssertKinds(items, "B");
		items[0].Width.Should().BeGreaterThan(0f);
	}

	[Fact]
	public void MapTextRun_NonBreakingHyphenVsRegularHyphen_DifferentBehavior()
	{
		var typeface = GetTypeface();
		var mapper = new TextRunToItemMapper(_engine);

		// Regular hyphen: break opportunity
		var withRegularHyphen = mapper.MapTextRun("well-known", typeface, 12f);
		// Non-breaking hyphen: no break opportunity
		var withNonBreakingHyphen = mapper.MapTextRun("well\u2011known", typeface, 12f);

		// Regular hyphen produces Box-Penalty-Box
		withRegularHyphen.Should().HaveCount(3);
		withRegularHyphen[1].Should().BeOfType<KnuthPlassPenalty>();

		// Non-breaking hyphen produces single Box
		withNonBreakingHyphen.Should().ContainSingle()
			.Which.Should().BeOfType<KnuthPlassBox>();
	}

	// --- NonBreakingHyphenRunElement tests ---

	[Fact]
	public void MapRunElements_NonBreakingHyphenElement_ProducesBox()
	{
		var items = MapElements(
			new TextRunElement { Text = "well" },
			new NonBreakingHyphenRunElement(),
			new TextRunElement { Text = "known" }
		);

		// Box("well") + Box(hyphen-width) + Box("known") — all boxes, no penalties
		AssertKinds(items, "BBB");
	}

	[Fact]
	public void MapRunElements_NonBreakingHyphenElement_HasPositiveWidth()
	{
		var items = MapElements(
			new NonBreakingHyphenRunElement()
		);

		AssertKinds(items, "B");
		items[0].Width.Should().BeGreaterThan(0f);
	}

	[Fact]
	public void MapRunElements_NonBreakingHyphenElement_WidthMatchesHyphenCharacter()
	{
		var typeface = GetTypeface();
		var mapper = new TextRunToItemMapper(_engine);

		// Measure a regular hyphen character for comparison
		var hyphenItems = mapper.MapTextRun("-", typeface, 12f);
		var expectedWidth = hyphenItems[0].Width;

		// Non-breaking hyphen element should have the same width
		var elements = new RunElement[]
		{
			new NonBreakingHyphenRunElement()
		};
		var items = mapper.MapRunElements(elements, typeface, 12f);

		items[0].Width.Should().BeApproximately(expectedWidth, 1f);
	}

	// --- Automatic hyphenation tests ---

	[Fact]
	public void MapTextRun_WithoutHyphenation_NoDiscretionaryBreaks()
	{
		// Without hyphenation dictionary, a single word produces a single box
		var items = MapText("computer");

		AssertKinds(items, "B");
	}

	[Fact]
	public void MapTextRun_WithHyphenation_InsertsDiscretionaryBreaks()
	{
		var dict = new HyphenationDictionary();
		dict.AddPattern("om1p");
		dict.AddPattern("pu1t");
		var mapper = new TextRunToItemMapper(_engine, dict);
		var typeface = GetTypeface();

		// "computer" with patterns → com|put|er
		var items = mapper.MapTextRun("computer", typeface, 12f);

		// Should have discretionary break penalties between segments
		// Box("com") + Penalty + Box("put") + Penalty + Box("er")
		AssertKinds(items, "BPBPB");
	}

	[Fact]
	public void MapTextRun_WithHyphenation_PenaltyIsFlagged()
	{
		var dict = new HyphenationDictionary();
		dict.AddPattern("om1p");
		var mapper = new TextRunToItemMapper(_engine, dict);
		var typeface = GetTypeface();

		var items = mapper.MapTextRun("computer", typeface, 12f);

		var penalty = items.OfType<KnuthPlassPenalty>().First();
		penalty.IsFlagged.Should().BeTrue();
	}

	[Fact]
	public void MapTextRun_WithHyphenation_PenaltyWidthIsHyphenWidth()
	{
		var dict = new HyphenationDictionary();
		dict.AddPattern("om1p");
		var mapper = new TextRunToItemMapper(_engine, dict);
		var typeface = GetTypeface();

		// Measure expected hyphen width
		var hyphenItems = mapper.MapTextRun("-", typeface, 12f);
		var expectedHyphenWidth = hyphenItems[0].Width;

		var items = mapper.MapTextRun("computer", typeface, 12f);

		var penalty = items.OfType<KnuthPlassPenalty>().First();
		penalty.Width.Should().BeApproximately(expectedHyphenWidth, 1f);
	}

	[Fact]
	public void MapTextRun_WithHyphenation_WordWithNoPoints_SingleBox()
	{
		var dict = new HyphenationDictionary();
		dict.AddPattern("xy1z"); // pattern that won't match "hello"
		var mapper = new TextRunToItemMapper(_engine, dict);
		var typeface = GetTypeface();

		var items = mapper.MapTextRun("hello", typeface, 12f);

		AssertKinds(items, "B");
	}

	[Fact]
	public void MapTextRun_WithHyphenation_ExplicitHyphenAndAutoHyphen_BothPresent()
	{
		var dict = new HyphenationDictionary();
		dict.AddPattern("ow1l");
		var mapper = new TextRunToItemMapper(_engine, dict);
		var typeface = GetTypeface();

		// "well-known" has explicit hyphen AND potential auto-hyphenation in "known"
		// Explicit: "well-" + penalty + "known" → then "known" gets auto-hyphenated to "kn" + penalty + "own"
		var items = mapper.MapTextRun("well-knowledge", typeface, 12f);

		// Should have explicit hyphen penalty AND discretionary penalties
		var penalties = items.OfType<KnuthPlassPenalty>().ToList();
		penalties.Count.Should().BeGreaterThanOrEqualTo(2);
	}

	[Fact]
	public void MapTextRun_WithHyphenation_MultipleWordsWithSpaces()
	{
		var dict = new HyphenationDictionary();
		dict.AddPattern("om1p");
		var mapper = new TextRunToItemMapper(_engine, dict);
		var typeface = GetTypeface();

		// "the computer" → Box("the") + Glue + Box("com") + Penalty + Box("puter")
		var items = mapper.MapTextRun("the computer", typeface, 12f);

		AssertKinds(items, "BGBPB");
	}
}
