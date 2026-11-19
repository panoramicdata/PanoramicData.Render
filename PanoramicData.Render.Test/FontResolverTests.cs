using AwesomeAssertions;
using SkiaSharp;
using Xunit;

namespace PanoramicData.Render.Test;

public partial class FontResolverTests
{
	[Fact]
	public void Constructor_WithNullDirectories_BuildsEmptyIndex()
	{
		var resolver = new FontResolver((IReadOnlyList<string>?)null);

		resolver.FamilyIndex.Should().BeEmpty();
	}

	[Fact]
	public void Constructor_WithEmptyDirectories_BuildsEmptyIndex()
	{
		var resolver = new FontResolver([]);

		resolver.FamilyIndex.Should().BeEmpty();
	}

	[Fact]
	public void Constructor_IgnoresMissingDirectories()
	{
		var missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
		var resolver = new FontResolver([missingPath]);

		resolver.FamilyIndex.Should().BeEmpty();
	}

	[Fact]
	public void Constructor_IndexesSupportedExtensionsRecursively()
	{
		using var tempDirectory = new TempDirectory();
		var root = tempDirectory.Path;
		var nested = Directory.CreateDirectory(Path.Combine(root, "nested")).FullName;
		var ttf = Path.Combine(root, "Heading.ttf");
		var otf = Path.Combine(nested, "Body.otf");
		var ttc = Path.Combine(nested, "Mono.ttc");
		var txt = Path.Combine(root, "Ignore.txt");
		File.WriteAllText(ttf, "dummy");
		File.WriteAllText(otf, "dummy");
		File.WriteAllText(ttc, "dummy");
		File.WriteAllText(txt, "dummy");
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
		var upper = Path.Combine(root, "Display.TTF");
		File.WriteAllText(upper, "dummy");
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
		var first = Path.Combine(firstDir, "Heading.ttf");
		var second = Path.Combine(secondDir, "Heading.otf");
		File.WriteAllText(first, "dummy");
		File.WriteAllText(second, "dummy");
		var resolver = new FontResolver([firstDir, secondDir]);
		resolver.FamilyIndex["Heading"].Should().Be(first);
	}

	[Fact]
	public void TryGetFontPath_PerformsCaseInsensitiveLookup()
	{
		using var tempDirectory = new TempDirectory();
		var root = tempDirectory.Path;
		var path = Path.Combine(root, "Heading.ttf");
		File.WriteAllText(path, "dummy");
		var resolver = new FontResolver([root]);
		resolver.TryGetFontPath("heading", out var resolved).Should().BeTrue();
		resolved.Should().Be(path);
	}

	[Fact]
	public void TryGetFontPath_WithUnknownFamily_ReturnsFalse()
	{
		var resolver = new FontResolver([]);
		resolver.TryGetFontPath("Unknown", out var path).Should().BeFalse();
		path.Should().BeNull();
	}

	[Fact]
	public void TryGetFontPath_WithNullOrWhitespaceFamily_ReturnsFalse()
	{
		var resolver = new FontResolver([]);

		resolver.TryGetFontPath(null!, out var p1).Should().BeFalse();
		resolver.TryGetFontPath(string.Empty, out var p2).Should().BeFalse();
		resolver.TryGetFontPath("   ", out var p3).Should().BeFalse();

		p1.Should().BeNull();
		p2.Should().BeNull();
		p3.Should().BeNull();
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
		resolver.TryGetFontPath("Unknown", out var resolved).Should().BeFalse();
		resolved.Should().BeNull();
	}

	[Fact]
	public void TryGetFontPath_WithSubstitution_ResolvesReplacementFamily()
	{
		using var tempDirectory = new TempDirectory();
		var root = tempDirectory.Path;
		var path = Path.Combine(root, "Replacement.ttf");
		File.WriteAllText(path, "dummy");

		var resolver = new FontResolver(new RenderOptions
		{
			FontDirectories = [root],
			FontSubstitutions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				["Requested"] = "Replacement"
			}
		});

