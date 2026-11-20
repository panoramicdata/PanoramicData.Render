using AwesomeAssertions;
using SkiaSharp;
using Xunit;

namespace PanoramicData.Render.Test;

public partial class FontResolverTests
{
	[Fact]
	public void TryGetTypeface_WithSameFamilyAndStyle_UsesCachedTypeface()
	{
		using var fixture = CreateCountingResolver(new RenderOptions(), ("Replacement.ttf", "Replacement"));
		var resolver = fixture.Resolver;

		resolver.TryGetTypeface("Replacement", bold: false, italic: false, out var first).Should().BeTrue();
		resolver.TryGetTypeface("Replacement", bold: false, italic: false, out var second).Should().BeTrue();

		ReferenceEquals(first, second).Should().BeTrue();
		fixture.FactoryCallCount.Should().Be(1);
	}

	[Fact]
	public void TryGetTypeface_WithDifferentStyles_CachesSeparatelyPerStyle()
	{
		using var fixture = CreateCountingResolver(new RenderOptions(), ("Replacement.ttf", "Replacement"));
		var resolver = fixture.Resolver;

		resolver.TryGetTypeface("Replacement", bold: false, italic: false, out var regular).Should().BeTrue();
		resolver.TryGetTypeface("Replacement", bold: true, italic: false, out var bold).Should().BeTrue();

		ReferenceEquals(regular, bold).Should().BeFalse();
		fixture.FactoryCallCount.Should().Be(2);
	}

	[Fact]
	public void TryGetTypeface_WithSubstitutionAndDirectFamily_ReusesResolvedFamilyCacheEntry()
	{
		using var fixture = CreateCountingResolver(new RenderOptions { FontSubstitutions = Subs("Requested", "Replacement") }, ("Replacement.ttf", "Replacement"));
		var resolver = fixture.Resolver;

		resolver.TryGetTypeface("Requested", bold: false, italic: true, out var substituted).Should().BeTrue();
		resolver.TryGetTypeface("Replacement", bold: false, italic: true, out var direct).Should().BeTrue();

		ReferenceEquals(substituted, direct).Should().BeTrue();
		fixture.FactoryCallCount.Should().Be(1);
	}

	[Fact]
	public void TryGetTypeface_WithConfiguredFallback_ResolvesAndCachesFallbackFamily()
	{
		using var fixture = CreateCountingResolver(new RenderOptions { FallbackFontFamily = "Fallback" }, ("Fallback.ttf", "Fallback"));
		var resolver = fixture.Resolver;

		resolver.TryGetTypeface("Missing", bold: false, italic: false, out var first).Should().BeTrue();
		resolver.TryGetTypeface("AlsoMissing", bold: false, italic: false, out var second).Should().BeTrue();

		ReferenceEquals(first, second).Should().BeTrue();
		fixture.FactoryCallCount.Should().Be(1);
	}

	[Fact]
	public void TryGetTypeface_WithSansSerifFallback_ResolvesAndCachesSansSerifFamily()
	{
		using var fixture = CreateCountingResolver(new RenderOptions(), ("Garamond.ttf", "Garamond"), ("Arial.ttf", "Arial"));
		var resolver = fixture.Resolver;

		resolver.TryGetTypeface("Missing", bold: true, italic: false, out var first).Should().BeTrue();
		resolver.TryGetTypeface("StillMissing", bold: true, italic: false, out var second).Should().BeTrue();

		ReferenceEquals(first, second).Should().BeTrue();
		fixture.FactoryCallCount.Should().Be(1);
	}

