namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

/// <summary>
/// Integration tests verifying end-to-end header/footer selection,
/// layout, positioning, and space reservation (step 3.3.7).
/// </summary>
public sealed class HeaderFooterIntegrationTests
{
	private static readonly HeaderFooterReference DefaultHeader = new(HeaderFooterKind.Default, "rHdr1");
	private static readonly HeaderFooterReference FirstHeader = new(HeaderFooterKind.First, "rHdr2");
	private static readonly HeaderFooterReference EvenHeader = new(HeaderFooterKind.Even, "rHdr3");
	private static readonly HeaderFooterReference DefaultFooter = new(HeaderFooterKind.Default, "rFtr1");
	private static readonly HeaderFooterReference FirstFooter = new(HeaderFooterKind.First, "rFtr2");

	private static HeaderFooterContent MakeContent(HeaderFooterKind kind, string relId, int paragraphCount)
	{
		var blocks = Enumerable.Range(0, paragraphCount)
			.Select(_ => (DocumentBlock)new ParagraphBlock { SourceElement = new Paragraph() })
			.ToList();
		return new HeaderFooterContent(kind, relId, blocks);
	}

	[Fact]
	public void ResolveAndLayout_DefaultHeader_ProducesCorrectHeight()
	{
		var section = new SectionInfo
		{
			HeaderReferences = [DefaultHeader],
		};
		var content = MakeContent(HeaderFooterKind.Default, "rHdr1", 2);

		var resolved = HeaderFooterResolver.ResolveHeader(section, isFirstPageOfSection: false, pageNumber: 1, evenAndOddHeaders: false);

		resolved.Should().NotBeNull();
		resolved!.RelationshipId.Should().Be("rHdr1");

		var (blocks, totalHeight) = HeaderFooterLayoutEngine.Layout(content);

		blocks.Should().HaveCount(2);
		totalHeight.Should().Be(480f); // 2 × 240
	}

	[Fact]
	public void ResolveAndLayout_FirstPageHeader_SelectedOnFirstPage()
	{
		var section = new SectionInfo
		{
			TitlePage = true,
			HeaderReferences = [DefaultHeader, FirstHeader],
		};

		var firstPageRef = HeaderFooterResolver.ResolveHeader(section, isFirstPageOfSection: true, pageNumber: 1, evenAndOddHeaders: false);
		var secondPageRef = HeaderFooterResolver.ResolveHeader(section, isFirstPageOfSection: false, pageNumber: 2, evenAndOddHeaders: false);

		firstPageRef!.Type.Should().Be(HeaderFooterKind.First);
		secondPageRef!.Type.Should().Be(HeaderFooterKind.Default);
	}

	[Fact]
	public void ResolveAndLayout_EvenOddHeaders_AlternateCorrectly()
	{
		var section = new SectionInfo
		{
			HeaderReferences = [DefaultHeader, EvenHeader],
		};

		var page1 = HeaderFooterResolver.ResolveHeader(section, false, 1, evenAndOddHeaders: true);
		var page2 = HeaderFooterResolver.ResolveHeader(section, false, 2, evenAndOddHeaders: true);
		var page3 = HeaderFooterResolver.ResolveHeader(section, false, 3, evenAndOddHeaders: true);

		page1!.Type.Should().Be(HeaderFooterKind.Default, "odd page");
		page2!.Type.Should().Be(HeaderFooterKind.Even, "even page");
		page3!.Type.Should().Be(HeaderFooterKind.Default, "odd page");
	}

	[Fact]
	public void Positioning_HeaderAndFooter_CorrectYPositions()
	{
		var section = new SectionInfo(); // defaults
		var headerContent = MakeContent(HeaderFooterKind.Default, "rHdr1", 1);
		var footerContent = MakeContent(HeaderFooterKind.Default, "rFtr1", 1);

		var (_, headerHeight) = HeaderFooterLayoutEngine.Layout(headerContent);
		var (_, footerHeight) = HeaderFooterLayoutEngine.Layout(footerContent);

		var headerTop = PageBuilder.ComputeHeaderTop(section);
		var contentTop = PageBuilder.ComputeContentTop(section, headerHeight);
		var footerTop = PageBuilder.ComputeFooterTop(section, footerHeight);

		// Header at 720, content at 1440 (header 240 fits in 720 gap), footer at 14400.
		headerTop.Should().Be(720f);
		contentTop.Should().Be(1440f, "header fits within margin gap");
		footerTop.Should().Be(14400f, "footer fits within margin gap");
	}

	[Fact]
	public void Positioning_LargeHeader_PushesContentDown()
	{
		var section = new SectionInfo(); // defaults
		// 5 paragraphs × 240 = 1200 twips header. Gap = 1440 - 720 = 720. Overflow = 480.
		var headerContent = MakeContent(HeaderFooterKind.Default, "rHdr1", 5);

		var (_, headerHeight) = HeaderFooterLayoutEngine.Layout(headerContent);
		headerHeight.Should().Be(1200f);

		var contentTop = PageBuilder.ComputeContentTop(section, headerHeight);
		// max(1440, 720 + 1200) = 1920.
		contentTop.Should().Be(1920f);

		var availableHeight = PageBuilder.ComputeAvailableContentHeight(section, headerHeight);
		// 15840 - 1920 - 1440 = 12480.
		availableHeight.Should().Be(12480f);
	}