		resolver.TryGetFontPath("Requested", out var resolved).Should().BeTrue();
		resolved.Should().Be(path);
	}

	[Fact]
	public void TryGetFontPath_WithDirectMatch_PrefersDirectFamilyOverSubstitution()
	{
		using var tempDirectory = new TempDirectory();
		var root = tempDirectory.Path;
		var directPath = Path.Combine(root, "Requested.ttf");
		var replacementPath = Path.Combine(root, "Replacement.ttf");
		File.WriteAllText(directPath, "dummy");
		File.WriteAllText(replacementPath, "dummy");

		var resolver = new FontResolver(new RenderOptions
		{
			FontDirectories = [root],
			FontSubstitutions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				["Requested"] = "Replacement"
			}
		});

		resolver.TryGetFontPath("Requested", out var resolved).Should().BeTrue();
		resolved.Should().Be(directPath);
	}

	[Fact]
	public void TryGetFontPath_WithCaseInsensitiveSubstitution_UsesReplacement()
	{
		using var tempDirectory = new TempDirectory();
		var root = tempDirectory.Path;
		var path = Path.Combine(root, "Replacement.ttf");
		File.WriteAllText(path, "dummy");

		var resolver = new FontResolver(new RenderOptions
		{
			FontDirectories = [root],
			FontSubstitutions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				["REQUESTED"] = "replacement"
			}
		});

		resolver.TryGetFontPath("requested", out var resolved).Should().BeTrue();
		resolved.Should().Be(path);
	}

	[Fact]
	public void TryGetFontPath_WithUnknownSubstitutionTarget_ReturnsFalse()
	{
		var resolver = new FontResolver(new RenderOptions
		{
			FontSubstitutions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				["Requested"] = "Missing"
			}
		});

		resolver.TryGetFontPath("Requested", out var resolved).Should().BeFalse();
		resolved.Should().BeNull();
	}

	[Fact]
	public void TryGetFontPath_WithWhitespaceSubstitutionTarget_ReturnsFalse()
	{
		var resolver = new FontResolver(new RenderOptions
		{
			FontSubstitutions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				["Requested"] = "   "
			}
		});

		resolver.TryGetFontPath("Requested", out var resolved).Should().BeFalse();
		resolved.Should().BeNull();
	}

	[Fact]
	public void TryGetFontPath_WithMissingFamily_UsesFallbackFontFamily()
	{
		using var tempDirectory = new TempDirectory();
		var root = tempDirectory.Path;
		var fallbackPath = Path.Combine(root, "Fallback.ttf");
		File.WriteAllText(fallbackPath, "dummy");

		var resolver = new FontResolver(new RenderOptions
		{
			FontDirectories = [root],
			FallbackFontFamily = "Fallback"
		});

		resolver.TryGetFontPath("Requested", out var resolved).Should().BeTrue();
		resolved.Should().Be(fallbackPath);
	}

	[Fact]
	public void TryGetFontPath_WithMissingSubstitutionTarget_UsesFallbackFontFamily()
	{
		using var tempDirectory = new TempDirectory();
		var root = tempDirectory.Path;
		var fallbackPath = Path.Combine(root, "Fallback.ttf");
		File.WriteAllText(fallbackPath, "dummy");

		var resolver = new FontResolver(new RenderOptions
		{
			FontDirectories = [root],
			FallbackFontFamily = "Fallback",
			FontSubstitutions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				["Requested"] = "Missing"
			}
		});

		resolver.TryGetFontPath("Requested", out var resolved).Should().BeTrue();
		resolved.Should().Be(fallbackPath);
	}

	[Fact]
	public void TryGetFontPath_WithNoConfiguredFallback_UsesFirstAvailableSansSerif()
	{
		using var tempDirectory = new TempDirectory();
		var root = tempDirectory.Path;
		var serifPath = Path.Combine(root, "TimesNewRoman.ttf");
		var sansPath = Path.Combine(root, "Arial.ttf");
		File.WriteAllText(serifPath, "dummy");
		File.WriteAllText(sansPath, "dummy");

		var resolver = new FontResolver(new RenderOptions
		{
			FontDirectories = [root]
		});

		resolver.TryGetFontPath("Requested", out var resolved).Should().BeTrue();
		resolved.Should().Be(sansPath);
	}

	[Fact]
	public void TryGetFontPath_WithNonPreferredSansSerif_UsesNameHeuristic()
	{
		using var tempDirectory = new TempDirectory();
		var root = tempDirectory.Path;
		var serifPath = Path.Combine(root, "Garamond.ttf");
		var sansPath = Path.Combine(root, "CustomSansDisplay.ttf");
		File.WriteAllText(serifPath, "dummy");
		File.WriteAllText(sansPath, "dummy");

		var resolver = new FontResolver(new RenderOptions
		{
			FontDirectories = [root]
		});

		resolver.TryGetFontPath("Requested", out var resolved).Should().BeTrue();
		resolved.Should().Be(sansPath);
	}

	[Fact]
	public void TryGetFontPath_WithNoFallbackOrSansSerif_ReturnsFalse()
	{
		using var tempDirectory = new TempDirectory();
		var root = tempDirectory.Path;
		var serifPath = Path.Combine(root, "Garamond.ttf");
		File.WriteAllText(serifPath, "dummy");

		var resolver = new FontResolver(new RenderOptions
		{
			FontDirectories = [root],
			FallbackFontFamily = "MissingFallback"
		});

		resolver.TryGetFontPath("Requested", out var resolved).Should().BeFalse();
		resolved.Should().BeNull();
	}

	[Fact]
	public void TryGetTypeface_WithMissingFamily_ReturnsFalse()
	{
		using var tempDirectory = new TempDirectory();
		var root = tempDirectory.Path;
		var serifPath = Path.Combine(root, "Garamond.ttf");
		File.WriteAllText(serifPath, "dummy");
		var factoryCallCount = 0;

		var resolver = new FontResolver(
			new RenderOptions
			{
				FontDirectories = [root],
				FallbackFontFamily = "MissingFallback"
			},
			new FakeFontMetadataReader(new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
			{
				[serifPath] = ["Garamond"]
			}),
			(_, _, _) =>
			{
				factoryCallCount++;
				return CreateTypefaceForTests();
			});

		resolver.TryGetTypeface("Requested", bold: false, italic: false, out var typeface).Should().BeFalse();
		typeface.Should().BeNull();
		factoryCallCount.Should().Be(0);
	}
}
