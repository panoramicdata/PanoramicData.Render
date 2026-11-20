namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using static PanoramicData.Render.Test.SectionPaginationBuilders;

public sealed class SectionBreakTests
{
	private static readonly SectionInfo DefaultSection = new();

	// --- IdentifySections tests ---

	[Fact]
	public void IdentifySections_NoBreaks_SingleSection()
	{
		var sections = PageBuilder.IdentifySections([MakeBlock(1000f), MakeBlock(2000f)], DefaultSection);

		sections.Should().ContainSingle();
		sections[0].Info.Should().BeSameAs(DefaultSection);
		sections[0].Blocks.Should().HaveCount(2);
	}

	[Fact]
	public void IdentifySections_OneBreak_TwoSections()
	{
		var sectionInfo = new SectionInfo { PageWidth = 9000 };

		var sections = PageBuilder.IdentifySections([MakeBlock(1000f), MakeSectionBreak(sectionInfo), MakeBlock(2000f)], DefaultSection);

		sections.Should().HaveCount(2);
		sections[0].Info.Should().BeSameAs(sectionInfo);
		sections[0].Blocks.Should().ContainSingle();
		sections[1].Info.Should().BeSameAs(DefaultSection);
		sections[1].Blocks.Should().ContainSingle();
	}

	[Fact]
	public void IdentifySections_EmptyBlocks_SingleEmptySection()
	{
		var sections = PageBuilder.IdentifySections([], DefaultSection);

		sections.Should().ContainSingle();
		sections[0].Info.Should().BeSameAs(DefaultSection);
		sections[0].Blocks.Should().BeEmpty();
	}

	[Fact]
	public void IdentifySections_BreakOnly_SingleSection()
	{
		var sectionInfo = new SectionInfo { PageWidth = 9000 };

		var sections = PageBuilder.IdentifySections([MakeSectionBreak(sectionInfo)], DefaultSection);

		// Only the section before the break is created; the trailing empty body section is omitted.
		sections.Should().ContainSingle();
		sections[0].Info.Should().BeSameAs(sectionInfo);
		sections[0].Blocks.Should().BeEmpty();
	}

	[Fact]
	public void IdentifySections_PreservesBreakType()
	{
		var firstSection = new SectionInfo { BreakType = SectionBreakType.EvenPage };
		var secondSection = new SectionInfo { BreakType = SectionBreakType.OddPage };
		var bodySection = new SectionInfo { BreakType = SectionBreakType.Continuous };

		var sections = PageBuilder.IdentifySections(
			[MakeBlock(1000f), MakeSectionBreak(firstSection), MakeBlock(2000f), MakeSectionBreak(secondSection), MakeBlock(3000f)],
			bodySection);

		sections.Should().HaveCount(3);
		// First section always uses NextPage regardless of its own break type.
		sections[0].BreakType.Should().Be(SectionBreakType.NextPage);
		// Second section preserves its own break type.
		sections[1].BreakType.Should().Be(SectionBreakType.OddPage);
		// Body section uses the body section info's break type.
		sections[2].BreakType.Should().Be(SectionBreakType.Continuous);
	}

	// --- PaginateDocument tests ---

	[Fact]
	public void PaginateDocument_NullBlocks_ThrowsArgumentNullException()
	{
		var act = () => PageBuilder.PaginateDocument(null!, DefaultSection);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("blocks");
	}

	[Fact]
	public void PaginateDocument_NullSection_ThrowsArgumentNullException()
	{
		var act = () => PageBuilder.PaginateDocument([], null!);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("bodySectionInfo");
	}

	[Fact]
	public void PaginateDocument_NoSectionBreaks_SameAsSingleSection()
	{
		var result = Paginate(DefaultSection, MakeBlock(1000f), MakeBlock(2000f));

		result.Should().ContainSingle();
		result[0].Blocks.Should().HaveCount(2);
	}

	[Fact]
	public void PaginateDocument_TwoSections_SeparatePages()
	{
		var firstSection = new SectionInfo
		{
			PageWidth = 9000,
			BreakType = SectionBreakType.NextPage
		};

		var result = Paginate(DefaultSection, MakeBlock(1000f), MakeSectionBreak(firstSection), MakeBlock(2000f));

		result.Should().HaveCount(2);
		result[0].Section.Should().BeSameAs(firstSection);
		result[1].Section.Should().BeSameAs(DefaultSection);
	}

	[Fact]
	public void PaginateDocument_PageNumbersContinueAcrossSections()
	{
		// the second 7000 block overflows to page 2
		var result = Paginate(DefaultSection, MakeBlock(7000f), MakeBlock(7000f), MakeSectionBreak(new SectionInfo()), MakeBlock(1000f));

		AssertPageNumbers(result, 1, 2, 3);
	}

