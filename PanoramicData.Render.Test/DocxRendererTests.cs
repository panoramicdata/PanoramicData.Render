namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
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
		result.FieldUpdateResult.Should().BeNull();
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
	public void Render_WithFieldUpdateEnabled_ReturnsFieldUpdateResult()
	{
		var renderer = new DocxRenderer(new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});
		using var stream = FieldUpdateEngineTests.CreateDocxWithPageFieldParagraphs();

		var result = renderer.Render(stream);

		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.IterationsRequired.Should().BeGreaterThan(0);
		result.FieldUpdateResult.UpdatedFields.Should().Contain(["PAGE", "NUMPAGES"]);
	}

	[Fact]
	public void Render_WithParagraphStyleRunFormatting_AppliesStyleCascadeToSvgText()
	{
		var renderer = new DocxRenderer(new RenderOptions());
		using var stream = CreateDocxWithHeadingStyleFormatting();

		var result = renderer.Render(stream);
		var svg = result.Pages[0].ToSvg();

		svg.Should().Contain("Styled heading text");
		svg.Should().Contain("font-weight=\"bold\"");
		svg.Should().Contain("font-size=\"28pt\"");
		svg.Should().Contain("fill=\"#ED7D31\"");
	}

	[Fact]
	public void Render_WithTableOfContentsFieldUpdate_ConvergesInTwoIterations()
	{
		var renderer = new DocxRenderer(new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});
		using var stream = FieldUpdateEngineTests.CreateDocxWithTableOfContentsField();

		var result = renderer.Render(stream);

		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.IterationsRequired.Should().Be(2);
		result.FieldUpdateResult.UpdatedFields.Should().Contain("TOC");
	}

	[Fact]
	public void Render_WithTableOfContentsFieldUpdateAndMaxIterationsOne_LogsWarningAndReturnsLatestComputedValues()
	{
		var logger = new RecordingLogger();
		var renderer = new DocxRenderer(new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions
			{
				MaxIterations = 1
			}
		}, logger);
		using var stream = FieldUpdateEngineTests.CreateDocxWithTableOfContentsField();

		var result = renderer.Render(stream);
		var firstPageSvg = result.Pages[0].ToSvg();

		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.IterationsRequired.Should().Be(1);
		result.FieldUpdateResult.UpdatedFields.Should().Contain("TOC");
		firstPageSvg.Should().Contain("Chapter One");
		firstPageSvg.Should().NotContain("Old Entry");
		logger.WarningMessages.Should().ContainSingle(message => message.Contains("did not converge within", StringComparison.Ordinal));
	}

	[Fact]
	public void Render_WithTableOfContentsFieldUpdateAndExpandedToc_ConvergesWithinThreeIterations()
	{
		var renderer = new DocxRenderer(new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});
		using var stream = FieldUpdateEngineTests.CreateDocxWithTableOfContentsFieldRequiringThirdPassConvergence();

		var result = renderer.Render(stream);

		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.IterationsRequired.Should().Be(3);
		result.FieldUpdateResult.UpdatedFields.Should().Contain("TOC");
	}

	[Fact]
	public void Render_WithDocumentPropertyFieldUpdate_RendersUpdatedPropertyValues()
	{
		var renderer = new DocxRenderer(new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions(),
			SourceFilename = "uploaded.docx"
		});
		using var stream = FieldUpdateEngineTests.CreateDocxWithDocumentPropertyFieldParagraphs();

		var result = renderer.Render(stream);
		var svg = result.Pages[0].ToSvg();

		svg.Should().Contain("Quarterly Report");
		svg.Should().Contain("Alice Example");
		svg.Should().Contain("Master Services Agreement");
		svg.Should().Contain("finance; forecast");
		svg.Should().Contain("Uploaded by browser client");
		svg.Should().Contain("uploaded.docx");
		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.UpdatedFields.Should().Contain(["AUTHOR", "DESCRIPTION", "FILENAME", "KEYWORDS", "SUBJECT", "TITLE"]);
	}

	[Fact]
	public void Render_WithTableOfContentsFieldUpdate_RendersGeneratedEntriesOnFirstPage()
	{
		var renderer = new DocxRenderer(new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});
		using var stream = FieldUpdateEngineTests.CreateDocxWithTableOfContentsField();

		var result = renderer.Render(stream);
		var svg = result.Pages[0].ToSvg();

		svg.Should().Contain("Chapter One");
		svg.Should().Contain("Chapter Two");
		svg.Should().NotContain("Old Entry");
		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.UpdatedFields.Should().Contain("TOC");
	}

	[Fact]
	public void Render_WithCustomStyleTableOfContentsFieldUpdate_RendersMappedEntriesOnFirstPage()
	{
		var renderer = new DocxRenderer(new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});
		using var stream = FieldUpdateEngineTests.CreateDocxWithCustomStyleTableOfContentsField();

		var result = renderer.Render(stream);
		var svg = result.Pages[0].ToSvg();

		svg.Should().Contain("Appendix A");
		svg.Should().NotContain("Old Entry");
		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.UpdatedFields.Should().Contain("TOC");
	}

	[Fact]
	public void Render_WithHyperlinkedTableOfContentsFieldUpdate_EmitsBookmarkLinksInSvg()
	{
		var renderer = new DocxRenderer(new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});
		using var stream = FieldUpdateEngineTests.CreateDocxWithHyperlinkedTableOfContentsField();

		var result = renderer.Render(stream);
		var firstPageSvg = result.Pages[0].ToSvg();
		var secondPageSvg = result.Pages[1].ToSvg();

		firstPageSvg.Should().Contain("<a xlink:href=\"#_TocChapterOne\">");
		firstPageSvg.Should().Contain("<a xlink:href=\"#_TocChapterTwo\">");
		secondPageSvg.Should().Contain("id=\"_TocChapterOne\"");
		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.UpdatedFields.Should().Contain("TOC");
	}

	[Fact]
	public void Render_WithHyperlinkedTableOfContentsFieldUpdateAndNoHeadingBookmarks_EmitsSyntheticBookmarkLinksInSvg()
	{
		var renderer = new DocxRenderer(new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});
		using var stream = FieldUpdateEngineTests.CreateDocxWithHyperlinkedTableOfContentsFieldWithoutHeadingBookmarks();

		var result = renderer.Render(stream);
		var firstPageSvg = result.Pages[0].ToSvg();
		var secondPageSvg = result.Pages[1].ToSvg();
		var thirdPageSvg = result.Pages[2].ToSvg();

		firstPageSvg.Should().Contain("<a xlink:href=\"#_TocGenerated1\">");
		firstPageSvg.Should().Contain("<a xlink:href=\"#_TocGenerated2\">");
		secondPageSvg.Should().Contain("id=\"_TocGenerated1\"");
		thirdPageSvg.Should().Contain("id=\"_TocGenerated2\"");
		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.UpdatedFields.Should().Contain("TOC");
	}

	[Fact]
	public void Render_WithTableOfContentsFieldUpdateAndExplicitTabLeaderTemplate_EmitsLeaderDotsInSvg()
	{
		var renderer = new DocxRenderer(new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});
		using var stream = FieldUpdateEngineTests.CreateDocxWithTableOfContentsFieldAndExplicitTabLeaderTemplate();

		var result = renderer.Render(stream);
		var firstPageSvg = result.Pages[0].ToSvg();

		firstPageSvg.Should().Contain("Chapter One");
		firstPageSvg.Should().Contain(">2<");
		firstPageSvg.Should().Contain(">.<");
		firstPageSvg.Should().NotContain("Old Entry");
		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.UpdatedFields.Should().Contain("TOC");
	}

	[Fact]
	public void Render_WithTableOfContentsFieldUpdateAndTemplateRunFormatting_PreservesBoldAndItalicInSvg()
	{
		var renderer = new DocxRenderer(new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});
		using var stream = FieldUpdateEngineTests.CreateDocxWithTableOfContentsFieldAndTemplateRunFormatting();

		var result = renderer.Render(stream);
		var firstPageSvg = result.Pages[0].ToSvg();

		firstPageSvg.Should().Contain("Chapter One");
		firstPageSvg.Should().Contain("font-weight=\"bold\"");
		firstPageSvg.Should().Contain("font-style=\"italic\"");
		firstPageSvg.Should().Contain("font-size=\"14pt\"");
		firstPageSvg.Should().Contain("font-size=\"10pt\"");
		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.UpdatedFields.Should().Contain("TOC");
	}

	[Fact]
	public void Render_WithTableOfContentsFieldUpdateAndStyleDefinedRunFormatting_UsesTocParagraphStyleFormattingInSvg()
	{
		var renderer = new DocxRenderer(new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});
		using var stream = FieldUpdateEngineTests.CreateDocxWithTableOfContentsFieldAndStyleDefinedRunFormatting();

		var result = renderer.Render(stream);
		var firstPageSvg = result.Pages[0].ToSvg();

		firstPageSvg.Should().Contain("Chapter One");
		firstPageSvg.Should().Contain("font-weight=\"bold\"");
		firstPageSvg.Should().Contain("font-size=\"14pt\"");
		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.UpdatedFields.Should().Contain("TOC");
	}

	[Fact]
	public void Render_WithTableOfFiguresFieldUpdate_RendersGeneratedEntriesOnFirstPage()
	{
		var renderer = new DocxRenderer(new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});
		using var stream = FieldUpdateEngineTests.CreateDocxWithTableOfFiguresField();

		var result = renderer.Render(stream);
		var firstPageSvg = result.Pages[0].ToSvg();

		firstPageSvg.Should().Contain("Figure 1. Overview");
		firstPageSvg.Should().Contain(">2<");
		firstPageSvg.Should().Contain("Figure 2. Details");
		firstPageSvg.Should().Contain(">3<");
		firstPageSvg.Should().NotContain("Old Figure");
		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.UpdatedFields.Should().Contain("TOF");
	}

	[Fact]
	public void Render_WithTableOfFiguresFieldUpdateAndSeqFigureParagraphs_RendersGeneratedEntriesOnFirstPage()
	{
		var renderer = new DocxRenderer(new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});
		using var stream = FieldUpdateEngineTests.CreateDocxWithTableOfFiguresFieldAndSeqFigureParagraphs();

		var result = renderer.Render(stream);
		var firstPageSvg = result.Pages[0].ToSvg();

		firstPageSvg.Should().Contain("1. Overview");
		firstPageSvg.Should().Contain(">2<");
		firstPageSvg.Should().Contain("2. Details");
		firstPageSvg.Should().Contain(">3<");
		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.UpdatedFields.Should().Contain("TOF");
	}

	[Fact]
	public void Render_WithPageRefFieldUpdate_RendersResolvedPageNumberInSvg()
	{
		var renderer = new DocxRenderer(new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});
		using var stream = FieldUpdateEngineTests.CreateDocxWithPageRefField();

		var result = renderer.Render(stream);
		var firstPageSvg = result.Pages[0].ToSvg();

		firstPageSvg.Should().Contain(">2<");
		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.UpdatedFields.Should().Contain("PAGEREF");
	}

	[Fact]
	public void Render_WithRefFieldUpdate_RendersResolvedBookmarkTextInSvg()
	{
		var renderer = new DocxRenderer(new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});
		using var stream = FieldUpdateEngineTests.CreateDocxWithRefField();

		var result = renderer.Render(stream);
		var firstPageSvg = result.Pages[0].ToSvg();

		firstPageSvg.Should().Contain("Target Text");
		result.FieldUpdateResult.Should().NotBeNull();
		result.FieldUpdateResult!.UpdatedFields.Should().Contain("REF");
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

	private sealed class RecordingLogger : ILogger
	{
		public List<string> WarningMessages { get; } = [];

		public IDisposable? BeginScope<TState>(TState state)
			where TState : notnull
		{
			return null;
		}

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			if (logLevel == LogLevel.Warning)
			{
				WarningMessages.Add(formatter(state, exception));
			}
		}
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

	private static MemoryStream CreateDocxWithHeadingStyleFormatting()
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			var paragraph = new Paragraph(
				new ParagraphProperties(new ParagraphStyleId { Val = "Heading1" }),
				new Run(new Text("Styled heading text")));
			mainPart.Document = new Document(new Body(paragraph));

			var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
			stylesPart.Styles = new Styles(
				new Style(
					new StyleName { Val = "Heading 1" },
					new StyleRunProperties(
						new Bold(),
						new FontSize { Val = "56" },
						new Color { Val = "ED7D31" }))
				{
					Type = StyleValues.Paragraph,
					StyleId = "Heading1"
				});
		}

		stream.Position = 0;
		return stream;
	}
}
