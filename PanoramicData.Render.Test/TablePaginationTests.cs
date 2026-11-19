using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

namespace PanoramicData.Render.Test;

public sealed class TablePaginationTests
{
	private static readonly SectionInfo DefaultSection = new();

	private static TablePlaceholderBlock NewTable() => new() { TableElement = new Table() };

	private static float[][] Rows(string spec) =>
		[.. spec.Split(';').Select(row => row.Split('+').Select(float.Parse).ToArray())];

	private static LayoutBlock Lead() => PaginationTestKit.Block("h9000");

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
		var block = NewTable();

		var result = PageBuilder.CreateTableLayoutBlock(block, [1000f, 2000f, 3000f]);

		result.Block.Should().BeSameAs(block);
		result.HeightTwips.Should().Be(6000f);
		result.LineHeights.Should().Equal(1000f, 2000f, 3000f);
		result.KeepLinesTogether.Should().BeFalse();
		result.WidowOrphanControl.Should().BeFalse();
	}

	[Theory]
	[InlineData(false, new[] { 5000f, 5000f, 5000f }, "5000+5000|5000")]
	[InlineData(true, new[] { 3000f, 3000f, 3000f }, "-,3000|3000+3000")]
	public void Paginate_TableLayoutBlock_SplitsAtRowBoundary(bool withLeadBlock, float[] rowHeights, string expected)
	{
		var table = PageBuilder.CreateTableLayoutBlock(NewTable(), rowHeights);
		LayoutBlock[] blocks = withLeadBlock ? [Lead(), table] : [table];

		var pages = PageBuilder.Paginate(blocks, DefaultSection);

		PaginationTestKit.HeightShape(pages).Should().Be(expected);
	}

	[Fact]
	public void CreateTableRowLayoutBlocks_CantSplitCountMismatch_ThrowsArgumentException()
	{
		var act = () => PageBuilder.CreateTableRowLayoutBlocks(NewTable(), [[1000f]], []);

		act.Should().Throw<ArgumentException>()
			.WithParameterName("cantSplitRows");
	}

	[Theory]
	[InlineData(false, "-,3000|3000")]
	[InlineData(true, "-|3000+3000")]
	public void Paginate_TableRowLayoutBlocks_HonoursCantSplit(bool cantSplit, string expected)
	{
		var rows = PageBuilder.CreateTableRowLayoutBlocks(NewTable(), [[3000f, 3000f]], [cantSplit]);

		var pages = PageBuilder.Paginate([Lead(), rows[0]], DefaultSection);

		PaginationTestKit.HeightShape(pages).Should().Be(expected);
	}

	[Theory]
	[InlineData("7000;7000", 0, "7000|7000")]
	[InlineData("2000;9000;4000", 1, "2000,9000|2000,4000")]
	[InlineData("2000;6000;6000;6000;6000", 1, "2000,6000|2000,6000|2000,6000|2000,6000")]
	[InlineData("2000;7000;7000;7000", 1, "2000,7000|2000,7000|2000,7000")]
	public void PaginateTableRows_RepeatsHeaderRows_OnContinuationPages(string rowSpec, int headerRows, string expected)
	{
		var rows = PageBuilder.CreateTableRowLayoutBlocks(NewTable(), Rows(rowSpec));

		var pages = PageBuilder.PaginateTableRows(rows, DefaultSection, headerRowCount: headerRows);

		PaginationTestKit.HeightShape(pages).Should().Be(expected);
		pages.Select(page => page.PageNumber).Should().Equal(Enumerable.Range(1, pages.Count));
	}

	[Fact]
	public void PaginateTableRows_InvalidHeaderRowCount_ThrowsArgumentOutOfRangeException()
	{
		var rows = PageBuilder.CreateTableRowLayoutBlocks(NewTable(), [[1000f]]);

		var act = () => PageBuilder.PaginateTableRows(rows, DefaultSection, headerRowCount: 2);

		act.Should().Throw<ArgumentOutOfRangeException>()
			.WithParameterName("headerRowCount");
	}

	[Fact]
	public void PaginateTableRows_MultiPage_WithCantSplitRows_KeepsRowAtomic()
	{
		var rowBlocks = PageBuilder.CreateTableRowLayoutBlocks(NewTable(), [[3000f, 3000f], [3000f]], [true, false]);

		var pages = PageBuilder.PaginateTableRows([rowBlocks[0], rowBlocks[1]], DefaultSection, headerRowCount: 0);
		var pagesWithLeadBlock = PageBuilder.Paginate([Lead(), pages[0].Blocks[0]], DefaultSection);

		PaginationTestKit.HeightShape(pagesWithLeadBlock).Should().Be("-|3000+3000");
	}
}
