using AwesomeAssertions;
using SkiaSharp;
using Xunit;

namespace PanoramicData.Render.Test;

public partial class FontResolverTests
{
	[Fact]
	public void TryResolveThemeFontFamily_WithNoThemeCandidate_ReturnsFalse()
	{
		using var setup = CreateResolverWithFamilies(new RenderOptions());

		var theme = CreateThemeInfo(
			majorFont: CreateEmptyThemeFontInfo(),
			minorFont: CreateEmptyThemeFontInfo());

		setup.Resolver.TryResolveThemeFontFamily(theme, useMajorFont: true, script: null, out var familyName).Should().BeFalse();
		familyName.Should().BeNull();
	}

	[Fact]
	public void TryResolveThemeFontFamily_WithNoThemeCandidate_UsesSansSerifFallback()
	{
		using var setup = CreateResolverWithFamilies(new RenderOptions(), "Arial");

		var theme = CreateThemeInfo(
			majorFont: CreateEmptyThemeFontInfo(),
			minorFont: CreateEmptyThemeFontInfo());

		setup.Resolver.TryResolveThemeFontFamily(theme, useMajorFont: true, script: null, out var familyName).Should().BeTrue();
		familyName.Should().Be("Arial");
	}

	[Fact]
	public void Constructor_WithTtcMetadataFamilies_IndexesAllFamiliesToSamePath()
	{
		using var tempDirectory = new TempDirectory();
		var root = tempDirectory.Path;
		var ttcPath = Path.Combine(root, "Collection.ttc");
		File.WriteAllText(ttcPath, "dummy");

		var resolver = new FontResolver([root], new FakeFontMetadataReader(new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
		{
			[ttcPath] = ["TtcFamilyA", "TtcFamilyB"]
		}));

		resolver.FamilyIndex.Should().ContainKey("TtcFamilyA");
		resolver.FamilyIndex.Should().ContainKey("TtcFamilyB");
		resolver.FamilyIndex["TtcFamilyA"].Should().Be(ttcPath);
		resolver.FamilyIndex["TtcFamilyB"].Should().Be(ttcPath);
	}

	[Fact]
	public void Constructor_WhenMetadataReaderReturnsEmpty_FallsBackToFileName()
	{
		using var tempDirectory = new TempDirectory();
		var root = tempDirectory.Path;
		var ttcPath = Path.Combine(root, "FallbackFamily.ttc");
		File.WriteAllText(ttcPath, "dummy");

		var resolver = new FontResolver([root], new FakeFontMetadataReader(new Dictionary<string, IReadOnlyList<string>>()));

		resolver.FamilyIndex.Should().ContainKey("FallbackFamily");
		resolver.FamilyIndex["FallbackFamily"].Should().Be(ttcPath);
	}

	[Fact]
	public void InternalConstructor_WithNullMetadataReader_ThrowsArgumentNullException()
	{
		var act = () => new FontResolver([], null!);

		act.Should().Throw<ArgumentNullException>();
	}

	private static string WriteFont(string directory, string fileName)
	{
		var path = Path.Combine(directory, fileName);
		File.WriteAllText(path, "dummy");
		return path;
	}

	private static Dictionary<string, string> Subs(string requested, string replacement)
		=> new(StringComparer.OrdinalIgnoreCase) { [requested] = replacement };

	private static FontResolver ResolverFor(string root, Dictionary<string, string>? substitutions = null, string? fallback = null)
	{
		var options = new RenderOptions { FontDirectories = [root] };
		if (substitutions is not null)
		{
			options.FontSubstitutions = substitutions;
		}

		if (fallback is not null)
		{
			options.FallbackFontFamily = fallback;
		}

		return new FontResolver(options);
	}

	private static void AssertResolvesTo(FontResolver resolver, string family, string expectedPath)
	{
		resolver.TryGetFontPath(family, out var resolved).Should().BeTrue();
		resolved.Should().Be(expectedPath);
	}

	private static void AssertUnresolved(FontResolver resolver, string family)
	{
		resolver.TryGetFontPath(family, out var resolved).Should().BeFalse();
		resolved.Should().BeNull();
	}

