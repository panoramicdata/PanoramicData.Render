namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;
using static PanoramicData.Render.Test.SectionPaginationBuilders;

/// <summary>
/// Integration tests verifying multi-section documents produce correct page sizes,
/// break positions, and section property propagation through the pipeline.
/// </summary>
public sealed class SectionIntegrationTests
{
	private static readonly SectionInfo DefaultSection = new();

	// --- Parsing integration: DocumentBlockParser produces SectionBreakBlocks ---

	[Fact]
	public void Parse_MultipleSections_ProducesSectionBreakBlocks()
	{
		using var stream = TestDocxBuilder.CreateDocxWithMultipleSections();
		using var doc = DocxDocument.Load(stream);

		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);

		// Should have: ParagraphBlock, SectionBreakBlock, ParagraphBlock
		blocks.Should().HaveCount(3);
		blocks[0].Should().BeOfType<ParagraphBlock>();
		blocks[1].Should().BeOfType<SectionBreakBlock>();
		blocks[2].Should().BeOfType<ParagraphBlock>();

		var sectionBreak = (SectionBreakBlock)blocks[1];
		sectionBreak.SectionInfo.PageWidth.Should().Be(16838);
		sectionBreak.SectionInfo.Orientation.Should().Be(PageOrientation.Landscape);
	}

	// --- Summary of all section features working together ---

	[Fact]
	public void ThreeSections_DifferentDimensions_CorrectPagination()
	{
		// Short section available = 2120, landscape available = 9360, default available = 12960
		var shortSection = new SectionInfo { PageHeight = 5000 };
		var landscapeSection = new SectionInfo
		{
			PageWidth = 15840,
			PageHeight = 12240,
			Orientation = PageOrientation.Landscape
		};

		var result = Paginate(
			DefaultSection,
			MakeBlock(1000f),
			MakeBlock(1500f), // 2500 > 2120, overflows short page
			MakeSectionBreak(shortSection),
			MakeBlock(5000f),
			MakeBlock(5000f), // 10000 > 9360, overflows landscape page
			MakeSectionBreak(landscapeSection),
			MakeBlock(6000f),
			MakeBlock(6000f)); // 12000 < 12960, fits on default page

		AssertPageNumbers(result, 1, 2, 3, 4, 5);
		result[0].Section.Should().BeSameAs(shortSection);
		result[1].Section.Should().BeSameAs(shortSection);
		result[2].Section.Should().BeSameAs(landscapeSection);
		result[3].Section.Should().BeSameAs(landscapeSection);
		result[4].Section.Should().BeSameAs(DefaultSection);
		result[4].Blocks.Should().HaveCount(2);
	}

	[Fact]
	public void OddPageBreak_WithSplitting_CorrectPageNumbers()
	{
		// Page 1 is the first section; the odd-page start inserts blank page 2; the 15000 block splits over pages 3 and 4.
		var bodySection = new SectionInfo { BreakType = SectionBreakType.OddPage };

		var result = Paginate(
			bodySection,
			MakeBlock(1000f),
			MakeSectionBreak(new SectionInfo()),
			MakeSplittableBlock([5000f, 5000f, 5000f]));

		AssertPageNumbers(result, 1, 2, 3, 4);
		result[0].Blocks.Should().ContainSingle();
		result[1].Blocks.Should().BeEmpty(); // blank page
	}

	[Fact]
	public void EvenPageBreak_WithKeepNext_CorrectBehavior()
	{
		// First section: 2 pages. Next page (3) is odd, so insert blank page 3 and start on 4.
		var bodySection = new SectionInfo { BreakType = SectionBreakType.EvenPage };

		var result = Paginate(
			bodySection,
			MakeBlock(7000f),
			MakeBlock(7000f),
			MakeSectionBreak(new SectionInfo()),
			MakeBlock(5000f, keepWithNext: true),
			MakeBlock(5000f, keepWithNext: true),
			MakeBlock(5000f)); // 15000 > 12960, keepNext pulls back

		result.Select(page => page.PageNumber).Take(4).Should().Equal(1, 2, 3, 4);
		result[2].Blocks.Should().BeEmpty();
	}

	[Fact]
	public void MultipleSections_DifferentMargins_CorrectAvailableHeight()
	{
		// Large margins: available = 15840 - 4000 - 4000 = 7840. Body: 12960.
		var largeMarginSection = new SectionInfo { MarginTop = 4000, MarginBottom = 4000 };

		var result = Paginate(
			DefaultSection,
			MakeBlock(4000f),
			MakeBlock(4000f), // 8000 > 7840, overflows
			MakeSectionBreak(largeMarginSection),
			MakeBlock(6000f),
			MakeBlock(6000f)); // 12000 < 12960, fits

		result.Should().HaveCount(3);
		result[0].Blocks.Should().ContainSingle();
		result[1].Blocks.Should().ContainSingle();
		result[2].Blocks.Should().HaveCount(2);
	}

	[Fact]
	public void SectionWithColumnCount_CarriedThroughPagination()
	{
		var twoColSection = new SectionInfo
		{
			BreakType = SectionBreakType.Continuous,
			ColumnCount = 2,
			LineNumbering = new LineNumberingInfo(CountBy: 5, Start: 1)
		};

		var result = Paginate(DefaultSection, MakeBlock(1000f), MakeSectionBreak(twoColSection), MakeBlock(1000f));

		result.Should().HaveCount(2);
		result[0].Section.ColumnCount.Should().Be(2);
		result[0].Section.LineNumbering.Should().NotBeNull();
		result[0].Section.LineNumbering!.Value.CountBy.Should().Be(5);
		result[1].Section.ColumnCount.Should().Be(1);
		result[1].Section.LineNumbering.Should().BeNull();
	}

	[Fact]
	public void EmptySection_ProducesNoPages()
	{
		// First section has content. Second section (body) has no content.
		var result = Paginate(DefaultSection, MakeBlock(1000f), MakeSectionBreak(new SectionInfo()));

		AssertPageNumbers(result, 1);
	}

	[Fact]
	public void ManySmallSections_PageNumbersContinue()
	{
		var sectionInfo = new SectionInfo();

		var result = Paginate(
			DefaultSection,
			MakeBlock(1000f),
			MakeSectionBreak(sectionInfo),
			MakeBlock(1000f),
			MakeSectionBreak(sectionInfo),
			MakeBlock(1000f),
			MakeSectionBreak(sectionInfo),
			MakeBlock(1000f));

		AssertPageNumbers(result, 1, 2, 3, 4);
	}
}
