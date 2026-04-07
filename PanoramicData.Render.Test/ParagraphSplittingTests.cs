using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

namespace PanoramicData.Render.Test;

public sealed class ParagraphSplittingTests
{
	private static readonly SectionInfo DefaultSection = new();

	/// <summary>
	/// Available content height for default section: 15840 - 1440 - 1440 = 12960 twips.
	/// </summary>
	private const float DefaultAvailableHeight = 12960f;

	// --- TrySplitBlock tests ---

	[Fact]
	public void TrySplitBlock_NoLineHeights_ReturnsNull()
	{
		var block = MakeBlock(5000f);

		var result = PageBuilder.TrySplitBlock(block, 3000f);

		result.Should().BeNull();
	}

	[Fact]
	public void TrySplitBlock_SingleLine_ReturnsNull()
	{
		var block = MakeSplittableBlock(0f, 0f, [5000f]);

		var result = PageBuilder.TrySplitBlock(block, 3000f);

		result.Should().BeNull();
	}

	[Fact]
	public void TrySplitBlock_AllLinesFit_ReturnsNull()
	{
		var block = MakeSplittableBlock(0f, 0f, [1000f, 1000f, 1000f]);

		var result = PageBuilder.TrySplitBlock(block, 5000f);

		result.Should().BeNull();
	}

	[Fact]
	public void TrySplitBlock_SplitsAtLineBoundary()
	{
		// 3 lines of 1000 each. Available space = 2500 → 2 lines fit.
		var block = MakeSplittableBlock(0f, 0f, [1000f, 1000f, 1000f]);

		var result = PageBuilder.TrySplitBlock(block, 2500f);

		result.Should().NotBeNull();
		result!.Value.First.LineHeights.Should().HaveCount(2);
		result.Value.Second.LineHeights.Should().HaveCount(1);
	}

	[Fact]
	public void TrySplitBlock_FirstPartHeight_IncludesSpaceBefore()
	{
		var block = MakeSplittableBlock(100f, 50f, [1000f, 1000f, 1000f]);

		// Available: 1500 → SpaceBefore(100) + line(1000) = 1100, + line(1000) = 2100 > 1500.
		// So 1 line fits.
		var result = PageBuilder.TrySplitBlock(block, 1500f);

		result.Should().NotBeNull();
		result!.Value.First.HeightTwips.Should().Be(1100f); // 100 + 1000
		result.Value.First.SpaceBefore.Should().Be(100f);
		result.Value.First.SpaceAfter.Should().Be(0f);
	}

	[Fact]
	public void TrySplitBlock_SecondPartHeight_IncludesSpaceAfter()
	{
		var block = MakeSplittableBlock(100f, 50f, [1000f, 1000f, 1000f]);

		var result = PageBuilder.TrySplitBlock(block, 1500f);

		result.Should().NotBeNull();
		result!.Value.Second.HeightTwips.Should().Be(2050f); // 1000 + 1000 + 50
		result.Value.Second.SpaceBefore.Should().Be(0f);
		result.Value.Second.SpaceAfter.Should().Be(50f);
	}

	[Fact]
	public void TrySplitBlock_BothPartsReferSameBlock()
	{
		var block = MakeSplittableBlock(0f, 0f, [1000f, 1000f]);

		var result = PageBuilder.TrySplitBlock(block, 1500f);

		result.Should().NotBeNull();
		result!.Value.First.Block.Should().BeSameAs(block.Block);
		result.Value.Second.Block.Should().BeSameAs(block.Block);
	}

	[Fact]
	public void TrySplitBlock_FirstLineExceedsSpace_StillPlacesOneLine()
	{
		// First line alone exceeds available space → it still gets placed (at least 1 line).
		var block = MakeSplittableBlock(0f, 0f, [5000f, 1000f]);

		var result = PageBuilder.TrySplitBlock(block, 3000f);

		result.Should().NotBeNull();
		result!.Value.First.LineHeights.Should().HaveCount(1);
		result.Value.First.HeightTwips.Should().Be(5000f);
		result.Value.Second.LineHeights.Should().HaveCount(1);
		result.Value.Second.HeightTwips.Should().Be(1000f);
	}

	// --- Paginate with splitting tests ---

