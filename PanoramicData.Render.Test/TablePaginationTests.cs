namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed class TablePaginationTests
{
	private static readonly SectionInfo DefaultSection = new();

	[Fact]
	public void CreateTableLayoutBlock_NullTable_ThrowsArgumentNullException()
	{
		var act = () => PageBuilder.CreateTableLayoutBlock(null!, [1000f]);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("tableBlock");
	}

	[Fact]
	public void CreateTableLayoutBlock_UsesRowHeights_AsLineBoundaries()
	{
		var block = new TablePlaceholderBlock { TableElement = new Table() };

		var result = PageBuilder.CreateTableLayoutBlock(block, [1000f, 2000f, 3000f]);

		result.Block.Should().BeSameAs(block);
		result.HeightTwips.Should().Be(6000f);
		result.LineHeights.Should().Equal(1000f, 2000f, 3000f);
		result.KeepLinesTogether.Should().BeFalse();
		result.WidowOrphanControl.Should().BeFalse();
	}

	[Fact]
	public void Paginate_TableLayoutBlock_SplitsAtRowBoundary()
	{
		var tableBlock = new TablePlaceholderBlock { TableElement = new Table() };
		var block = PageBuilder.CreateTableLayoutBlock(tableBlock, [5000f, 5000f, 5000f]);

		var pages = PageBuilder.Paginate([block], DefaultSection);

		pages.Should().HaveCount(2);
		pages[0].Blocks.Should().ContainSingle();
		pages[0].Blocks[0].LineHeights.Should().Equal(5000f, 5000f);
		pages[1].Blocks.Should().ContainSingle();
		pages[1].Blocks[0].LineHeights.Should().Equal(5000f);
	}

	[Fact]
	public void Paginate_TableLayoutBlock_WithRemainingSpace_SplitsUsingAvailableHeight()
	{
		var before = new LayoutBlock(new ParagraphBlock { SourceElement = new Paragraph() }, 9000f);
		var tableBlock = new TablePlaceholderBlock { TableElement = new Table() };
		var table = PageBuilder.CreateTableLayoutBlock(tableBlock, [3000f, 3000f, 3000f]);

		var pages = PageBuilder.Paginate([before, table], DefaultSection);

		pages.Should().HaveCount(2);
		pages[0].Blocks.Should().HaveCount(2);
		pages[0].Blocks[1].LineHeights.Should().Equal(3000f);
		pages[1].Blocks.Should().ContainSingle();
		pages[1].Blocks[0].LineHeights.Should().Equal(3000f, 3000f);
	}

	[Fact]
	public void CreateTableRowLayoutBlocks_CantSplitCountMismatch_ThrowsArgumentException()
	{
		var tableBlock = new TablePlaceholderBlock { TableElement = new Table() };
		var act = () => PageBuilder.CreateTableRowLayoutBlocks(tableBlock, [[1000f]], []);

		act.Should().Throw<ArgumentException>()
			.WithParameterName("cantSplitRows");
	}

	[Fact]
	public void Paginate_TableRowLayoutBlocks_CantSplitFalse_SplitsRowAtLineBoundary()
	{
		var before = new LayoutBlock(new ParagraphBlock { SourceElement = new Paragraph() }, 9000f);
		var tableBlock = new TablePlaceholderBlock { TableElement = new Table() };
		var rows = PageBuilder.CreateTableRowLayoutBlocks(
			tableBlock,
			[[3000f, 3000f]],
			[false]);

		var pages = PageBuilder.Paginate([before, rows[0]], DefaultSection);

		pages.Should().HaveCount(2);
		pages[0].Blocks.Should().HaveCount(2);
		pages[0].Blocks[1].LineHeights.Should().Equal(3000f);
		pages[1].Blocks.Should().ContainSingle();
		pages[1].Blocks[0].LineHeights.Should().Equal(3000f);
	}

	[Fact]
	public void Paginate_TableRowLayoutBlocks_CantSplitTrue_MovesEntireRowToNextPage()
	{
		var before = new LayoutBlock(new ParagraphBlock { SourceElement = new Paragraph() }, 9000f);
		var tableBlock = new TablePlaceholderBlock { TableElement = new Table() };
		var rows = PageBuilder.CreateTableRowLayoutBlocks(
			tableBlock,
			[[3000f, 3000f]],
			[true]);

		var pages = PageBuilder.Paginate([before, rows[0]], DefaultSection);

		pages.Should().HaveCount(2);
		pages[0].Blocks.Should().ContainSingle();
		pages[1].Blocks.Should().ContainSingle();
		pages[1].Blocks[0].LineHeights.Should().Equal(3000f, 3000f);
	}
}
