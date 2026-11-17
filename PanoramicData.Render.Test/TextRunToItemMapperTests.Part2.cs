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

		items.Should().HaveCount(1);
		items[0].Should().BeOfType<KnuthPlassPenalty>();

		var penalty = (KnuthPlassPenalty)items[0];
		penalty.Penalty.Should().Be(float.NegativeInfinity);
		penalty.BreakType.Should().Be(RunBreakType.Column);
	}

	[Fact]
	public void MapRunElements_TabElement_ProducesGlue()
	{
		var typeface = GetTypeface();
		var mapper = new TextRunToItemMapper(_engine);
		var elements = new RunElement[]
		{
			new TextRunElement { Text = "a" },
			new TabRunElement(),
			new TextRunElement { Text = "b" }
		};

		var items = mapper.MapRunElements(elements, typeface, 12f);

		// Box("a") + Glue(tab) + Box("b")
		items.Should().HaveCount(3);
		items[0].Should().BeOfType<KnuthPlassBox>();
		items[1].Should().BeOfType<KnuthPlassGlue>();
		items[2].Should().BeOfType<KnuthPlassBox>();
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

		items.Should().ContainSingle()
			.Which.Should().BeOfType<KnuthPlassBox>();
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

		items.Should().HaveCount(3);
		items[0].Should().BeOfType<KnuthPlassBox>();
		items[1].Should().BeOfType<KnuthPlassBox>();
		items[2].Should().BeOfType<KnuthPlassBox>();
		items[1].Width.Should().BeApproximately(720f, 0.001f);
	}

	[Fact]
	public void MapRunElements_MultipleTextElements_ConcatenatesItems()
	{
		var typeface = GetTypeface();
		var mapper = new TextRunToItemMapper(_engine);
		var elements = new RunElement[]
		{
			new TextRunElement { Text = "Hello" },
			new TextRunElement { Text = " world" }
		};

		var items = mapper.MapRunElements(elements, typeface, 12f);

		// Box("Hello") from first, then Glue(" ") + Box("world") from second
		items.Should().HaveCount(3);
		items[0].Should().BeOfType<KnuthPlassBox>();
		items[1].Should().BeOfType<KnuthPlassGlue>();
		items[2].Should().BeOfType<KnuthPlassBox>();
	}

	[Fact]
	public void MapRunElements_BreakBetweenWords_ProducesCorrectSequence()
	{
		var typeface = GetTypeface();
		var mapper = new TextRunToItemMapper(_engine);
		var elements = new RunElement[]
		{
			new TextRunElement { Text = "first line" },
			new BreakRunElement { BreakType = RunBreakType.Line },
			new TextRunElement { Text = "second line" }
		};

		var items = mapper.MapRunElements(elements, typeface, 12f);

		// Box("first") Glue Box("line") ForcedBreak Box("second") Glue Box("line")
		items.Should().HaveCount(7);
		items[0].Should().BeOfType<KnuthPlassBox>();
		items[1].Should().BeOfType<KnuthPlassGlue>();
		items[2].Should().BeOfType<KnuthPlassBox>();
		items[3].Should().BeOfType<KnuthPlassPenalty>();
		items[4].Should().BeOfType<KnuthPlassBox>();
		items[5].Should().BeOfType<KnuthPlassGlue>();
		items[6].Should().BeOfType<KnuthPlassBox>();

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
		var typeface = GetTypeface();
		var mapper = new TextRunToItemMapper(_engine);

		// "100\u00A0kg" should be a single box (no break opportunity)
		var items = mapper.MapTextRun("100\u00A0kg", typeface, 12f);

		items.Should().ContainSingle()
			.Which.Should().BeOfType<KnuthPlassBox>();
	}

	[Fact]
	public void MapTextRun_NonBreakingSpaceBetweenWords_NoGlue()
	{
		var typeface = GetTypeface();
		var mapper = new TextRunToItemMapper(_engine);

		// Words joined by non-breaking space should not produce glue
		var items = mapper.MapTextRun("Mr.\u00A0Smith", typeface, 12f);

		// Should be treated as a single word (including the hyphen split logic doesn't affect it)
		items.Should().ContainSingle()
			.Which.Should().BeOfType<KnuthPlassBox>();
		items[0].Width.Should().BeGreaterThan(0f);
	}

	[Fact]
	public void MapTextRun_NonBreakingSpaceWithRegularSpaces_BreakOnlyAtRegularSpaces()
	{
		var typeface = GetTypeface();
		var mapper = new TextRunToItemMapper(_engine);

		// "100\u00A0kg each" — non-breaking space joins "100" and "kg", but regular space after "kg" is a break
		var items = mapper.MapTextRun("100\u00A0kg each", typeface, 12f);

		// Box("100\u00A0kg") + Glue + Box("each")
		items.Should().HaveCount(3);
		items[0].Should().BeOfType<KnuthPlassBox>();
		items[1].Should().BeOfType<KnuthPlassGlue>();
		items[2].Should().BeOfType<KnuthPlassBox>();
	}

	// --- Non-breaking hyphen tests (U+2011) ---

	[Fact]
	public void MapTextRun_NonBreakingHyphen_DoesNotCreatePenalty()
	{
		var typeface = GetTypeface();
		var mapper = new TextRunToItemMapper(_engine);

		// Non-breaking hyphen (U+2011) should NOT create a break opportunity
		var items = mapper.MapTextRun("well\u2011known", typeface, 12f);

		// Should be a single box — no penalty break at non-breaking hyphen
		items.Should().ContainSingle()
			.Which.Should().BeOfType<KnuthPlassBox>();
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
		var typeface = GetTypeface();
		var mapper = new TextRunToItemMapper(_engine);
		var elements = new RunElement[]
		{
			new TextRunElement { Text = "well" },
			new NonBreakingHyphenRunElement(),
			new TextRunElement { Text = "known" }
		};

		var items = mapper.MapRunElements(elements, typeface, 12f);

		// Box("well") + Box(hyphen-width) + Box("known") — all boxes, no penalties
		items.Should().HaveCount(3);
		items[0].Should().BeOfType<KnuthPlassBox>();
		items[1].Should().BeOfType<KnuthPlassBox>();
		items[2].Should().BeOfType<KnuthPlassBox>();
	}

	[Fact]
	public void MapRunElements_NonBreakingHyphenElement_HasPositiveWidth()
	{
		var typeface = GetTypeface();
		var mapper = new TextRunToItemMapper(_engine);
		var elements = new RunElement[]
		{
			new NonBreakingHyphenRunElement()
		};

		var items = mapper.MapRunElements(elements, typeface, 12f);

		items.Should().ContainSingle()
			.Which.Should().BeOfType<KnuthPlassBox>();
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
		var typeface = GetTypeface();
		var mapper = new TextRunToItemMapper(_engine);

		// Without hyphenation dictionary, a single word produces a single box
		var items = mapper.MapTextRun("computer", typeface, 12f);

		items.Should().ContainSingle()
			.Which.Should().BeOfType<KnuthPlassBox>();
	}

	[Fact]
	public void MapTextRun_WithHyphenation_InsertsDiscretionaryBreaks()
	{
		var typeface = GetTypeface();
		var dict = new HyphenationDictionary();
		dict.AddPattern("om1p");
		dict.AddPattern("pu1t");
		var mapper = new TextRunToItemMapper(_engine, dict);

		// "computer" with patterns → com|put|er
		var items = mapper.MapTextRun("computer", typeface, 12f);

		// Should have discretionary break penalties between segments
		// Box("com") + Penalty + Box("put") + Penalty + Box("er")
		items.Should().HaveCount(5);
		items[0].Should().BeOfType<KnuthPlassBox>();
		items[1].Should().BeOfType<KnuthPlassPenalty>();
		items[2].Should().BeOfType<KnuthPlassBox>();
		items[3].Should().BeOfType<KnuthPlassPenalty>();
		items[4].Should().BeOfType<KnuthPlassBox>();
	}

	[Fact]
	public void MapTextRun_WithHyphenation_PenaltyIsFlagged()
	{
		var typeface = GetTypeface();
		var dict = new HyphenationDictionary();
		dict.AddPattern("om1p");
		var mapper = new TextRunToItemMapper(_engine, dict);

		var items = mapper.MapTextRun("computer", typeface, 12f);

		var penalty = items.OfType<KnuthPlassPenalty>().First();
		penalty.IsFlagged.Should().BeTrue();
	}

	[Fact]
	public void MapTextRun_WithHyphenation_PenaltyWidthIsHyphenWidth()
	{
		var typeface = GetTypeface();
		var dict = new HyphenationDictionary();
		dict.AddPattern("om1p");
		var mapper = new TextRunToItemMapper(_engine, dict);

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
		var typeface = GetTypeface();
		var dict = new HyphenationDictionary();
		dict.AddPattern("xy1z"); // pattern that won't match "hello"
		var mapper = new TextRunToItemMapper(_engine, dict);

		var items = mapper.MapTextRun("hello", typeface, 12f);

		items.Should().ContainSingle()
			.Which.Should().BeOfType<KnuthPlassBox>();
	}

	[Fact]
	public void MapTextRun_WithHyphenation_ExplicitHyphenAndAutoHyphen_BothPresent()
	{
		var typeface = GetTypeface();
		var dict = new HyphenationDictionary();
		dict.AddPattern("ow1l");
		var mapper = new TextRunToItemMapper(_engine, dict);

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
		var typeface = GetTypeface();
		var dict = new HyphenationDictionary();
		dict.AddPattern("om1p");
		var mapper = new TextRunToItemMapper(_engine, dict);

		// "the computer" → Box("the") + Glue + Box("com") + Penalty + Box("puter")
		var items = mapper.MapTextRun("the computer", typeface, 12f);

		items.Should().HaveCount(5);
		items[0].Should().BeOfType<KnuthPlassBox>(); // "the"
		items[1].Should().BeOfType<KnuthPlassGlue>(); // space
		items[2].Should().BeOfType<KnuthPlassBox>(); // "com"
		items[3].Should().BeOfType<KnuthPlassPenalty>(); // discretionary
		items[4].Should().BeOfType<KnuthPlassBox>(); // "puter"
	}
}
