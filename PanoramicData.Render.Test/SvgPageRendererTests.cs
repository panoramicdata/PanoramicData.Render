namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed class SvgPageRendererTests
{
	[Fact]
	public void RenderPages_TwoPages_ReturnsTwoStandaloneSvgDocuments()
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

		var svgPages = SvgPageRenderer.RenderPages([page1, page2]);

		svgPages.Should().HaveCount(2);
		svgPages[0].Should().Contain("viewBox=\"0 0 12240 15840\"");
		svgPages[1].Should().Contain("viewBox=\"0 0 10000 14000\"");
		svgPages[0].Should().Contain("Page one");
		svgPages[1].Should().Contain("Page two");
	}

	[Fact]
	public void RenderPages_WithPageRange_RendersSelectedSubsetOnly()
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

		var page1 = CreatePage(1, "Page one");
		var page2 = CreatePage(2, "Page two");
		var page3 = CreatePage(3, "Page three");

		var svgPages = SvgPageRenderer.RenderPages([page1, page2, page3], new RenderOptions { PageRange = 1..3 });

		svgPages.Should().HaveCount(2);
		svgPages[0].Should().Contain("Page two");
		svgPages[1].Should().Contain("Page three");
	}
}
