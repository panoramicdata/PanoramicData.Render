using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

namespace PanoramicData.Render.Test;

public sealed class ParagraphSplittingTests
{
	[Theory]
	[InlineData("h5000", 3000f, "null")]
	[InlineData("l5000!f", 3000f, "null")]
	[InlineData("l3x1000!f", 5000f, "null")]
	[InlineData("l3x1000!f", 2500f, "2/1")]
	public void TrySplitBlock_SplitsAtLineBoundary(string block, float available, string expected)
	{
		var result = PageBuilder.TrySplitBlock(PaginationTestKit.Block(block), available);

		PaginationTestKit.DescribeSplit(result).Should().Be(expected);
	}

	[Theory]
	[InlineData("l3x1000/100/50!f", 1500f, "First", "1:1100/100/0")]
	[InlineData("l3x1000/100/50!f", 1500f, "Second", "2:2050/0/50")]
	[InlineData("l5000,1000!f", 3000f, "Both", "1:5000/0/0 1:1000/0/0")]
	public void TrySplitBlock_PartsCarryExpectedMetrics(string block, float available, string part, string expected)
	{
		var result = PageBuilder.TrySplitBlock(PaginationTestKit.Block(block), available);

		result.Should().NotBeNull();
		PaginationTestKit.MetricsOf(result!.Value, part).Should().Be(expected);
	}

	[Fact]
	public void TrySplitBlock_BothPartsReferSameBlock()
	{
		var block = PaginationTestKit.Block("l2x1000!f");

		var result = PageBuilder.TrySplitBlock(block, 1500f);

		result.Should().NotBeNull();
		result!.Value.First.Block.Should().BeSameAs(block.Block);
		result.Value.Second.Block.Should().BeSameAs(block.Block);
	}

	[Fact]
	public void TrySplitBlock_ContinuationPreservesLogicalLineStartIndex()
	{
		var paragraph = new ParagraphBlock { SourceElement = new Paragraph(new Run(new Text("Split me"))) };
		var block = new LayoutBlock(paragraph, 3000f, LineHeights: [1000f, 1000f, 1000f], WidowOrphanControl: false, LineStartIndex: 4);

		var result = PageBuilder.TrySplitBlock(block, 2500f);

		result.Should().NotBeNull();
		result!.Value.First.LineStartIndex.Should().Be(4);
		result.Value.Second.LineStartIndex.Should().Be(6);
	}

	[Theory]
	[InlineData("h10000 l5x1000!f", "-,2|3")]
	[InlineData("h10000 h5000", "-|-")]
	[InlineData("l30x1000!f", "12|12|6")]
	[InlineData("h10000 l5x1000!f h500", "-,2|3,-")]
	public void Paginate_PlacesBlocksAsExpected(string spec, string expectedShape)
	{
		var result = PaginationTestKit.Paginate(spec);

		PaginationTestKit.Shape(result).Should().Be(expectedShape);
	}

	[Theory]
	[InlineData("l10x1400/200/0!f", 0, "9:12800/200/0")]
	[InlineData("l10x1400/200/300!f", 1, "1:1700/0/300")]
	public void Paginate_SplitPreservesSpacing(string spec, int page, string expected)
	{
		var result = PaginationTestKit.Paginate(spec);

		result.Should().HaveCount(2);
		PaginationTestKit.Metrics(result[page].Blocks[0]).Should().Be(expected);
	}

	[Fact]
	public void Paginate_PageNumbersContinueAcrossSplits()
	{
		var result = PaginationTestKit.Paginate("l30x1000!f");

		result.Select(page => page.PageNumber).Should().Equal(1, 2, 3);
	}
}
