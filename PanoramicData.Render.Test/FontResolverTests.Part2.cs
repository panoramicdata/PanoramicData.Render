using AwesomeAssertions;
using SkiaSharp;
using Xunit;

namespace PanoramicData.Render.Test;

public partial class FontResolverTests
{
	[Fact]
	public void TryGetTypeface_WithSameFamilyAndStyle_UsesCachedTypeface()
	{
		var root = CreateTempDirectory();
		var path = Path.Combine(root, "Replacement.ttf");
		File.WriteAllText(path, "dummy");
		var factoryCallCount = 0;

		try
		{
			var resolver = new FontResolver(
				new RenderOptions
				{
					FontDirectories = [root]
				},
				new FakeFontMetadataReader(new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
				{
					[path] = ["Replacement"]
				}),
				(_, _, _) =>
				{
					factoryCallCount++;
					return CreateTypefaceForTests();
				});

			resolver.TryGetTypeface("Replacement", bold: false, italic: false, out var first).Should().BeTrue();
			resolver.TryGetTypeface("Replacement", bold: false, italic: false, out var second).Should().BeTrue();

			ReferenceEquals(first, second).Should().BeTrue();
			factoryCallCount.Should().Be(1);
		}
		finally
		{
			Directory.Delete(root, true);
		}
	}

	[Fact]
	public void TryGetTypeface_WithDifferentStyles_CachesSeparatelyPerStyle()
	{
		var root = CreateTempDirectory();
		var path = Path.Combine(root, "Replacement.ttf");
		File.WriteAllText(path, "dummy");
		var factoryCallCount = 0;

		try
		{
			var resolver = new FontResolver(
				new RenderOptions
				{
					FontDirectories = [root]
				},
				new FakeFontMetadataReader(new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
				{
					[path] = ["Replacement"]
				}),
				(_, _, _) =>
				{
					factoryCallCount++;
					return CreateTypefaceForTests();
				});

			resolver.TryGetTypeface("Replacement", bold: false, italic: false, out var regular).Should().BeTrue();
			resolver.TryGetTypeface("Replacement", bold: true, italic: false, out var bold).Should().BeTrue();

			ReferenceEquals(regular, bold).Should().BeFalse();
			factoryCallCount.Should().Be(2);
		}
		finally
		{
			Directory.Delete(root, true);
		}
	}

	[Fact]
	public void TryGetTypeface_WithSubstitutionAndDirectFamily_ReusesResolvedFamilyCacheEntry()
	{
		var root = CreateTempDirectory();
		var path = Path.Combine(root, "Replacement.ttf");
		File.WriteAllText(path, "dummy");
		var factoryCallCount = 0;

		try
		{
			var resolver = new FontResolver(
				new RenderOptions
				{
					FontDirectories = [root],
					FontSubstitutions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
					{
						["Requested"] = "Replacement"
					}
				},
				new FakeFontMetadataReader(new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
				{
					[path] = ["Replacement"]
				}),
				(_, _, _) =>
				{
					factoryCallCount++;
					return CreateTypefaceForTests();
				});

			resolver.TryGetTypeface("Requested", bold: false, italic: true, out var substituted).Should().BeTrue();
			resolver.TryGetTypeface("Replacement", bold: false, italic: true, out var direct).Should().BeTrue();

			ReferenceEquals(substituted, direct).Should().BeTrue();
			factoryCallCount.Should().Be(1);
		}
		finally
		{
			Directory.Delete(root, true);
		}
	}

	[Fact]
	public void TryGetTypeface_WithConfiguredFallback_ResolvesAndCachesFallbackFamily()
	{
		var root = CreateTempDirectory();
		var path = Path.Combine(root, "Fallback.ttf");
		File.WriteAllText(path, "dummy");
		var factoryCallCount = 0;

		try
		{
			var resolver = new FontResolver(
				new RenderOptions
				{
					FontDirectories = [root],
					FallbackFontFamily = "Fallback"
				},
				new FakeFontMetadataReader(new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
				{
					[path] = ["Fallback"]
				}),
				(_, _, _) =>
				{
					factoryCallCount++;
					return CreateTypefaceForTests();
				});

			resolver.TryGetTypeface("Missing", bold: false, italic: false, out var first).Should().BeTrue();
			resolver.TryGetTypeface("AlsoMissing", bold: false, italic: false, out var second).Should().BeTrue();

			ReferenceEquals(first, second).Should().BeTrue();
			factoryCallCount.Should().Be(1);
		}
		finally
		{
			Directory.Delete(root, true);
		}
	}

