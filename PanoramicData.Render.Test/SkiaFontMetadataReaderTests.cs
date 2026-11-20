namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public class SkiaFontMetadataReaderTests
{
	[Fact]
	public void Constructor_WithNullReader_ThrowsArgumentNullException()
	{
		var act = () => new SkiaFontMetadataReader(null!);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void ReadFamilyNames_WithNullWhitespaceOrMissingPath_ReturnsEmpty()
	{
		var reader = new SkiaFontMetadataReader((_, _) => "unused");

		reader.ReadFamilyNames(null!).Should().BeEmpty();
		reader.ReadFamilyNames(string.Empty).Should().BeEmpty();
		reader.ReadFamilyNames("   ").Should().BeEmpty();
		reader.ReadFamilyNames(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))).Should().BeEmpty();
	}

	[Fact]
	public void ReadFamilyNames_WithNonTtcFile_ReadsIndexZeroOnly()
	{
		var (result, calls) = ReadFamilies("Body.ttf", _ => "Body Family");

		result.Should().Equal("Body Family");
		calls.Should().Equal(0);
	}

	[Fact]
	public void ReadFamilyNames_WithNonTtcWhitespaceFamily_ReturnsEmpty()
	{
		var (result, _) = ReadFamilies("Body.otf", _ => "   ");

		result.Should().BeEmpty();
	}

	[Fact]
	public void ReadFamilyNames_WithTtcFile_EnumeratesUntilFirstNullAfterFirstFace()
	{
		var (result, calls) = ReadFamilies("Collection.ttc", index => index switch { 0 => "FaceA", 1 => "FaceB", _ => null });

		result.Should().BeEquivalentTo("FaceA", "FaceB");
		calls.Should().Equal(0, 1, 2);
	}

	[Fact]
	public void ReadFamilyNames_WithTtcAndLeadingNull_ContinuesPastIndexZero()
	{
		var (result, calls) = ReadFamilies("Collection.ttc", index => index == 1 ? "RecoveredFace" : null);

		result.Should().Equal("RecoveredFace");
		calls.Should().Equal(0, 1, 2);
	}

	[Fact]
	public void ReadFamilyNames_WithTtcCaseVariantDuplicates_DeduplicatesFamilies()
	{
		var (result, _) = ReadFamilies("Collection.ttc", index => index switch { 0 => "FaceA", 1 => "facea", _ => null });

		result.Should().ContainSingle().Which.Should().Be("FaceA");
	}
	[Fact]
	public void ReadFamilyNames_WithDefaultReaderAndInvalidFontData_ReturnsEmpty()
	{
		using var tempFont = new TempFontFile("NotAFont.ttf");
		var fontPath = tempFont.Path;

		var reader = new SkiaFontMetadataReader();

		reader.ReadFamilyNames(fontPath).Should().BeEmpty();
	}

	[Fact]
	public void ReadFamilyNames_WithDefaultReaderAndARealFont_ReturnsFamily()
	{
		var reader = new SkiaFontMetadataReader();
		var result = reader.ReadFamilyNames(TestFonts.SansRegularPath);

		result.Should().ContainSingle().Which.Should().Be(TestFonts.SansFamily);
	}

	private static (IReadOnlyList<string> Families, List<int> Calls) ReadFamilies(string fileName, Func<int, string?> familyAt)
	{
		using var tempFont = new TempFontFile(fileName);
		var calls = new List<int>();
		var reader = new SkiaFontMetadataReader((_, index) =>
		{
			calls.Add(index);
			return familyAt(index);
		});
		return (reader.ReadFamilyNames(tempFont.Path), calls);
	}

	private sealed class TempFontFile : IDisposable
	{
		private readonly string _directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"PanoramicData.Render.Test.{Guid.NewGuid():N}");

		public TempFontFile(string fileName)
		{
			Directory.CreateDirectory(_directory);
			Path = System.IO.Path.Combine(_directory, fileName);
			File.WriteAllText(Path, "dummy");
		}

		public string Path { get; }

		public void Dispose() => Directory.Delete(_directory, true);
	}
}