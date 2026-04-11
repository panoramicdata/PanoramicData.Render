namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

/// <summary>
/// Comprehensive tests verifying column flow mechanics, Y-position accuracy,
/// break position handling, and column geometry. Covers step 7.4.7.
/// </summary>
public sealed class ColumnFlowTests
{
	private static readonly SectionInfo TwoColumnSection = new() { ColumnCount = 2 };

	private static readonly SectionInfo ThreeColumnSection = new() { ColumnCount = 3 };

	// Default section: PageHeight=15840, MarginTop=1440, MarginBottom=1440 → available=12960
	// 2-col equal: spacing=720, colWidth=(9360-720)/2=4320. Col0: X=1440, Col1: X=6480
	// 3-col equal: spacing=720, colWidth=(9360-1440)/3=2640. Col0: X=1440, Col1: X=4800, Col2: X=8160

	// --- Y-position accuracy ---

	[Fact]
	public void Paginate_TwoColumns_YTwipsAccumulatesAndResetsOnColumnAdvance()
	{
		// ForceColumnBreak prevents balancing, so placements are raw.
		// Blocks: A(2000) + B(3000) in col 0, then C(1000 + break) + D(1500) in col 1.
		var blocks = new[]
		{
			MakeBlock(2000f),
			MakeBlock(3000f),
			MakeBlock(1000f, forceColumnBreak: true),
			MakeBlock(1500f),
		};

		var result = PageBuilder.Paginate(blocks, TwoColumnSection);

		result.Should().ContainSingle();
		var page = result[0];
		page.BlockPlacements.Should().HaveCount(4);

		// Col 0: A at Y=1440, B at Y=1440+2000=3440
		page.BlockPlacements[0].ColumnIndex.Should().Be(0);
		page.BlockPlacements[0].YTwips.Should().Be(1440f);
		page.BlockPlacements[1].ColumnIndex.Should().Be(0);
		page.BlockPlacements[1].YTwips.Should().Be(3440f);

		// Col 1: Y resets — C at Y=1440, D at Y=1440+1000=2440
		page.BlockPlacements[2].ColumnIndex.Should().Be(1);
		page.BlockPlacements[2].YTwips.Should().Be(1440f);
		page.BlockPlacements[3].ColumnIndex.Should().Be(1);
		page.BlockPlacements[3].YTwips.Should().Be(2440f);

		// X positions match column geometry
		page.BlockPlacements[0].XTwips.Should().Be(1440f);
		page.BlockPlacements[1].XTwips.Should().Be(1440f);
		page.BlockPlacements[2].XTwips.Should().Be(6480f);
		page.BlockPlacements[3].XTwips.Should().Be(6480f);

		// Column widths
		page.BlockPlacements[0].ContentWidthTwips.Should().Be(4320f);
		page.BlockPlacements[2].ContentWidthTwips.Should().Be(4320f);
	}

	// --- Three-column flow ---

	[Fact]
	public void Paginate_ThreeEqualColumns_BalancesContentEvenly()
	{
		// 9 lines × 1000 = 9000 total. All fit in col 0 initially.
		// Balance: target = 9000/3 = 3000 → 3 lines per column.
		var blocks = new[]
		{
			MakeSplittableBlock(Enumerable.Repeat(1000f, 9).ToArray()),
		};

		var result = PageBuilder.Paginate(blocks, ThreeColumnSection);

		result.Should().ContainSingle();
		var page = result[0];
		page.Blocks.Should().HaveCount(3);
		page.BlockPlacements.Should().HaveCount(3);

		page.Blocks[0].LineHeights.Should().HaveCount(3);
		page.BlockPlacements[0].ColumnIndex.Should().Be(0);
		page.BlockPlacements[0].XTwips.Should().Be(1440f);

		page.Blocks[1].LineHeights.Should().HaveCount(3);
		page.BlockPlacements[1].ColumnIndex.Should().Be(1);
		page.BlockPlacements[1].XTwips.Should().Be(4800f);

		page.Blocks[2].LineHeights.Should().HaveCount(3);
		page.BlockPlacements[2].ColumnIndex.Should().Be(2);
		page.BlockPlacements[2].XTwips.Should().Be(8160f);
	}

	// --- Force page break in multi-column ---

	[Fact]
	public void Paginate_ForcePageBreakBefore_InTwoColumnSection_StartsNewPage()
	{
		var blocks = new[]
		{
			MakeBlock(1000f),
			MakeBlock(1000f, forcePageBreak: true),
		};

		var result = PageBuilder.Paginate(blocks, TwoColumnSection);

		result.Should().HaveCount(2);
		result[0].Blocks.Should().ContainSingle();
		result[0].BlockPlacements[0].ColumnIndex.Should().Be(0);
		result[0].PageNumber.Should().Be(1);
		result[1].Blocks.Should().ContainSingle();
		result[1].BlockPlacements[0].ColumnIndex.Should().Be(0);
		result[1].PageNumber.Should().Be(2);
	}

