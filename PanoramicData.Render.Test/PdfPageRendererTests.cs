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
		CountPageObjects(pdf).Should().BeGreaterThanOrEqualTo(2);
	}

	private static int CountPageObjects(byte[] pdfBytes)
	{
		var text = Encoding.ASCII.GetString(pdfBytes);
		var matches = Regex.Matches(text, @"/Type\s*/Page(?!s)", RegexOptions.CultureInvariant);
		return matches.Count;
	}
}
