using AwesomeAssertions;
using SkiaSharp;
using Xunit;

namespace PanoramicData.Render.Test;

public partial class FontResolverTests
{
	[Fact]
	public void Constructor_WithNullDirectories_BuildsEmptyIndex()
	{
		new FontResolver((IReadOnlyList<string>?)null).FamilyIndex.Should().BeEmpty();
	}

	[Fact]
	public void Constructor_WithEmptyDirectories_BuildsEmptyIndex()
	{
		new FontResolver([]).FamilyIndex.Should().BeEmpty();
	}

	[Fact]
	public void Constructor_IgnoresMissingDirectories()
	{
		var missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

		new FontResolver([missingPath]).FamilyIndex.Should().BeEmpty();
	}

	[Fact]
	public void Constructor_IndexesSupportedExtensionsRecursively()
	{
		using var tempDirectory = new TempDirectory();
		var root = tempDirectory.Path;
		var nested = Directory.CreateDirectory(Path.Combine(root, "nested")).FullName;
		var ttf = WriteFont(root, "Heading.ttf");
		var otf = WriteFont(nested, "Body.otf");
		var ttc = WriteFont(nested, "Mono.ttc");
		WriteFont(root, "Ignore.txt");
		var resolver = new FontResolver([root]);
		resolver.FamilyIndex.Keys.Should().BeEquivalentTo("Heading", "Body", "Mono");
		resolver.FamilyIndex["Heading"].Should().Be(ttf);
		resolver.FamilyIndex["Body"].Should().Be(otf);
		resolver.FamilyIndex["Mono"].Should().Be(ttc);
	}

	[Fact]
	public void Constructor_TreatsExtensionsCaseInsensitively()
	{
		using var tempDirectory = new TempDirectory();
		var root = tempDirectory.Path;
		var upper = WriteFont(root, "Display.TTF");
		var resolver = new FontResolver([root]);
		resolver.FamilyIndex.Should().ContainKey("Display");
		resolver.FamilyIndex["Display"].Should().Be(upper);
	}

	[Fact]
	public void Constructor_WithDuplicateFamilyNames_KeepsFirstIndexedPath()
	{
		using var tempDirectory = new TempDirectory();
		var root = tempDirectory.Path;
		var firstDir = Directory.CreateDirectory(Path.Combine(root, "a")).FullName;
		var secondDir = Directory.CreateDirectory(Path.Combine(root, "b")).FullName;
		var first = WriteFont(firstDir, "Heading.ttf");
		WriteFont(secondDir, "Heading.otf");
		var resolver = new FontResolver([firstDir, secondDir]);
		resolver.FamilyIndex["Heading"].Should().Be(first);
	}

	[Fact]
	public void TryGetFontPath_PerformsCaseInsensitiveLookup()
	{
		using var tempDirectory = new TempDirectory();
		var root = tempDirectory.Path;
		var path = WriteFont(root, "Heading.ttf");
		AssertResolvesTo(new FontResolver([root]), "heading", path);
	}

	[Fact]
	public void TryGetFontPath_WithUnknownFamily_ReturnsFalse()
	{
		AssertUnresolved(new FontResolver([]), "Unknown");
	}

	[Fact]
	public void TryGetFontPath_WithNullOrWhitespaceFamily_ReturnsFalse()
	{
		var resolver = new FontResolver([]);

		AssertUnresolved(resolver, null!);
		AssertUnresolved(resolver, string.Empty);
		AssertUnresolved(resolver, "   ");
	}

