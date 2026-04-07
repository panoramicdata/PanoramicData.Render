using AwesomeAssertions;
using SkiaSharp;
using Xunit;

namespace PanoramicData.Render.Test;

public class FontPropertiesTests
{
	[Fact]
	public void Constructor_SetsAllProperties()
	{
		var props = new FontProperties("Arial", 12f, Bold: true, Italic: false);

		props.FamilyName.Should().Be("Arial");
		props.SizePoints.Should().Be(12f);
		props.Bold.Should().BeTrue();
		props.Italic.Should().BeFalse();
	}

	[Fact]
	public void SizeTwips_ConvertPointsToTwipsCorrectly()
	{
		// 12 pt × 20 twips/pt = 240 twips
		var props = new FontProperties("Arial", 12f, false, false);

		props.SizeTwips.Should().Be(240f);
	}

	[Fact]
	public void SizeHalfPoints_ReturnsDoubleOfPointSize()
	{
		var props = new FontProperties("Arial", 12f, false, false);

		props.SizeHalfPoints.Should().Be(24f);
	}

	[Fact]
	public void Default_UsesCalibri11pt()
	{
		FontProperties.Default.FamilyName.Should().Be("Calibri");
		FontProperties.Default.SizePoints.Should().Be(11f);
		FontProperties.Default.Bold.Should().BeFalse();
		FontProperties.Default.Italic.Should().BeFalse();
	}

	[Fact]
	public void Default_SizeTwips_Is220()
	{
		// 11 pt × 20 = 220 twips
		FontProperties.Default.SizeTwips.Should().Be(220f);
	}

	[Fact]
	public void Default_SizeHalfPoints_Is22()
	{
		FontProperties.Default.SizeHalfPoints.Should().Be(22f);
	}

	[Theory]
	[InlineData(24, 12f)]
	[InlineData(22, 11f)]
	[InlineData(48, 24f)]
	[InlineData(1, 0.5f)]
	[InlineData(0, 0f)]
	public void FromHalfPoints_ConvertsSizeCorrectly(int halfPoints, float expectedPoints)
	{
		var props = FontProperties.FromHalfPoints("Times New Roman", halfPoints);

		props.SizePoints.Should().Be(expectedPoints);
		props.FamilyName.Should().Be("Times New Roman");
	}

	[Fact]
	public void FromHalfPoints_DefaultBoldAndItalicAreFalse()
	{
		var props = FontProperties.FromHalfPoints("Arial", 24);

		props.Bold.Should().BeFalse();
		props.Italic.Should().BeFalse();
	}

	[Fact]
	public void FromHalfPoints_WithBoldItalic_SetsCorrectly()
	{
		var props = FontProperties.FromHalfPoints("Arial", 24, bold: true, italic: true);

		props.Bold.Should().BeTrue();
		props.Italic.Should().BeTrue();
	}

	[Fact]
	public void FromHalfPoints_WithOddValue_ProducesHalfPointSize()
	{
		// 15 half-points = 7.5 pt
		var props = FontProperties.FromHalfPoints("Arial", 15);

		props.SizePoints.Should().Be(7.5f);
	}

	[Fact]
	public void Equality_SameValues_AreEqual()
	{
		var a = new FontProperties("Arial", 12f, true, false);
		var b = new FontProperties("Arial", 12f, true, false);

		a.Should().Be(b);
		(a == b).Should().BeTrue();
	}

	[Fact]
	public void Equality_DifferentFamily_AreNotEqual()
	{
		var a = new FontProperties("Arial", 12f, false, false);
		var b = new FontProperties("Calibri", 12f, false, false);

		a.Should().NotBe(b);
	}

	[Fact]
	public void Equality_DifferentSize_AreNotEqual()
	{
		var a = new FontProperties("Arial", 12f, false, false);
		var b = new FontProperties("Arial", 14f, false, false);

		a.Should().NotBe(b);
	}

	[Fact]
	public void Equality_DifferentBold_AreNotEqual()
	{
		var a = new FontProperties("Arial", 12f, true, false);
		var b = new FontProperties("Arial", 12f, false, false);

		a.Should().NotBe(b);
	}

	[Fact]
	public void Equality_DifferentItalic_AreNotEqual()
	{
		var a = new FontProperties("Arial", 12f, false, true);
		var b = new FontProperties("Arial", 12f, false, false);

		a.Should().NotBe(b);
	}

	[Fact]
	public void TryResolveTypeface_WithNullResolver_ThrowsArgumentNullException()
	{
		var props = FontProperties.Default;

		var act = () => props.TryResolveTypeface(null!, out _);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void TryResolveTypeface_WithKnownFont_ResolvesTypeface()
	{
		var root = CreateTempDirectory();
		var fontPath = Path.Combine(root, "TestFont.ttf");
		File.WriteAllText(fontPath, "dummy");

		try
		{
			var stubTypeface = SKTypeface.Default;
			var resolver = new FontResolver(
				new RenderOptions { FontDirectories = [root] },
				new StubFontMetadataReader("TestFont"),
				(_, _, _) => stubTypeface);

			var props = new FontProperties("TestFont", 12f, false, false);
			var result = props.TryResolveTypeface(resolver, out var typeface);

			result.Should().BeTrue();
			typeface.Should().BeSameAs(stubTypeface);
		}
		finally
		{
			Directory.Delete(root, true);
		}
	}

	[Fact]
	public void TryResolveTypeface_WithUnknownFont_ReturnsFalse()
	{
		var resolver = new FontResolver(new RenderOptions());

		var props = new FontProperties("NonExistent", 12f, false, false);
		var result = props.TryResolveTypeface(resolver, out var typeface);

		result.Should().BeFalse();
		typeface.Should().BeNull();
	}

	[Fact]
	public void TryResolveTypeface_PassesBoldAndItalicToResolver()
	{
		var root = CreateTempDirectory();
		var fontPath = Path.Combine(root, "TestFont.ttf");
		File.WriteAllText(fontPath, "dummy");

		try
		{
			bool capturedBold = false;
			bool capturedItalic = false;
			var stubTypeface = SKTypeface.Default;

			var resolver = new FontResolver(
				new RenderOptions { FontDirectories = [root] },
				new StubFontMetadataReader("TestFont"),
				(_, bold, italic) =>
				{
					capturedBold = bold;
					capturedItalic = italic;
					return stubTypeface;
				});

			var props = new FontProperties("TestFont", 14f, Bold: true, Italic: true);
			props.TryResolveTypeface(resolver, out _);

			capturedBold.Should().BeTrue();
			capturedItalic.Should().BeTrue();
		}
		finally
		{
			Directory.Delete(root, true);
		}
	}

	[Fact]
	public void DefaultFamilyName_IsCalibri()
	{
		FontProperties.DefaultFamilyName.Should().Be("Calibri");
	}

	[Fact]
	public void DefaultSizePoints_Is11()
	{
		FontProperties.DefaultSizePoints.Should().Be(11f);
	}

	[Theory]
	[InlineData(0f, 0f)]
	[InlineData(1f, 20f)]
	[InlineData(72f, 1440f)]
	public void SizeTwips_VariousSizes(float points, float expectedTwips)
	{
		var props = new FontProperties("Arial", points, false, false);

		props.SizeTwips.Should().Be(expectedTwips);
	}

	private static string CreateTempDirectory()
	{
		var path = Path.Combine(Path.GetTempPath(), "FontPropertiesTest_" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(path);
		return path;
	}

	private sealed class StubFontMetadataReader(string familyName) : IFontMetadataReader
	{
		public IReadOnlyList<string> ReadFamilyNames(string filePath)
		{
			return [familyName];
		}
	}
}
