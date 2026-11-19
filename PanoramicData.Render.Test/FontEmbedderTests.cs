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
		using var temp = new TempDirectory();
		var tempDir = temp.Path;

		var result = FontEmbedder.GetEmbeddedFontData("NonexistentFont", [tempDir]);

		result.Should().BeNull();
	}

	[Fact]
	public void GetEmbeddedFontData_WithValidFontFile_ReturnsBase64()
	{
		using var temp = new TempDirectory();
		var tempDir = temp.Path;

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

	[Fact]
	public void GetEmbeddedFontData_CachesResult()
	{
		using var temp = new TempDirectory();
		var tempDir = temp.Path;

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

	private sealed class TempDirectory : IDisposable
	{
		public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "FontEmbedderTest_" + Guid.NewGuid());

		public TempDirectory() => Directory.CreateDirectory(Path);

		public void Dispose() => Directory.Delete(Path, true);
	}
}