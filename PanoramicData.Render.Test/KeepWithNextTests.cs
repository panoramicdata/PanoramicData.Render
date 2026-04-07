namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed class KeepWithNextTests
{
	private static readonly SectionInfo DefaultSection = new();

	[Fact]
	public void Paginate_KeepWithNext_BothFit_StayOnSamePage()
	{
		var blocks = new[]
		{
			MakeBlock(5000f, keepWithNext: true),
			MakeBlock(5000f),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().ContainSingle();
		result[0].Blocks.Should().HaveCount(2);
	}

	[Fact]
	public void Paginate_KeepWithNext_NextDoesNotFit_BothMoveToNextPage()
	{
		// Block A (keepNext): 6000. Block B (keepNext): 6000. Block C: 4000.
		// A+B = 12000 ≤ 12960 → fit. Adding C = 16000 > 12960.
		// C doesn't fit. Tail has B (keepNext) → pull back B.
		// Page 1: [A]. Page 2: [B, C].
		var blocks = new[]
		{
			MakeBlock(6000f),
			MakeBlock(6000f, keepWithNext: true),
			MakeBlock(4000f),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().HaveCount(2);
		result[0].Blocks.Should().ContainSingle(); // A only
		result[1].Blocks.Should().HaveCount(2); // B + C
	}

	[Fact]
	public void Paginate_KeepWithNextChain_AllMoveTogether()
	{
		// A: 4000, B(keepNext): 4000, C(keepNext): 4000, D: 4000.
		// A+B = 8000 fit. +C = 12000 ≤ 12960 fit. +D = 16000 > 12960.
		// D doesn't fit. Tail: C(keepNext), B(keepNext) → pull back both.
		// Page 1: [A]. Page 2: [B, C, D].
		var blocks = new[]
		{
			MakeBlock(4000f),
			MakeBlock(4000f, keepWithNext: true),
			MakeBlock(4000f, keepWithNext: true),
			MakeBlock(4000f),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().HaveCount(2);
		result[0].Blocks.Should().ContainSingle();
		result[1].Blocks.Should().HaveCount(3);
	}

	[Fact]
	public void Paginate_KeepWithNext_EntirePageIsChain_NoInfiniteLoop()
	{
		// All blocks on page have keepNext and next block doesn't fit.
		// Since pullBackCount == currentPageBlocks.Count, don't pull back (avoid infinite loop).
		// Instead, start new page normally.
		var blocks = new[]
		{
			MakeBlock(7000f, keepWithNext: true),
			MakeBlock(7000f), // doesn't fit (14000 > 12960)
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().HaveCount(2);
		result[0].Blocks.Should().ContainSingle();
		result[1].Blocks.Should().ContainSingle();
	}

	[Fact]
	public void Paginate_KeepWithNext_WithForceBreak_BreakTakesPriority()
	{
		// A(keepNext): 5000, B(forceBreak): 5000.
		// forceBreak triggers new page before keepNext pull-back.
		var blocks = new[]
		{
			MakeBlock(5000f, keepWithNext: true),
			MakeBlock(5000f, forceBreak: true),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		// Force break creates new page. A is on page 1, B on page 2.
		result.Should().HaveCount(2);
	}

	[Fact]
	public void Paginate_KeepWithNext_PageNumbersCorrect()
	{
		var blocks = new[]
		{
			MakeBlock(6000f),
			MakeBlock(6000f, keepWithNext: true),
			MakeBlock(4000f),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result[0].PageNumber.Should().Be(1);
		result[1].PageNumber.Should().Be(2);
	}

	[Fact]
	public void LayoutBlock_KeepWithNext_DefaultIsFalse()
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var block = new LayoutBlock(para, 100f);

		block.KeepWithNext.Should().BeFalse();
	}

	[Fact]
	public void LayoutBlock_KeepWithNext_CanBeSetTrue()
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var block = new LayoutBlock(para, 100f, KeepWithNext: true);

		block.KeepWithNext.Should().BeTrue();
	}

	private static LayoutBlock MakeBlock(float heightTwips, bool keepWithNext = false, bool forceBreak = false)
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		return new LayoutBlock(para, heightTwips, KeepWithNext: keepWithNext, ForcePageBreakBefore: forceBreak);
	}
}
