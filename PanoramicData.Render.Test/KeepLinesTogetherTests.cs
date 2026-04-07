namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed class KeepLinesTogetherTests
{
	private static readonly SectionInfo DefaultSection = new();

	[Fact]
	public void TrySplitBlock_KeepLinesTogether_ReturnsNull()
	{
		var block = MakeSplittableBlock(0f, 0f, [1000f, 1000f, 1000f], keepLines: true);

		var result = PageBuilder.TrySplitBlock(block, 2500f);

		result.Should().BeNull();
	}

	[Fact]
	public void TrySplitBlock_KeepLinesDisabled_Splits()
	{
		var block = MakeSplittableBlock(0f, 0f, [1000f, 1000f, 1000f], keepLines: false);

		var result = PageBuilder.TrySplitBlock(block, 2500f);

		result.Should().NotBeNull();
	}

	[Fact]
	public void Paginate_KeepLinesTogether_MovesWholeBlockToNextPage()
	{
		// Block A: 10000. Block B (keepLines): 3 lines × 1500 = 4500.
		// A+B = 14500 > 12960. Can't split B → move whole B to next page.
		var blocks = new[]
		{
			MakeBlock(10000f),
			MakeSplittableBlock(0f, 0f, [1500f, 1500f, 1500f], keepLines: true),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().HaveCount(2);
		result[0].Blocks.Should().ContainSingle();
		result[1].Blocks.Should().ContainSingle();
		result[1].Blocks[0].LineHeights.Should().HaveCount(3);
	}

	[Fact]
	public void Paginate_KeepLinesTogether_FitsOnPage_NoEffect()
	{
		// Block with keepLines that fits entirely on the page.
		var blocks = new[]
		{
			MakeBlock(5000f),
			MakeSplittableBlock(0f, 0f, [1000f, 1000f], keepLines: true),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().ContainSingle();
		result[0].Blocks.Should().HaveCount(2);
	}

	[Fact]
	public void Paginate_KeepLinesTogether_OversizedOnEmptyPage_PlacedAnyway()
	{
		// Block with keepLines larger than a page → placed on an empty page anyway.
		var blocks = new[]
		{
			MakeSplittableBlock(0f, 0f, [5000f, 5000f, 5000f], keepLines: true),
		};

		var result = PageBuilder.Paginate(blocks, DefaultSection);

		result.Should().ContainSingle();
		result[0].Blocks.Should().ContainSingle();
	}

	[Fact]
	public void LayoutBlock_KeepLinesTogether_DefaultIsFalse()
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var block = new LayoutBlock(para, 100f);

		block.KeepLinesTogether.Should().BeFalse();
	}

	[Fact]
	public void LayoutBlock_KeepLinesTogether_CanBeSetTrue()
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var block = new LayoutBlock(para, 100f, KeepLinesTogether: true);

		block.KeepLinesTogether.Should().BeTrue();
	}

	private static LayoutBlock MakeBlock(float heightTwips)
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		return new LayoutBlock(para, heightTwips);
	}

	private static LayoutBlock MakeSplittableBlock(
		float spaceBefore, float spaceAfter, float[] lineHeights, bool keepLines = false)
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var totalHeight = spaceBefore + lineHeights.Sum() + spaceAfter;
		return new LayoutBlock(para, totalHeight, spaceBefore, spaceAfter, lineHeights,
			KeepLinesTogether: keepLines, WidowOrphanControl: false);
	}
}
