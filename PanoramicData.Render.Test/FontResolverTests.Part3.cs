using AwesomeAssertions;
using SkiaSharp;
using Xunit;

namespace PanoramicData.Render.Test;

public partial class FontResolverTests
{
	[Fact]
	public void TryResolveThemeFontFamily_WithNoThemeCandidate_ReturnsFalse()
	{
		var setup = CreateResolverWithFamilies(new RenderOptions());

		try
		{
			var theme = CreateThemeInfo(
				majorFont: CreateEmptyThemeFontInfo(),
				minorFont: CreateEmptyThemeFontInfo());

			setup.Resolver.TryResolveThemeFontFamily(theme, useMajorFont: true, script: null, out var familyName).Should().BeFalse();
			familyName.Should().BeNull();
		}
		finally
		{
			Directory.Delete(setup.Root, true);
		}
	}

	[Fact]
	public void TryResolveThemeFontFamily_WithNoThemeCandidate_UsesSansSerifFallback()
	{
		var setup = CreateResolverWithFamilies(new RenderOptions(), "Arial");

		try
		{
			var theme = CreateThemeInfo(
				majorFont: CreateEmptyThemeFontInfo(),
				minorFont: CreateEmptyThemeFontInfo());

			setup.Resolver.TryResolveThemeFontFamily(theme, useMajorFont: true, script: null, out var familyName).Should().BeTrue();
			familyName.Should().Be("Arial");
		}
		finally
		{
			Directory.Delete(setup.Root, true);
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

	private static (string Root, FontResolver Resolver) CreateResolverWithFamilies(RenderOptions options, params string[] familyNames)
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
		return (root, new FontResolver(options, new FakeFontMetadataReader(metadata), (_, _, _) => null));
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
