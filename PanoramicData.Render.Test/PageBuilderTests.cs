namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed class PageBuilderTests
{
	private static readonly SectionInfo DefaultSection = new();

	/// <summary>
	/// Available content height for default section: 15840 - 1440 - 1440 = 12960 twips.
	/// </summary>
	private const float DefaultAvailableHeight = 12960f;

	[Fact]
	public void Paginate_EmptyBlocks_ReturnsEmptyList()
	{
		var result = PageBuilder.Paginate([], DefaultSection);

		result.Should().BeEmpty();
	}

	[Fact]
	public void Paginate_SingleBlockFits_ReturnsSinglePage()
	{
		var blocks = new[] { MakeBlock(1000f) };

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().ContainSingle();
		result[0].Blocks.Should().ContainSingle();
	}

	[Fact]
	public void Paginate_SinglePage_HasPageNumberOne()
	{
		var blocks = new[] { MakeBlock(1000f) };

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result[0].PageNumber.Should().Be(1);
	}

	[Fact]
	public void Paginate_SinglePage_ReferencesCorrectSection()
	{
		var section = new SectionInfo { PageWidth = 9000 };
		var blocks = new[] { MakeBlock(1000f) };

		var result = PageBuilder.Paginate(blocks, section);

		result[0].Section.Should().BeSameAs(section);
	}

	[Fact]
	public void Paginate_MultipleBlocksFitOnOnePage_ReturnsSinglePage()
	{
		var blocks = new[]
		{
			MakeBlock(3000f),
			MakeBlock(3000f),
			MakeBlock(3000f),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().ContainSingle();
		result[0].Blocks.Should().HaveCount(3);
	}

	[Fact]
	public void Paginate_BlocksExceedPageHeight_SpillToSecondPage()
	{
		// Two blocks: each 7000 twips. Total 14000 > 12960 available.
		var blocks = new[]
		{
			MakeBlock(7000f),
			MakeBlock(7000f),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().HaveCount(2);
		result[0].Blocks.Should().ContainSingle();
		result[1].Blocks.Should().ContainSingle();
	}

	[Fact]
	public void Paginate_ManyBlocks_CorrectPageCount()
	{
		// 10 blocks of 4000 each. Available 12960 → 3 per page → 4 pages (3+3+3+1).
		var blocks = Enumerable.Range(0, 10).Select(_ => MakeBlock(4000f)).ToArray();

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().HaveCount(4);
		result[0].Blocks.Should().HaveCount(3);
		result[1].Blocks.Should().HaveCount(3);
		result[2].Blocks.Should().HaveCount(3);
		result[3].Blocks.Should().HaveCount(1);
	}

	[Fact]
	public void Paginate_ExactFit_NoOverflow()
	{
		// 3 blocks that exactly fill a page: 12960 / 3 = 4320 each.
		var blocks = new[]
		{
			MakeBlock(4320f),
			MakeBlock(4320f),
			MakeBlock(4320f),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().ContainSingle();
		result[0].Blocks.Should().HaveCount(3);
	}

	[Fact]
	public void Paginate_SlightlyOverExactFit_SpillsToNextPage()
	{
		// 3 blocks at 4320 + one tiny block → page 2.
		var blocks = new[]
		{
			MakeBlock(4320f),
			MakeBlock(4320f),
			MakeBlock(4320f),
			MakeBlock(1f),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().HaveCount(2);
		result[0].Blocks.Should().HaveCount(3);
		result[1].Blocks.Should().ContainSingle();
	}

	[Fact]
	public void Paginate_BlockTallerThanPage_GetsOwnPage()
	{
		// A block taller than the available height still gets placed.
		var blocks = new[]
		{
			MakeBlock(1000f),
			MakeBlock(20000f), // taller than 12960 available
			MakeBlock(1000f),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().HaveCount(3);
		result[0].Blocks.Should().ContainSingle();
		result[1].Blocks.Should().ContainSingle();
		result[1].Blocks[0].HeightTwips.Should().Be(20000f);
		result[2].Blocks.Should().ContainSingle();
	}

	[Fact]
	public void Paginate_PageNumbersAreSequential()
	{
		var blocks = Enumerable.Range(0, 6).Select(_ => MakeBlock(5000f)).ToArray();

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		for (var i = 0; i < result.Count; i++)
		{
			result[i].PageNumber.Should().Be(i + 1);
		}
	}

	[Fact]
	public void Paginate_AllPagesReferenceSection()
	{
		var section = new SectionInfo { PageHeight = 10000 };
		var blocks = Enumerable.Range(0, 4).Select(_ => MakeBlock(5000f)).ToArray();

		var result = PageBuilder.Paginate(blocks, section);

		foreach (var page in result)
		{
			page.Section.Should().BeSameAs(section);
		}
	}

	[Fact]
	public void Paginate_CustomMargins_AffectsAvailableHeight()
	{
		// Custom margins: top=2000 + bottom=3000 → available = 15840 - 5000 = 10840.
		var section = new SectionInfo { MarginTop = 2000, MarginBottom = 3000 };
		var blocks = new[]
		{
			MakeBlock(6000f),
			MakeBlock(6000f), // total 12000 > 10840
		};

		var result = PageBuilder.Paginate(blocks, section);

		result.Should().HaveCount(2);
	}

	[Fact]
	public void Paginate_NullBlocks_ThrowsArgumentNullException()
	{
		var act = () => PageBuilder.Paginate(null!, DefaultSection);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("blocks");
	}

	[Fact]
	public void Paginate_NullSection_ThrowsArgumentNullException()
	{
		var act = () => PageBuilder.Paginate([], null!);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("section");
	}

	[Fact]
	public void Paginate_ZeroHeightBlocks_AllFitOnOnePage()
	{
		var blocks = Enumerable.Range(0, 100).Select(_ => MakeBlock(0f)).ToArray();

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().ContainSingle();
		result[0].Blocks.Should().HaveCount(100);
	}

	private static LayoutBlock MakeBlock(float heightTwips)
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		return new LayoutBlock(para, heightTwips);
	}

	// --- Header/footer height reservation tests (step 3.3.4) ---

	[Fact]
	public void ComputeAvailableContentHeight_NoHeaderFooter_ReturnsPageMinusMargins()
	{
		var result = PageBuilder.ComputeAvailableContentHeight(DefaultSection);

		result.Should().Be(DefaultAvailableHeight);
	}

	[Fact]
	public void ComputeAvailableContentHeight_HeaderFitsInMargin_NoReduction()
	{
		// Default: MarginTop=1440, MarginHeader=720 → 720 twips for header.
		// Header of 500 fits: available height unchanged.
		var result = PageBuilder.ComputeAvailableContentHeight(DefaultSection, headerHeight: 500f);

		result.Should().Be(DefaultAvailableHeight);
	}

	[Fact]
	public void ComputeAvailableContentHeight_HeaderOverflowsMargin_ReducesAvailable()
	{
		// Header = 1000: effectiveTop = max(1440, 720+1000) = 1720.
		// Available = 15840 - 1720 - 1440 = 12680.
		var result = PageBuilder.ComputeAvailableContentHeight(DefaultSection, headerHeight: 1000f);

		result.Should().Be(12680f);
	}

	[Fact]
	public void ComputeAvailableContentHeight_FooterOverflowsMargin_ReducesAvailable()
	{
		// Footer = 1000: effectiveBottom = max(1440, 720+1000) = 1720.
		// Available = 15840 - 1440 - 1720 = 12680.
		var result = PageBuilder.ComputeAvailableContentHeight(DefaultSection, footerHeight: 1000f);

		result.Should().Be(12680f);
	}

	[Fact]
	public void ComputeAvailableContentHeight_BothOverflow_ReducesBoth()
	{
		// Header=1000 → effectiveTop=1720; Footer=1000 → effectiveBottom=1720.
		// Available = 15840 - 1720 - 1720 = 12400.
		var result = PageBuilder.ComputeAvailableContentHeight(DefaultSection, headerHeight: 1000f, footerHeight: 1000f);

		result.Should().Be(12400f);
	}

	[Fact]
	public void ComputeAvailableContentHeight_MassiveOverflow_ClampsToZero()
	{
		var result = PageBuilder.ComputeAvailableContentHeight(DefaultSection, headerHeight: 10000f, footerHeight: 10000f);

		result.Should().Be(0f);
	}

	[Fact]
	public void Paginate_WithHeaderHeight_ReducesAvailableSpace()
	{
		// Two blocks of 6480 each = 12960 = DefaultAvailableHeight → 1 page without header.
		var blocks = new[] { MakeBlock(6480f), MakeBlock(6480f) };

		var resultNoHeader = PageBuilder.Paginate(blocks, DefaultSection);
		resultNoHeader.Should().ContainSingle("blocks fit exactly on one page without header");

		// With header overflow: headerHeight=1000 → available=12680. 6480+6480=12960 > 12680 → 2 pages.
		var resultWithHeader = PageBuilder.Paginate(blocks, DefaultSection, headerHeight: 1000f);
		resultWithHeader.Should().HaveCount(2, "header overflow reduces available space");
	}

	[Fact]
	public void Paginate_WithFooterHeight_ReducesAvailableSpace()
	{
		var blocks = new[] { MakeBlock(6480f), MakeBlock(6480f) };

		var resultWithFooter = PageBuilder.Paginate(blocks, DefaultSection, footerHeight: 1000f);
		resultWithFooter.Should().HaveCount(2, "footer overflow reduces available space");
	}

	[Fact]
	public void Paginate_HeaderFitsInMargin_NoEffectOnPagination()
	{
		// Header of 500 fits within the 720-twip margin space → no change to pagination.
		var blocks = new[] { MakeBlock(6480f), MakeBlock(6480f) };

		var result = PageBuilder.Paginate(blocks, DefaultSection, headerHeight: 500f);

		result.Should().ContainSingle("header fits in margin; no pagination change");
	}

	// --- Position computation tests (step 3.3.5) ---

	[Fact]
	public void ComputeHeaderTop_ReturnsMarginHeader()
	{
		// Default MarginHeader = 720.
		PageBuilder.ComputeHeaderTop(DefaultSection).Should().Be(720f);
	}

	[Fact]
	public void ComputeHeaderTop_CustomMarginHeader()
	{
		var section = new SectionInfo { MarginHeader = 360 };

		PageBuilder.ComputeHeaderTop(section).Should().Be(360f);
	}

	[Fact]
	public void ComputeContentTop_NoHeader_ReturnsMarginTop()
	{
		// Default MarginTop = 1440.
		PageBuilder.ComputeContentTop(DefaultSection).Should().Be(1440f);
	}

	[Fact]
	public void ComputeContentTop_HeaderFitsInMargin_StillMarginTop()
	{
		// Header 500 < gap (1440 - 720 = 720): contentTop stays at 1440.
		PageBuilder.ComputeContentTop(DefaultSection, headerHeight: 500f).Should().Be(1440f);
	}

	[Fact]
	public void ComputeContentTop_HeaderOverflows_PushedDown()
	{
		// Header 1000: max(1440, 720+1000) = 1720.
		PageBuilder.ComputeContentTop(DefaultSection, headerHeight: 1000f).Should().Be(1720f);
	}

	[Fact]
	public void ComputeFooterTop_NoFooter_ReturnsPageHeightMinusMarginBottom()
	{
		// Default: 15840 - max(1440, 720+0) = 15840 - 1440 = 14400.
		PageBuilder.ComputeFooterTop(DefaultSection).Should().Be(14400f);
	}

	[Fact]
	public void ComputeFooterTop_FooterFitsInMargin_Unchanged()
	{
		// Footer 500 < gap (1440 - 720 = 720): still 14400.
		PageBuilder.ComputeFooterTop(DefaultSection, footerHeight: 500f).Should().Be(14400f);
	}

	[Fact]
	public void ComputeFooterTop_FooterOverflows_MovedUp()
	{
		// Footer 1000: max(1440, 720+1000) = 1720. FooterTop = 15840 - 1720 = 14120.
		PageBuilder.ComputeFooterTop(DefaultSection, footerHeight: 1000f).Should().Be(14120f);
	}

	[Fact]
	public void LayoutPage_HeaderBlocks_CanBeSet()
	{
		var headerBlock = MakeBlock(240f);
		var page = new LayoutPage
		{
			Section = DefaultSection,
			PageNumber = 1,
			Blocks = [],
			HeaderBlocks = [headerBlock],
			HeaderTopTwips = 720f,
			ContentTopTwips = 1440f,
			FooterTopTwips = 14400f,
		};

		page.HeaderBlocks.Should().ContainSingle();
		page.HeaderTopTwips.Should().Be(720f);
		page.ContentTopTwips.Should().Be(1440f);
	}

	[Fact]
	public void LayoutPage_FooterBlocks_CanBeSet()
	{
		var footerBlock = MakeBlock(240f);
		var page = new LayoutPage
		{
			Section = DefaultSection,
			PageNumber = 1,
			Blocks = [],
			FooterBlocks = [footerBlock],
			FooterTopTwips = 14120f,
		};

		page.FooterBlocks.Should().ContainSingle();
		page.FooterTopTwips.Should().Be(14120f);
	}

	[Fact]
	public void LayoutPage_NullHeaderAndFooter_IsDefault()
	{
		var page = new LayoutPage
		{
			Section = DefaultSection,
			PageNumber = 1,
			Blocks = [],
		};

		page.HeaderBlocks.Should().BeNull();
		page.FooterBlocks.Should().BeNull();
		page.HeaderTopTwips.Should().Be(0f);
		page.ContentTopTwips.Should().Be(0f);
		page.FooterTopTwips.Should().Be(0f);
	}

	// --- Header/footer distance tests (step 3.3.6) ---

	[Fact]
	public void ComputeHeaderTop_CustomDistance_UsesMarginHeader()
	{
		// Custom headerDistance (w:header) = 500 twips.
		var section = new SectionInfo { MarginHeader = 500 };

		PageBuilder.ComputeHeaderTop(section).Should().Be(500f);
	}

	[Fact]
	public void ComputeFooterTop_CustomDistance_AffectsPosition()
	{
		// Custom footerDistance (w:footer) = 500 twips.
		// effectiveBottom = max(1440, 500+0) = 1440. FooterTop = 15840 - 1440 = 14400.
		var section = new SectionInfo { MarginFooter = 500 };

		PageBuilder.ComputeFooterTop(section).Should().Be(14400f);
	}

	[Fact]
	public void ComputeContentTop_LargeHeaderDistance_GivesMoreSpace()
	{
		// If MarginHeader is very close to MarginTop (e.g., 1400), there's barely any space.
		// A small header (100 twips) would overflow: max(1440, 1400+100) = 1500.
		var section = new SectionInfo { MarginHeader = 1400 };

		PageBuilder.ComputeContentTop(section, headerHeight: 100f).Should().Be(1500f);
	}

	[Fact]
	public void ComputeAvailableContentHeight_SmallHeaderDistance_HeaderOverflowsSooner()
	{
		// MarginHeader=200 means header starts very high on page.
		// Header of 500: max(1440, 200+500) = 1440 → no overflow (fits in margin).
		var section = new SectionInfo { MarginHeader = 200 };

		PageBuilder.ComputeAvailableContentHeight(section, headerHeight: 500f).Should().Be(12960f);
	}

	[Fact]
	public void ComputeAvailableContentHeight_LargeFooterDistance_ReducesOverflowThreshold()
	{
		// MarginFooter=1400, footer of 100: max(1440, 1400+100) = 1500 → overflow of 60.
		// Available = 15840 - 1440 - 1500 = 12900.
		var section = new SectionInfo { MarginFooter = 1400 };

		PageBuilder.ComputeAvailableContentHeight(section, footerHeight: 100f).Should().Be(12900f);
	}

	[Fact]
	public void Paginate_CustomHeaderDistance_AffectsPagination()
	{
		// MarginHeader=1400, header=100 → contentTop=1500, effectiveBottom=1440.
		// Available = 15840 - 1500 - 1440 = 12900.
		// One block of 12960 > 12900 but is placed on empty page (oversized).
		// Two blocks of 6480 = 12960 > 12900 → 2 pages.
		var section = new SectionInfo { MarginHeader = 1400 };
		var blocks = new[] { MakeBlock(6480f), MakeBlock(6480f) };

		var result = PageBuilder.Paginate(blocks, section, headerHeight: 100f);

		result.Should().HaveCount(2);
	}

	// --- Footnote space reservation tests (step 3.4.3) ---

	[Fact]
	public void ComputeAvailableContentHeight_WithFootnoteHeight_ReducesSpace()
	{
		// Default available = 12960. Footnote 500 → 12460.
		var result = PageBuilder.ComputeAvailableContentHeight(DefaultSection, footnoteHeight: 500f);

		result.Should().Be(12460f);
	}

	[Fact]
	public void ComputeAvailableContentHeight_FootnoteAndHeaderOverflow_BothReduce()
	{
		// Header 1000 → effectiveTop 1720. Footnote 500.
		// Available = 15840 - 1720 - 1440 - 500 = 12180.
		var result = PageBuilder.ComputeAvailableContentHeight(DefaultSection, headerHeight: 1000f, footnoteHeight: 500f);

		result.Should().Be(12180f);
	}

	[Fact]
	public void ComputeAvailableContentHeight_FootnoteAndFooterOverflow_BothReduce()
	{
		// Footer 1000 → effectiveBottom 1720. Footnote 500.
		// Available = 15840 - 1440 - 1720 - 500 = 12180.
		var result = PageBuilder.ComputeAvailableContentHeight(DefaultSection, footerHeight: 1000f, footnoteHeight: 500f);

		result.Should().Be(12180f);
	}

	[Fact]
	public void ComputeAvailableContentHeight_MassiveFootnote_ClampsToZero()
	{
		var result = PageBuilder.ComputeAvailableContentHeight(DefaultSection, footnoteHeight: 20000f);

		result.Should().Be(0f);
	}

	[Fact]
	public void ComputeFootnoteTop_NoFooterOrFootnotes_AtFooterTop()
	{
		// FooterTop = 15840 - 1440 = 14400. FootnoteTop = 14400 - 0 = 14400.
		PageBuilder.ComputeFootnoteTop(DefaultSection).Should().Be(14400f);
	}

	[Fact]
	public void ComputeFootnoteTop_WithFootnoteHeight_AboveFooter()
	{
		// FooterTop = 14400. FootnoteTop = 14400 - 500 = 13900.
		PageBuilder.ComputeFootnoteTop(DefaultSection, footnoteHeight: 500f).Should().Be(13900f);
	}

	[Fact]
	public void ComputeFootnoteTop_WithFooterOverflow_Adjusts()
	{
		// Footer 1000 → effectiveBottom 1720. FooterTop = 15840 - 1720 = 14120.
		// FootnoteTop = 14120 - 500 = 13620.
		PageBuilder.ComputeFootnoteTop(DefaultSection, footerHeight: 1000f, footnoteHeight: 500f).Should().Be(13620f);
	}

	[Fact]
	public void Paginate_WithFootnoteHeight_ReducesAvailableSpace()
	{
		// Available without footnote: 12960. Two blocks of 6480 = 12960 → 1 page.
		// With footnote 500: 12460. Two blocks of 6480 = 12960 > 12460 → 2 pages.
		var blocks = new[] { MakeBlock(6480f), MakeBlock(6480f) };

		var resultNoFn = PageBuilder.Paginate(blocks, DefaultSection);
		resultNoFn.Should().ContainSingle();

		var resultWithFn = PageBuilder.Paginate(blocks, DefaultSection, footnoteHeight: 500f);
		resultWithFn.Should().HaveCount(2);
	}
}
