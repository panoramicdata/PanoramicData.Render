namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

/// <summary>
/// Tests for <see cref="DocxRenderer"/>, <see cref="RenderResult"/>, and <see cref="RenderedPage"/>.
/// </summary>
public sealed class DocxRendererTests
{
	[Fact]
	public void Constructor_NullOptions_ThrowsArgumentNullException()
	{
		var act = () => new DocxRenderer(null!);
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void Constructor_NullLogger_ThrowsArgumentNullException()
	{
		var act = () => new DocxRenderer(new RenderOptions(), null!);
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public async Task RenderAsync_NullStream_ThrowsArgumentNullException()
	{
		var renderer = new DocxRenderer(new RenderOptions());
		var act = () => renderer.RenderAsync(null!);
		await act.Should().ThrowAsync<ArgumentNullException>();
	}

	[Fact]
	public void Render_NullStream_ThrowsArgumentNullException()
	{
		var renderer = new DocxRenderer(new RenderOptions());
		var act = () => renderer.Render(null!);
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public async Task RenderAsync_MinimalDocx_ReturnsPages()
	{
		var renderer = new DocxRenderer(new RenderOptions());
		using var stream = TestDocxBuilder.CreateMinimalDocx();

		var result = await renderer.RenderAsync(stream, TestContext.Current.CancellationToken);

		result.Should().NotBeNull();
		result.Pages.Should().NotBeEmpty();
		result.Pages[0].PageNumber.Should().Be(1);
	}

	[Fact]
	public void Render_MinimalDocx_ReturnsPages()
	{
		var renderer = new DocxRenderer(new RenderOptions());
		using var stream = TestDocxBuilder.CreateMinimalDocx();

		var result = renderer.Render(stream);

		result.Should().NotBeNull();
		result.Pages.Should().NotBeEmpty();
	}

	[Fact]
	public async Task RenderAsync_MinimalDocx_PagesHaveDimensions()
	{
		var renderer = new DocxRenderer(new RenderOptions());
		using var stream = TestDocxBuilder.CreateMinimalDocx();

		var result = await renderer.RenderAsync(stream, TestContext.Current.CancellationToken);

		var page = result.Pages[0];
		page.WidthPoints.Should().BeGreaterThan(0);
		page.HeightPoints.Should().BeGreaterThan(0);
	}

	[Fact]
	public void RenderedPage_ToSvg_ProducesSvg()
	{
		var renderer = new DocxRenderer(new RenderOptions());
		using var stream = TestDocxBuilder.CreateMinimalDocx();

		var result = renderer.Render(stream);
		var svg = result.Pages[0].ToSvg();

		svg.Should().Contain("<svg");
		svg.Should().Contain("</svg>");
	}

	[Fact]
	public void RenderResult_ToPdf_ProducesPdfBytes()
	{
		var renderer = new DocxRenderer(new RenderOptions());
		using var stream = TestDocxBuilder.CreateMinimalDocx();

		var result = renderer.Render(stream);
		var pdfBytes = result.ToPdf();

		pdfBytes.Should().NotBeEmpty();
	}

	[Fact]
	public async Task RenderResult_ToPdfAsync_WritesToStream()
	{
		var renderer = new DocxRenderer(new RenderOptions());
		using var stream = TestDocxBuilder.CreateMinimalDocx();

		var result = await renderer.RenderAsync(stream, TestContext.Current.CancellationToken);

		using var output = new MemoryStream();
		await result.ToPdfAsync(output, cancellationToken: TestContext.Current.CancellationToken);

		output.Length.Should().BeGreaterThan(0);
	}

	[Fact]
	public async Task RenderResult_ToPdfAsync_NullStream_ThrowsArgumentNullException()
	{
		var renderer = new DocxRenderer(new RenderOptions());
		using var stream = TestDocxBuilder.CreateMinimalDocx();
		var result = await renderer.RenderAsync(stream, TestContext.Current.CancellationToken);

		var act = () => result.ToPdfAsync(null!, cancellationToken: TestContext.Current.CancellationToken);
		await act.Should().ThrowAsync<ArgumentNullException>();
	}

	[Fact]
	public void RenderedPage_DefaultPageDimensions_MatchUsLetter()
	{
		var renderer = new DocxRenderer(new RenderOptions());
		using var stream = TestDocxBuilder.CreateMinimalDocx();

		var result = renderer.Render(stream);
		var page = result.Pages[0];

		// US Letter: 8.5 × 11 inches = 612 × 792 points
		page.WidthPoints.Should().BeApproximately(612, 1);
		page.HeightPoints.Should().BeApproximately(792, 1);
	}

	[Fact]
	public void Render_MultiParagraphDocx_ProducesOutput()
	{
		using var docxStream = CreateDocxWithParagraphs(3);
		var renderer = new DocxRenderer(new RenderOptions());

		var result = renderer.Render(docxStream);

		result.Pages.Should().NotBeEmpty();
		var svg = result.Pages[0].ToSvg();
		svg.Should().Contain("<svg");
	}

	[Fact]
	public async Task RenderAsync_CancellationRequested_ThrowsOperationCanceledException()
	{
		var renderer = new DocxRenderer(new RenderOptions());
		using var stream = TestDocxBuilder.CreateMinimalDocx();
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		var act = () => renderer.RenderAsync(stream, cts.Token);
		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public void Render_FullDocx_ProducesOutput()
	{
		var renderer = new DocxRenderer(new RenderOptions());
		using var stream = TestDocxBuilder.CreateFullDocx();

		var result = renderer.Render(stream);

		result.Pages.Should().NotBeEmpty();
		result.Pages[0].ToSvg().Should().Contain("<svg");
		result.ToPdf().Should().NotBeEmpty();
	}

	[Fact]
	public void Render_WithPdfMetadata_ProducesPdf()
	{
		var renderer = new DocxRenderer(new RenderOptions());
		using var stream = TestDocxBuilder.CreateMinimalDocx();
		var result = renderer.Render(stream);

		var metadata = new PdfMetadata("Test Title", "Test Author", DateTime.UtcNow);
		var pdf = result.ToPdf(metadata);

		pdf.Should().NotBeEmpty();
	}

	[Fact]
	public void Render_IsThreadSafe()
	{
		var renderer = new DocxRenderer(new RenderOptions());
		var exceptions = new List<Exception>();

		Parallel.For(0, 10, _ =>
		{
			try
			{
				using var stream = TestDocxBuilder.CreateMinimalDocx();
				var result = renderer.Render(stream);
				result.Pages.Should().NotBeEmpty();
			}
			catch (Exception ex)
			{
				lock (exceptions) { exceptions.Add(ex); }
			}
		});

		exceptions.Should().BeEmpty();
	}

	[Fact]
	public void RenderedPage_LayoutPage_ExposesInternalPage()
	{
		var renderer = new DocxRenderer(new RenderOptions());
		using var stream = TestDocxBuilder.CreateMinimalDocx();

		var result = renderer.Render(stream);
		var page = result.Pages[0];

		page.LayoutPage.Should().NotBeNull();
		page.LayoutPage.PageNumber.Should().Be(1);
	}

	private static MemoryStream CreateDocxWithParagraphs(int count)
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			var body = new Body();
			for (var i = 0; i < count; i++)
			{
				body.AppendChild(new Paragraph(new Run(new Text($"Paragraph {i + 1}"))));
			}

			mainPart.Document = new Document(body);
		}

		stream.Position = 0;
		return stream;
	}
}
