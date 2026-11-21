using AwesomeAssertions;
using SkiaSharp;
using Xunit;

namespace PanoramicData.Render.Test;

public partial class FontResolverTests
{
	[Fact]
	public void TryGetTypeface_WithSameFamilyAndStyle_UsesCachedTypeface()
	{
		AssertCaching(new RenderOptions(), [("Replacement.ttf", "Replacement")], ("Replacement", false, false), ("Replacement", false, false), true, 1);
	}

	[Fact]
	public void TryGetTypeface_WithDifferentStyles_CachesSeparatelyPerStyle()
	{
		AssertCaching(new RenderOptions(), [("Replacement.ttf", "Replacement")], ("Replacement", false, false), ("Replacement", true, false), false, 2);
	}

	[Fact]
	public void TryGetTypeface_WithSubstitutionAndDirectFamily_ReusesResolvedFamilyCacheEntry()
	{
		AssertCaching(new RenderOptions { FontSubstitutions = Subs("Requested", "Replacement") }, [("Replacement.ttf", "Replacement")], ("Requested", false, true), ("Replacement", false, true), true, 1);
	}

	[Fact]
	public void TryGetTypeface_WithConfiguredFallback_ResolvesAndCachesFallbackFamily()
	{
		AssertCaching(new RenderOptions { FallbackFontFamily = "Fallback" }, [("Fallback.ttf", "Fallback")], ("Missing", false, false), ("AlsoMissing", false, false), true, 1);
	}

	[Fact]
	public void TryGetTypeface_WithSansSerifFallback_ResolvesAndCachesSansSerifFamily()
	{
		AssertCaching(new RenderOptions(), [("Garamond.ttf", "Garamond"), ("Arial.ttf", "Arial")], ("Missing", true, false), ("StillMissing", true, false), true, 1);
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
		AssertDefaultFactory("Replacement.ttf", "Replacement", true, path => File.Copy(TestFonts.SansRegularPath, path));
	}

	[Fact]
	public void TryGetTypeface_WithDefaultFactoryAndUnreadableFont_ReturnsFalse()
	{
		AssertDefaultFactory("Broken.ttf", "Broken", false, path => File.WriteAllText(path, "not a font"));
	}

	[Fact]
	public void TryResolveThemeFontFamily_WithMajorLatin_ReturnsResolvedLatinFamily()
	{
		AssertThemeFamily(new RenderOptions { FontSubstitutions = Subs("Heading Theme", "Resolved Heading") }, "Resolved Heading", (ThemeFont("Heading Theme", "East Major", "Complex Major"), CreateEmptyThemeFontInfo()), true, null);
	}

	[Fact]
	public void TryResolveThemeFontFamily_WithExactScriptMatch_PrefersSupplementalFont()
	{
		AssertThemeFamily(new RenderOptions(), "Heading Japanese", (ThemeFont("Heading Latin", "Heading EastAsian", "Heading Complex", Subs("Jpan", "Heading Japanese")), CreateEmptyThemeFontInfo()), true, "Jpan");
	}

	[Fact]
	public void TryResolveThemeFontFamily_WithEastAsianScript_UsesEastAsianFallback()
	{
		AssertThemeFamily(new RenderOptions(), "Body EastAsian", (CreateEmptyThemeFontInfo(), ThemeFont("Body Latin", "Body EastAsian", "Body Complex")), false, "Hans");
	}

	[Fact]
	public void TryResolveThemeFontFamily_WithComplexScript_UsesComplexScriptFallback()
	{
		AssertThemeFamily(new RenderOptions(), "Body Complex", (CreateEmptyThemeFontInfo(), ThemeFont("Body Latin", "Body EastAsian", "Body Complex")), false, "Arab");
	}

	[Fact]
	public void TryResolveThemeFontFamily_WithMissingThemeFamily_UsesFontResolverFallbackChain()
	{
		AssertThemeFamily(new RenderOptions { FallbackFontFamily = "Fallback Family" }, "Fallback Family", (ThemeFont("Missing Theme Font", null, null), CreateEmptyThemeFontInfo()), true, null);
	}
}
