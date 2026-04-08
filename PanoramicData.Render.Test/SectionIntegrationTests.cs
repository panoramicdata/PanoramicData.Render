namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

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
		// Section 1: short page (5000 tall, available = 5000 - 1440 - 1440 = 2120)
		var shortSection = new SectionInfo { PageHeight = 5000 };
		// Section 2: landscape (12240 tall, available = 12240 - 1440 - 1440 = 9360)
		var landscapeSection = new SectionInfo
		{
			PageWidth = 15840,
			PageHeight = 12240,
			Orientation = PageOrientation.Landscape
		};
		// Body section: default (15840 tall, available = 12960)
		var blocks = new[]
		{
			MakeBlock(1000f),
			MakeBlock(1500f), // 2500 > 2120, overflows short page
			MakeSectionBreak(shortSection),
			MakeBlock(5000f),
			MakeBlock(5000f), // 10000 > 9360, overflows landscape page
			MakeSectionBreak(landscapeSection),
			MakeBlock(6000f),
			MakeBlock(6000f), // 12000 < 12960, fits on default page
		};

		var result = PageBuilder.PaginateDocument(blocks, DefaultSection);

		// Section 1: 2 pages (block 1 on page 1, block 2 on page 2)
		// Section 2: 2 pages (block 3 on page 3, block 4 on page 4)
		// Body: 1 page (blocks 5-6 on page 5)
		result.Should().HaveCount(5);
		result[0].Section.Should().BeSameAs(shortSection);
		result[0].PageNumber.Should().Be(1);
		result[1].Section.Should().BeSameAs(shortSection);
		result[1].PageNumber.Should().Be(2);
		result[2].Section.Should().BeSameAs(landscapeSection);
		result[2].PageNumber.Should().Be(3);
		result[3].Section.Should().BeSameAs(landscapeSection);
		result[3].PageNumber.Should().Be(4);
		result[4].Section.Should().BeSameAs(DefaultSection);
		result[4].PageNumber.Should().Be(5);
		result[4].Blocks.Should().HaveCount(2);
	}

	[Fact]
	public void OddPageBreak_WithSplitting_CorrectPageNumbers()
	{
		// First section fills 1 page. Body section wants odd page start.
		// Since next page would be 2 (even), a blank page is inserted.
		// Then the body content is large enough to need splitting.
		var firstSection = new SectionInfo();
		var bodySection = new SectionInfo { BreakType = SectionBreakType.OddPage };
		var blocks = new[]
		{
			MakeBlock(1000f), // page 1
			MakeSectionBreak(firstSection),
			MakeSplittableBlock(0f, 0f, [5000f, 5000f, 5000f]), // 15000 total, needs splitting
		};

		var result = PageBuilder.PaginateDocument(blocks, bodySection);

		// Page 1: first section. Page 2: blank. Page 3: split-first (2 lines = 10000).
		// Page 4: split-rest (1 line = 5000).
		result.Should().HaveCount(4);
		result[0].PageNumber.Should().Be(1);
		result[0].Blocks.Should().ContainSingle();
		result[1].PageNumber.Should().Be(2);
		result[1].Blocks.Should().BeEmpty(); // blank page
		result[2].PageNumber.Should().Be(3);
		result[3].PageNumber.Should().Be(4);
	}

	[Fact]
	public void EvenPageBreak_WithKeepNext_CorrectBehavior()
	{
		// First section: 2 pages. Body section wants even page start.
		// Next page (3) is odd → insert blank page 3, start on 4.
		var firstSection = new SectionInfo();
		var bodySection = new SectionInfo { BreakType = SectionBreakType.EvenPage };
		var blocks = new[]
		{
			MakeBlock(7000f),
			MakeBlock(7000f), // overflows to page 2
			MakeSectionBreak(firstSection),
			MakeBlock(5000f, keepWithNext: true),
			MakeBlock(5000f, keepWithNext: true),
			MakeBlock(5000f), // 15000 > 12960, keepNext pulls back
		};

		var result = PageBuilder.PaginateDocument(blocks, bodySection);

		// Pages 1-2: first section. Page 3: blank. Page 4+: body.
		result[0].PageNumber.Should().Be(1);
		result[1].PageNumber.Should().Be(2);
		result[2].PageNumber.Should().Be(3);
		result[2].Blocks.Should().BeEmpty();
		result[3].PageNumber.Should().Be(4);
	}

	[Fact]
	public void MultipleSections_DifferentMargins_CorrectAvailableHeight()
	{
		// Section 1: large margins (available = 15840 - 4000 - 4000 = 7840)
		var largeMarginSection = new SectionInfo { MarginTop = 4000, MarginBottom = 4000 };
		// Body: default margins (available = 12960)
		var blocks = new[]
		{
			MakeBlock(4000f),
			MakeBlock(4000f), // 8000 > 7840, overflows
			MakeSectionBreak(largeMarginSection),
			MakeBlock(6000f),
			MakeBlock(6000f), // 12000 < 12960, fits
		};

		var result = PageBuilder.PaginateDocument(blocks, DefaultSection);

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
		var blocks = new[]
		{
			MakeBlock(1000f),
			MakeSectionBreak(twoColSection),
			MakeBlock(1000f),
		};

		var result = PageBuilder.PaginateDocument(blocks, DefaultSection);

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
		var firstSection = new SectionInfo();
		var blocks = new[]
		{
			MakeBlock(1000f),
			MakeSectionBreak(firstSection),
		};

		var result = PageBuilder.PaginateDocument(blocks, DefaultSection);

		result.Should().ContainSingle();
		result[0].PageNumber.Should().Be(1);
	}

	[Fact]
	public void ManySmallSections_PageNumbersContinue()
	{
		var sectionInfo = new SectionInfo();
		var blocks = new[]
		{
			MakeBlock(1000f),
			MakeSectionBreak(sectionInfo),
			MakeBlock(1000f),
			MakeSectionBreak(sectionInfo),
			MakeBlock(1000f),
			MakeSectionBreak(sectionInfo),
			MakeBlock(1000f),
		};

		var result = PageBuilder.PaginateDocument(blocks, DefaultSection);

		result.Should().HaveCount(4);
		result[0].PageNumber.Should().Be(1);
		result[1].PageNumber.Should().Be(2);
		result[2].PageNumber.Should().Be(3);
		result[3].PageNumber.Should().Be(4);
	}

	private static LayoutBlock MakeBlock(float heightTwips, bool keepWithNext = false, bool forceBreak = false)
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		return new LayoutBlock(para, heightTwips, KeepWithNext: keepWithNext, ForcePageBreakBefore: forceBreak);
	}

	private static LayoutBlock MakeSplittableBlock(float spaceBefore, float spaceAfter, float[] lineHeights)
	{
		var totalHeight = spaceBefore + lineHeights.Sum() + spaceAfter;
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		return new LayoutBlock(para, totalHeight, spaceBefore, spaceAfter, lineHeights, WidowOrphanControl: false);
	}

	private static LayoutBlock MakeSectionBreak(SectionInfo sectionInfo) =>
		new(new SectionBreakBlock { SectionInfo = sectionInfo }, 0f);
}