	[Fact]
	public void PaginateDocument_EvenPageBreak_InsertsBlankPage()
	{
		var bodySection = new SectionInfo { BreakType = SectionBreakType.EvenPage };

		var result = Paginate(bodySection, MakeBlock(1000f), MakeSectionBreak(new SectionInfo()), MakeBlock(1000f));

		// Page 1 is the content; the next page is 2, which is already even, so no blank page is needed.
		AssertPageNumbers(result, 1, 2);
	}

	[Fact]
	public void PaginateDocument_OddPageBreak_InsertsBlankPage()
	{
		var bodySection = new SectionInfo { BreakType = SectionBreakType.OddPage };

		// First section occupies pages 1 and 2; the next page is 3 (odd), so no blank page is needed.
		var result = Paginate(bodySection, MakeBlock(7000f), MakeBlock(7000f), MakeSectionBreak(new SectionInfo()), MakeBlock(1000f));

		AssertPageNumbers(result, 1, 2, 3);
	}

	[Fact]
	public void PaginateDocument_OddPageBreak_NextIsEven_InsertsBlank()
	{
		var bodySection = new SectionInfo { BreakType = SectionBreakType.OddPage };

		// Page 1 then next would be 2 (even): insert blank page 2, start on 3.
		var result = Paginate(bodySection, MakeBlock(1000f), MakeSectionBreak(new SectionInfo()), MakeBlock(1000f));

		AssertPageNumbers(result, 1, 2, 3);
		result[1].Blocks.Should().BeEmpty(); // blank page
	}

	[Fact]
	public void PaginateDocument_EvenPageBreak_NextIsOdd_InsertsBlank()
	{
		var bodySection = new SectionInfo { BreakType = SectionBreakType.EvenPage };

		// Pages 1-2 then next would be 3 (odd): insert blank page 3, start on 4.
		var result = Paginate(bodySection, MakeBlock(7000f), MakeBlock(7000f), MakeSectionBreak(new SectionInfo()), MakeBlock(1000f));

		AssertPageNumbers(result, 1, 2, 3, 4);
		result[2].Blocks.Should().BeEmpty(); // blank page
	}

	[Fact]
	public void PaginateDocument_ContinuousBreak_StartsOnNewPage()
	{
		// Continuous break with same page dimensions starts a new page for simplicity.
		var bodySection = new SectionInfo { BreakType = SectionBreakType.Continuous };

		var result = Paginate(bodySection, MakeBlock(1000f), MakeSectionBreak(new SectionInfo()), MakeBlock(2000f));

		result.Should().HaveCount(2);
	}

	[Fact]
	public void PaginateDocument_EmptyBlocks_EmptyResult()
	{
		var result = PageBuilder.PaginateDocument([], DefaultSection);

		result.Should().BeEmpty();
	}

	// --- DocumentSection tests ---

	[Fact]
	public void DocumentSection_Properties_ReturnAssignedValues()
	{
		var info = new SectionInfo { PageWidth = 9000 };
		LayoutBlock[] blocks = { MakeBlock(100f) };
		var section = new DocumentSection(info, blocks, SectionBreakType.EvenPage);

		section.Info.Should().BeSameAs(info);
		section.Blocks.Should().BeSameAs(blocks);
		section.BreakType.Should().Be(SectionBreakType.EvenPage);
	}

	// --- Per-section page dimensions (step 3.2.2) ---

	[Fact]
	public void PaginateDocument_DifferentPageHeights_UsesPerSectionHeight()
	{
		// Short section: available height = 5000 - 1440 - 1440 = 2120. Default page: 12960.
		var shortSection = new SectionInfo { PageHeight = 5000 };

		var result = Paginate(
			DefaultSection,
			MakeBlock(1000f),
			MakeBlock(1000f),
			MakeBlock(500f), // total 2500 overflows the short page
			MakeSectionBreak(shortSection),
			MakeBlock(6000f),
			MakeBlock(6000f)); // total 12000 fits on the default page

		result.Should().HaveCount(3);
		// Short section takes 2 pages.
		result[0].Section.Should().BeSameAs(shortSection);
		result[0].Blocks.Should().HaveCount(2);
		result[1].Section.Should().BeSameAs(shortSection);
		result[1].Blocks.Should().ContainSingle();
		// Default body section fits on 1 page.
		result[2].Section.Should().BeSameAs(DefaultSection);
		result[2].Blocks.Should().HaveCount(2);
	}