	[Fact]
	public void Paginate_SplittableParagraphStraddlesBoundary_SplitsAcrossPages()
	{
		// First block takes 10000 twips. Second block has 5 lines of 1000 = 5000 total.
		// Available: 12960. After first block: 2960 remaining → 2 lines fit.
		var blocks = new[]
		{
			MakeBlock(10000f),
			MakeSplittableBlock(0f, 0f, [1000f, 1000f, 1000f, 1000f, 1000f]),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().HaveCount(2);
		result[0].Blocks.Should().HaveCount(2); // original + first part of split
		result[0].Blocks[1].LineHeights.Should().HaveCount(2);
		result[1].Blocks.Should().ContainSingle();
		result[1].Blocks[0].LineHeights.Should().HaveCount(3);
	}

	[Fact]
	public void Paginate_AtomicBlockTooLarge_MovedToNextPage()
	{
		// First block fills 10000, second is 5000 atomic → doesn't fit (15000 > 12960).
		// Since no LineHeights, it moves to a new page.
		var blocks = new[]
		{
			MakeBlock(10000f),
			MakeBlock(5000f),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().HaveCount(2);
		result[0].Blocks.Should().ContainSingle();
		result[1].Blocks.Should().ContainSingle();
	}

	[Fact]
	public void Paginate_SplittableAcrossMultiplePages()
	{
		// One big paragraph: 30 lines of 1000 each = 30000 twips.
		// Available: 12960 per page.
		// Page 1: 12 lines (12000 ≤ 12960), 13th would be 13000 > 12960.
		// Page 2: 12 lines.
		// Page 3: 6 lines.
		var lineHeights = Enumerable.Range(0, 30).Select(_ => 1000f).ToArray();
		var blocks = new[] { MakeSplittableBlock(0f, 0f, lineHeights) };

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().HaveCount(3);
		result[0].Blocks[0].LineHeights.Should().HaveCount(12);
		result[1].Blocks[0].LineHeights.Should().HaveCount(12);
		result[2].Blocks[0].LineHeights.Should().HaveCount(6);
	}

	[Fact]
	public void Paginate_SplitPreservesSpaceBefore()
	{
		// Block with SpaceBefore=200, 10 lines of 1400 = 14200 total.
		// Available: 12960. SpaceBefore(200) + 9 lines(12600) = 12800 ≤ 12960, 10th = 14200 > 12960.
		// First part: SpaceBefore(200) + 9 lines.
		var blocks = new[] { MakeSplittableBlock(200f, 0f, Enumerable.Repeat(1400f, 10).ToArray()) };

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().HaveCount(2);
		result[0].Blocks[0].SpaceBefore.Should().Be(200f);
		result[0].Blocks[0].SpaceAfter.Should().Be(0f);
		result[0].Blocks[0].LineHeights.Should().HaveCount(9);
	}

	[Fact]
	public void Paginate_SplitPreservesSpaceAfter()
	{
		var blocks = new[] { MakeSplittableBlock(200f, 300f, Enumerable.Repeat(1400f, 10).ToArray()) };

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().HaveCount(2);
		result[1].Blocks[0].SpaceBefore.Should().Be(0f);
		result[1].Blocks[0].SpaceAfter.Should().Be(300f);
	}

	[Fact]
	public void Paginate_SplittableBlockFollowedByMore_CorrectLayout()
	{
		// Block A: 10000 twips. Block B: splittable 5 lines × 1000 = 5000. Block C: 500 twips.
		// Page 1: A(10000) + B-first(2 lines = 2000) = 12000.
		// Page 2: B-rest(3 lines = 3000) + C(500) = 3500.
		var blocks = new[]
		{
			MakeBlock(10000f),
			MakeSplittableBlock(0f, 0f, [1000f, 1000f, 1000f, 1000f, 1000f]),
			MakeBlock(500f),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().HaveCount(2);
		result[0].Blocks.Should().HaveCount(2);
		result[1].Blocks.Should().HaveCount(2);
	}

	[Fact]
	public void Paginate_PageNumbersContinueAcrossSplits()
	{
		var lineHeights = Enumerable.Range(0, 30).Select(_ => 1000f).ToArray();
		var blocks = new[] { MakeSplittableBlock(0f, 0f, lineHeights) };

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		for (var i = 0; i < result.Count; i++)
		{
			result[i].PageNumber.Should().Be(i + 1);
		}
	}

	private static LayoutBlock MakeBlock(float heightTwips)
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		return new LayoutBlock(para, heightTwips);
	}

	private static LayoutBlock MakeSplittableBlock(float spaceBefore, float spaceAfter, float[] lineHeights)
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var totalHeight = spaceBefore + lineHeights.Sum() + spaceAfter;
		return new LayoutBlock(para, totalHeight, spaceBefore, spaceAfter, lineHeights,
			WidowOrphanControl: false);
	}
}
