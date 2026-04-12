namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Diagnostics;
using Xunit;

/// <summary>
/// Verifies rendering performance targets from the non-functional requirements.
/// </summary>
public sealed class PerformanceTargetTests
{
	/// <summary>
	/// A simple 1-page document must render in under 500ms.
	/// </summary>
	[Fact]
	public void SimpleOnePage_RendersUnder500ms()
	{
		using var stream = TestDocxBuilder.CreateMinimalDocx();
		var renderer = new DocxRenderer(new RenderOptions());

		// Warm up
		stream.Position = 0;
		renderer.Render(stream);

		stream.Position = 0;
		var sw = Stopwatch.StartNew();
		var result = renderer.Render(stream);
		_ = result.Pages[0].ToSvg();
		_ = result.ToPdf();
		sw.Stop();

		sw.ElapsedMilliseconds.Should().BeLessThan(500);
	}

	/// <summary>
	/// A 50-paragraph document should render in under 10 seconds.
	/// </summary>
	[Fact]
	public void FiftyParagraphs_RendersUnder10s()
	{
		using var stream = CreateMultiParagraphDocx(50);
		var renderer = new DocxRenderer(new RenderOptions());

		var sw = Stopwatch.StartNew();
		var result = renderer.Render(stream);
		var svgPages = new List<string>();
		foreach (var page in result.Pages)
		{
			svgPages.Add(page.ToSvg());
		}

		var pdf = result.ToPdf();
		sw.Stop();

		svgPages.Should().NotBeEmpty();
		pdf.Should().NotBeEmpty();
		sw.Elapsed.TotalSeconds.Should().BeLessThan(10);
	}

	/// <summary>
	/// Rendering with default options should produce valid output within performance budget.
	/// </summary>
	[Fact]
	public void TableDocument_RendersWithinBudget()
	{
		using var stream = CreateDocxWithTable(5, 3);
		var renderer = new DocxRenderer(new RenderOptions());

		var sw = Stopwatch.StartNew();
		var result = renderer.Render(stream);
		_ = result.Pages[0].ToSvg();
		_ = result.ToPdf();
		sw.Stop();

		sw.ElapsedMilliseconds.Should().BeLessThan(2000);
	}

	private static MemoryStream CreateMultiParagraphDocx(int paragraphCount)
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			var body = new Body();
			for (var i = 0; i < paragraphCount; i++)
			{
				body.AppendChild(new Paragraph(
					new Run(new Text($"This is paragraph number {i + 1} with some sample text to measure rendering performance."))));
			}

			body.AppendChild(new SectionProperties(
				new PageSize { Width = 12240, Height = 15840 },
				new PageMargin { Top = 1440, Right = 1440, Bottom = 1440, Left = 1440 }));
			mainPart.Document = new Document(body);
		}

		stream.Position = 0;
		return stream;
	}

	private static MemoryStream CreateDocxWithTable(int rows, int cols)
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			var body = new Body();

			var table = new Table();
			for (var r = 0; r < rows; r++)
			{
				var row = new TableRow();
				for (var c = 0; c < cols; c++)
				{
					row.AppendChild(new TableCell(
						new Paragraph(new Run(new Text($"R{r + 1}C{c + 1}")))));
				}

				table.AppendChild(row);
			}

			body.AppendChild(table);
			mainPart.Document = new Document(body);
		}

		stream.Position = 0;
		return stream;
	}
}
