namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using SkiaSharp;
using Xunit;

public sealed class SvgRasterizerTests
{
	[Fact]
	public void RasterizeToPng_SimpleSvg_ProducesValidPng()
	{
		const string svg = """<svg xmlns="http://www.w3.org/2000/svg" width="100" height="100"><rect width="100" height="100" fill="red"/></svg>""";

		var png = SvgRasterizer.RasterizeToPng(svg);

		png.Should().NotBeEmpty();
		// PNG magic bytes
		png[0].Should().Be(0x89);
		png[1].Should().Be(0x50); // 'P'
		png[2].Should().Be(0x4E); // 'N'
		png[3].Should().Be(0x47); // 'G'
	}

	[Fact]
	public void RasterizeToPng_At150Dpi_ProducesScaledImage()
	{
		const string svg = """<svg xmlns="http://www.w3.org/2000/svg" width="96" height="96"><rect width="96" height="96" fill="blue"/></svg>""";

		var png = SvgRasterizer.RasterizeToPng(svg, dpi: 150f);

		using var bitmap = SKBitmap.Decode(png);
		// 96px at 150 DPI / 96 DPI = 150px
		bitmap.Width.Should().Be(150);
		bitmap.Height.Should().Be(150);
	}

	[Fact]
	public void RasterizeToPng_At96Dpi_ProducesUnscaledImage()
	{
		const string svg = """<svg xmlns="http://www.w3.org/2000/svg" width="200" height="100"><rect width="200" height="100" fill="green"/></svg>""";

		var png = SvgRasterizer.RasterizeToPng(svg, dpi: 96f);

		using var bitmap = SKBitmap.Decode(png);
		bitmap.Width.Should().Be(200);
		bitmap.Height.Should().Be(100);
	}

	[Fact]
	public void RasterizeToBitmap_SimpleSvg_ReturnsBitmap()
	{
		const string svg = """<svg xmlns="http://www.w3.org/2000/svg" width="50" height="50"><circle cx="25" cy="25" r="25" fill="yellow"/></svg>""";

		using var bitmap = SvgRasterizer.RasterizeToBitmap(svg, dpi: 96f);

		bitmap.Width.Should().Be(50);
		bitmap.Height.Should().Be(50);
	}

	[Fact]
	public void RasterizeToPng_NullOrWhiteSpace_ThrowsArgumentException()
	{
		var actNull = () => SvgRasterizer.RasterizeToPng(null!);
		var actEmpty = () => SvgRasterizer.RasterizeToPng("");
		var actWhitespace = () => SvgRasterizer.RasterizeToPng("   ");

		actNull.Should().Throw<ArgumentException>();
		actEmpty.Should().Throw<ArgumentException>();
		actWhitespace.Should().Throw<ArgumentException>();
	}

	[Fact]
	public void RasterizeToPng_RenderedSvgPage_ProducesValidPng()
	{
		// Use the actual SVG renderer to produce SVG, then rasterize it
		var paragraph = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
			new DocumentFormat.OpenXml.Wordprocessing.Run(
				new DocumentFormat.OpenXml.Wordprocessing.Text("Hello World")));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};

		var svgPages = SvgPageRenderer.RenderPages([page]);
		svgPages.Should().ContainSingle();

		var png = SvgRasterizer.RasterizeToPng(svgPages[0]);

		png.Should().NotBeEmpty();
		png.Length.Should().BeGreaterThan(100);
	}
}
