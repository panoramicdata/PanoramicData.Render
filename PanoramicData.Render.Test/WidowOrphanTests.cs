namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed class WidowOrphanTests
{
	private static readonly SectionInfo DefaultSection = new();

	// --- TrySplitBlock widow/orphan constraint tests ---

	[Fact]
	public void TrySplitBlock_WithWidowControl_OrphanViolation_ReturnsNull()
	{
		// 3 lines of 5000 each. Available space = 5500 → natural split at 1 line.
		// Orphan: 1 < 2 → can't split.
		var block = MakeSplittableBlock(0f, 0f, [5000f, 5000f, 5000f], widowControl: true);

		var result = PageBuilder.TrySplitBlock(block, 5500f);

		result.Should().BeNull();
	}

	[Fact]
	public void TrySplitBlock_WithWidowControl_WidowViolation_PullsBackLines()
	{
		// 5 lines of 1000 each. Available = 4500 → natural split at 4 lines.
		// Remaining = 1 < 2 → pull back: linesFitting = 5 - 2 = 3. Orphan 3 ≥ 2 → OK.
		var block = MakeSplittableBlock(0f, 0f, [1000f, 1000f, 1000f, 1000f, 1000f], widowControl: true);

		var result = PageBuilder.TrySplitBlock(block, 4500f);

		result.Should().NotBeNull();
		result!.Value.First.LineHeights.Should().HaveCount(3);
		result.Value.Second.LineHeights.Should().HaveCount(2);
	}

	[Fact]
	public void TrySplitBlock_WithWidowControl_BothConstraintsSatisfied()
	{
		// 4 lines of 1000. Available = 2500 → natural split at 2. Remaining = 2. Both ≥ 2 → OK.
		var block = MakeSplittableBlock(0f, 0f, [1000f, 1000f, 1000f, 1000f], widowControl: true);

		var result = PageBuilder.TrySplitBlock(block, 2500f);

		result.Should().NotBeNull();
		result!.Value.First.LineHeights.Should().HaveCount(2);
		result.Value.Second.LineHeights.Should().HaveCount(2);
	}

	[Fact]
	public void TrySplitBlock_WithWidowControl_TooFewLines_ReturnsNull()
	{
		// 3 lines. Min = 2 on each side → need 4. Can't split.
		var block = MakeSplittableBlock(0f, 0f, [1000f, 1000f, 1000f], widowControl: true);

		// Available lets 2 fit, remaining = 1 < 2. Pull back: linesFitting = 3-2=1. Orphan: 1 < 2 → null.
		var result = PageBuilder.TrySplitBlock(block, 2500f);

		result.Should().BeNull();
	}

	[Fact]
	public void TrySplitBlock_WithoutWidowControl_SplitsFreely()
	{
		// Same setup as orphan violation test, but widowControl disabled.
		var block = MakeSplittableBlock(0f, 0f, [5000f, 5000f, 5000f], widowControl: false);

		var result = PageBuilder.TrySplitBlock(block, 5500f);

		result.Should().NotBeNull();
		result!.Value.First.LineHeights.Should().HaveCount(1);
		result.Value.Second.LineHeights.Should().HaveCount(2);
	}

	[Fact]
	public void TrySplitBlock_WidowControl_PropagatedToSplitParts()
	{
		var block = MakeSplittableBlock(0f, 0f, [1000f, 1000f, 1000f, 1000f], widowControl: true);

		var result = PageBuilder.TrySplitBlock(block, 2500f);

		result.Should().NotBeNull();
		result!.Value.First.WidowOrphanControl.Should().BeTrue();
		result.Value.Second.WidowOrphanControl.Should().BeTrue();
	}

	[Fact]
	public void TrySplitBlock_WidowControlDisabled_PropagatedToSplitParts()
	{
		var block = MakeSplittableBlock(0f, 0f, [1000f, 1000f, 1000f, 1000f], widowControl: false);

		var result = PageBuilder.TrySplitBlock(block, 2500f);

		result.Should().NotBeNull();
		result!.Value.First.WidowOrphanControl.Should().BeFalse();
		result.Value.Second.WidowOrphanControl.Should().BeFalse();
	}

	// --- Paginate-level widow/orphan tests ---

	[Fact]
	public void Paginate_WidowControl_PreventsOrphanOnCurrentPage()
	{
		// First block: 12000. Remaining: 960 twips. Second block: 3 lines of 500.
		// Natural split: 1 line (500 ≤ 960). But orphan: 1 < 2 → can't split → move whole block to page 2.
		var blocks = new[]
		{
			MakeBlock(12000f),
			MakeSplittableBlock(0f, 0f, [500f, 500f, 500f], widowControl: true),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().HaveCount(2);
		result[0].Blocks.Should().ContainSingle(); // only the first block
		result[1].Blocks.Should().ContainSingle(); // whole second block, unsplit
		result[1].Blocks[0].LineHeights.Should().HaveCount(3);
	}

	[Fact]
	public void Paginate_WidowControlDisabled_AllowsOrphan()
	{
		// Same setup but widowControl disabled → split freely.
		var blocks = new[]
		{
			MakeBlock(12000f),
			MakeSplittableBlock(0f, 0f, [500f, 500f, 500f], widowControl: false),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().HaveCount(2);
		result[0].Blocks.Should().HaveCount(2); // first block + partial second
		result[0].Blocks[1].LineHeights.Should().HaveCount(1);
		result[1].Blocks.Should().ContainSingle();
		result[1].Blocks[0].LineHeights.Should().HaveCount(2);
	}

	[Fact]
	public void LayoutBlock_WidowOrphanControl_DefaultIsTrue()
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var block = new LayoutBlock(para, 100f);

		block.WidowOrphanControl.Should().BeTrue();
	}

	[Fact]
	public void LayoutBlock_WidowOrphanControl_CanBeSetFalse()
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var block = new LayoutBlock(para, 100f, WidowOrphanControl: false);

		block.WidowOrphanControl.Should().BeFalse();
	}

	[Fact]
	public void DefaultWidowOrphanMinLines_IsTwo()
	{
		PageBuilder.DefaultWidowOrphanMinLines.Should().Be(2);
	}

	private static LayoutBlock MakeBlock(float heightTwips)
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		return new LayoutBlock(para, heightTwips);
	}

	private static LayoutBlock MakeSplittableBlock(
		float spaceBefore, float spaceAfter, float[] lineHeights, bool widowControl = true)
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var totalHeight = spaceBefore + lineHeights.Sum() + spaceAfter;
		return new LayoutBlock(para, totalHeight, spaceBefore, spaceAfter, lineHeights,
			WidowOrphanControl: widowControl);
	}
}
