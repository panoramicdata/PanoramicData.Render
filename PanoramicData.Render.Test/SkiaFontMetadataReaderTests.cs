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
		var tempDir = CreateTempDirectory();
		var fontPath = Path.Combine(tempDir, "Body.ttf");
		File.WriteAllText(fontPath, "dummy");
		var calls = new List<int>();

		try
		{
			var reader = new SkiaFontMetadataReader((_, index) =>
			{
				calls.Add(index);
				return "Body Family";
			});

			var result = reader.ReadFamilyNames(fontPath);

			result.Should().Equal("Body Family");
			calls.Should().Equal(0);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[Fact]
	public void ReadFamilyNames_WithNonTtcWhitespaceFamily_ReturnsEmpty()
	{
		var tempDir = CreateTempDirectory();
		var fontPath = Path.Combine(tempDir, "Body.otf");
		File.WriteAllText(fontPath, "dummy");

		try
		{
			var reader = new SkiaFontMetadataReader((_, _) => "   ");

			reader.ReadFamilyNames(fontPath).Should().BeEmpty();
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[Fact]
	public void ReadFamilyNames_WithTtcFile_EnumeratesUntilFirstNullAfterFirstFace()
	{
		var tempDir = CreateTempDirectory();
		var fontPath = Path.Combine(tempDir, "Collection.ttc");
		File.WriteAllText(fontPath, "dummy");
		var calls = new List<int>();

		try
		{
			var reader = new SkiaFontMetadataReader((_, index) =>
			{
				calls.Add(index);
				return index switch
				{
					0 => "FaceA",
					1 => "FaceB",
					_ => null
				};
			});

			var result = reader.ReadFamilyNames(fontPath);

			result.Should().BeEquivalentTo("FaceA", "FaceB");
			calls.Should().Equal(0, 1, 2);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[Fact]
	public void ReadFamilyNames_WithTtcAndLeadingNull_ContinuesPastIndexZero()
	{
		var tempDir = CreateTempDirectory();
		var fontPath = Path.Combine(tempDir, "Collection.ttc");
		File.WriteAllText(fontPath, "dummy");
		var calls = new List<int>();

		try
		{
			var reader = new SkiaFontMetadataReader((_, index) =>
			{
				calls.Add(index);
				return index switch
				{
					0 => null,
					1 => "RecoveredFace",
					_ => null
				};
			});

			var result = reader.ReadFamilyNames(fontPath);

			result.Should().Equal("RecoveredFace");
			calls.Should().Equal(0, 1, 2);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[Fact]
	public void ReadFamilyNames_WithTtcCaseVariantDuplicates_DeduplicatesFamilies()
	{
		var tempDir = CreateTempDirectory();
		var fontPath = Path.Combine(tempDir, "Collection.ttc");
		File.WriteAllText(fontPath, "dummy");

		try
		{
			var reader = new SkiaFontMetadataReader((_, index) =>
			{
				return index switch
				{
					0 => "FaceA",
					1 => "facea",
					_ => null
				};
			});

			var result = reader.ReadFamilyNames(fontPath);

			result.Should().ContainSingle().Which.Should().Be("FaceA");
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[Fact]
	public void ReadFamilyNames_WithDefaultReaderAndInvalidFontData_ReturnsEmpty()
	{
		var tempDir = CreateTempDirectory();
		var fontPath = Path.Combine(tempDir, "NotAFont.ttf");
		File.WriteAllText(fontPath, "dummy");

		try
		{
			var reader = new SkiaFontMetadataReader();

			reader.ReadFamilyNames(fontPath).Should().BeEmpty();
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[Fact]
	public void ReadFamilyNames_WithDefaultReaderAndARealFont_ReturnsFamily()
	{
		var reader = new SkiaFontMetadataReader();
		var result = reader.ReadFamilyNames(TestFonts.SansRegularPath);

		result.Should().ContainSingle().Which.Should().Be(TestFonts.SansFamily);
	}

	private static string CreateTempDirectory()
	{
		var path = Path.Combine(Path.GetTempPath(), $"PanoramicData.Render.Test.{Guid.NewGuid():N}");
		Directory.CreateDirectory(path);
		return path;
	}
}