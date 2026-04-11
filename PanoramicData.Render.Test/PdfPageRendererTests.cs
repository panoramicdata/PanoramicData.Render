namespace PanoramicData.Render.Test;

using System.Text;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed class PdfPageRendererTests
{
	private static readonly byte[] TinyPng =
	[
		137, 80, 78, 71, 13, 10, 26, 10,
		0, 0, 0, 13, 73, 72, 68, 82,
		0, 0, 0, 1, 0, 0, 0, 1,
		8, 6, 0, 0, 0, 31, 21, 196,
		137, 0, 0, 0, 13, 73, 68, 65,
		84, 120, 156, 99, 248, 255, 255, 63,
		0, 5, 254, 2, 254, 65, 201, 209,
		46, 0, 0, 0, 0, 73, 69, 78,
		68, 174, 66, 96, 130
	];

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

	[Fact]
	public void RenderPages_WithHyperlinkElement_ProducesValidPdf()
	{
		var hyperlink = new Hyperlink(new Run(new Text("Click"))) { Anchor = "target" };
		var paragraph = new Paragraph(hyperlink);
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};

		var pdf = PdfPageRenderer.RenderPages([page]);

		pdf.Should().NotBeEmpty();
		CountPageObjects(pdf).Should().Be(1);
	}

	[Fact]
	public void RenderPages_WithBookmarkStart_ProducesValidPdf()
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
				BookmarkStarts = [new BookmarkStartInfo(1, "myDest")]
			}, 300f)]
		};

		var pdf = PdfPageRenderer.RenderPages([page]);

		pdf.Should().NotBeEmpty();
		pdf.Length.Should().BeGreaterThan(500);
	}

	[Fact]
	public void RenderPages_HyperlinkAndBookmark_ProducesValidPdf()
	{
		var bookmarkedParagraph = new Paragraph(new Run(new Text("Target")));
		var linkParagraph = new Paragraph(new Hyperlink(new Run(new Text("Go"))) { Anchor = "dest" });
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
					BookmarkStarts = [new BookmarkStartInfo(1, "dest")]
				}, 300f),
				new LayoutBlock(new ParagraphBlock { SourceElement = linkParagraph }, 300f)
			]
		};

		var pdf = PdfPageRenderer.RenderPages([page]);

		pdf.Should().NotBeEmpty();
		CountPageObjects(pdf).Should().Be(1);
	}

	[Fact]
	public void RenderPages_TextWatermark_ProducesValidPdf()
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
				Text = "DRAFT",
				FontFamily = "Calibri",
				FillColor = "#C0C0C0",
				Opacity = 0.5f,
				RotationDegrees = 315f,
				WidthTwips = 8000f,
				HeightTwips = 2000f
			}
		};

		var pdf = PdfPageRenderer.RenderPages([page]);

		pdf.Should().NotBeEmpty();
		pdf.Length.Should().BeGreaterThan(500);
	}

	[Fact]
	public void RenderPages_ImageWatermark_ProducesValidPdf()
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
				Kind = WatermarkKind.Image,
				ResolvedImageData = new ImageData(TinyPng, "image/png"),
				Opacity = 0.5f,
				WidthTwips = 7200f,
				HeightTwips = 5400f
			}
		};

		var pdf = PdfPageRenderer.RenderPages([page]);

		pdf.Should().NotBeEmpty();
		pdf.Length.Should().BeGreaterThan(500);
	}

	[Fact]
	public void RenderPages_TabStopWithDotLeader_ProducesValidPdf()
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
			new Run(new Text("Item") { Space = SpaceProcessingModeValues.Preserve }),
			new Run(new TabChar()),
			new Run(new Text("99") { Space = SpaceProcessingModeValues.Preserve }));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};

		var pdf = PdfPageRenderer.RenderPages([page]);

		pdf.Should().NotBeEmpty();
		pdf.Length.Should().BeGreaterThan(500);
	}

	[Fact]
	public void RenderPages_HeaderWithRightTab_ProducesValidPdf()
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
			new Run(new Text("Title") { Space = SpaceProcessingModeValues.Preserve }),
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

		var pdf = PdfPageRenderer.RenderPages([page]);

		pdf.Should().NotBeEmpty();
		pdf.Length.Should().BeGreaterThan(500);
	}
}
