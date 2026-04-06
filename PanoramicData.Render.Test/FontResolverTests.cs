namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public class FontResolverTests
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
		var root = CreateTempDirectory();
		var nested = Directory.CreateDirectory(Path.Combine(root, "nested")).FullName;
		var ttf = Path.Combine(root, "Heading.ttf");
		var otf = Path.Combine(nested, "Body.otf");
		var ttc = Path.Combine(nested, "Mono.ttc");
		var txt = Path.Combine(root, "Ignore.txt");
		File.WriteAllText(ttf, "dummy");
		File.WriteAllText(otf, "dummy");
		File.WriteAllText(ttc, "dummy");
		File.WriteAllText(txt, "dummy");
		try
		{
			var resolver = new FontResolver([root]);
			resolver.FamilyIndex.Keys.Should().BeEquivalentTo("Heading", "Body", "Mono");
			resolver.FamilyIndex["Heading"].Should().Be(ttf);
			resolver.FamilyIndex["Body"].Should().Be(otf);
			resolver.FamilyIndex["Mono"].Should().Be(ttc);
		}
		finally
		{
			Directory.Delete(root, true);
		}
	}

	[Fact]
	public void Constructor_TreatsExtensionsCaseInsensitively()
	{
		var root = CreateTempDirectory();
		var upper = Path.Combine(root, "Display.TTF");
		File.WriteAllText(upper, "dummy");
		try
		{
			var resolver = new FontResolver([root]);
			resolver.FamilyIndex.Should().ContainKey("Display");
			resolver.FamilyIndex["Display"].Should().Be(upper);
		}
		finally
		{
			Directory.Delete(root, true);
		}
	}

	[Fact]
	public void Constructor_WithDuplicateFamilyNames_KeepsFirstIndexedPath()
	{
		var root = CreateTempDirectory();
		var firstDir = Directory.CreateDirectory(Path.Combine(root, "a")).FullName;
		var secondDir = Directory.CreateDirectory(Path.Combine(root, "b")).FullName;
		var first = Path.Combine(firstDir, "Heading.ttf");
		var second = Path.Combine(secondDir, "Heading.otf");
		File.WriteAllText(first, "dummy");
		File.WriteAllText(second, "dummy");
		try
		{
			var resolver = new FontResolver([firstDir, secondDir]);
			resolver.FamilyIndex["Heading"].Should().Be(first);
		}
		finally
		{
			Directory.Delete(root, true);
		}
	}

	[Fact]
	public void TryGetFontPath_PerformsCaseInsensitiveLookup()
	{
		var root = CreateTempDirectory();
		var path = Path.Combine(root, "Heading.ttf");
		File.WriteAllText(path, "dummy");
		try
		{
			var resolver = new FontResolver([root]);
			resolver.TryGetFontPath("heading", out var resolved).Should().BeTrue();
			resolved.Should().Be(path);
		}
		finally
		{
			Directory.Delete(root, true);
		}
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
		var root = CreateTempDirectory();
		var path = Path.Combine(root, "Replacement.ttf");
		File.WriteAllText(path, "dummy");

		try
		{
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
		finally
		{
			Directory.Delete(root, true);
		}
	}

	[Fact]
	public void TryGetFontPath_WithDirectMatch_PrefersDirectFamilyOverSubstitution()
	{
		var root = CreateTempDirectory();
		var directPath = Path.Combine(root, "Requested.ttf");
		var replacementPath = Path.Combine(root, "Replacement.ttf");
		File.WriteAllText(directPath, "dummy");
		File.WriteAllText(replacementPath, "dummy");

		try
		{
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
		finally
		{
			Directory.Delete(root, true);
		}
	}

	[Fact]
	public void TryGetFontPath_WithCaseInsensitiveSubstitution_UsesReplacement()
	{
		var root = CreateTempDirectory();
		var path = Path.Combine(root, "Replacement.ttf");
		File.WriteAllText(path, "dummy");

		try
		{
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
		finally
		{
			Directory.Delete(root, true);
		}
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
		var root = CreateTempDirectory();
		var fallbackPath = Path.Combine(root, "Fallback.ttf");
		File.WriteAllText(fallbackPath, "dummy");

		try
		{
			var resolver = new FontResolver(new RenderOptions
			{
				FontDirectories = [root],
				FallbackFontFamily = "Fallback"
			});

			resolver.TryGetFontPath("Requested", out var resolved).Should().BeTrue();
			resolved.Should().Be(fallbackPath);
		}
		finally
		{
			Directory.Delete(root, true);
		}
	}

	[Fact]
	public void TryGetFontPath_WithMissingSubstitutionTarget_UsesFallbackFontFamily()
	{
		var root = CreateTempDirectory();
		var fallbackPath = Path.Combine(root, "Fallback.ttf");
		File.WriteAllText(fallbackPath, "dummy");

		try
		{
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
		finally
		{
			Directory.Delete(root, true);
		}
	}

	[Fact]
	public void TryGetFontPath_WithNoConfiguredFallback_UsesFirstAvailableSansSerif()
	{
		var root = CreateTempDirectory();
		var serifPath = Path.Combine(root, "TimesNewRoman.ttf");
		var sansPath = Path.Combine(root, "Arial.ttf");
		File.WriteAllText(serifPath, "dummy");
		File.WriteAllText(sansPath, "dummy");

		try
		{
			var resolver = new FontResolver(new RenderOptions
			{
				FontDirectories = [root]
			});

			resolver.TryGetFontPath("Requested", out var resolved).Should().BeTrue();
			resolved.Should().Be(sansPath);
		}
		finally
		{
			Directory.Delete(root, true);
		}
	}

	[Fact]
	public void TryGetFontPath_WithNonPreferredSansSerif_UsesNameHeuristic()
	{
		var root = CreateTempDirectory();
		var serifPath = Path.Combine(root, "Garamond.ttf");
		var sansPath = Path.Combine(root, "CustomSansDisplay.ttf");
		File.WriteAllText(serifPath, "dummy");
		File.WriteAllText(sansPath, "dummy");

		try
		{
			var resolver = new FontResolver(new RenderOptions
			{
				FontDirectories = [root]
			});

			resolver.TryGetFontPath("Requested", out var resolved).Should().BeTrue();
			resolved.Should().Be(sansPath);
		}
		finally
		{
			Directory.Delete(root, true);
		}
	}

	[Fact]
	public void TryGetFontPath_WithNoFallbackOrSansSerif_ReturnsFalse()
	{
		var root = CreateTempDirectory();
		var serifPath = Path.Combine(root, "Garamond.ttf");
		File.WriteAllText(serifPath, "dummy");

		try
		{
			var resolver = new FontResolver(new RenderOptions
			{
				FontDirectories = [root],
				FallbackFontFamily = "MissingFallback"
			});

			resolver.TryGetFontPath("Requested", out var resolved).Should().BeFalse();
			resolved.Should().BeNull();
		}
		finally
		{
			Directory.Delete(root, true);
		}
	}

	[Fact]
	public void Constructor_WithTtcMetadataFamilies_IndexesAllFamiliesToSamePath()
	{
		var root = CreateTempDirectory();
		var ttcPath = Path.Combine(root, "Collection.ttc");
		File.WriteAllText(ttcPath, "dummy");

		try
		{
			var resolver = new FontResolver([root], new FakeFontMetadataReader(new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
			{
				[ttcPath] = ["TtcFamilyA", "TtcFamilyB"]
			}));

			resolver.FamilyIndex.Should().ContainKey("TtcFamilyA");
			resolver.FamilyIndex.Should().ContainKey("TtcFamilyB");
			resolver.FamilyIndex["TtcFamilyA"].Should().Be(ttcPath);
			resolver.FamilyIndex["TtcFamilyB"].Should().Be(ttcPath);
		}
		finally
		{
			Directory.Delete(root, true);
		}
	}

	[Fact]
	public void Constructor_WhenMetadataReaderReturnsEmpty_FallsBackToFileName()
	{
		var root = CreateTempDirectory();
		var ttcPath = Path.Combine(root, "FallbackFamily.ttc");
		File.WriteAllText(ttcPath, "dummy");

		try
		{
			var resolver = new FontResolver([root], new FakeFontMetadataReader(new Dictionary<string, IReadOnlyList<string>>()));

			resolver.FamilyIndex.Should().ContainKey("FallbackFamily");
			resolver.FamilyIndex["FallbackFamily"].Should().Be(ttcPath);
		}
		finally
		{
			Directory.Delete(root, true);
		}
	}

	[Fact]
	public void InternalConstructor_WithNullMetadataReader_ThrowsArgumentNullException()
	{
		var act = () => new FontResolver([], null!);

		act.Should().Throw<ArgumentNullException>();
	}

	private static string CreateTempDirectory()
	{
		var path = Path.Combine(Path.GetTempPath(), $"PanoramicData.Render.Test.{Guid.NewGuid():N}");
		Directory.CreateDirectory(path);
		return path;
	}

	private sealed class FakeFontMetadataReader : IFontMetadataReader
	{
		private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _map;

		public FakeFontMetadataReader(IReadOnlyDictionary<string, IReadOnlyList<string>> map)
		{
			_map = map;
		}

		public IReadOnlyList<string> ReadFamilyNames(string filePath)
		{
			return _map.TryGetValue(filePath, out var names) ? names : [];
		}
	}
}
