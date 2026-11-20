namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using static PanoramicData.Render.Test.RendererScenarioBuilders;

public sealed class SvgPageRendererTests
{
	private static string RenderSingle(LayoutPage page)
	{
		var svgPages = SvgPageRenderer.RenderPages([page]);
		svgPages.Should().ContainSingle();
		return svgPages[0];
	}

	private static Paragraph BuildFieldParagraph()
		=> new(
			new Run(new Text("Page ")),
			new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
			new Run(new FieldCode(" PAGE ")),
			new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
			new Run(new Text("1")),
			new Run(new FieldChar { FieldCharType = FieldCharValues.End }),
			new Run(new Text(" of ")),
			new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
			new Run(new FieldCode(" NUMPAGES ")),
			new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
			new Run(new Text("1")),
			new Run(new FieldChar { FieldCharType = FieldCharValues.End }));

	[Fact]
	public void RenderPages_TwoPages_ReturnsTwoStandaloneSvgDocuments()
	{
		var page1 = TestLayoutPageFactory.CreateTextPage(1, "Page one");
		var page2 = TestLayoutPageFactory.CreateTextPage(2, "Page two", 10000, 14000);

		var svgPages = SvgPageRenderer.RenderPages([page1, page2]);

		svgPages.Should().HaveCount(2);
		svgPages[0].Should().Contain("viewBox=\"0 0 816 1056\"");
		svgPages[1].Should().Contain("viewBox=\"0 0 666.667 933.333\"");
		svgPages[0].Should().Contain("Page one");
		svgPages[1].Should().Contain("Page two");
	}

	[Fact]
	public void RenderPages_WithPageRange_RendersSelectedSubsetOnly()
	{
		var pages = new[] { "Page one", "Page two", "Page three" }
			.Select((text, index) => TestLayoutPageFactory.CreateTextPage(index + 1, text))
			.ToArray();

		var svgPages = SvgPageRenderer.RenderPages(pages, new RenderOptions { PageRange = 1..3 });

		svgPages.Should().HaveCount(2);
		svgPages[0].Should().Contain("Page two");
		svgPages[1].Should().Contain("Page three");
	}

	[Fact]
	public void RenderPages_WithPageAndNumPagesFields_RendersComputedValues()
	{
		var page1 = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock { SourceElement = BuildFieldParagraph() }, 300f)], 1);
		var page2 = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock { SourceElement = BuildFieldParagraph() }, 300f)], 2);

		var svgPages = SvgPageRenderer.RenderPages([page1, page2]);

		svgPages.Should().HaveCount(2);
		svgPages[0].Should().Contain("Page 1 of 2");
		svgPages[1].Should().Contain("Page 2 of 2");
	}

	[Fact]
	public void RenderPages_WithHyperlinkField_EmitsSvgAnchor()
	{
		var paragraph = new Paragraph(
			new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
			new Run(new FieldCode(" HYPERLINK \"https://example.com\" ")),
			new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
			new Run(new Text("Open link")),
			new Run(new FieldChar { FieldCharType = FieldCharValues.End }));

		var svg = RenderSingle(ParagraphPage(paragraph));

		svg.Should().Contain("Open link");
		svg.Should().Contain("<a xlink:href=\"https://example.com\">");
	}

	[Fact]
	public void RenderPages_WithHyperlinkElement_EmitsSvgAnchor()
	{
		var svg = RenderSingle(ParagraphPage(HyperlinkParagraph("Jump", "target")));

		svg.Should().Contain("Jump");
		svg.Should().Contain("<a xlink:href=\"#target\">");
	}

	[Fact]
	public void RenderPages_WithBookmarkStart_EmitsSvgAnchorId()
	{
		var svg = RenderSingle(BookmarkedPage("Bookmarked", "section1"));

		svg.Should().Contain("id=\"section1\"");
	}

	[Fact]
	public void RenderPages_HyperlinkAndBookmark_EmitsBothInSvg()
	{
		var svg = RenderSingle(BookmarkAndLinkPage("Target heading", "See heading", "heading1"));

		svg.Should().Contain("id=\"heading1\"");
		svg.Should().Contain("<a xlink:href=\"#heading1\">");
	}

	[Fact]
	public void RenderPages_TextWatermark_EmitsRotatedTextInSvg()
	{
		var svg = RenderSingle(BodyPageWithWatermark(TextWatermark("CONFIDENTIAL", "Arial", "silver", 0.4f)));

		svg.Should().Contain("CONFIDENTIAL");
		svg.Should().Contain("transform=\"rotate(315");
		svg.Should().Contain("fill-opacity=");
	}

	[Fact]
	public void RenderPages_ImageWatermark_EmitsImageInSvg()
	{
		var svg = RenderSingle(BodyPageWithWatermark(ImageWatermark([1, 2, 3])));

		svg.Should().Contain("<image");
		svg.Should().Contain("opacity=\"0.5\"");
		svg.Should().Contain("xlink:href=\"data:image/png;base64,");
	}

	[Fact]
	public void RenderPages_TabStopWithDotLeader_EmitsDotCharactersInSvg()
	{
		var svg = RenderSingle(ParagraphPage(DotLeaderTabParagraph("Chapter 1", "5")));

		svg.Should().Contain("Chapter 1");
		svg.Should().Contain(">5<");
		// Dot leaders should produce "." text elements
		svg.Should().Contain(">.<");
	}

	[Fact]
	public void RenderPages_RightTabInHeader_EmitsHeaderTextInSvg()
	{
		var svg = RenderSingle(RightTabHeaderPage("Document Title"));

		svg.Should().Contain("Document Title");
		svg.Should().Contain("Page 1");
		svg.Should().Contain("Body");
	}

	[Fact]
	public void RenderPages_BiDiParagraphWithRtlRun_EmitsTextInSvg()
	{
		var svg = RenderSingle(BiDiPage(BiDiParagraph("مرحبا")));

		svg.Should().Contain("مرحبا");
	}

	[Fact]
	public void RenderPages_MixedBiDiParagraph_EmitsBothLtrAndRtlTextInSvg()
	{
		var svg = RenderSingle(BiDiPage(MixedBiDiParagraph()));

		svg.Should().Contain("שלום");
		svg.Should().Contain("Hello");
	}

	[Fact]
	public void RenderPages_InlineSdtRun_RendersContentInSvg()
	{
		var svg = RenderSingle(ParagraphPage(InlineSdtParagraph()));

		svg.Should().Contain("SDT Content");
	}

	[Fact]
	public void RenderPages_SdtRunMixedWithNormalRun_RendersBothInSvg()
	{
		var svg = RenderSingle(ParagraphPage(SdtMixedWithNormalParagraph()));

		svg.Should().Contain("Normal ");
		svg.Should().Contain("Controlled");
	}
}