	[Fact]
	public void PaginateDocument_LandscapeSection_UsesLandscapeDimensions()
	{
		// Landscape available height = 12240 - 1440 - 1440 = 9360
		var landscapeSection = new SectionInfo
		{
			PageWidth = 15840,
			PageHeight = 12240,
			Orientation = PageOrientation.Landscape
		};

		var result = Paginate(DefaultSection, MakeBlock(5000f), MakeBlock(5000f), MakeSectionBreak(landscapeSection), MakeBlock(5000f));

		result.Should().HaveCount(3);
		result[0].Section.PageWidth.Should().Be(15840);
		result[0].Section.Orientation.Should().Be(PageOrientation.Landscape);
		result[2].Section.Should().BeSameAs(DefaultSection);
	}

	[Fact]
	public void PaginateDocument_PerSectionDimensions_CarriedInLayoutPage()
	{
		var section1 = new SectionInfo { PageWidth = 9000, PageHeight = 10000 };

		var result = Paginate(DefaultSection, MakeBlock(1000f), MakeSectionBreak(section1), MakeBlock(1000f));

		result[0].Section.PageWidth.Should().Be(9000);
		result[0].Section.PageHeight.Should().Be(10000);
		result[1].Section.PageWidth.Should().Be(12240);
		result[1].Section.PageHeight.Should().Be(15840);
	}

	// --- Continuous breaks with column counts (step 3.2.4) ---

	[Fact]
	public void PaginateDocument_ContinuousBreakWithDifferentColumnCount_TracksColumnCount()
	{
		var twoColumnSection = new SectionInfo
		{
			BreakType = SectionBreakType.Continuous,
			ColumnCount = 2
		};

		var result = Paginate(twoColumnSection, MakeBlock(1000f), MakeSectionBreak(new SectionInfo { ColumnCount = 1 }), MakeBlock(1000f));

		result.Should().HaveCount(2);
		result[0].Section.ColumnCount.Should().Be(1);
		result[1].Section.ColumnCount.Should().Be(2);
	}

	[Fact]
	public void IdentifySections_ContinuousBreak_PreservesSectionBoundary()
	{
		var continuousSection = new SectionInfo
		{
			BreakType = SectionBreakType.Continuous,
			ColumnCount = 3
		};

		var sections = PageBuilder.IdentifySections([MakeBlock(1000f), MakeSectionBreak(continuousSection), MakeBlock(2000f)], DefaultSection);

		sections.Should().HaveCount(2);
		sections[0].Info.ColumnCount.Should().Be(3);
		sections[1].Info.ColumnCount.Should().Be(1);
	}

	// --- Per-section margins (step 3.2.3) ---

	[Fact]
	public void PaginateDocument_DifferentMargins_AffectsAvailableHeight()
	{
		// Large margins: available = 15840 - 5000 - 5000 = 5840
		var largeMarginSection = new SectionInfo { MarginTop = 5000, MarginBottom = 5000 };

		var result = Paginate(
			DefaultSection,
			MakeBlock(3000f),
			MakeBlock(3000f), // overflows 5840
			MakeSectionBreak(largeMarginSection),
			MakeBlock(3000f),
			MakeBlock(3000f)); // fits on default page

		result.Should().HaveCount(3);
		result[0].Section.MarginTop.Should().Be(5000);
		result[0].Blocks.Should().ContainSingle();
		result[1].Section.MarginTop.Should().Be(5000);
		result[1].Blocks.Should().ContainSingle();
		// Both blocks fit on default page.
		result[2].Blocks.Should().HaveCount(2);
	}

	[Fact]
	public void PaginateDocument_ZeroMargins_MaximisesAvailableHeight()
	{
		// Zero margins: available = 15840
		var zeroMarginSection = new SectionInfo { MarginTop = 0, MarginBottom = 0 };

		var result = Paginate(
			DefaultSection,
			MakeBlock(7000f),
			MakeBlock(7000f), // fits 15840
			MakeSectionBreak(zeroMarginSection),
			MakeBlock(7000f),
			MakeBlock(7000f)); // overflows default 12960

		result.Should().HaveCount(3);
		result[0].Blocks.Should().HaveCount(2); // both fit in zero-margin section
		result[1].Blocks.Should().ContainSingle();
		result[2].Blocks.Should().ContainSingle();
	}

	[Fact]
	public void Paginate_MarginsCarriedInSectionInfo()
	{
		var section = new SectionInfo
		{
			MarginTop = 2000,
			MarginBottom = 3000,
			MarginLeft = 1800,
			MarginRight = 1800,
			MarginGutter = 720
		};

		var result = PageBuilder.Paginate([MakeBlock(1000f)], section);

		result.Should().ContainSingle();
		result[0].Section.MarginLeft.Should().Be(1800);
		result[0].Section.MarginRight.Should().Be(1800);
		result[0].Section.MarginGutter.Should().Be(720);
	}
}