	// --- Force column break on empty column (no-op) ---

	[Fact]
	public void Paginate_ForceColumnBreakBefore_WhenColumnEmpty_RemainsInCurrentColumn()
	{
		var blocks = new[]
		{
			MakeBlock(1000f, forceColumnBreak: true),
		};

		var result = PageBuilder.Paginate(blocks, TwoColumnSection);

		result.Should().ContainSingle();
		result[0].Blocks.Should().ContainSingle();
		result[0].BlockPlacements[0].ColumnIndex.Should().Be(0);
	}

	// --- Oversized unsplittable block ---

	[Fact]
	public void Paginate_UnsplittableOversizedBlock_ForcePlacedInColumn()
	{
		// 14000 > available 12960 but no LineHeights so can't split.
		var blocks = new[]
		{
			MakeBlock(14000f),
		};

		var result = PageBuilder.Paginate(blocks, TwoColumnSection);

		result.Should().ContainSingle();
		result[0].Blocks.Should().ContainSingle();
		result[0].BlockPlacements[0].ColumnIndex.Should().Be(0);
		result[0].BlockPlacements[0].YTwips.Should().Be(1440f);
	}

	// --- Header height reduces column available height ---

	[Fact]
	public void Paginate_WithHeaderHeight_ReducesAvailableColumnSpace()
	{
		// With header=2000: contentTop = max(1440, 720+2000)=2720
		// Available = 15840 - 2720 - 1440 = 11680
		// 24 lines × 1000 = 24000. Without header: fits on 1 page (12+12).
		// With header: col 0 fits 11, col 1 fits 11 → 22, remainder 2 → page 2.
		var blocks = new[]
		{
			MakeSplittableBlock(Enumerable.Repeat(1000f, 24).ToArray()),
		};

		var withHeader = PageBuilder.Paginate(blocks, TwoColumnSection, headerHeight: 2000f);
		var withoutHeader = PageBuilder.Paginate(blocks, TwoColumnSection);

		withoutHeader.Should().ContainSingle("24 lines fit on 1 page without header");
		withHeader.Should().HaveCount(2, "header reduces available height, requiring 2 pages");
		withHeader[0].ContentTopTwips.Should().Be(2720f);
	}

	// --- Column geometry (ComputeColumnRegions) ---

	[Fact]
	public void ComputeColumnRegions_ThreeEqualColumns_CorrectXPositionsAndWidths()
	{
		var regions = PageBuilder.ComputeColumnRegions(ThreeColumnSection);

		regions.Should().HaveCount(3);

		// Col 0: X=1440, W=2640
		regions[0].XTwips.Should().Be(1440f);
		regions[0].WidthTwips.Should().Be(2640f);

		// Col 1: X=1440+2640+720=4800, W=2640
		regions[1].XTwips.Should().Be(4800f);
		regions[1].WidthTwips.Should().Be(2640f);

		// Col 2: X=4800+2640+720=8160, W=2640
		regions[2].XTwips.Should().Be(8160f);
		regions[2].WidthTwips.Should().Be(2640f);
	}

	// --- Natural overflow from last column starts new page ---

	[Fact]
	public void Paginate_NaturalOverflow_FromLastColumnToNewPage()
	{
		// 3 unsplittable blocks of 7000 each.
		// Col 0: A(7000). B(14000>12960) → overflow to col 1: B(7000).
		// C(14000>12960) → overflow → last column → new page: C in col 0.
		var blocks = new[]
		{
			MakeBlock(7000f),
			MakeBlock(7000f),
			MakeBlock(7000f),
		};

		var result = PageBuilder.Paginate(blocks, TwoColumnSection);

		result.Should().HaveCount(2);
		result[0].Blocks.Should().HaveCount(2);
		result[0].BlockPlacements[0].ColumnIndex.Should().Be(0);
		result[0].BlockPlacements[1].ColumnIndex.Should().Be(1);
		result[0].PageNumber.Should().Be(1);

		result[1].Blocks.Should().ContainSingle();
		result[1].BlockPlacements[0].ColumnIndex.Should().Be(0);
		result[1].PageNumber.Should().Be(2);
	}

	// --- Splittable block splits at column boundary ---

	[Fact]
	public void Paginate_SplittableBlock_SplitsAtColumnBoundary()
	{
		// 20 lines × 1000 = 20000. Available = 12960.
		// Col 0 fits 12 lines (12000 ≤ 12960). Remaining 8 → col 1.
		// ForceColumnBreak on trailing block creates page 2, preventing last-page balancing on page 1.
		var blocks = new[]
		{
			MakeSplittableBlock(Enumerable.Repeat(1000f, 20).ToArray()),
			MakeBlock(1000f, forceColumnBreak: true),
		};

		var result = PageBuilder.Paginate(blocks, TwoColumnSection);

		result.Should().HaveCount(2);

		// Page 1: raw split (not the last page, so no balancing)
		result[0].Blocks.Should().HaveCount(2);
		result[0].Blocks[0].LineHeights.Should().HaveCount(12);
		result[0].BlockPlacements[0].ColumnIndex.Should().Be(0);
		result[0].Blocks[1].LineHeights.Should().HaveCount(8);
		result[0].BlockPlacements[1].ColumnIndex.Should().Be(1);

		// Page 2: trailing block (balance skipped due to ForceColumnBreakBefore)
		result[1].Blocks.Should().ContainSingle();
		result[1].BlockPlacements[0].ColumnIndex.Should().Be(0);
	}

