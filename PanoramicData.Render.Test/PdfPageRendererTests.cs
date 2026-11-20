namespace PanoramicData.Render.Test;

using System.Text;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using static PanoramicData.Render.Test.RendererScenarioBuilders;

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
		var page1 = TestLayoutPageFactory.CreateTextPage(1, "Page one");
		var page2 = TestLayoutPageFactory.CreateTextPage(2, "Page two", 10000, 14000);

		var pdf = PdfPageRenderer.RenderPages([page1, page2]);

		pdf.Should().NotBeEmpty();
		CountPageObjects(pdf).Should().Be(2);
		ContainsMediaBox(pdf, 612, 792).Should().BeTrue();
		ContainsMediaBox(pdf, 500, 700).Should().BeTrue();
	}

	[Fact]
	public void RenderPages_WithMetadata_WritesTitleAndAuthorToPdf()
	{
		var page = TestLayoutPageFactory.CreateTextPage(1, "Metadata page");

		var metadata = new PdfMetadata("Sample Title", "Sample Author", new DateTime(2026, 4, 10, 12, 0, 0, DateTimeKind.Utc));

		var pdf = PdfPageRenderer.RenderPages([page], metadata: metadata);

		var text = Encoding.ASCII.GetString(pdf);
		text.Should().Contain("Sample Title");
		text.Should().Contain("Sample Author");
	}

	[Fact]
	public void RenderPages_WithPageRange_RendersOnlySelectedPageSubset()
	{
		var pages = new[] { "One", "Two", "Three" }
			.Select((text, index) => TestLayoutPageFactory.CreateTextPage(index + 1, text))
			.ToArray();

		var pdf = PdfPageRenderer.RenderPages(pages, new RenderOptions { PageRange = 1..3 });

		CountPageObjects(pdf).Should().Be(2);
	}

	private static void AssertRendersPdf(LayoutPage page, int minLength = 0, int? pageObjects = null)
	{
		var pdf = PdfPageRenderer.RenderPages([page]);
		pdf.Should().NotBeEmpty();
		pdf.Length.Should().BeGreaterThan(minLength);
		if (pageObjects is { } count)
		{
			CountPageObjects(pdf).Should().Be(count);
		}
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
		AssertRendersPdf(ParagraphPage(HyperlinkParagraph("Click", "target")), pageObjects: 1);
	}

	[Fact]
	public void RenderPages_WithBookmarkStart_ProducesValidPdf()
	{
		AssertRendersPdf(BookmarkedPage("Bookmarked", "myDest"), 500);
	}

	[Fact]
	public void RenderPages_HyperlinkAndBookmark_ProducesValidPdf()
	{
		AssertRendersPdf(BookmarkAndLinkPage("Target", "Go", "dest"), pageObjects: 1);
	}

	[Fact]
	public void RenderPages_TextWatermark_ProducesValidPdf()
	{
		AssertRendersPdf(BodyPageWithWatermark(TextWatermark("DRAFT", "Calibri", "#C0C0C0", 0.5f)), 500);
	}

	[Fact]
	public void RenderPages_ImageWatermark_ProducesValidPdf()
	{
		AssertRendersPdf(BodyPageWithWatermark(ImageWatermark(TinyPng)), 500);
	}

	[Fact]
	public void RenderPages_TabStopWithDotLeader_ProducesValidPdf()
	{
		AssertRendersPdf(ParagraphPage(DotLeaderTabParagraph("Item", "99")), 500);
	}

	[Fact]
	public void RenderPages_HeaderWithRightTab_ProducesValidPdf()
	{
		AssertRendersPdf(RightTabHeaderPage("Title"), 500);
	}

	[Fact]
	public void RenderPages_BiDiParagraph_ProducesValidPdf()
	{
		AssertRendersPdf(BiDiPage(BiDiParagraph("مرحبا")), 100);
	}

	[Fact]
	public void RenderPages_MixedBiDiParagraph_ProducesValidPdf()
	{
		AssertRendersPdf(BiDiPage(MixedBiDiParagraph()), 100);
	}

	[Fact]
	public void RenderPages_InlineSdtRun_ProducesValidPdf()
	{
		AssertRendersPdf(ParagraphPage(InlineSdtParagraph()), 100);
	}

	[Fact]
	public void RenderPages_SdtRunMixedWithNormalRun_ProducesValidPdf()
	{
		AssertRendersPdf(ParagraphPage(SdtMixedWithNormalParagraph()), 100);
	}
}
