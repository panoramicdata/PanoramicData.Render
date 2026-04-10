namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed class ColumnBalancingTests
{
	private static readonly SectionInfo TwoColumnSection = new() { ColumnCount = 2 };

	[Fact]
	public void Paginate_TwoColumnSection_BalancesSinglePageContentAcrossColumns()
	{
		var blocks = new[]
		{
			MakeSplittableBlock([1000f, 1000f, 1000f, 1000f]),
		};

		var result = PageBuilder.Paginate(blocks, TwoColumnSection);

		result.Should().ContainSingle();
		result[0].BlockPlacements.Should().HaveCount(2);
		result[0].Blocks.Should().HaveCount(2);
		result[0].BlockPlacements[0].ColumnIndex.Should().Be(0);
		result[0].BlockPlacements[1].ColumnIndex.Should().Be(1);
		result[0].Blocks[0].LineHeights.Should().HaveCount(2);
		result[0].Blocks[1].LineHeights.Should().HaveCount(2);
	}

	[Fact]
	public void Paginate_TwoColumnSection_BalancesOnlyTheFinalPage()
	{
		var blocks = new[]
		{
			MakeSplittableBlock(Enumerable.Repeat(1000f, 30).ToArray()),
		};

		var result = PageBuilder.Paginate(blocks, TwoColumnSection);

		result.Should().HaveCount(2);
		result[0].Blocks.Should().HaveCount(2);
		result[0].Blocks[0].LineHeights.Should().HaveCount(12);
		result[0].Blocks[1].LineHeights.Should().HaveCount(12);
		result[1].Blocks.Should().HaveCount(2);
		result[1].BlockPlacements[0].ColumnIndex.Should().Be(0);
		result[1].BlockPlacements[1].ColumnIndex.Should().Be(1);
		result[1].Blocks[0].LineHeights.Should().HaveCount(3);
		result[1].Blocks[1].LineHeights.Should().HaveCount(3);
	}

	[Fact]
	public void Paginate_TwoColumnSection_WithExplicitColumnBreak_DoesNotRebalanceFinalPage()
	{
		var blocks = new[]
		{
			MakeSplittableBlock([1000f, 1000f, 1000f, 1000f]),
			MakeBlock(1000f, forceColumnBreak: true),
		};

		var result = PageBuilder.Paginate(blocks, TwoColumnSection);

		result.Should().ContainSingle();
		result[0].Blocks.Should().HaveCount(2);
		result[0].Blocks[0].LineHeights.Should().HaveCount(4);
		result[0].BlockPlacements[0].ColumnIndex.Should().Be(0);
		result[0].BlockPlacements[1].ColumnIndex.Should().Be(1);
	}

	private static LayoutBlock MakeSplittableBlock(float[] lineHeights)
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		var totalHeight = lineHeights.Sum();
		return new LayoutBlock(para, totalHeight, LineHeights: lineHeights, WidowOrphanControl: false);
	}

	private static LayoutBlock MakeBlock(float heightTwips, bool forceColumnBreak = false)
	{
		var para = new ParagraphBlock { SourceElement = new Paragraph() };
		return new LayoutBlock(para, heightTwips, ForceColumnBreakBefore: forceColumnBreak);
	}
}