namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
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
		svgPages[0].Should().Contain("viewBox=\"0 0 816 1056\"");
		svgPages[1].Should().Contain("viewBox=\"0 0 666.667 933.333\"");
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

	[Fact]
	public void RenderPages_WithPageAndNumPagesFields_RendersComputedValues()
	{
		Paragraph BuildFieldParagraph()
		{
			return new Paragraph(
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
		}

		var page1 = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = BuildFieldParagraph() }, 300f)]
		};
		var page2 = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 2,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = BuildFieldParagraph() }, 300f)]
		};

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
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};

		var svgPages = SvgPageRenderer.RenderPages([page]);

		svgPages.Should().ContainSingle();
		svgPages[0].Should().Contain("Open link");
		svgPages[0].Should().Contain("<a xlink:href=\"https://example.com\">");
	}

	[Fact]
	public void RenderPages_WithHyperlinkElement_EmitsSvgAnchor()
	{
		var hyperlink = new Hyperlink(new Run(new Text("Jump")))
		{
			Anchor = "target"
		};
		var paragraph = new Paragraph(hyperlink);
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};

		var svgPages = SvgPageRenderer.RenderPages([page]);

		svgPages.Should().ContainSingle();
		svgPages[0].Should().Contain("Jump");
		svgPages[0].Should().Contain("<a xlink:href=\"#target\">");
	}

	[Fact]
	public void RenderPages_WithBookmarkStart_EmitsSvgAnchorId()
	{
		var paragraph = new Paragraph(new Run(new Text("Bookmarked")));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock
			{
				SourceElement = paragraph,
				BookmarkStarts = [new BookmarkStartInfo(1, "section1")]
			}, 300f)]
		};

		var svgPages = SvgPageRenderer.RenderPages([page]);

		svgPages.Should().ContainSingle();
		svgPages[0].Should().Contain("id=\"section1\"");
	}

	[Fact]
	public void RenderPages_HyperlinkAndBookmark_EmitsBothInSvg()
	{
		var bookmarkedParagraph = new Paragraph(new Run(new Text("Target heading")));
		var linkParagraph = new Paragraph(new Hyperlink(new Run(new Text("See heading"))) { Anchor = "heading1" });
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks =
			[
				new LayoutBlock(new ParagraphBlock
				{
					SourceElement = bookmarkedParagraph,
					BookmarkStarts = [new BookmarkStartInfo(1, "heading1")]
				}, 300f),
				new LayoutBlock(new ParagraphBlock { SourceElement = linkParagraph }, 300f)
			]
		};

		var svgPages = SvgPageRenderer.RenderPages([page]);

		svgPages.Should().ContainSingle();
		svgPages[0].Should().Contain("id=\"heading1\"");
		svgPages[0].Should().Contain("<a xlink:href=\"#heading1\">");
	}

	[Fact]
	public void RenderPages_TextWatermark_EmitsRotatedTextInSvg()
	{
		var paragraph = new Paragraph(new Run(new Text("Body")));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)],
			Watermark = new WatermarkInfo
			{
				Kind = WatermarkKind.Text,
				Text = "CONFIDENTIAL",
				FontFamily = "Arial",
				FillColor = "silver",
				Opacity = 0.4f,
				RotationDegrees = 315f,
				WidthTwips = 8000f,
				HeightTwips = 2000f
			}
		};

		var svgPages = SvgPageRenderer.RenderPages([page]);

		svgPages.Should().ContainSingle();
		svgPages[0].Should().Contain("CONFIDENTIAL");
		svgPages[0].Should().Contain("transform=\"rotate(315");
		svgPages[0].Should().Contain("fill-opacity=");
	}

	[Fact]
	public void RenderPages_ImageWatermark_EmitsImageInSvg()
	{
		var paragraph = new Paragraph(new Run(new Text("Body")));
		var imageData = new ImageData([1, 2, 3], "image/png");
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)],
			Watermark = new WatermarkInfo
			{
				Kind = WatermarkKind.Image,
				ResolvedImageData = imageData,
				Opacity = 0.5f,
				WidthTwips = 7200f,
				HeightTwips = 5400f
			}
		};

		var svgPages = SvgPageRenderer.RenderPages([page]);

		svgPages.Should().ContainSingle();
		svgPages[0].Should().Contain("<image");
		svgPages[0].Should().Contain("opacity=\"0.5\"");
		svgPages[0].Should().Contain("xlink:href=\"data:image/png;base64,");
	}

	[Fact]
	public void RenderPages_TabStopWithDotLeader_EmitsDotCharactersInSvg()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(
				new Tabs(
					new DocumentFormat.OpenXml.Wordprocessing.TabStop
					{
						Val = TabStopValues.Right,
						Position = 9360,
						Leader = TabStopLeaderCharValues.Dot
					}
				)),
			new Run(new Text("Chapter 1") { Space = SpaceProcessingModeValues.Preserve }),
			new Run(new TabChar()),
			new Run(new Text("5") { Space = SpaceProcessingModeValues.Preserve }));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};

		var svgPages = SvgPageRenderer.RenderPages([page]);

		svgPages.Should().ContainSingle();
		svgPages[0].Should().Contain("Chapter 1");
		svgPages[0].Should().Contain(">5<");
		// Dot leaders should produce "." text elements
		svgPages[0].Should().Contain(">.<");
	}

	[Fact]
	public void RenderPages_RightTabInHeader_EmitsHeaderTextInSvg()
	{
		var headerParagraph = new Paragraph(
			new ParagraphProperties(
				new Tabs(
					new DocumentFormat.OpenXml.Wordprocessing.TabStop
					{
						Val = TabStopValues.Right,
						Position = 9360
					}
				)),
			new Run(new Text("Document Title") { Space = SpaceProcessingModeValues.Preserve }),
			new Run(new TabChar()),
			new Run(new Text("Page 1") { Space = SpaceProcessingModeValues.Preserve }));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1440,
			HeaderTopTwips = 720,
			HeaderBlocks = [new LayoutBlock(new ParagraphBlock { SourceElement = headerParagraph }, 240f)],
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = new Paragraph(new Run(new Text("Body"))) }, 300f)]
		};

		var svgPages = SvgPageRenderer.RenderPages([page]);

		svgPages.Should().ContainSingle();
		svgPages[0].Should().Contain("Document Title");
		svgPages[0].Should().Contain("Page 1");
		svgPages[0].Should().Contain("Body");
	}

	[Fact]
	public void RenderPages_BiDiParagraphWithRtlRun_EmitsTextInSvg()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(new BiDi()),
			new Run(
				new RunProperties(new RightToLeftText()),
				new Text("مرحبا")));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph, IsBiDi = true }, 300f)]
		};

		var svgPages = SvgPageRenderer.RenderPages([page]);

		svgPages.Should().ContainSingle();
		svgPages[0].Should().Contain("مرحبا");
	}

	[Fact]
	public void RenderPages_MixedBiDiParagraph_EmitsBothLtrAndRtlTextInSvg()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(new BiDi()),
			new Run(
				new RunProperties(new RightToLeftText()),
				new Text("שלום") { Space = SpaceProcessingModeValues.Preserve }),
			new Run(new Text(" Hello") { Space = SpaceProcessingModeValues.Preserve }));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph, IsBiDi = true }, 300f)]
		};

		var svgPages = SvgPageRenderer.RenderPages([page]);

		svgPages.Should().ContainSingle();
		svgPages[0].Should().Contain("שלום");
		svgPages[0].Should().Contain("Hello");
	}

	[Fact]
	public void RenderPages_InlineSdtRun_RendersContentInSvg()
	{
		var paragraph = new Paragraph(
			new SdtRun(
				new SdtContentRun(
					new Run(new Text("SDT Content")))));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};

		var svgPages = SvgPageRenderer.RenderPages([page]);

		svgPages.Should().ContainSingle();
		svgPages[0].Should().Contain("SDT Content");
	}

	[Fact]
	public void RenderPages_SdtRunMixedWithNormalRun_RendersBothInSvg()
	{
		var paragraph = new Paragraph(
			new Run(new Text("Normal ") { Space = SpaceProcessingModeValues.Preserve }),
			new SdtRun(
				new SdtContentRun(
					new Run(new Text("Controlled")))));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};

		var svgPages = SvgPageRenderer.RenderPages([page]);

		svgPages.Should().ContainSingle();
		svgPages[0].Should().Contain("Normal ");
		svgPages[0].Should().Contain("Controlled");
	}
}
