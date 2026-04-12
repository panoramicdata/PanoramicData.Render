namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using SkiaSharp;
using Xunit;

public sealed class PerceptualImageDiffTests
{
	[Fact]
	public void ComputeSsim_IdenticalImages_Returns1()
	{
		using var bitmap = CreateSolidBitmap(100, 100, SKColors.Red);
		using var clone = bitmap.Copy();

		var ssim = PerceptualImageDiff.ComputeSsim(bitmap, clone);

		ssim.Should().Be(1f);
	}

	[Fact]
	public void ComputeSsim_CompletelyDifferentImages_ReturnsLowValue()
	{
		using var black = CreateSolidBitmap(64, 64, SKColors.Black);
		using var white = CreateSolidBitmap(64, 64, SKColors.White);

		var ssim = PerceptualImageDiff.ComputeSsim(black, white);

		ssim.Should().BeLessThan(0.01f);
	}

	[Fact]
	public void ComputeSsim_SimilarImages_ReturnsHighValue()
	{
		using var original = CreateSolidBitmap(64, 64, new SKColor(128, 128, 128));
		using var slightlyDifferent = CreateSolidBitmap(64, 64, new SKColor(130, 130, 130));

		var ssim = PerceptualImageDiff.ComputeSsim(original, slightlyDifferent);

		ssim.Should().BeGreaterThan(0.95f);
	}

	[Fact]
	public void ComputeSsim_DifferentDimensions_ThrowsArgumentException()
	{
		using var small = CreateSolidBitmap(50, 50, SKColors.Red);
		using var large = CreateSolidBitmap(100, 100, SKColors.Red);

		var act = () => PerceptualImageDiff.ComputeSsim(small, large);

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	public void ComputeSsim_NullExpected_ThrowsArgumentNullException()
	{
		using var bitmap = CreateSolidBitmap(10, 10, SKColors.Red);

		var act = () => PerceptualImageDiff.ComputeSsim(null!, bitmap);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void ComputeSsim_FromPngBytes_Works()
	{
		using var bitmap = CreateSolidBitmap(64, 64, SKColors.Blue);
		var png = EncodeToPng(bitmap);

		var ssim = PerceptualImageDiff.ComputeSsim(png, png);

		ssim.Should().Be(1f);
	}

	[Fact]
	public void CreateDiffImage_IdenticalImages_ProducesWhiteImage()
	{
		using var bitmap = CreateSolidBitmap(32, 32, SKColors.Green);
		using var clone = bitmap.Copy();

		var diffPng = PerceptualImageDiff.CreateDiffImage(bitmap, clone);

		diffPng.Should().NotBeEmpty();
		using var diffBitmap = SKBitmap.Decode(diffPng);
		// All pixels should be white (no differences)
		var centerPixel = diffBitmap.GetPixel(16, 16);
		centerPixel.Red.Should().Be(255);
		centerPixel.Green.Should().Be(255);
		centerPixel.Blue.Should().Be(255);
	}

	[Fact]
	public void CreateDiffImage_DifferentImages_ProducesDiffPixels()
	{
		using var black = CreateSolidBitmap(32, 32, SKColors.Black);
		using var white = CreateSolidBitmap(32, 32, SKColors.White);

		var diffPng = PerceptualImageDiff.CreateDiffImage(black, white);

		using var diffBitmap = SKBitmap.Decode(diffPng);
		var pixel = diffBitmap.GetPixel(16, 16);
		// Should be red (difference detected)
		pixel.Red.Should().Be(255);
	}

	[Fact]
	public void ComputeSsim_RenderedSvgAgainstItself_Returns1()
	{
		var paragraph = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
			new DocumentFormat.OpenXml.Wordprocessing.Run(
				new DocumentFormat.OpenXml.Wordprocessing.Text("Test")));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};

		var svgPages = SvgPageRenderer.RenderPages([page]);
		var png1 = SvgRasterizer.RasterizeToPng(svgPages[0]);
		var png2 = SvgRasterizer.RasterizeToPng(svgPages[0]);

		var ssim = PerceptualImageDiff.ComputeSsim(png1, png2);

		ssim.Should().Be(1f);
	}

	private static SKBitmap CreateSolidBitmap(int width, int height, SKColor color)
	{
		var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
		using var canvas = new SKCanvas(bitmap);
		canvas.Clear(color);
		return bitmap;
	}

	private static byte[] EncodeToPng(SKBitmap bitmap)
	{
		using var image = SKImage.FromBitmap(bitmap);
		using var data = image.Encode(SKEncodedImageFormat.Png, 100);
		return data.ToArray();
	}
}
