namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class HyphenationDictionaryTests
{
	// --- Guard tests ---

	[Fact]
	public void AddPattern_NullPattern_ThrowsArgumentNullException()
	{
		var dict = new HyphenationDictionary();

		var act = () => dict.AddPattern(null!);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void AddPattern_EmptyPattern_ThrowsArgumentException()
	{
		var dict = new HyphenationDictionary();

		var act = () => dict.AddPattern("");

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	public void FindHyphenationPoints_NullWord_ThrowsArgumentNullException()
	{
		var dict = new HyphenationDictionary();

		var act = () => dict.FindHyphenationPoints(null!);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void LoadPatterns_NullReader_ThrowsArgumentNullException()
	{
		var dict = new HyphenationDictionary();

		var act = () => dict.LoadPatterns(null!);

		act.Should().Throw<ArgumentNullException>();
	}

	// --- Empty/short word tests ---

	[Fact]
	public void FindHyphenationPoints_EmptyWord_ReturnsEmpty()
	{
		var dict = new HyphenationDictionary();

		var points = dict.FindHyphenationPoints("");

		points.Should().BeEmpty();
	}

	[Fact]
	public void FindHyphenationPoints_ShortWord_ReturnsEmpty()
	{
		var dict = new HyphenationDictionary();
		dict.AddPattern("ab1c");

		// Word "abc" is too short (length 3 < MinWordLength 4)
		var points = dict.FindHyphenationPoints("abc");

		points.Should().BeEmpty();
	}

	[Fact]
	public void FindHyphenationPoints_WordWithNoMatchingPatterns_ReturnsEmpty()
	{
		var dict = new HyphenationDictionary();
		dict.AddPattern("xy1z");

		var points = dict.FindHyphenationPoints("hello");

		points.Should().BeEmpty();
	}

	// --- Pattern parsing ---

	[Fact]
	public void AddPattern_SimplePattern_IsUsedInMatching()
	{
		var dict = new HyphenationDictionary();
		// Pattern "or1i" means: allow hyphenation between 'r' and 'i' in the sequence "ori"
		dict.AddPattern("or1i");

		// "origin" has "ori" at position 0 → hyphenation between r(pos 1) and i(pos 2)
		var points = dict.FindHyphenationPoints("origin");

		// Position 2 is between 'r' and 'i', but min prefix = 2 so position 2 is allowed
		points.Should().Contain(2);
	}

	[Fact]
	public void AddPattern_WordStartPattern_MatchesAtStart()
	{
		var dict = new HyphenationDictionary();
		// Pattern ".hy1p" means: at word start, allow hyphenation between 'y' and 'p'
		dict.AddPattern(".hy1p");

		var points = dict.FindHyphenationPoints("hyphen");

		points.Should().Contain(2);
	}

	[Fact]
	public void AddPattern_WordEndPattern_MatchesAtEnd()
	{
		var dict = new HyphenationDictionary();
		// Pattern "n1ing." means: at word end, allow hyphenation between 'n' and 'i' in "ning"
		dict.AddPattern("n1ing.");

		var points = dict.FindHyphenationPoints("running");

		// "ning." at end, 'n' is at position 3, so hyphenation point at position 4
		points.Should().Contain(4);
	}

	// --- Max rule ---

	[Fact]
	public void FindHyphenationPoints_CompetingPatterns_UsesMaxLevel()
	{
		var dict = new HyphenationDictionary();
		// Pattern allowing break: "or1i"
		dict.AddPattern("or1i");
		// Pattern suppressing break at same position with higher even level: "or2i"
		dict.AddPattern("or2i");

		var points = dict.FindHyphenationPoints("origin");

		// Level 2 (even) suppresses hyphenation
		points.Should().NotContain(2);
	}

	[Fact]
	public void FindHyphenationPoints_HigherOddOverridesEven_AllowsHyphenation()
	{
		var dict = new HyphenationDictionary();
		dict.AddPattern("or2i");
		dict.AddPattern("ri3g");

		var points = dict.FindHyphenationPoints("origin");

		// Between 'r' and 'i': max(2) = 2 → even → no break
		// Between 'i' and 'g': max(3) = 3 → odd → break allowed
		points.Should().NotContain(2);
		points.Should().Contain(3);
	}

	// --- Min prefix/suffix ---

	[Fact]
	public void FindHyphenationPoints_RespectsMinPrefix()
	{
		var dict = new HyphenationDictionary { MinPrefix = 3 };
		dict.AddPattern("or1i");

		var points = dict.FindHyphenationPoints("origin");

		// Position 2 is within min prefix (3 chars), so suppressed
		points.Should().NotContain(2);
	}

	[Fact]
	public void FindHyphenationPoints_RespectsMinSuffix()
	{
		var dict = new HyphenationDictionary { MinSuffix = 4 };
		dict.AddPattern("ig1i");

		var points = dict.FindHyphenationPoints("origin");

		// Position 4 leaves only 2 chars suffix ("in"), less than minSuffix=4
		points.Should().NotContain(4);
	}

	// --- LoadPatterns ---

	[Fact]
	public void LoadPatterns_LoadsMultiplePatterns()
	{
		var dict = new HyphenationDictionary();
		using var reader = new StringReader("or1i\nig1i");

		dict.LoadPatterns(reader);

		// Both patterns should be active
		var points = dict.FindHyphenationPoints("origin");
		points.Should().Contain(2); // from or1i
		points.Should().Contain(4); // from ig1i
	}

	[Fact]
	public void LoadPatterns_SkipsEmptyLines()
	{
		var dict = new HyphenationDictionary();
		using var reader = new StringReader("or1i\n\n\nig1i\n");

		dict.LoadPatterns(reader);

		var points = dict.FindHyphenationPoints("origin");
		points.Should().Contain(2);
		points.Should().Contain(4);
	}

	[Fact]
	public void LoadPatterns_SkipsCommentLines()
	{
		var dict = new HyphenationDictionary();
		using var reader = new StringReader("% this is a comment\nor1i\n% another comment\nig1i");

		dict.LoadPatterns(reader);

		var points = dict.FindHyphenationPoints("origin");
		points.Should().Contain(2);
		points.Should().Contain(4);
	}

	// --- Case insensitivity ---

	[Fact]
	public void FindHyphenationPoints_CaseInsensitive()
	{
		var dict = new HyphenationDictionary();
		dict.AddPattern("or1i");

		var points = dict.FindHyphenationPoints("ORIGIN");

		points.Should().Contain(2);
	}

	// --- Realistic words ---

	[Fact]
	public void FindHyphenationPoints_KnownWord_ComputerWithPatterns()
	{
		var dict = new HyphenationDictionary();
		// Patterns that should make "com-put-er" work
		dict.AddPattern("om1p");
		dict.AddPattern("pu1t");

		var points = dict.FindHyphenationPoints("computer");

		// com|put|er → positions 3 and 5
		points.Should().Contain(3);
		points.Should().Contain(5);
	}

	// --- MinWordLength ---

	[Fact]
	public void FindHyphenationPoints_ExactMinWordLength_AllowsHyphenation()
	{
		var dict = new HyphenationDictionary { MinWordLength = 4 };
		dict.AddPattern("te1s");

		// "test" is exactly 4 chars, meets minimum; break between 'e' and 's'
		var points = dict.FindHyphenationPoints("test");

		points.Should().Contain(2);
	}

	// --- PatternCount ---

	[Fact]
	public void PatternCount_InitiallyZero()
	{
		var dict = new HyphenationDictionary();

		dict.PatternCount.Should().Be(0);
	}

	[Fact]
	public void PatternCount_AfterAddingPatterns_ReflectsCount()
	{
		var dict = new HyphenationDictionary();
		dict.AddPattern("or1i");
		dict.AddPattern("gi1n");

		dict.PatternCount.Should().Be(2);
	}

	[Fact]
	public void PatternCount_DuplicatePattern_OverwritesNotDuplicates()
	{
		var dict = new HyphenationDictionary();
		dict.AddPattern("or1i");
		dict.AddPattern("or3i"); // same letters, different levels → overwrites

		dict.PatternCount.Should().Be(1);
	}
}
