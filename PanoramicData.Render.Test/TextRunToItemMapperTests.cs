namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using SkiaSharp;
using Xunit;

public class TextRunToItemMapperTests
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
	public void MapTextRun_NullText_ThrowsArgumentNullException()
	{
		var mapper = new TextRunToItemMapper(_engine);
		var typeface = SKTypeface.Default;

		var act = () => mapper.MapTextRun(null!, typeface, 12f);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void MapTextRun_NullTypeface_ThrowsArgumentNullException()
	{
		var mapper = new TextRunToItemMapper(_engine);

		var act = () => mapper.MapTextRun("hello", null!, 12f);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void MapTextRun_NonPositiveFontSize_ThrowsArgumentOutOfRangeException()
	{
		var mapper = new TextRunToItemMapper(_engine);
		var typeface = SKTypeface.Default;

		var act = () => mapper.MapTextRun("hello", typeface, 0f);

		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	// --- Empty and whitespace ---

	[Fact]
	public void MapTextRun_EmptyString_ReturnsEmpty()
	{
		var mapper = new TextRunToItemMapper(_engine);
		var typeface = SKTypeface.Default;

		var items = mapper.MapTextRun("", typeface, 12f);

		items.Should().BeEmpty();
	}

	[Fact]
	public void MapTextRun_SingleSpace_ReturnsGlue()
	{
		var typeface = GetTypeface();
		var mapper = new TextRunToItemMapper(_engine);

		var items = mapper.MapTextRun(" ", typeface, 12f);

		items.Should().HaveCount(1);
		items[0].Should().BeOfType<KnuthPlassGlue>();

		var glue = (KnuthPlassGlue)items[0];
		glue.Width.Should().BeGreaterThan(0f);
		glue.Stretch.Should().BeGreaterThan(0f);
		glue.Shrink.Should().BeGreaterThan(0f);
	}

	// --- Single word ---

	[Fact]
	public void MapTextRun_SingleWord_ReturnsSingleBox()
	{
		var typeface = GetTypeface();
		var mapper = new TextRunToItemMapper(_engine);

		var items = mapper.MapTextRun("Hello", typeface, 12f);

		items.Should().HaveCount(1);
		items[0].Should().BeOfType<KnuthPlassBox>();
		items[0].Width.Should().BeGreaterThan(0f);
	}

	// --- Word with spaces ---

	[Fact]
	public void MapTextRun_TwoWords_ReturnsBoxGlueBox()
	{
		var typeface = GetTypeface();
		var mapper = new TextRunToItemMapper(_engine);

		var items = mapper.MapTextRun("Hello world", typeface, 12f);

		items.Should().HaveCount(3);
		items[0].Should().BeOfType<KnuthPlassBox>();
		items[1].Should().BeOfType<KnuthPlassGlue>();
		items[2].Should().BeOfType<KnuthPlassBox>();
	}

	[Fact]
	public void MapTextRun_MultipleSpaces_ProducesSingleGlue()
	{
		var typeface = GetTypeface();
		var mapper = new TextRunToItemMapper(_engine);

		var items = mapper.MapTextRun("Hello   world", typeface, 12f);

		// Multiple consecutive spaces should be collapsed into a single glue
		// with the combined width of all the spaces.
		items.Should().HaveCount(3);
		items[0].Should().BeOfType<KnuthPlassBox>();
		items[1].Should().BeOfType<KnuthPlassGlue>();
		items[2].Should().BeOfType<KnuthPlassBox>();

		// The glue width should be at least as wide as a single space
		var singleSpaceItems = mapper.MapTextRun(" ", typeface, 12f);
		var singleGlue = (KnuthPlassGlue)singleSpaceItems[0];
		var multiGlue = (KnuthPlassGlue)items[1];
		multiGlue.Width.Should().BeGreaterThan(singleGlue.Width);
	}

	[Fact]
	public void MapTextRun_LeadingSpaces_ReturnsGlueBeforeBox()
	{
		var typeface = GetTypeface();
		var mapper = new TextRunToItemMapper(_engine);

		var items = mapper.MapTextRun(" Hello", typeface, 12f);

		items.Should().HaveCount(2);
		items[0].Should().BeOfType<KnuthPlassGlue>();
		items[1].Should().BeOfType<KnuthPlassBox>();
	}

	[Fact]
	public void MapTextRun_TrailingSpaces_ReturnsBoxThenGlue()
	{
		var typeface = GetTypeface();
		var mapper = new TextRunToItemMapper(_engine);

		var items = mapper.MapTextRun("Hello ", typeface, 12f);

		items.Should().HaveCount(2);
		items[0].Should().BeOfType<KnuthPlassBox>();
		items[1].Should().BeOfType<KnuthPlassGlue>();
	}

	// --- Hyphens ---

	[Fact]
	public void MapTextRun_WordWithHyphen_ProducesPenaltyBetweenParts()
	{
		var typeface = GetTypeface();
		var mapper = new TextRunToItemMapper(_engine);

		var items = mapper.MapTextRun("well-known", typeface, 12f);

		// Expected: Box("well") + Penalty(hyphen, flagged) + Box("-known")
		// The hyphen is part of the first word visually, but the break opportunity
		// comes after the hyphen. We model this as:
		// Box("well-") + Penalty(0, flagged) + Box("known")
		items.Should().HaveCount(3);
		items[0].Should().BeOfType<KnuthPlassBox>(); // "well-"
		items[1].Should().BeOfType<KnuthPlassPenalty>(); // break opportunity
		items[2].Should().BeOfType<KnuthPlassBox>(); // "known"

		var penalty = (KnuthPlassPenalty)items[1];
		penalty.IsFlagged.Should().BeTrue();
		penalty.Width.Should().Be(0f); // hyphen is already in the first box
	}

	[Fact]
	public void MapTextRun_WordEndingWithHyphen_ProducesBoxOnly()
	{
		var typeface = GetTypeface();
		var mapper = new TextRunToItemMapper(_engine);

		var items = mapper.MapTextRun("well-", typeface, 12f);

		// A trailing hyphen is just part of the box
		items.Should().HaveCount(1);
		items[0].Should().BeOfType<KnuthPlassBox>();
	}

	// --- Three words ---

	[Fact]
	public void MapTextRun_ThreeWords_ProducesFiveItems()
	{
		var typeface = GetTypeface();
		var mapper = new TextRunToItemMapper(_engine);

		var items = mapper.MapTextRun("the quick fox", typeface, 12f);

		items.Should().HaveCount(5);
		items[0].Should().BeOfType<KnuthPlassBox>();
		items[1].Should().BeOfType<KnuthPlassGlue>();
		items[2].Should().BeOfType<KnuthPlassBox>();
		items[3].Should().BeOfType<KnuthPlassGlue>();
		items[4].Should().BeOfType<KnuthPlassBox>();
	}

	// --- Box width accuracy ---

	[Fact]
	public void MapTextRun_BoxWidth_MatchesMeasurementEngine()
	{
		var typeface = GetTypeface();
		var mapper = new TextRunToItemMapper(_engine);

		var items = mapper.MapTextRun("Hello", typeface, 12f);

		var expectedAdvances = _engine.MeasureGlyphAdvancesInTwips(typeface, 12f, "Hello");
		var expectedWidth = expectedAdvances.Sum();

		items.Should().HaveCount(1);
		items[0].Width.Should().BeApproximately(expectedWidth, 1f);
	}

	// --- Glue stretch/shrink ratios ---

	[Fact]
	public void MapTextRun_GlueStretchAndShrink_AreProportionalToSpaceWidth()
	{
		var typeface = GetTypeface();
		var mapper = new TextRunToItemMapper(_engine);

		var items = mapper.MapTextRun("a b", typeface, 12f);

		var glue = (KnuthPlassGlue)items[1];

		// Stretch should be approximately 1/2 of space width
		glue.Stretch.Should().BeApproximately(glue.Width / 2f, 0.01f);
		// Shrink should be approximately 1/3 of space width
		glue.Shrink.Should().BeApproximately(glue.Width / 3f, 0.01f);
	}

	// --- Multiple hyphens ---

	[Fact]
	public void MapTextRun_MultipleHyphens_ProducesCorrectSequence()
	{
		var typeface = GetTypeface();
		var mapper = new TextRunToItemMapper(_engine);

		var items = mapper.MapTextRun("a-b-c", typeface, 12f);

		// Box("a-") Penalty Box("b-") Penalty Box("c")
		items.Should().HaveCount(5);
		items[0].Should().BeOfType<KnuthPlassBox>();
		items[1].Should().BeOfType<KnuthPlassPenalty>();
		items[2].Should().BeOfType<KnuthPlassBox>();
		items[3].Should().BeOfType<KnuthPlassPenalty>();
		items[4].Should().BeOfType<KnuthPlassBox>();
	}

	// --- Mixed spaces and hyphens ---

	[Fact]
	public void MapTextRun_SpacesAndHyphens_ProducesCorrectSequence()
	{
		var typeface = GetTypeface();
		var mapper = new TextRunToItemMapper(_engine);

		var items = mapper.MapTextRun("well-known fact", typeface, 12f);

		// Box("well-") Penalty Box("known") Glue Box("fact")
		items.Should().HaveCount(5);
		items[0].Should().BeOfType<KnuthPlassBox>();
		items[1].Should().BeOfType<KnuthPlassPenalty>();
		items[2].Should().BeOfType<KnuthPlassBox>();
		items[3].Should().BeOfType<KnuthPlassGlue>();
		items[4].Should().BeOfType<KnuthPlassBox>();
	}

	// --- Tab handling ---

	[Fact]
	public void MapTextRun_TabCharacter_TreatedAsSpace()
	{
		var typeface = GetTypeface();
		var mapper = new TextRunToItemMapper(_engine);

		var items = mapper.MapTextRun("a\tb", typeface, 12f);

		// Tab within text content is treated like a space (glue)
		items.Should().HaveCount(3);
		items[0].Should().BeOfType<KnuthPlassBox>();
		items[1].Should().BeOfType<KnuthPlassGlue>();
		items[2].Should().BeOfType<KnuthPlassBox>();
	}

	// --- MapRunElements tests ---

	[Fact]
	public void MapRunElements_NullElements_ThrowsArgumentNullException()
	{
		var mapper = new TextRunToItemMapper(_engine);
		var typeface = SKTypeface.Default;

		var act = () => mapper.MapRunElements(null!, typeface, 12f);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void MapRunElements_EmptyList_ReturnsEmpty()
	{
		var mapper = new TextRunToItemMapper(_engine);
		var typeface = SKTypeface.Default;

		var items = mapper.MapRunElements([], typeface, 12f);

		items.Should().BeEmpty();
	}

	[Fact]
	public void MapRunElements_NonPositiveFontSize_ThrowsArgumentOutOfRangeException()
	{
		var mapper = new TextRunToItemMapper(_engine);
		var typeface = SKTypeface.Default;

		var act = () => mapper.MapRunElements([], typeface, 0f);

		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void MapRunElements_SingleTextElement_ReturnsBoxItems()
	{
		var typeface = GetTypeface();
		var mapper = new TextRunToItemMapper(_engine);
		var elements = new RunElement[]
		{
			new TextRunElement { Text = "Hello world" }
		};

		var items = mapper.MapRunElements(elements, typeface, 12f);

		// Box-Glue-Box for "Hello world"
		items.Should().HaveCount(3);
		items[0].Should().BeOfType<KnuthPlassBox>();
		items[1].Should().BeOfType<KnuthPlassGlue>();
		items[2].Should().BeOfType<KnuthPlassBox>();
	}

	[Fact]
	public void MapRunElements_LineBreak_ProducesForcedBreakPenalty()
	{
		var typeface = GetTypeface();
		var mapper = new TextRunToItemMapper(_engine);
		var elements = new RunElement[]
		{
			new TextRunElement { Text = "Hello" },
			new BreakRunElement { BreakType = RunBreakType.Line },
			new TextRunElement { Text = "world" }
		};

		var items = mapper.MapRunElements(elements, typeface, 12f);

		// Box("Hello") + ForcedBreak + Box("world")
		items.Should().HaveCount(3);
		items[0].Should().BeOfType<KnuthPlassBox>();
		items[1].Should().BeOfType<KnuthPlassPenalty>();
		items[2].Should().BeOfType<KnuthPlassBox>();

		var penalty = (KnuthPlassPenalty)items[1];
		penalty.Penalty.Should().Be(float.NegativeInfinity);
		penalty.BreakType.Should().Be(RunBreakType.Line);
	}

	[Fact]
	public void MapRunElements_PageBreak_ProducesForcedBreakWithPageType()
	{
		var typeface = GetTypeface();
		var mapper = new TextRunToItemMapper(_engine);
		var elements = new RunElement[]
		{
			new TextRunElement { Text = "Before" },
			new BreakRunElement { BreakType = RunBreakType.Page }
		};

		var items = mapper.MapRunElements(elements, typeface, 12f);

		items.Should().HaveCount(2);
		items[0].Should().BeOfType<KnuthPlassBox>();
		items[1].Should().BeOfType<KnuthPlassPenalty>();

		var penalty = (KnuthPlassPenalty)items[1];
		penalty.Penalty.Should().Be(float.NegativeInfinity);
		penalty.BreakType.Should().Be(RunBreakType.Page);
	}

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
}