	// --- PaginateDocument section transition into multi-column ---

	[Fact]
	public void PaginateDocument_MultiColumnSection_ProducesColumnPlacements()
	{
		// Section 1 (2-col): splittable block with 4 lines → balanced 2+2.
		// Body section (single-col): one small block.
		var twoColSection = new SectionInfo { ColumnCount = 2 };
		var bodySection = new SectionInfo();
		var blocks = new[]
		{
			MakeSplittableBlock([1000f, 1000f, 1000f, 1000f]),
			MakeSectionBreak(twoColSection),
			MakeBlock(1000f),
		};

		var result = PageBuilder.PaginateDocument(blocks, bodySection);

		result.Should().HaveCount(2);

		// Section 1: 2-column balanced
		result[0].Section.ColumnCount.Should().Be(2);
		result[0].Blocks.Should().HaveCount(2);
		result[0].BlockPlacements.Should().HaveCount(2);
		result[0].BlockPlacements[0].ColumnIndex.Should().Be(0);
		result[0].BlockPlacements[1].ColumnIndex.Should().Be(1);

		// Body section: single column, no column placements
		result[1].Section.ColumnCount.Should().Be(1);
		result[1].Blocks.Should().ContainSingle();
	}

	// --- Three-column flow fills sequentially before page break ---

	[Fact]
	public void Paginate_ThreeColumns_NaturalOverflowFillsAllColumnsBeforeNewPage()
	{
		// 3 unsplittable blocks of 7000.
		// Col 0: A(7000). B(14000>12960) → col 1. C(14000>12960) → col 2.
		// All 3 fit one per column (7000 ≤ 12960 per column).
		var blocks = new[]
		{
			MakeBlock(7000f),
			MakeBlock(7000f),
			MakeBlock(7000f),
		};

		var result = PageBuilder.Paginate(blocks, ThreeColumnSection);

		result.Should().ContainSingle();
		result[0].Blocks.Should().HaveCount(3);
		result[0].BlockPlacements[0].ColumnIndex.Should().Be(0);
		result[0].BlockPlacements[1].ColumnIndex.Should().Be(1);
		result[0].BlockPlacements[2].ColumnIndex.Should().Be(2);
	}

	// --- Block split straddles column AND page boundary ---

	[Fact]
	public void Paginate_SplittableBlock_FillsBothColumnsAndOverflowsToNewPage()
	{
		// 30 lines × 1000 = 30000. Available per column = 12960.
		// Raw flow: col 0 = 12 lines (12000), col 1 = 12 lines (12000), page 2 col 0 = 6 lines.
		// Page 2 is balanced: 6/2 = 3 lines per column.
		var blocks = new[]
		{
			MakeSplittableBlock(Enumerable.Repeat(1000f, 30).ToArray()),
		};

		var result = PageBuilder.Paginate(blocks, TwoColumnSection);

		result.Should().HaveCount(2);

		// Page 1: 24 lines split 12+12 across columns
		result[0].Blocks.Should().HaveCount(2);
		result[0].Blocks[0].LineHeights.Should().HaveCount(12);
		result[0].Blocks[1].LineHeights.Should().HaveCount(12);

		// Page 2: 6 lines balanced 3+3
		result[1].Blocks.Should().HaveCount(2);
		result[1].Blocks[0].LineHeights.Should().HaveCount(3);
		result[1].Blocks[1].LineHeights.Should().HaveCount(3);

		// Total lines preserved
		var totalLines = result.SelectMany(p => p.Blocks)
			.Sum(b => b.LineHeights!.Count);
		totalLines.Should().Be(30);
	}

	// --- Helpers ---

	private static LayoutBlock MakeBlock(
		float heightTwips,
		bool forceColumnBreak = false,
		bool forcePageBreak = false)
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		return new LayoutBlock(para, heightTwips,
			ForceColumnBreakBefore: forceColumnBreak,
			ForcePageBreakBefore: forcePageBreak);
	}

	private static LayoutBlock MakeSplittableBlock(float[] lineHeights)
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var totalHeight = lineHeights.Sum();
		return new LayoutBlock(para, totalHeight, LineHeights: lineHeights, WidowOrphanControl: false);
	}

	private static LayoutBlock MakeSectionBreak(SectionInfo sectionInfo) =>
		new(new SectionBreakBlock { SectionInfo = sectionInfo }, 0f);
}
