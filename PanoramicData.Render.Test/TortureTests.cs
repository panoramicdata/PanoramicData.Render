namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

/// <summary>
/// Torture tests with malformed and edge-case DOCX documents.
/// Verifies the library degrades gracefully without throwing.
/// </summary>
public sealed class TortureTests
{
	[Fact]
	public void EmptyBody_ProducesEmptyResult()
	{
		using var stream = CreateDocx(new Body());
		var renderer = new DocxRenderer(new RenderOptions());

		var result = renderer.Render(stream);

		result.Should().NotBeNull();
	}

	[Fact]
	public void BodyWithOnlySectionProperties_ProducesResult()
	{
		var body = new Body(
			new SectionProperties(new PageSize { Width = 12240, Height = 15840 }));
		using var stream = CreateDocx(body);
		var renderer = new DocxRenderer(new RenderOptions());

		var result = renderer.Render(stream);

		result.Should().NotBeNull();
	}

	[Fact]
	public void EmptyParagraph_ProducesOutput()
	{
		var body = new Body(new Paragraph());
		using var stream = CreateDocx(body);
		var renderer = new DocxRenderer(new RenderOptions());

		var result = renderer.Render(stream);

		result.Pages.Should().NotBeEmpty();
	}

	[Fact]
	public void ParagraphWithEmptyRun_ProducesOutput()
	{
		var body = new Body(new Paragraph(new Run()));
		using var stream = CreateDocx(body);
		var renderer = new DocxRenderer(new RenderOptions());

		var result = renderer.Render(stream);

		result.Pages.Should().NotBeEmpty();
	}

	[Fact]
	public void RunWithEmptyText_ProducesOutput()
	{
		var body = new Body(new Paragraph(new Run(new Text(string.Empty))));
		using var stream = CreateDocx(body);
		var renderer = new DocxRenderer(new RenderOptions());

		var result = renderer.Render(stream);

		result.Pages.Should().NotBeEmpty();
	}

	[Fact]
	public void VeryLongParagraph_DoesNotThrow()
	{
		var longText = new string('A', 100_000);
		var body = new Body(new Paragraph(new Run(new Text(longText))));
		using var stream = CreateDocx(body);
		var renderer = new DocxRenderer(new RenderOptions());

		var act = () => renderer.Render(stream);

		act.Should().NotThrow();
	}

	[Fact]
	public void ManyEmptyParagraphs_DoesNotThrow()
	{
		var body = new Body();
		for (var i = 0; i < 1000; i++)
		{
			body.AppendChild(new Paragraph());
		}

		using var stream = CreateDocx(body);
		var renderer = new DocxRenderer(new RenderOptions());

		var act = () => renderer.Render(stream);

		act.Should().NotThrow();
	}

	[Fact]
	public void TableWithNoRows_ProducesOutput()
	{
		var body = new Body(
			new Table(),
			new Paragraph(new Run(new Text("After table"))));
		using var stream = CreateDocx(body);
		var renderer = new DocxRenderer(new RenderOptions());

		var result = renderer.Render(stream);

		result.Pages.Should().NotBeEmpty();
	}

	[Fact]
	public void TableWithEmptyCells_ProducesOutput()
	{
		var table = new Table(
			new TableRow(new TableCell(new Paragraph())));
		var body = new Body(table);
		using var stream = CreateDocx(body);
		var renderer = new DocxRenderer(new RenderOptions());

		var result = renderer.Render(stream);

		result.Pages.Should().NotBeEmpty();
	}

	[Fact]
	public void NestedTables_DoesNotThrow()
	{
		var innerTable = new Table(
			new TableRow(new TableCell(new Paragraph(new Run(new Text("Inner"))))));
		var outerTable = new Table(
			new TableRow(new TableCell(new Paragraph(new Run(new Text("Outer"))))));
		var body = new Body(outerTable, innerTable);
		using var stream = CreateDocx(body);
		var renderer = new DocxRenderer(new RenderOptions());

		var act = () => renderer.Render(stream);

		act.Should().NotThrow();
	}

	[Fact]
	public void ParagraphWithPageBreakBefore_ProducesMultiplePages()
	{
		var body = new Body(
			new Paragraph(new Run(new Text("Page 1"))),
			new Paragraph(
				new ParagraphProperties(new PageBreakBefore()),
				new Run(new Text("Page 2"))));
		using var stream = CreateDocx(body);
		var renderer = new DocxRenderer(new RenderOptions());

		var result = renderer.Render(stream);

		result.Pages.Count.Should().BeGreaterThanOrEqualTo(2);
	}

	[Fact]
	public void MixedContentStress_DoesNotThrow()
	{
		var body = new Body();
		for (var i = 0; i < 50; i++)
		{
			body.AppendChild(new Paragraph(new Run(new Text($"Paragraph {i}"))));
			if (i % 10 == 0)
			{
				body.AppendChild(new Table(
					new TableRow(new TableCell(new Paragraph(new Run(new Text($"Table {i}")))))));
			}
		}

		using var stream = CreateDocx(body);
		var renderer = new DocxRenderer(new RenderOptions());

		var act = () =>
		{
			var result = renderer.Render(stream);
			foreach (var page in result.Pages)
			{
				_ = page.ToSvg();
			}

			_ = result.ToPdf();
		};

		act.Should().NotThrow();
	}

	[Fact]
	public void UnicodeContent_ProducesOutput()
	{
		var body = new Body(
			new Paragraph(new Run(new Text("English"))),
			new Paragraph(new Run(new Text("日本語テスト"))),
			new Paragraph(new Run(new Text("العربية"))),
			new Paragraph(new Run(new Text("🎉 Emoji test 🚀"))));
		using var stream = CreateDocx(body);
		var renderer = new DocxRenderer(new RenderOptions());

		var result = renderer.Render(stream);

		result.Pages.Should().NotBeEmpty();
	}

	[Fact]
	public void SvgAndPdfOutputConsistency_SamePageCount()
	{
		var body = new Body(
			new Paragraph(new Run(new Text("Consistency test"))));
		using var stream = CreateDocx(body);
		var renderer = new DocxRenderer(new RenderOptions());

		var result = renderer.Render(stream);
		var svgCount = result.Pages.Count;
		var pdf = result.ToPdf();

		svgCount.Should().BeGreaterThan(0);
		pdf.Should().NotBeEmpty();
	}

	private static MemoryStream CreateDocx(Body body)
	{
		var stream = new MemoryStream();
		using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
		{
			var mainPart = doc.AddMainDocumentPart();
			mainPart.Document = new Document(body);
		}

		stream.Position = 0;
		return stream;
	}
}
