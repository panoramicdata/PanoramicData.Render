namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

/// <summary>
/// Verifies thread safety of the rendering pipeline under concurrent use.
/// </summary>
public sealed class ThreadSafetyTests
{
	[Fact]
	public void SvgRenderPages_ConcurrentRenders_AllSucceed()
	{
		const int concurrency = 20;
		var pages = CreateTestPages();
		var exceptions = new List<Exception>();

		Parallel.For(0, concurrency, _ =>
		{
			try
			{
				var result = SvgPageRenderer.RenderPages(pages);
				result.Should().ContainSingle();
				result[0].Should().Contain("<svg");
			}
			catch (Exception ex)
			{
				lock (exceptions) { exceptions.Add(ex); }
			}
		});

		exceptions.Should().BeEmpty();
	}

	[Fact]
	public void PdfRenderPages_ConcurrentRenders_AllSucceed()
	{
		const int concurrency = 20;
		var pages = CreateTestPages();
		var exceptions = new List<Exception>();

		Parallel.For(0, concurrency, _ =>
		{
			try
			{
				var result = PdfPageRenderer.RenderPages(pages);
				result.Should().NotBeEmpty();
			}
			catch (Exception ex)
			{
				lock (exceptions) { exceptions.Add(ex); }
			}
		});

		exceptions.Should().BeEmpty();
	}

	[Fact]
	public void FontResolver_ConcurrentTypefaceResolution_NoExceptions()
	{
		const int concurrency = 50;
		var resolver = new FontResolver([]);
		var exceptions = new List<Exception>();

		Parallel.For(0, concurrency, _ =>
		{
			try
			{
				resolver.TryGetTypeface("Arial", false, false, out var tf1);
				resolver.TryGetTypeface("Times New Roman", true, false, out var tf2);
				resolver.TryGetTypeface("Courier New", false, true, out var tf3);
			}
			catch (Exception ex)
			{
				lock (exceptions) { exceptions.Add(ex); }
			}
		});

		exceptions.Should().BeEmpty();
	}

	[Fact]
	public void MeasurementEngine_ConcurrentMeasurements_AllSucceed()
	{
		const int concurrency = 20;
		var engine = new MeasurementEngine();
		var exceptions = new List<Exception>();

		Parallel.For(0, concurrency, i =>
		{
			try
			{
				using var typeface = SkiaSharp.SKTypeface.Default;
				var result = engine.MeasureGlyphAdvances(typeface, 12f, $"Test text {i}");
				result.Should().NotBeEmpty();
			}
			catch (Exception ex)
			{
				lock (exceptions) { exceptions.Add(ex); }
			}
		});

		exceptions.Should().BeEmpty();
	}

	[Fact]
	public void ConcurrentRenders_DifferentDocuments_AllSucceed()
	{
		const int concurrency = 100;
		var exceptions = new List<Exception>();

		Parallel.For(0, concurrency, i =>
		{
			try
			{
				var paragraph = new Paragraph(new Run(new Text($"Document {i}")));
				var page = new LayoutPage
				{
					Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
					PageNumber = 1,
					ContentTopTwips = 1000,
					Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
				};

				var svgResult = SvgPageRenderer.RenderPages([page]);
				svgResult.Should().ContainSingle();

				var pdfResult = PdfPageRenderer.RenderPages([page]);
				pdfResult.Should().NotBeEmpty();
			}
			catch (Exception ex)
			{
				lock (exceptions) { exceptions.Add(ex); }
			}
		});

		exceptions.Should().BeEmpty();
	}

	private static LayoutPage[] CreateTestPages()
	{
		var paragraph = new Paragraph(new Run(new Text("Thread safety test")));
		return
		[
			new LayoutPage
			{
				Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
				PageNumber = 1,
				ContentTopTwips = 1000,
				Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
			}
		];
	}
}
