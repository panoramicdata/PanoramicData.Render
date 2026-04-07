namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed class ExplicitPageBreakTests
{
	private static readonly SectionInfo DefaultSection = new();

	[Fact]
	public void Paginate_ForcePageBreakBefore_StartsNewPage()
	{
		var blocks = new[]
		{
			MakeBlock(1000f),
			MakeBlock(1000f, forceBreak: true),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().HaveCount(2);
		result[0].Blocks.Should().ContainSingle();
		result[1].Blocks.Should().ContainSingle();
	}

	[Fact]
	public void Paginate_ForcePageBreakBefore_FirstBlock_NoEmptyPage()
	{
		// Force break on the very first block should NOT create an empty page.
		var blocks = new[]
		{
			MakeBlock(1000f, forceBreak: true),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().ContainSingle();
		result[0].PageNumber.Should().Be(1);
	}

	[Fact]
	public void Paginate_MultipleForceBreaks_CreatesMultiplePages()
	{
		var blocks = new[]
		{
			MakeBlock(100f),
			MakeBlock(100f, forceBreak: true),
			MakeBlock(100f, forceBreak: true),
			MakeBlock(100f, forceBreak: true),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().HaveCount(4);
		for (var i = 0; i < 4; i++)
		{
			result[i].PageNumber.Should().Be(i + 1);
			result[i].Blocks.Should().ContainSingle();
		}
	}

	[Fact]
	public void Paginate_ForceBreakWithContentAfter_GroupsCorrectly()
	{
		var blocks = new[]
		{
			MakeBlock(500f),
			MakeBlock(500f),
			MakeBlock(500f, forceBreak: true),
			MakeBlock(500f),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().HaveCount(2);
		result[0].Blocks.Should().HaveCount(2);
		result[1].Blocks.Should().HaveCount(2);
	}

	[Fact]
	public void Paginate_ForceBreakAndOverflow_BothRespected()
	{
		// First two blocks fill 12000. Third has a force break.
		// Even though there's space left (960 twips), the break starts a new page.
		var blocks = new[]
		{
			MakeBlock(6000f),
			MakeBlock(6000f),
			MakeBlock(6000f, forceBreak: true),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().HaveCount(2);
		result[0].Blocks.Should().HaveCount(2);
		result[1].Blocks.Should().ContainSingle();
	}

	[Fact]
	public void Paginate_ForceBreakOnSplittableBlock_StartsNewPageThenFills()
	{
		// First block: 10000. Second block: force break + splittable 5 lines × 1000 = 5000.
		// Force break → finalize page with first block. Then splittable block starts fresh page.
		var blocks = new[]
		{
			MakeBlock(10000f),
			MakeSplittableBlock(0f, 0f, [1000f, 1000f, 1000f, 1000f, 1000f], forceBreak: true),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().HaveCount(2);
		result[0].Blocks.Should().ContainSingle();
		result[1].Blocks.Should().ContainSingle();
		result[1].Blocks[0].LineHeights.Should().HaveCount(5);
	}

	[Fact]
	public void LayoutBlock_ForcePageBreakBefore_DefaultIsFalse()
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var block = new LayoutBlock(para, 100f);

		block.ForcePageBreakBefore.Should().BeFalse();
	}

	[Fact]
	public void LayoutBlock_ForcePageBreakBefore_CanBeSetTrue()
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var block = new LayoutBlock(para, 100f, ForcePageBreakBefore: true);

		block.ForcePageBreakBefore.Should().BeTrue();
	}

	private static LayoutBlock MakeBlock(float heightTwips, bool forceBreak = false)
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		return new LayoutBlock(para, heightTwips, ForcePageBreakBefore: forceBreak);
	}

	private static LayoutBlock MakeSplittableBlock(
		float spaceBefore, float spaceAfter, float[] lineHeights, bool forceBreak = false)
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var totalHeight = spaceBefore + lineHeights.Sum() + spaceAfter;
		return new LayoutBlock(para, totalHeight, spaceBefore, spaceAfter, lineHeights, forceBreak);
	}
}
