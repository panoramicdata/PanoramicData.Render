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
		using var fixture = CreateCountingResolver(new RenderOptions
		{
			FontSubstitutions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				["Requested"] = "Replacement"
			}
		}, ("Replacement.ttf", "Replacement"));
		var resolver = fixture.Resolver;

		resolver.TryGetTypeface("Requested", bold: false, italic: true, out var substituted).Should().BeTrue();
		resolver.TryGetTypeface("Replacement", bold: false, italic: true, out var direct).Should().BeTrue();

		ReferenceEquals(substituted, direct).Should().BeTrue();
		fixture.FactoryCallCount.Should().Be(1);
	}

	[Fact]
	public void TryGetTypeface_WithConfiguredFallback_ResolvesAndCachesFallbackFamily()
	{
		using var fixture = CreateCountingResolver(new RenderOptions
		{
			FallbackFontFamily = "Fallback"
		}, ("Fallback.ttf", "Fallback"));
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
		var path = Path.Combine(root, "Replacement.ttf");
		File.WriteAllText(path, "dummy");

		var resolver = new FontResolver(
			new RenderOptions
			{
				FontDirectories = [root]
			},
			new FakeFontMetadataReader(new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
			{
				[path] = ["Replacement"]
			}),
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

		var resolver = new FontResolver(
			[root],
			new FakeFontMetadataReader(new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
			{
				[path] = ["Replacement"]
			}));

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

		var resolver = new FontResolver(
			[root],
			new FakeFontMetadataReader(new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
			{
				[path] = ["Broken"]
			}));

		resolver.TryGetTypeface("Broken", bold: false, italic: false, out var typeface).Should().BeFalse();
		typeface.Should().BeNull();
	}

	[Fact]
	public void TryResolveThemeFontFamily_WithMajorLatin_ReturnsResolvedLatinFamily()
	{
		var options = new RenderOptions
		{
			FontSubstitutions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				["Heading Theme"] = "Resolved Heading"
			}
		};
		using var setup = CreateResolverWithFamilies(options, "Resolved Heading");

		var theme = CreateThemeInfo(
			majorFont: new ThemeFontInfo
		{
			Latin = "Heading Theme",
			EastAsian = "East Major",
			ComplexScript = "Complex Major",
			ScriptFonts = new Dictionary<string, string>()
		},
			minorFont: CreateEmptyThemeFontInfo());

		setup.Resolver.TryResolveThemeFontFamily(theme, useMajorFont: true, script: null, out var familyName).Should().BeTrue();
		familyName.Should().Be("Resolved Heading");
	}

	[Fact]
	public void TryResolveThemeFontFamily_WithExactScriptMatch_PrefersSupplementalFont()
	{
		using var setup = CreateResolverWithFamilies(new RenderOptions(), "Heading Japanese");

		var theme = CreateThemeInfo(
			majorFont: new ThemeFontInfo
		{
			Latin = "Heading Latin",
			EastAsian = "Heading EastAsian",
			ComplexScript = "Heading Complex",
			ScriptFonts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				["Jpan"] = "Heading Japanese"
			}
		},
			minorFont: CreateEmptyThemeFontInfo());

		setup.Resolver.TryResolveThemeFontFamily(theme, useMajorFont: true, script: "Jpan", out var familyName).Should().BeTrue();
		familyName.Should().Be("Heading Japanese");
	}

	[Fact]
	public void TryResolveThemeFontFamily_WithEastAsianScript_UsesEastAsianFallback()
	{
		using var setup = CreateResolverWithFamilies(new RenderOptions(), "Body EastAsian");

		var theme = CreateThemeInfo(
			majorFont: CreateEmptyThemeFontInfo(),
			minorFont: new ThemeFontInfo
		{
			Latin = "Body Latin",
			EastAsian = "Body EastAsian",
			ComplexScript = "Body Complex",
			ScriptFonts = new Dictionary<string, string>()
		});

		setup.Resolver.TryResolveThemeFontFamily(theme, useMajorFont: false, script: "Hans", out var familyName).Should().BeTrue();
		familyName.Should().Be("Body EastAsian");
	}

	[Fact]
	public void TryResolveThemeFontFamily_WithComplexScript_UsesComplexScriptFallback()
	{
		using var setup = CreateResolverWithFamilies(new RenderOptions(), "Body Complex");

		var theme = CreateThemeInfo(
			majorFont: CreateEmptyThemeFontInfo(),
			minorFont: new ThemeFontInfo
		{
			Latin = "Body Latin",
			EastAsian = "Body EastAsian",
			ComplexScript = "Body Complex",
			ScriptFonts = new Dictionary<string, string>()
		});

		setup.Resolver.TryResolveThemeFontFamily(theme, useMajorFont: false, script: "Arab", out var familyName).Should().BeTrue();
		familyName.Should().Be("Body Complex");
	}

	[Fact]
	public void TryResolveThemeFontFamily_WithMissingThemeFamily_UsesFontResolverFallbackChain()
	{
		using var setup = CreateResolverWithFamilies(new RenderOptions
		{
			FallbackFontFamily = "Fallback Family"
		}, "Fallback Family");

		var theme = CreateThemeInfo(
			majorFont: new ThemeFontInfo
		{
			Latin = "Missing Theme Font",
			EastAsian = null,
			ComplexScript = null,
			ScriptFonts = new Dictionary<string, string>()
		},
			minorFont: CreateEmptyThemeFontInfo());

		setup.Resolver.TryResolveThemeFontFamily(theme, useMajorFont: true, script: null, out var familyName).Should().BeTrue();
		familyName.Should().Be("Fallback Family");
	}
}
