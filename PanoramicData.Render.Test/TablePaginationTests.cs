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

	[Fact]
	public void PaginateTableRows_HeaderRowCountZero_DelegatesWithoutRepeating()
	{
		var tableBlock = new TablePlaceholderBlock { TableElement = new Table() };
		var rows = PageBuilder.CreateTableRowLayoutBlocks(tableBlock, [[7000f], [7000f]]);

		var pages = PageBuilder.PaginateTableRows(rows, DefaultSection, headerRowCount: 0);

		pages.Should().HaveCount(2);
		pages[0].Blocks.Should().ContainSingle();
		pages[1].Blocks.Should().ContainSingle();
	}

	[Fact]
	public void PaginateTableRows_RepeatsHeaderRows_OnContinuationPages()
	{
		var tableBlock = new TablePlaceholderBlock { TableElement = new Table() };
		var rows = PageBuilder.CreateTableRowLayoutBlocks(
			tableBlock,
			[[2000f], [9000f], [4000f]]);

		var pages = PageBuilder.PaginateTableRows(rows, DefaultSection, headerRowCount: 1);

		pages.Should().HaveCount(2);
		pages[0].Blocks.Should().HaveCount(2);
		pages[0].Blocks[0].HeightTwips.Should().Be(2000f);
		pages[0].Blocks[1].HeightTwips.Should().Be(9000f);

		pages[1].Blocks.Should().HaveCount(2);
		pages[1].Blocks[0].HeightTwips.Should().Be(2000f); // repeated header row
		pages[1].Blocks[1].HeightTwips.Should().Be(4000f);
	}

	[Fact]
	public void PaginateTableRows_InvalidHeaderRowCount_ThrowsArgumentOutOfRangeException()
	{
		var tableBlock = new TablePlaceholderBlock { TableElement = new Table() };
		var rows = PageBuilder.CreateTableRowLayoutBlocks(tableBlock, [[1000f]]);

		var act = () => PageBuilder.PaginateTableRows(rows, DefaultSection, headerRowCount: 2);

		act.Should().Throw<ArgumentOutOfRangeException>()
			.WithParameterName("headerRowCount");
	}

	[Fact]
	public void PaginateTableRows_MultiPage_RepeatsHeaderOnEveryContinuationPage()
	{
		var tableBlock = new TablePlaceholderBlock { TableElement = new Table() };
		var rows = PageBuilder.CreateTableRowLayoutBlocks(
			tableBlock,
			[[2000f], [6000f], [6000f], [6000f], [6000f]]);

		var pages = PageBuilder.PaginateTableRows(rows, DefaultSection, headerRowCount: 1);

		pages.Should().HaveCountGreaterThan(1);

		// First page starts with the original header row.
		pages[0].Blocks.Should().NotBeEmpty();
		pages[0].Blocks[0].HeightTwips.Should().Be(2000f);

		// Every continuation page begins with the repeated header row.
		for (var i = 1; i < pages.Count; i++)
		{
			pages[i].Blocks.Should().NotBeEmpty();
			pages[i].Blocks[0].HeightTwips.Should().Be(2000f);
		}
	}

	[Fact]
	public void PaginateTableRows_MultiPage_PageNumbersIncreaseSequentially()
	{
		var tableBlock = new TablePlaceholderBlock { TableElement = new Table() };
		var rows = PageBuilder.CreateTableRowLayoutBlocks(
			tableBlock,
			[[2000f], [7000f], [7000f], [7000f]]);

		var pages = PageBuilder.PaginateTableRows(rows, DefaultSection, headerRowCount: 1);

		pages.Should().HaveCountGreaterThan(1);
		for (var i = 0; i < pages.Count; i++)
		{
			pages[i].PageNumber.Should().Be(i + 1);
		}
	}

	[Fact]
	public void PaginateTableRows_MultiPage_WithCantSplitRows_KeepsRowAtomic()
	{
		var before = new LayoutBlock(new ParagraphBlock { SourceElement = new Paragraph() }, 9000f);
		var tableBlock = new TablePlaceholderBlock { TableElement = new Table() };
		var rowBlocks = PageBuilder.CreateTableRowLayoutBlocks(
			tableBlock,
			[[3000f, 3000f], [3000f]],
			[true, false]);

		var pages = PageBuilder.PaginateTableRows([rowBlocks[0], rowBlocks[1]], DefaultSection, headerRowCount: 0);
		var pagesWithLeadBlock = PageBuilder.Paginate([before, pages[0].Blocks[0]], DefaultSection);

		// The cantSplit row should move whole when constrained by preceding content.
		pagesWithLeadBlock.Should().HaveCount(2);
		pagesWithLeadBlock[0].Blocks.Should().ContainSingle();
		pagesWithLeadBlock[1].Blocks.Should().ContainSingle();
		pagesWithLeadBlock[1].Blocks[0].LineHeights.Should().Equal(3000f, 3000f);
	}
}