	[Fact]
	public void Positioning_LargeFooter_ReducesContentArea()
	{
		var section = new SectionInfo();
		var footerContent = MakeContent(HeaderFooterKind.Default, "rFtr1", 5);

		var (_, footerHeight) = HeaderFooterLayoutEngine.Layout(footerContent);
		footerHeight.Should().Be(1200f);

		var footerTop = PageBuilder.ComputeFooterTop(section, footerHeight);
		// max(1440, 720 + 1200) = 1920. FooterTop = 15840 - 1920 = 13920.
		footerTop.Should().Be(13920f);

		var availableHeight = PageBuilder.ComputeAvailableContentHeight(section, footerHeight: footerHeight);
		// 15840 - 1440 - 1920 = 12480.
		availableHeight.Should().Be(12480f);
	}

	[Fact]
	public void EndToEnd_HeaderAndFooterReducePagination()
	{
		var section = new SectionInfo();
		// 5-paragraph header (1200) + 5-paragraph footer (1200).
		var headerContent = MakeContent(HeaderFooterKind.Default, "rHdr1", 5);
		var footerContent = MakeContent(HeaderFooterKind.Default, "rFtr1", 5);

		var (_, headerHeight) = HeaderFooterLayoutEngine.Layout(headerContent);
		var (_, footerHeight) = HeaderFooterLayoutEngine.Layout(footerContent);

		// Available without header/footer: 12960. With: 15840 - 1920 - 1920 = 12000.
		var available = PageBuilder.ComputeAvailableContentHeight(section, headerHeight, footerHeight);
		available.Should().Be(12000f);

		// Two blocks of 6480 each = 12960. Without h/f → 1 page. With h/f → 2 pages.
		var blocks = new[]
		{
			MakeBlock(6480f),
			MakeBlock(6480f),
		};

		var pagesWithout = PageBuilder.Paginate(blocks, section);
		pagesWithout.Should().ContainSingle();

		var pagesWith = PageBuilder.Paginate(blocks, section, headerHeight, footerHeight);
		pagesWith.Should().HaveCount(2);
	}

	[Fact]
	public void EndToEnd_FirstPageDifferentHeader_DifferentAvailableHeight()
	{
		// First page has a large header (5 paragraphs = 1200 twips, overflows by 480).
		// Subsequent pages have a small header (1 paragraph = 240 twips, fits in gap).
		var section = new SectionInfo
		{
			TitlePage = true,
			HeaderReferences = [DefaultHeader, FirstHeader],
		};

		var firstHeaderContent = MakeContent(HeaderFooterKind.First, "rHdr2", 5);
		var defaultHeaderContent = MakeContent(HeaderFooterKind.Default, "rHdr1", 1);

		var (_, firstHeaderHeight) = HeaderFooterLayoutEngine.Layout(firstHeaderContent);
		var (_, defaultHeaderHeight) = HeaderFooterLayoutEngine.Layout(defaultHeaderContent);

		var firstPageAvailable = PageBuilder.ComputeAvailableContentHeight(section, firstHeaderHeight);
		var subsequentPageAvailable = PageBuilder.ComputeAvailableContentHeight(section, defaultHeaderHeight);

		// First page: 15840 - 1920 - 1440 = 12480.
		firstPageAvailable.Should().Be(12480f);
		// Subsequent pages: 15840 - 1440 - 1440 = 12960 (no overflow).
		subsequentPageAvailable.Should().Be(12960f);
		firstPageAvailable.Should().BeLessThan(subsequentPageAvailable);
	}

	[Fact]
	public void Footer_ResolveAndLayout_CorrectSelection()
	{
		var section = new SectionInfo
		{
			TitlePage = true,
			FooterReferences = [DefaultFooter, FirstFooter],
		};

		var firstPageFooter = HeaderFooterResolver.ResolveFooter(section, isFirstPageOfSection: true, pageNumber: 1, evenAndOddHeaders: false);
		var secondPageFooter = HeaderFooterResolver.ResolveFooter(section, isFirstPageOfSection: false, pageNumber: 2, evenAndOddHeaders: false);

		firstPageFooter!.Type.Should().Be(HeaderFooterKind.First);
		secondPageFooter!.Type.Should().Be(HeaderFooterKind.Default);

		var footerContent = MakeContent(HeaderFooterKind.First, "rFtr2", 3);
		var (blocks, height) = HeaderFooterLayoutEngine.Layout(footerContent);

		blocks.Should().HaveCount(3);
		height.Should().Be(720f);
	}

	[Fact]
	public void NoHeaderOrFooter_PositionsAtDefaults()
	{
		var section = new SectionInfo();

		var header = HeaderFooterResolver.ResolveHeader(section, false, 1, false);
		var footer = HeaderFooterResolver.ResolveFooter(section, false, 1, false);

		header.Should().BeNull();
		footer.Should().BeNull();

		// Without header/footer, available = full content area.
		var available = PageBuilder.ComputeAvailableContentHeight(section);
		available.Should().Be(12960f);
	}

	private static LayoutBlock MakeBlock(float heightTwips)
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		return new LayoutBlock(para, heightTwips);
	}
}