	[Fact]
	public void TryGetTypeface_WithSansSerifFallback_ResolvesAndCachesSansSerifFamily()
	{
		var root = CreateTempDirectory();
		var serifPath = Path.Combine(root, "Garamond.ttf");
		var sansPath = Path.Combine(root, "Arial.ttf");
		File.WriteAllText(serifPath, "dummy");
		File.WriteAllText(sansPath, "dummy");
		var factoryCallCount = 0;

		try
		{
			var resolver = new FontResolver(
				new RenderOptions
				{
					FontDirectories = [root]
				},
				new FakeFontMetadataReader(new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
				{
					[serifPath] = ["Garamond"],
					[sansPath] = ["Arial"]
				}),
				(_, _, _) =>
				{
					factoryCallCount++;
					return CreateTypefaceForTests();
				});

			resolver.TryGetTypeface("Missing", bold: true, italic: false, out var first).Should().BeTrue();
			resolver.TryGetTypeface("StillMissing", bold: true, italic: false, out var second).Should().BeTrue();

			ReferenceEquals(first, second).Should().BeTrue();
			factoryCallCount.Should().Be(1);
		}
		finally
		{
			Directory.Delete(root, true);
		}
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
		var root = CreateTempDirectory();
		var path = Path.Combine(root, "Replacement.ttf");
		File.WriteAllText(path, "dummy");

		try
		{
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
		finally
		{
			Directory.Delete(root, true);
		}
	}

	[Fact]
	public void TryGetTypeface_WithDefaultFactoryAndARealFont_ReturnsTypeface()
	{
		var root = CreateTempDirectory();
		var path = Path.Combine(root, "Replacement.ttf");
		File.Copy(TestFonts.SansRegularPath, path);

		try
		{
			var resolver = new FontResolver(
				[root],
				new FakeFontMetadataReader(new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
				{
					[path] = ["Replacement"]
				}));

			resolver.TryGetTypeface("Replacement", bold: false, italic: false, out var typeface).Should().BeTrue();
			typeface.Should().NotBeNull();
		}
		finally
		{
			Directory.Delete(root, true);
		}
	}

	[Fact]
	public void TryGetTypeface_WithDefaultFactoryAndUnreadableFont_ReturnsFalse()
	{
		var root = CreateTempDirectory();
		var path = Path.Combine(root, "Broken.ttf");
		File.WriteAllText(path, "not a font");

		try
		{
			var resolver = new FontResolver(
				[root],
				new FakeFontMetadataReader(new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
				{
					[path] = ["Broken"]
				}));

			resolver.TryGetTypeface("Broken", bold: false, italic: false, out var typeface).Should().BeFalse();
			typeface.Should().BeNull();
		}
		finally
		{
			Directory.Delete(root, true);
		}
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
		var setup = CreateResolverWithFamilies(options, "Resolved Heading");

		try
		{
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
		finally
		{
			Directory.Delete(setup.Root, true);
		}
	}

	[Fact]
	public void TryResolveThemeFontFamily_WithExactScriptMatch_PrefersSupplementalFont()
	{
		var setup = CreateResolverWithFamilies(new RenderOptions(), "Heading Japanese");

		try
		{
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
		finally
		{
			Directory.Delete(setup.Root, true);
		}
	}

	[Fact]
	public void TryResolveThemeFontFamily_WithEastAsianScript_UsesEastAsianFallback()
	{
		var setup = CreateResolverWithFamilies(new RenderOptions(), "Body EastAsian");

		try
		{
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
		finally
		{
			Directory.Delete(setup.Root, true);
		}
	}

	[Fact]
	public void TryResolveThemeFontFamily_WithComplexScript_UsesComplexScriptFallback()
	{
		var setup = CreateResolverWithFamilies(new RenderOptions(), "Body Complex");

		try
		{
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
		finally
		{
			Directory.Delete(setup.Root, true);
		}
	}

	[Fact]
	public void TryResolveThemeFontFamily_WithMissingThemeFamily_UsesFontResolverFallbackChain()
	{
		var setup = CreateResolverWithFamilies(new RenderOptions
		{
			FallbackFontFamily = "Fallback Family"
		}, "Fallback Family");

		try
		{
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
		finally
		{
			Directory.Delete(setup.Root, true);
		}
	}
}