	[Fact]
	public void InternalRenderOptionsConstructor_WithNullTypefaceFactory_ThrowsArgumentNullException()
	{
		var act = () => new FontResolver(new RenderOptions(), new FakeFontMetadataReader(new Dictionary<string, IReadOnlyList<string>>()), null!);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void TryGetTypeface_WhenFactoryReturnsNull_ReturnsFalse()
	{
		using var tempDirectory = new TempDirectory();
		var root = tempDirectory.Path;
		var path = WriteFont(root, "Replacement.ttf");

		var resolver = new FontResolver(
			new RenderOptions { FontDirectories = [root] },
			MetadataFor(path, "Replacement"),
			(_, _, _) => null);

		resolver.TryGetTypeface("Replacement", bold: false, italic: false, out var typeface).Should().BeFalse();
		typeface.Should().BeNull();
	}

	[Fact]
	public void TryGetTypeface_WithDefaultFactoryAndARealFont_ReturnsTypeface()
	{
		using var tempDirectory = new TempDirectory();
		var root = tempDirectory.Path;
		var path = Path.Combine(root, "Replacement.ttf");
		File.Copy(TestFonts.SansRegularPath, path);

		var resolver = new FontResolver([root], MetadataFor(path, "Replacement"));

		resolver.TryGetTypeface("Replacement", bold: false, italic: false, out var typeface).Should().BeTrue();
		typeface.Should().NotBeNull();
	}

	[Fact]
	public void TryGetTypeface_WithDefaultFactoryAndUnreadableFont_ReturnsFalse()
	{
		using var tempDirectory = new TempDirectory();
		var root = tempDirectory.Path;
		var path = Path.Combine(root, "Broken.ttf");
		File.WriteAllText(path, "not a font");

		var resolver = new FontResolver([root], MetadataFor(path, "Broken"));

		resolver.TryGetTypeface("Broken", bold: false, italic: false, out var typeface).Should().BeFalse();
		typeface.Should().BeNull();
	}

	[Fact]
	public void TryResolveThemeFontFamily_WithMajorLatin_ReturnsResolvedLatinFamily()
	{
		using var setup = CreateResolverWithFamilies(new RenderOptions { FontSubstitutions = Subs("Heading Theme", "Resolved Heading") }, "Resolved Heading");

		AssertThemeFamily(setup, ThemeFont("Heading Theme", "East Major", "Complex Major"), CreateEmptyThemeFontInfo(), true, null, "Resolved Heading");
	}

	[Fact]
	public void TryResolveThemeFontFamily_WithExactScriptMatch_PrefersSupplementalFont()
	{
		using var setup = CreateResolverWithFamilies(new RenderOptions(), "Heading Japanese");
		var major = ThemeFont("Heading Latin", "Heading EastAsian", "Heading Complex", Subs("Jpan", "Heading Japanese"));

		AssertThemeFamily(setup, major, CreateEmptyThemeFontInfo(), true, "Jpan", "Heading Japanese");
	}

	[Fact]
	public void TryResolveThemeFontFamily_WithEastAsianScript_UsesEastAsianFallback()
	{
		using var setup = CreateResolverWithFamilies(new RenderOptions(), "Body EastAsian");
		var minor = ThemeFont("Body Latin", "Body EastAsian", "Body Complex");

		AssertThemeFamily(setup, CreateEmptyThemeFontInfo(), minor, false, "Hans", "Body EastAsian");
	}

	[Fact]
	public void TryResolveThemeFontFamily_WithComplexScript_UsesComplexScriptFallback()
	{
		using var setup = CreateResolverWithFamilies(new RenderOptions(), "Body Complex");
		var minor = ThemeFont("Body Latin", "Body EastAsian", "Body Complex");

		AssertThemeFamily(setup, CreateEmptyThemeFontInfo(), minor, false, "Arab", "Body Complex");
	}

	[Fact]
	public void TryResolveThemeFontFamily_WithMissingThemeFamily_UsesFontResolverFallbackChain()
	{
		using var setup = CreateResolverWithFamilies(new RenderOptions { FallbackFontFamily = "Fallback Family" }, "Fallback Family");

		AssertThemeFamily(setup, ThemeFont("Missing Theme Font", null, null), CreateEmptyThemeFontInfo(), true, null, "Fallback Family");
	}
}
