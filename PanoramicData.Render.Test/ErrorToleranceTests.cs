namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Globalization;
using System.Text.RegularExpressions;
using SkiaSharp;
using Xunit;

/// <summary>
/// Verifies graceful error handling across the rendering pipeline.
/// </summary>
public sealed class ErrorToleranceTests
{
	// ---- Null argument handling ----

	[Fact]
	public void SvgRenderPages_NullPages_ThrowsArgumentNullException()
	{
		var act = () => SvgPageRenderer.RenderPages(null!);
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void PdfRenderPages_NullPages_ThrowsArgumentNullException()
	{
		var act = () => PdfPageRenderer.RenderPages(null!);
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void EmitDocument_NullPages_ThrowsArgumentNullException()
	{
		using var target = new PdfRenderTarget(12240, 15840);
		var act = () => RenderCommandEmitter.EmitDocument(null!, target);
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void EmitDocument_NullTarget_ThrowsArgumentNullException()
	{
		var act = () => RenderCommandEmitter.EmitDocument([], null!);
		act.Should().Throw<ArgumentNullException>();
	}

	// ---- Empty input handling ----

	[Fact]
	public void SvgRenderPages_EmptyList_ReturnsEmptyList()
	{
		var result = SvgPageRenderer.RenderPages([]);
		result.Should().BeEmpty();
	}

	[Fact]
	public void PdfRenderPages_EmptyList_ReturnsEmptyBytes()
	{
		var result = PdfPageRenderer.RenderPages([]);
		result.Should().BeEmpty();
	}

	[Fact]
	public void DocumentBlockParser_EmptyBody_ReturnsEmptyList()
	{
		var body = new Body();
		var blocks = DocumentBlockParser.Parse(body);
		blocks.Should().BeEmpty();
	}

	[Fact]
	public void DocumentBlockParser_NullBody_ThrowsArgumentNullException()
	{
		var act = () => DocumentBlockParser.Parse(null!);
		act.Should().Throw<ArgumentNullException>();
	}

	// ---- Paragraph with no content ----

	[Fact]
	public void SvgRenderPages_EmptyParagraph_DoesNotThrow()
	{
		var paragraph = new Paragraph();
		var page = CreatePage(paragraph);

		var act = () => SvgPageRenderer.RenderPages([page]);
		act.Should().NotThrow();
	}

	[Fact]
	public void PdfRenderPages_EmptyParagraph_DoesNotThrow()
	{
		var paragraph = new Paragraph();
		var page = CreatePage(paragraph);

		var act = () => PdfPageRenderer.RenderPages([page]);
		act.Should().NotThrow();
	}

	[Fact]
	public void SvgRenderPages_MixedPageSizes_UsesPerPageAspectRatio()
	{
		var pages = new[]
		{
			CreatePage(new Paragraph(new Run(new Text("Portrait"))), pageNumber: 1, pageWidthTwips: 12240, pageHeightTwips: 15840),
			CreatePage(new Paragraph(new Run(new Text("Landscape"))), pageNumber: 2, pageWidthTwips: 15840, pageHeightTwips: 12240)
		};

		var svgs = SvgPageRenderer.RenderPages(pages);

		svgs.Should().HaveCount(2);
		var first = ExtractViewBoxSize(svgs[0]);
		var second = ExtractViewBoxSize(svgs[1]);

		first.Width.Should().BeApproximately(816f, 0.01f);
		first.Height.Should().BeApproximately(1056f, 0.01f);
		second.Width.Should().BeApproximately(1056f, 0.01f);
		second.Height.Should().BeApproximately(816f, 0.01f);
	}

	[Fact]
	public void SvgRenderPages_InvalidPaginationDimensions_FallsBackAndContinues()
	{
		var pages = new[]
		{
			CreatePage(new Paragraph(new Run(new Text("Broken page"))), pageNumber: 1, pageWidthTwips: 0, pageHeightTwips: -1),
			CreatePage(new Paragraph(new Run(new Text("Healthy page"))), pageNumber: 2, pageWidthTwips: 12240, pageHeightTwips: 15840)
		};

		var act = () => SvgPageRenderer.RenderPages(pages);
		act.Should().NotThrow();

		var svgs = SvgPageRenderer.RenderPages(pages);
		svgs.Should().HaveCount(2);
		svgs[1].Should().Contain("Healthy page");

		var fallback = ExtractViewBoxSize(svgs[0]);
		fallback.Width.Should().BeApproximately(816f, 0.01f);
		fallback.Height.Should().BeApproximately(1056f, 0.01f);
	}

	// ---- Corrupt image data ----

	[Fact]
	public void PdfDrawImage_CorruptData_DoesNotThrow()
	{
		using var target = new PdfRenderTarget(12240, 15840);
		target.BeginPage(12240, 15840);

		var corruptImage = new ImageData([0xFF, 0x00, 0xAB], "image/png");
		var rect = new RenderRect(0, 0, 1440, 1440);

		var act = () => target.DrawImage(corruptImage, rect);
		act.Should().NotThrow(); // Should silently skip
	}

	// ---- Font resolution failures ----

	[Fact]
	public void FontResolver_UnknownFamily_ReturnsFalse()
	{
		var resolver = new FontResolver([]);
		var result = resolver.TryGetTypeface("NonExistentFontFamily-12345", false, false, out var typeface);

		result.Should().BeFalse();
		typeface.Should().BeNull();
	}

	[Fact]
	public void FontResolver_EmptyDirectories_DoesNotThrow()
	{
		var act = () => new FontResolver([]);
		act.Should().NotThrow();
	}

	// ---- KnuthPlass with edge cases ----

	[Fact]
	public void KnuthPlass_EmptyItems_ReturnsEmpty()
	{
		var result = KnuthPlassAlgorithm.FindBreaks([], 5000f);
		result.Should().BeEmpty();
	}

	[Fact]
	public void KnuthPlass_NegativeLineWidth_ThrowsArgumentOutOfRange()
	{
		var act = () => KnuthPlassAlgorithm.FindBreaks([], -1f);
		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	// ---- MeasurementEngine with edge cases ----

	[Fact]
	public void MeasurementEngine_EmptyText_ReturnsEmptyGlyphs()
	{
		var engine = new MeasurementEngine();
		using var typeface = SKTypeface.Default;

		var result = engine.MeasureGlyphAdvances(typeface, 12f, string.Empty);
		result.Should().BeEmpty();
	}

	[Fact]
	public void MeasurementEngine_ZeroFontSize_ThrowsArgumentOutOfRange()
	{
		var engine = new MeasurementEngine();
		using var typeface = SKTypeface.Default;

		var act = () => engine.MeasureGlyphAdvances(typeface, 0f, "test");
		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	// ---- Render target with invalid dimensions ----

	[Fact]
	public void PdfRenderTarget_ZeroWidth_ThrowsArgumentOutOfRange()
	{
		var act = () => new PdfRenderTarget(0, 15840);
		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void SvgRenderTarget_ZeroHeight_ThrowsArgumentOutOfRange()
	{
		var act = () => new SvgRenderTarget(12240, 0, new RenderOptions());
		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	// ---- TableParser with malformed table ----

	[Fact]
	public void TableParser_EmptyTable_DoesNotThrow()
	{
		var table = new Table();
		var act = () => TableParser.Parse(table);
		act.Should().NotThrow();
	}

	[Fact]
	public void TableParser_RowWithNoCells_DoesNotThrow()
	{
		var table = new Table(new TableRow());
		var result = TableParser.Parse(table);
		result.Rows.Should().ContainSingle();
		result.Rows[0].Cells.Should().BeEmpty();
	}

	// ---- PageBuilder with edge cases ----

	[Fact]
	public void PageBuilder_EmptyBlocks_ReturnsEmptyPages()
	{
		var section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginTop = 1440, MarginBottom = 1440 };
		var pages = PageBuilder.Paginate([], section);
		pages.Should().BeEmpty();
	}

	// ---- Mixed: full pipeline with minimal valid input ----

	[Fact]
	public void FullPipeline_WhitespaceOnlyParagraph_ProducesOutput()
	{
		var paragraph = new Paragraph(new Run(new Text(" ") { Space = SpaceProcessingModeValues.Preserve }));
		var page = CreatePage(paragraph);

		var svgResult = SvgPageRenderer.RenderPages([page]);
		svgResult.Should().ContainSingle();
		svgResult[0].Should().Contain("<svg");

		var pdfResult = PdfPageRenderer.RenderPages([page]);
		pdfResult.Should().NotBeEmpty();
	}

	private static LayoutPage CreatePage(Paragraph paragraph, int pageNumber = 1, int pageWidthTwips = 12240, int pageHeightTwips = 15840) => new()
	{
		Section = new SectionInfo { PageWidth = pageWidthTwips, PageHeight = pageHeightTwips, MarginLeft = 720, MarginRight = 720 },
		PageNumber = pageNumber,
		ContentTopTwips = 1000,
		Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
	};

	private static (float Width, float Height) ExtractViewBoxSize(string svg)
	{
		var match = Regex.Match(svg, "viewBox=\"0 0 ([0-9.]+) ([0-9.]+)\"");
		match.Success.Should().BeTrue("rendered SVG should contain a viewBox with page size");

		var width = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
		var height = float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
		return (width, height);
	}
}
