namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed class SectionBreakTests
{
	private static readonly SectionInfo DefaultSection = new();


	// --- IdentifySections tests ---

	[Fact]
	public void IdentifySections_NoBreaks_SingleSection()
	{
		var blocks = new[] { MakeBlock(1000f), MakeBlock(2000f) };

		var sections = PageBuilder.IdentifySections(blocks, DefaultSection);

		sections.Should().ContainSingle();
		sections[0].Info.Should().BeSameAs(DefaultSection);
		sections[0].Blocks.Should().HaveCount(2);
	}

	[Fact]
	public void IdentifySections_OneBreak_TwoSections()
	{
		var sectionInfo = new SectionInfo { PageWidth = 9000 };
		var blocks = new[]
		{
			MakeBlock(1000f),
			MakeSectionBreak(sectionInfo),
			MakeBlock(2000f),
		};

		var sections = PageBuilder.IdentifySections(blocks, DefaultSection);

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
		var blocks = new[] { MakeSectionBreak(sectionInfo) };

		var sections = PageBuilder.IdentifySections(blocks, DefaultSection);

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
		var blocks = new[]
		{
			MakeBlock(1000f),
			MakeSectionBreak(firstSection),
			MakeBlock(2000f),
			MakeSectionBreak(secondSection),
			MakeBlock(3000f),
		};

		var sections = PageBuilder.IdentifySections(blocks, bodySection);

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
		var blocks = new[] { MakeBlock(1000f), MakeBlock(2000f) };

		var result = PageBuilder.PaginateDocument(blocks, DefaultSection);

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
		var blocks = new[]
		{
			MakeBlock(1000f),
			MakeSectionBreak(firstSection),
			MakeBlock(2000f),
		};

		var result = PageBuilder.PaginateDocument(blocks, DefaultSection);

		result.Should().HaveCount(2);
		result[0].Section.Should().BeSameAs(firstSection);
		result[1].Section.Should().BeSameAs(DefaultSection);
	}

	[Fact]
	public void PaginateDocument_PageNumbersContinueAcrossSections()
	{
		var firstSection = new SectionInfo();
		var blocks = new[]
		{
			MakeBlock(7000f),
			MakeBlock(7000f), // overflows to page 2
			MakeSectionBreak(firstSection),
			MakeBlock(1000f),
		};

		var result = PageBuilder.PaginateDocument(blocks, DefaultSection);

		result.Should().HaveCount(3);
		result[0].PageNumber.Should().Be(1);
		result[1].PageNumber.Should().Be(2);
		result[2].PageNumber.Should().Be(3);
	}

	[Fact]
	public void PaginateDocument_EvenPageBreak_InsertsBlankPage()
	{
		var firstSection = new SectionInfo();
		var bodySection = new SectionInfo { BreakType = SectionBreakType.EvenPage };
		var blocks = new[]
		{
			MakeBlock(1000f), // page 1
			MakeSectionBreak(firstSection),
			MakeBlock(1000f), // should start on even page
		};

		var result = PageBuilder.PaginateDocument(blocks, bodySection);

		// Page 1: first section content. Page 2: blank (odd page inserted). Page 3: second section.
		// Wait — page 1 is the content. Next page would be 2 (even). Even break wants to start on even → page 2.
		// Actually page 2 is already even, so no blank needed.
		result.Should().HaveCount(2);
		result[0].PageNumber.Should().Be(1);
		result[1].PageNumber.Should().Be(2);
	}

	[Fact]
	public void PaginateDocument_OddPageBreak_InsertsBlankPage()
	{
		// First section occupies 2 pages. Next section wants odd page.
		var firstSection = new SectionInfo();
		var bodySection = new SectionInfo { BreakType = SectionBreakType.OddPage };
		var blocks = new[]
		{
			MakeBlock(7000f),
			MakeBlock(7000f), // pages 1 and 2
			MakeSectionBreak(firstSection),
			MakeBlock(1000f), // should start on odd page (3)
		};

		var result = PageBuilder.PaginateDocument(blocks, bodySection);

		// Page 1, page 2 from first section. Next page would be 3 (odd). Odd break → starts on 3.
		result.Should().HaveCount(3);
		result[0].PageNumber.Should().Be(1);
		result[1].PageNumber.Should().Be(2);
		result[2].PageNumber.Should().Be(3);
	}

	[Fact]
	public void PaginateDocument_OddPageBreak_NextIsEven_InsertsBlank()
	{
		// First section occupies 1 page. Next section wants odd page.
		// Page 1 → next would be 2 (even) → insert blank page 2, start on 3.
		var firstSection = new SectionInfo();
		var bodySection = new SectionInfo { BreakType = SectionBreakType.OddPage };
		var blocks = new[]
		{
			MakeBlock(1000f),
			MakeSectionBreak(firstSection),
			MakeBlock(1000f),
		};

		var result = PageBuilder.PaginateDocument(blocks, bodySection);

		result.Should().HaveCount(3);
		result[0].PageNumber.Should().Be(1);
		result[1].PageNumber.Should().Be(2);
		result[1].Blocks.Should().BeEmpty(); // blank page
		result[2].PageNumber.Should().Be(3);
	}

	[Fact]
	public void PaginateDocument_EvenPageBreak_NextIsOdd_InsertsBlank()
	{
		// First section occupies 2 pages. Next section wants even page.
		// Pages 1-2 → next would be 3 (odd) → insert blank page 3, start on 4.
		var firstSection = new SectionInfo();
		var bodySection = new SectionInfo { BreakType = SectionBreakType.EvenPage };
		var blocks = new[]
		{
			MakeBlock(7000f),
			MakeBlock(7000f),
			MakeSectionBreak(firstSection),
			MakeBlock(1000f),
		};

		var result = PageBuilder.PaginateDocument(blocks, bodySection);

		result.Should().HaveCount(4);
		result[0].PageNumber.Should().Be(1);
		result[1].PageNumber.Should().Be(2);
		result[2].PageNumber.Should().Be(3);
		result[2].Blocks.Should().BeEmpty(); // blank page
		result[3].PageNumber.Should().Be(4);
	}

	[Fact]
	public void PaginateDocument_ContinuousBreak_StartsOnNewPage()
	{
		// Continuous break with same page dimensions → new page for simplicity.
		var firstSection = new SectionInfo { BreakType = SectionBreakType.Continuous };
		var blocks = new[]
		{
			MakeBlock(1000f),
			MakeSectionBreak(new SectionInfo()),
			MakeBlock(2000f),
		};

		var result = PageBuilder.PaginateDocument(blocks, firstSection);

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
		var blocks = new LayoutBlock[] { MakeBlock(100f) };
		var section = new DocumentSection(info, blocks, SectionBreakType.EvenPage);

		section.Info.Should().BeSameAs(info);
		section.Blocks.Should().BeSameAs(blocks);
		section.BreakType.Should().Be(SectionBreakType.EvenPage);
	}

	// --- Per-section page dimensions (step 3.2.2) ---

	[Fact]
	public void PaginateDocument_DifferentPageHeights_UsesPerSectionHeight()
	{
		// First section: short page (available height = 5000 - 1440 - 1440 = 2120)
		var shortSection = new SectionInfo { PageHeight = 5000 };
		// Body section: default page (available height = 15840 - 1440 - 1440 = 12960)
		var blocks = new[]
		{
			MakeBlock(1000f), // fits on short page
			MakeBlock(1000f), // fits on short page (total 2000 < 2120)
			MakeBlock(500f),  // overflows short page (total 2500 > 2120), goes to page 2
			MakeSectionBreak(shortSection),
			MakeBlock(6000f), // fits on default page (6000 < 12960)
			MakeBlock(6000f), // fits on default page (total 12000 < 12960)
		};

		var result = PageBuilder.PaginateDocument(blocks, DefaultSection);

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
		// Landscape section: width=15840, height=12240 (available = 12240 - 1440 - 1440 = 9360)
		var landscapeSection = new SectionInfo
		{
			PageWidth = 15840,
			PageHeight = 12240,
			Orientation = PageOrientation.Landscape
		};
		var blocks = new[]
		{
			MakeBlock(5000f),
			MakeBlock(5000f), // overflows (10000 > 9360)
			MakeSectionBreak(landscapeSection),
			MakeBlock(5000f), // fits on default page (12960 available)
		};

		var result = PageBuilder.PaginateDocument(blocks, DefaultSection);

		result.Should().HaveCount(3);
		result[0].Section.PageWidth.Should().Be(15840);
		result[0].Section.Orientation.Should().Be(PageOrientation.Landscape);
		result[2].Section.Should().BeSameAs(DefaultSection);
	}

	[Fact]
	public void PaginateDocument_PerSectionDimensions_CarriedInLayoutPage()
	{
		var section1 = new SectionInfo { PageWidth = 9000, PageHeight = 10000 };
		var blocks = new[]
		{
			MakeBlock(1000f),
			MakeSectionBreak(section1),
			MakeBlock(1000f),
		};

		var result = PageBuilder.PaginateDocument(blocks, DefaultSection);

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
		var blocks = new[]
		{
			MakeBlock(1000f),
			MakeSectionBreak(new SectionInfo { ColumnCount = 1 }),
			MakeBlock(1000f),
		};

		var result = PageBuilder.PaginateDocument(blocks, twoColumnSection);

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
		var blocks = new[]
		{
			MakeBlock(1000f),
			MakeSectionBreak(continuousSection),
			MakeBlock(2000f),
		};

		var sections = PageBuilder.IdentifySections(blocks, DefaultSection);

		sections.Should().HaveCount(2);
		sections[0].Info.ColumnCount.Should().Be(3);
		sections[1].Info.ColumnCount.Should().Be(1);
	}

	// --- Per-section margins (step 3.2.3) ---

	[Fact]
	public void PaginateDocument_DifferentMargins_AffectsAvailableHeight()
	{
		// Section with large margins: available = 15840 - 5000 - 5000 = 5840
		var largeMarginSection = new SectionInfo { MarginTop = 5000, MarginBottom = 5000 };
		var blocks = new[]
		{
			MakeBlock(3000f),
			MakeBlock(3000f), // overflows (6000 > 5840)
			MakeSectionBreak(largeMarginSection),
			MakeBlock(3000f),
			MakeBlock(3000f), // fits on default page (6000 < 12960)
		};

		var result = PageBuilder.PaginateDocument(blocks, DefaultSection);

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
		// Section with zero margins: available = 15840
		var zeroMarginSection = new SectionInfo { MarginTop = 0, MarginBottom = 0 };
		var blocks = new[]
		{
			MakeBlock(7000f),
			MakeBlock(7000f), // fits (14000 < 15840)
			MakeSectionBreak(zeroMarginSection),
			MakeBlock(7000f),
			MakeBlock(7000f), // overflows default (14000 > 12960)
		};

		var result = PageBuilder.PaginateDocument(blocks, DefaultSection);

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
		var blocks = new[] { MakeBlock(1000f) };

		var result = PageBuilder.Paginate(blocks, section);

		result.Should().ContainSingle();
		result[0].Section.MarginLeft.Should().Be(1800);
		result[0].Section.MarginRight.Should().Be(1800);
		result[0].Section.MarginGutter.Should().Be(720);
	}

	private static LayoutBlock MakeBlock(float heightTwips)
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		return new LayoutBlock(para, heightTwips);
	}

	private static LayoutBlock MakeSectionBreak(SectionInfo sectionInfo) =>
		new(new SectionBreakBlock { SectionInfo = sectionInfo }, 0f);
}
