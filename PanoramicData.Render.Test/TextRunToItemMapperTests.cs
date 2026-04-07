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
}