	[Fact]
	public void Constructor_WithRenderOptionsNull_ThrowsArgumentNullException()
	{
		var act = () => new FontResolver((RenderOptions)null!);
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void Constructor_WithDefaultRenderOptions_BuildsEmptyIndex()
	{
		var resolver = new FontResolver(new RenderOptions());

		resolver.FamilyIndex.Should().BeEmpty();
		AssertUnresolved(resolver, "Unknown");
	}

	[Fact]
	public void TryGetFontPath_WithSubstitution_ResolvesReplacementFamily()
	{
		using var tempDirectory = new TempDirectory();
		var root = tempDirectory.Path;
		var path = WriteFont(root, "Replacement.ttf");

		AssertResolvesTo(ResolverFor(root, Subs("Requested", "Replacement")), "Requested", path);
	}

	[Fact]
	public void TryGetFontPath_WithDirectMatch_PrefersDirectFamilyOverSubstitution()
	{
		using var tempDirectory = new TempDirectory();
		var root = tempDirectory.Path;
		var directPath = WriteFont(root, "Requested.ttf");
		WriteFont(root, "Replacement.ttf");

		AssertResolvesTo(ResolverFor(root, Subs("Requested", "Replacement")), "Requested", directPath);
	}

	[Fact]
	public void TryGetFontPath_WithCaseInsensitiveSubstitution_UsesReplacement()
	{
		using var tempDirectory = new TempDirectory();
		var root = tempDirectory.Path;
		var path = WriteFont(root, "Replacement.ttf");

		AssertResolvesTo(ResolverFor(root, Subs("REQUESTED", "replacement")), "requested", path);
	}

	[Fact]
	public void TryGetFontPath_WithUnknownSubstitutionTarget_ReturnsFalse()
	{
		AssertUnresolved(new FontResolver(new RenderOptions { FontSubstitutions = Subs("Requested", "Missing") }), "Requested");
	}

	[Fact]
	public void TryGetFontPath_WithWhitespaceSubstitutionTarget_ReturnsFalse()
	{
		AssertUnresolved(new FontResolver(new RenderOptions { FontSubstitutions = Subs("Requested", "   ") }), "Requested");
	}

	[Fact]
	public void TryGetFontPath_WithMissingFamily_UsesFallbackFontFamily()
	{
		using var tempDirectory = new TempDirectory();
		var root = tempDirectory.Path;
		var fallbackPath = WriteFont(root, "Fallback.ttf");

		AssertResolvesTo(ResolverFor(root, null, "Fallback"), "Requested", fallbackPath);
	}

	[Fact]
	public void TryGetFontPath_WithMissingSubstitutionTarget_UsesFallbackFontFamily()
	{
		using var tempDirectory = new TempDirectory();
		var root = tempDirectory.Path;
		var fallbackPath = WriteFont(root, "Fallback.ttf");

		AssertResolvesTo(ResolverFor(root, Subs("Requested", "Missing"), "Fallback"), "Requested", fallbackPath);
	}

	[Fact]
	public void TryGetFontPath_WithNoConfiguredFallback_UsesFirstAvailableSansSerif()
	{
		using var tempDirectory = new TempDirectory();
		var root = tempDirectory.Path;
		WriteFont(root, "TimesNewRoman.ttf");
		var sansPath = WriteFont(root, "Arial.ttf");

		AssertResolvesTo(ResolverFor(root), "Requested", sansPath);
	}

	[Fact]
	public void TryGetFontPath_WithNonPreferredSansSerif_UsesNameHeuristic()
	{
		using var tempDirectory = new TempDirectory();
		var root = tempDirectory.Path;
		WriteFont(root, "Garamond.ttf");
		var sansPath = WriteFont(root, "CustomSansDisplay.ttf");

		AssertResolvesTo(ResolverFor(root), "Requested", sansPath);
	}

	[Fact]
	public void TryGetFontPath_WithNoFallbackOrSansSerif_ReturnsFalse()
	{
		using var tempDirectory = new TempDirectory();
		var root = tempDirectory.Path;
		WriteFont(root, "Garamond.ttf");

		AssertUnresolved(ResolverFor(root, null, "MissingFallback"), "Requested");
	}

	[Fact]
	public void TryGetTypeface_WithMissingFamily_ReturnsFalse()
	{
		using var fixture = CreateCountingResolver(new RenderOptions
		{
			FallbackFontFamily = "MissingFallback"
		}, ("Garamond.ttf", "Garamond"));
		var resolver = fixture.Resolver;

		resolver.TryGetTypeface("Requested", bold: false, italic: false, out var typeface).Should().BeFalse();
		typeface.Should().BeNull();
		fixture.FactoryCallCount.Should().Be(0);
	}
}
