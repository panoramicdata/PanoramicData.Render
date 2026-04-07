namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

/// <summary>
/// Cross-cutting integration tests verifying page break positions for documents
/// with known pagination behavior, exercising multiple PageBuilder features together.
/// </summary>
public sealed class PaginationIntegrationTests
{
	private static readonly SectionInfo DefaultSection = new();

	/// <summary>
	/// Default available height: 15840 - 1440 - 1440 = 12960 twips.
	/// </summary>
	private const float AvailableHeight = 12960f;

	[Fact]
	public void MixedBlocks_AtomicAndSplittable_PaginateCorrectly()
	{
		// Atomic block: 6000. Splittable block: 4 lines × 2000 = 8000.
		// Total: 14000 > 12960. Split: 6000 + 3 lines (6000) = 12000 ≤ 12960. 4th = 14000 > 12960.
		// Page 1: [atomic(6000), split-first(3 lines)]. Page 2: [split-rest(1 line)].
		var blocks = new[]
		{
			MakeBlock(6000f),
			MakeSplittableBlock(0f, 0f, [2000f, 2000f, 2000f, 2000f]),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().HaveCount(2);
		result[0].Blocks.Should().HaveCount(2);
		result[0].Blocks[1].LineHeights.Should().HaveCount(3);
		result[1].Blocks.Should().ContainSingle();
		result[1].Blocks[0].LineHeights.Should().HaveCount(1);
	}

	[Fact]
	public void ForceBreak_WithKeepNext_BreakWins()
	{
		// A(keepNext): 5000. B(forceBreak): 5000. C: 1000.
		// Force break on B → new page before B. A stays on page 1 alone.
		var blocks = new[]
		{
			MakeBlock(5000f, keepWithNext: true),
			MakeBlock(5000f, forceBreak: true),
			MakeBlock(1000f),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().HaveCount(2);
		result[0].Blocks.Should().ContainSingle();
		result[1].Blocks.Should().HaveCount(2);
	}

	[Fact]
	public void KeepNext_WithWidowOrphan_BothRespected()
	{
		// A: 10000. B(keepNext): 2000. C: 3 lines × 500 = 1500.
		// A+B = 12000. +C = 13500 > 12960.
		// C doesn't fit. B has keepNext → pull back B.
		// Page 1: [A]. Page 2: [B, C].
		var blocks = new[]
		{
			MakeBlock(10000f),
			MakeBlock(2000f, keepWithNext: true),
			MakeSplittableBlock(0f, 0f, [500f, 500f, 500f]),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().HaveCount(2);
		result[0].Blocks.Should().ContainSingle();
		result[1].Blocks.Should().HaveCount(2);
	}

	[Fact]
	public void KeepLines_PreventsSplitEvenWhenSplittable()
	{
		// A: 10000. B(keepLines, 4 lines × 1000): doesn't fit (14000 > 12960).
		// Can't split B → move to page 2.
		var blocks = new[]
		{
			MakeBlock(10000f),
			MakeSplittableBlock(0f, 0f, [1000f, 1000f, 1000f, 1000f], keepLines: true),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().HaveCount(2);
		result[0].Blocks.Should().ContainSingle();
		result[1].Blocks.Should().ContainSingle();
		result[1].Blocks[0].LineHeights.Should().HaveCount(4);
	}

	[Fact]
	public void SpaceBeforeAfter_SplitCorrectly()
	{
		// Block with SpaceBefore=500, 10 lines × 1200, SpaceAfter=300. Total: 12800.
		// Fits on one page (12800 ≤ 12960).
		var blocks = new[]
		{
			MakeSplittableBlock(500f, 300f, Enumerable.Repeat(1200f, 10).ToArray()),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().ContainSingle();
	}

	[Fact]
	public void MultipleFeatures_ComplexDocument()
	{
		// Simulates a document with:
		// - Heading (keepNext, keepLines): 800 twips
		// - Body paragraph (splittable): 20 lines × 600 = 12000
		// - Another heading (keepNext, keepLines): 800
		// - Short body: 2000
		//
		// Page 1: Heading(800) + partial body = 12160 (12960-800 = 12160 → 20 lines, 12000 ≤ 12160).
		// Everything fits! = 800 + 12000 = 12800 ≤ 12960. Then heading = 13600 > 12960.
		// Heading(keepNext) doesn't fit → page 2 with heading + short body.
		var blocks = new[]
		{
			MakeBlock(800f, keepWithNext: true, keepLines: true),
			MakeSplittableBlock(0f, 0f, Enumerable.Repeat(600f, 20).ToArray()),
			MakeBlock(800f, keepWithNext: true, keepLines: true),
			MakeBlock(2000f),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().HaveCount(2);
		result[0].Blocks.Should().HaveCount(2); // heading + body
		result[1].Blocks.Should().HaveCount(2); // heading + short body
	}

	[Fact]
	public void EmptyDocument_NoPages()
	{
		var result = PageBuilder.Paginate([], DefaultSection);

		result.Should().BeEmpty();
	}

	[Fact]
	public void SingleSmallParagraph_OnePage()
	{
		var blocks = new[] { MakeBlock(500f) };

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().ContainSingle();
		result[0].PageNumber.Should().Be(1);
	}

	[Fact]
	public void CustomMargins_ReduceAvailableSpace()
	{
		// Margins: top=3000, bottom=3000 → available = 15840 - 6000 = 9840.
		var section = new SectionInfo { MarginTop = 3000, MarginBottom = 3000 };
		var blocks = new[]
		{
			MakeBlock(5000f),
			MakeBlock(5000f), // total 10000 > 9840
		};

		var result = PageBuilder.Paginate(blocks, section);

		result.Should().HaveCount(2);
	}

	[Fact]
	public void AllPageNumbersSequentialAcrossAllFeatures()
	{
		var blocks = new[]
		{
			MakeBlock(6000f),
			MakeBlock(6000f, keepWithNext: true),
			MakeBlock(6000f, forceBreak: true),
			MakeSplittableBlock(0f, 0f, Enumerable.Repeat(2000f, 10).ToArray()),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		for (var i = 0; i < result.Count; i++)
		{
			result[i].PageNumber.Should().Be(i + 1);
		}
	}

	[Fact]
	public void AllPagesReferenceCorrectSection()
	{
		var section = new SectionInfo { PageWidth = 9000 };
		var blocks = new[]
		{
			MakeBlock(7000f),
			MakeBlock(7000f),
			MakeBlock(7000f),
		};

		var result = PageBuilder.Paginate(blocks, section);

		foreach (var page in result)
		{
			page.Section.Should().BeSameAs(section);
		}
	}

	private static LayoutBlock MakeBlock(
		float heightTwips,
		bool keepWithNext = false,
		bool keepLines = false,
		bool forceBreak = false)
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		return new LayoutBlock(para, heightTwips,
			ForcePageBreakBefore: forceBreak,
			KeepWithNext: keepWithNext,
			KeepLinesTogether: keepLines);
	}

	private static LayoutBlock MakeSplittableBlock(
		float spaceBefore, float spaceAfter, float[] lineHeights,
		bool keepLines = false)
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var totalHeight = spaceBefore + lineHeights.Sum() + spaceAfter;
		return new LayoutBlock(para, totalHeight, spaceBefore, spaceAfter, lineHeights,
			KeepLinesTogether: keepLines, WidowOrphanControl: false);
	}
}
