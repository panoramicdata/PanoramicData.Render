namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class FontEmbedderTests
{
	[Fact]
	public void GetEmbeddedFontData_WithEmptyFontDirectories_ReturnsNull()
	{
		var result = FontEmbedder.GetEmbeddedFontData("Calibri", []);

		result.Should().BeNull();
	}

	[Fact]
	public void GetEmbeddedFontData_WithNullFontName_ReturnsNull()
	{
		var result = FontEmbedder.GetEmbeddedFontData(null!, []);

		result.Should().BeNull();
	}

	[Fact]
	public void GetEmbeddedFontData_WithNonexistentFont_ReturnsNull()
	{
		var tempDir = Path.Combine(Path.GetTempPath(), "FontEmbedderTest_" + Guid.NewGuid());
		Directory.CreateDirectory(tempDir);

		try
		{
			var result = FontEmbedder.GetEmbeddedFontData("NonexistentFont", [tempDir]);

			result.Should().BeNull();
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[Fact]
	public void GetEmbeddedFontData_WithValidFontFile_ReturnsBase64()
	{
		var tempDir = Path.Combine(Path.GetTempPath(), "FontEmbedderTest_" + Guid.NewGuid());
		Directory.CreateDirectory(tempDir);

		try
		{
			// Create a minimal test font file (just some bytes - not a real font)
			var testFontPath = Path.Combine(tempDir, "TestFont.ttf");
			var testData = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 };
			File.WriteAllBytes(testFontPath, testData);

			var result = FontEmbedder.GetEmbeddedFontData("TestFont", [tempDir]);

			// Should return Base64-encoded string
			result.Should().NotBeNull();
			// Verify it's valid Base64 by trying to decode it
			var decoded = Convert.FromBase64String(result!);
			decoded.Should().Equal(testData);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[Fact]
	public void GetEmbeddedFontData_CachesResult()
	{
		var tempDir = Path.Combine(Path.GetTempPath(), "FontEmbedderTest_" + Guid.NewGuid());
		Directory.CreateDirectory(tempDir);

		try
		{
			var testFontPath = Path.Combine(tempDir, "TestFont.ttf");
			var testData = new byte[] { 0xAA, 0xBB, 0xCC };
			File.WriteAllBytes(testFontPath, testData);

			// First call - loads from disk
			var result1 = FontEmbedder.GetEmbeddedFontData("TestFont", [tempDir]);

			// Delete the file
			File.Delete(testFontPath);

			// Second call - should return same result from cache even though file is gone
			var result2 = FontEmbedder.GetEmbeddedFontData("TestFont", [tempDir]);

			result1.Should().Be(result2);
			result2.Should().NotBeNull();
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}
}
