using AwesomeAssertions;
using SkiaSharp;
using Xunit;

namespace PanoramicData.Render.Test;

public partial class TextRunToItemMapperTests
{
	private readonly MeasurementEngine _engine = new();

	// The shipped test font, not a system font, so the results are the same on every machine.
	private static SKTypeface GetTypeface() => TestFonts.Sans;

	private IReadOnlyList<KnuthPlassItem> MapText(string text)
		=> new TextRunToItemMapper(_engine).MapTextRun(text, GetTypeface(), 12f);

	private IReadOnlyList<KnuthPlassItem> MapElements(params RunElement[] elements)
		=> new TextRunToItemMapper(_engine).MapRunElements(elements, GetTypeface(), 12f);

	private static void AssertKinds(IReadOnlyList<KnuthPlassItem> items, string kinds)
	{
		items.Select(item => item switch { KnuthPlassBox => 'B', KnuthPlassGlue => 'G', _ => 'P' }).Should().Equal(kinds.ToCharArray());
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
		var items = MapText(" ");

		AssertKinds(items, "G");

		var glue = (KnuthPlassGlue)items[0];
		glue.Width.Should().BeGreaterThan(0f);
		glue.Stretch.Should().BeGreaterThan(0f);
		glue.Shrink.Should().BeGreaterThan(0f);
	}

	// --- Single word ---

	[Fact]
	public void MapTextRun_SingleWord_ReturnsSingleBox()
	{
		var items = MapText("Hello");

		AssertKinds(items, "B");
		items[0].Width.Should().BeGreaterThan(0f);
	}

	// --- Word with spaces ---

	[Fact]
	public void MapTextRun_TwoWords_ReturnsBoxGlueBox()
	{
		var items = MapText("Hello world");

		AssertKinds(items, "BGB");
	}

	[Fact]
	public void MapTextRun_MultipleSpaces_ProducesSingleGlue()
	{
		var items = MapText("Hello   world");

		// Multiple consecutive spaces should be collapsed into a single glue
		// with the combined width of all the spaces.
		AssertKinds(items, "BGB");

		// The glue width should be at least as wide as a single space
		var singleSpaceItems = MapText(" ");
		var singleGlue = (KnuthPlassGlue)singleSpaceItems[0];
		var multiGlue = (KnuthPlassGlue)items[1];
		multiGlue.Width.Should().BeGreaterThan(singleGlue.Width);
	}

	[Fact]
	public void MapTextRun_LeadingSpaces_ReturnsGlueBeforeBox()
	{
		var items = MapText(" Hello");

		AssertKinds(items, "GB");
	}

	[Fact]
	public void MapTextRun_TrailingSpaces_ReturnsBoxThenGlue()
	{
		var items = MapText("Hello ");

		AssertKinds(items, "BG");
	}

	// --- Hyphens ---

	[Fact]
	public void MapTextRun_WordWithHyphen_ProducesPenaltyBetweenParts()
	{
		var items = MapText("well-known");

		// Expected: Box("well") + Penalty(hyphen, flagged) + Box("-known")
		// The hyphen is part of the first word visually, but the break opportunity
		// comes after the hyphen. We model this as:
		// Box("well-") + Penalty(0, flagged) + Box("known")
		AssertKinds(items, "BPB");

		var penalty = (KnuthPlassPenalty)items[1];
		penalty.IsFlagged.Should().BeTrue();
		penalty.Width.Should().Be(0f); // hyphen is already in the first box
	}

	[Fact]
	public void MapTextRun_WordEndingWithHyphen_ProducesBoxOnly()
	{
		var items = MapText("well-");

		// A trailing hyphen is just part of the box
		AssertKinds(items, "B");
	}

	// --- Three words ---

	[Fact]
	public void MapTextRun_ThreeWords_ProducesFiveItems()
	{
		var items = MapText("the quick fox");

		AssertKinds(items, "BGBGB");
	}

	// --- Box width accuracy ---

	[Fact]
	public void MapTextRun_BoxWidth_MatchesMeasurementEngine()
	{
		var items = MapText("Hello");

		var expectedAdvances = _engine.MeasureGlyphAdvancesInTwips(GetTypeface(), 12f, "Hello");
		var expectedWidth = expectedAdvances.Sum();

		items.Should().HaveCount(1);
		items[0].Width.Should().BeApproximately(expectedWidth, 1f);
	}

	// --- Glue stretch/shrink ratios ---

	[Fact]
	public void MapTextRun_GlueStretchAndShrink_AreProportionalToSpaceWidth()
	{
		var items = MapText("a b");

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
		var items = MapText("a-b-c");

		// Box("a-") Penalty Box("b-") Penalty Box("c")
		AssertKinds(items, "BPBPB");
	}

	// --- Mixed spaces and hyphens ---

	[Fact]
	public void MapTextRun_SpacesAndHyphens_ProducesCorrectSequence()
	{
		var items = MapText("well-known fact");

		// Box("well-") Penalty Box("known") Glue Box("fact")
		AssertKinds(items, "BPBGB");
	}

	// --- Tab handling ---

	[Fact]
	public void MapTextRun_TabCharacter_TreatedAsSpace()
	{
		var items = MapText("a\tb");

		// Tab within text content is treated like a space (glue)
		AssertKinds(items, "BGB");
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
		var items = MapElements(
			new TextRunElement { Text = "Hello world" }
		);

		// Box-Glue-Box for "Hello world"
		AssertKinds(items, "BGB");
	}

	[Fact]
	public void MapRunElements_LineBreak_ProducesForcedBreakPenalty()
	{
		var items = MapElements(
			new TextRunElement { Text = "Hello" },
			new BreakRunElement { BreakType = RunBreakType.Line },
			new TextRunElement { Text = "world" }
		);

		// Box("Hello") + ForcedBreak + Box("world")
		AssertKinds(items, "BPB");

		var penalty = (KnuthPlassPenalty)items[1];
		penalty.Penalty.Should().Be(float.NegativeInfinity);
		penalty.BreakType.Should().Be(RunBreakType.Line);
	}

	[Fact]
	public void MapRunElements_PageBreak_ProducesForcedBreakWithPageType()
	{
		var items = MapElements(
			new TextRunElement { Text = "Before" },
			new BreakRunElement { BreakType = RunBreakType.Page }
		);

		AssertKinds(items, "BP");

		var penalty = (KnuthPlassPenalty)items[1];
		penalty.Penalty.Should().Be(float.NegativeInfinity);
		penalty.BreakType.Should().Be(RunBreakType.Page);
	}
}