	private static FakeFontMetadataReader MetadataFor(string path, string family)
		=> new(new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase) { [path] = [family] });

	private static ThemeFontInfo ThemeFont(string? latin, string? eastAsian, string? complexScript, Dictionary<string, string>? scripts = null)
		=> new()
		{
			Latin = latin,
			EastAsian = eastAsian,
			ComplexScript = complexScript,
			ScriptFonts = scripts ?? []
		};

	private static void AssertThemeFamily(ResolverSetup setup, ThemeFontInfo major, ThemeFontInfo minor, bool useMajorFont, string? script, string? expected)
	{
		var theme = CreateThemeInfo(major, minor);
		setup.Resolver.TryResolveThemeFontFamily(theme, useMajorFont, script, out var familyName).Should().Be(expected is not null);
		familyName.Should().Be(expected);
	}

	private sealed class CountingResolver(string root) : IDisposable
	{
		public int FactoryCallCount { get; set; }

		public FontResolver Resolver { get; set; } = null!;

		public void Dispose() => Directory.Delete(root, true);
	}

	private static CountingResolver CreateCountingResolver(RenderOptions options, params (string FileName, string Family)[] fonts)
	{
		var root = CreateTempDirectory();
		var metadata = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
		foreach (var (fileName, family) in fonts)
		{
			var path = Path.Combine(root, fileName);
			File.WriteAllText(path, "dummy");
			metadata[path] = [family];
		}

		options.FontDirectories = [root];
		var fixture = new CountingResolver(root);
		fixture.Resolver = new FontResolver(options, new FakeFontMetadataReader(metadata), (_, _, _) =>
		{
			fixture.FactoryCallCount++;
			return CreateTypefaceForTests();
		});
		return fixture;
	}
	private sealed class TempDirectory : IDisposable
	{
		public string Path { get; } = CreateTempDirectory();

		public void Dispose() => Directory.Delete(Path, true);
	}

	private sealed class ResolverSetup(string root, FontResolver resolver) : IDisposable
	{
		public string Root { get; } = root;

		public FontResolver Resolver { get; } = resolver;

		public void Dispose() => Directory.Delete(Root, true);
	}

	private static string CreateTempDirectory()
	{
		var path = Path.Combine(Path.GetTempPath(), $"PanoramicData.Render.Test.{Guid.NewGuid():N}");
		Directory.CreateDirectory(path);
		return path;
	}

	private static SKTypeface CreateTypefaceForTests() => TestFonts.CreateSans();

	private static ThemeInfo CreateThemeInfo(ThemeFontInfo majorFont, ThemeFontInfo minorFont)
	{
		return new ThemeInfo
		{
			MajorFont = majorFont,
			MinorFont = minorFont,
			Colors = new Dictionary<string, string>()
		};
	}

	private static ResolverSetup CreateResolverWithFamilies(RenderOptions options, params string[] familyNames)
	{
		var root = CreateTempDirectory();
		var metadata = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
		for (var index = 0; index < familyNames.Length; index++)
		{
			var path = Path.Combine(root, $"font-{index}.ttf");
			File.WriteAllText(path, "dummy");
			metadata[path] = [familyNames[index]];
		}

		options.FontDirectories = [root];
		return new ResolverSetup(root, new FontResolver(options, new FakeFontMetadataReader(metadata), (_, _, _) => null));
	}

	private static ThemeFontInfo CreateEmptyThemeFontInfo()
	{
		return new ThemeFontInfo
		{
			Latin = null,
			EastAsian = null,
			ComplexScript = null,
			ScriptFonts = new Dictionary<string, string>()
		};
	}

	private sealed class FakeFontMetadataReader(IReadOnlyDictionary<string, IReadOnlyList<string>> map) : IFontMetadataReader
	{
		private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _map = map;

		public IReadOnlyList<string> ReadFamilyNames(string filePath)
		{
			return _map.TryGetValue(filePath, out var names) ? names : [];
		}
	}
}
