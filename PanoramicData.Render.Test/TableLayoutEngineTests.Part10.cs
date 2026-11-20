namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;
using static PanoramicData.Render.Test.TableLayoutBuilders;

public sealed partial class TableLayoutEngineTests
{
	// ---- Vertical alignment (4.2.5) ----

	[Fact]
	public void ComputeVerticalContentOffset_TopAlignment_ReturnsZero()
	{
		AssertVerticalOffset(500f, 240f, CellVerticalAlignment.Top, 0f);
	}

	[Fact]
	public void ComputeVerticalContentOffset_CenterAlignment_CentersContent()
	{
		// (500 - 240) / 2 = 130
		AssertVerticalOffset(500f, 240f, CellVerticalAlignment.Center, 130f);
	}

	[Fact]
	public void ComputeVerticalContentOffset_BottomAlignment_PushesContentDown()
	{
		// 500 - 240 = 260
		AssertVerticalOffset(500f, 240f, CellVerticalAlignment.Bottom, 260f);
	}

	[Fact]
	public void ComputeVerticalContentOffset_ContentTallerThanCell_ReturnsZero()
	{
		// Center: (300 - 500) / 2 = -100 clamped to 0
		AssertVerticalOffset(300f, 500f, CellVerticalAlignment.Center, 0f);
	}

	[Fact]
	public void ComputeVerticalContentOffset_BottomWithContentTallerThanCell_ReturnsZero()
	{
		AssertVerticalOffset(300f, 500f, CellVerticalAlignment.Bottom, 0f);
	}

	[Fact]
	public void ComputeVerticalContentOffset_CenterWithEqualHeights_ReturnsZero()
	{
		AssertVerticalOffset(240f, 240f, CellVerticalAlignment.Center, 0f);
	}

	// ---- Fixed layout integration (4.2.6) ----

	[Fact]
	public void Layout_FullPipeline_CellPositionsWithMarginsAndContent()
	{
		var margins = new CellMargins(50f, 100f, 50f, 100f);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(3000f), new TableGridColumn(5000f)],
			Rows = [RowOf(ParagraphCell(1, margins), ParagraphCell(2, margins))],
		};

		var positions = TableLayoutEngine.ComputeCellPositions(TableLayoutEngine.Layout(table, 9600f));

		// cell00: 50 + 240 + 50 = 340; cell01: 50 + 480 + 50 = 580; row height = 580
		positions.Should().HaveCount(2);
		positions[0].Height.Should().Be(580f);
		positions[1].Height.Should().Be(580f);
		TableLayoutEngine.ComputeContentWidth(positions[0].Width, margins).Should().Be(2800f); // 3000 - 100 - 100
		TableLayoutEngine.ComputeContentWidth(positions[1].Width, margins).Should().Be(4800f); // 5000 - 100 - 100
	}

	[Fact]
	public void Layout_FullPipeline_VerticalAlignmentIntegration()
	{
		var cell = ParagraphCell(1, verticalAlignment: CellVerticalAlignment.Center);
		var table = SingleColumnTable(new TableRowElement { Cells = [cell], HeightTwips = 600f });

		var positions = TableLayoutEngine.ComputeCellPositions(TableLayoutEngine.Layout(table, 9600f));

		// Row height: max(600, 240) = 600. Content height: 240. Offset: (600-240)/2 = 180
		positions.Should().HaveCount(1);
		positions[0].Height.Should().Be(600f);
		AssertCellOffset(cell, 600f, 180f);
	}

	[Fact]
	public void Layout_FullPipeline_MultiRowWithMarginsAndAlignment()
	{
		var row0Cell = ParagraphCell(1, new CellMargins(100f, 50f, 100f, 50f), verticalAlignment: CellVerticalAlignment.Bottom);
		var table = SingleColumnTable(
			new TableRowElement { Cells = [row0Cell], HeightTwips = 600f },
			RowOf(ParagraphCell(2)));

		var positions = TableLayoutEngine.ComputeCellPositions(TableLayoutEngine.Layout(table, 9600f));

		positions.Should().HaveCount(2);

		// Row 0: max(600, 100+240+100=440) = 600
		positions[0].Y.Should().Be(0f);
		positions[0].Height.Should().Be(600f);

		// Row 1: 2x240 = 480 (no margins)
		positions[1].Y.Should().Be(600f);
		positions[1].Height.Should().Be(480f);

		// Vertical offset for row0 cell (bottom alignment): 600 - 440 = 160
		AssertCellOffset(row0Cell, 600f, 160f);
	}

	private static void AssertVerticalOffset(float cellHeight, float contentHeight, CellVerticalAlignment alignment, float expected)
	{
		TableLayoutEngine.ComputeVerticalContentOffset(cellHeight, contentHeight, alignment).Should().Be(expected);
	}

	private static void AssertCellOffset(TableCellElement cell, float rowHeight, float expected)
	{
		var (_, contentHeight) = TableLayoutEngine.LayoutCellContent(cell);
		AssertVerticalOffset(rowHeight, contentHeight, cell.VerticalAlignment, expected);
	}
}
