namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed partial class PageBuilderTests
{
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
}
