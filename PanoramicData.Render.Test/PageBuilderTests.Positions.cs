namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed partial class PageBuilderTests
{
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
