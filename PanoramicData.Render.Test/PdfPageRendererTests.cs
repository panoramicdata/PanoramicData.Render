namespace PanoramicData.Render.Test;

using System.Text;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed class PdfPageRendererTests
{
	[Fact]
	public void RenderPages_EmptyInput_ReturnsEmptyPdfBytes()
	{
		var pdf = PdfPageRenderer.RenderPages([]);

		pdf.Should().BeEmpty();
	}

	[Fact]
	public void RenderPages_TwoPages_ProducesPdfWithTwoPageObjects()
	{
		var page1 = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks =
			[
				new LayoutBlock(new ParagraphBlock
				{
					SourceElement = new Paragraph(new Run(new Text("Page one")))
				}, 300f)
			]
		};
		var page2 = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 10000, PageHeight = 14000, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 2,
			ContentTopTwips = 1000,
			Blocks =
			[
				new LayoutBlock(new ParagraphBlock
				{
					SourceElement = new Paragraph(new Run(new Text("Page two")))
				}, 300f)
			]
		};

		var pdf = PdfPageRenderer.RenderPages([page1, page2]);

		pdf.Should().NotBeEmpty();
		CountPageObjects(pdf).Should().Be(2);
		ContainsMediaBox(pdf, 612, 792).Should().BeTrue();
		ContainsMediaBox(pdf, 500, 700).Should().BeTrue();
	}

	[Fact]
	public void RenderPages_WithMetadata_WritesTitleAndAuthorToPdf()
	{
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks =
			[
				new LayoutBlock(new ParagraphBlock
				{
					SourceElement = new Paragraph(new Run(new Text("Metadata page")))
				}, 300f)
			]
		};

		var metadata = new PdfMetadata("Sample Title", "Sample Author", new DateTime(2026, 4, 10, 12, 0, 0, DateTimeKind.Utc));

		var pdf = PdfPageRenderer.RenderPages([page], metadata: metadata);

		var text = Encoding.ASCII.GetString(pdf);
		text.Should().Contain("Sample Title");
		text.Should().Contain("Sample Author");
	}

	[Fact]
	public void RenderPages_WithPageRange_RendersOnlySelectedPageSubset()
	{
		LayoutPage CreatePage(int pageNumber, string text)
		{
			return new LayoutPage
			{
				Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
				PageNumber = pageNumber,
				ContentTopTwips = 1000,
				Blocks =
				[
					new LayoutBlock(new ParagraphBlock
					{
						SourceElement = new Paragraph(new Run(new Text(text)))
					}, 300f)
				]
			};
		}

		var page1 = CreatePage(1, "One");
		var page2 = CreatePage(2, "Two");
		var page3 = CreatePage(3, "Three");

		var pdf = PdfPageRenderer.RenderPages([page1, page2, page3], new RenderOptions { PageRange = 1..3 });

		CountPageObjects(pdf).Should().Be(2);
	}

	private static int CountPageObjects(byte[] pdfBytes)
	{
		var text = Encoding.ASCII.GetString(pdfBytes);
		var matches = Regex.Matches(text, @"/Type\s*/Page(?!s)", RegexOptions.CultureInvariant);
		return matches.Count;
	}

	private static bool ContainsMediaBox(byte[] pdfBytes, int widthPoints, int heightPoints)
	{
		var text = Encoding.ASCII.GetString(pdfBytes);
		var pattern = $@"/MediaBox\s*\[\s*0\s+0\s+{widthPoints}(?:\.0+)?\s+{heightPoints}(?:\.0+)?\s*\]";
		return Regex.IsMatch(text, pattern, RegexOptions.CultureInvariant);
	}
}
