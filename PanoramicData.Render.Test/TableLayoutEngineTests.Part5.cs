namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed partial class TableLayoutEngineTests
{
	[Fact]
	public void Layout_ResultContainsSourceTable()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(4800f)],
			Rows = [new TableRowElement { Cells = [MakeCell()] }],
		};

		var result = TableLayoutEngine.Layout(table, 9600f);

		result.Table.Should().BeSameAs(table);
	}

	[Fact]
	public void Layout_ColumnOffsetsMatchWidths()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(2000f), new TableGridColumn(3000f)],
			Rows = [new TableRowElement { Cells = [MakeCell(), MakeCell(), MakeCell()] }],
		};

		var result = TableLayoutEngine.Layout(table, 9600f);

		result.ColumnOffsets[0].Should().Be(0f);
		result.ColumnOffsets[1].Should().Be(1000f);
		result.ColumnOffsets[2].Should().Be(3000f);
		result.ColumnWidths[0].Should().Be(1000f);
		result.ColumnWidths[1].Should().Be(2000f);
		result.ColumnWidths[2].Should().Be(3000f);
	}

	// ---- ComputeCellPositions (4.2.2) ----

	[Fact]
	public void ComputeCellPositions_NullLayout_ThrowsArgumentNullException()
	{
		var act = () => TableLayoutEngine.ComputeCellPositions(null!);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("layout");
	}

	[Fact]
	public void ComputeCellPositions_EmptyTable_ReturnsEmpty()
	{
		var table = new TableElement { GridColumns = [], Rows = [] };
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var positions = TableLayoutEngine.ComputeCellPositions(layout);

		positions.Should().BeEmpty();
	}

	[Fact]
	public void ComputeCellPositions_SingleCell_CorrectPosition()
	{
		var cell = MakeCell();
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(4800f)],
			Rows = [new TableRowElement { Cells = [cell], HeightTwips = 300f }],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var positions = TableLayoutEngine.ComputeCellPositions(layout);

		positions.Should().HaveCount(1);
		positions[0].RowIndex.Should().Be(0);
		positions[0].ColumnIndex.Should().Be(0);
		positions[0].X.Should().Be(0f);
		positions[0].Y.Should().Be(0f);
		positions[0].Width.Should().Be(4800f);
		positions[0].Height.Should().Be(300f);
		positions[0].Cell.Should().BeSameAs(cell);
	}

	[Fact]
	public void ComputeCellPositions_TwoByTwo_CorrectPositions()
	{
		var c00 = MakeCell();
		var c01 = MakeCell();
		var c10 = MakeCell();
		var c11 = MakeCell();
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(2000f), new TableGridColumn(3000f)],
			Rows =
			[
				new TableRowElement { Cells = [c00, c01], HeightTwips = 400f },
				new TableRowElement { Cells = [c10, c11], HeightTwips = 500f },
			],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var positions = TableLayoutEngine.ComputeCellPositions(layout);

		positions.Should().HaveCount(4);

		var p00 = positions.First(p => p.Cell == c00);
		p00.X.Should().Be(0f);
		p00.Y.Should().Be(0f);
		p00.Width.Should().Be(2000f);
		p00.Height.Should().Be(400f);

		var p01 = positions.First(p => p.Cell == c01);
		p01.X.Should().Be(2000f);
		p01.Y.Should().Be(0f);
		p01.Width.Should().Be(3000f);
		p01.Height.Should().Be(400f);

		var p10 = positions.First(p => p.Cell == c10);
		p10.X.Should().Be(0f);
		p10.Y.Should().Be(400f);
		p10.Width.Should().Be(2000f);
		p10.Height.Should().Be(500f);

		var p11 = positions.First(p => p.Cell == c11);
		p11.X.Should().Be(2000f);
		p11.Y.Should().Be(400f);
		p11.Width.Should().Be(3000f);
		p11.Height.Should().Be(500f);
	}

	[Fact]
	public void ComputeCellPositions_HorizontalMerge_SpansColumns()
	{
		var merged = MakeCell(gridSpan: 2);
		var regular = MakeCell();
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(2000f), new TableGridColumn(3000f)],
			Rows = [new TableRowElement { Cells = [merged, regular], HeightTwips = 400f }],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var positions = TableLayoutEngine.ComputeCellPositions(layout);

		positions.Should().HaveCount(2);

		var pm = positions.First(p => p.Cell == merged);
		pm.X.Should().Be(0f);
		pm.Width.Should().Be(3000f); // 1000 + 2000

		var pr = positions.First(p => p.Cell == regular);
		pr.X.Should().Be(3000f);
		pr.Width.Should().Be(3000f);
	}

	[Fact]
	public void ComputeCellPositions_VerticalMerge_SpansRows()
	{
		var restart = MakeCell(verticalMerge: VerticalMergeState.Restart);
		var cont = MakeCell(verticalMerge: VerticalMergeState.Continue);
		var r0c1 = MakeCell();
		var r1c1 = MakeCell();
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(2000f), new TableGridColumn(3000f)],
			Rows =
			[
				new TableRowElement { Cells = [restart, r0c1], HeightTwips = 300f },
				new TableRowElement { Cells = [cont, r1c1], HeightTwips = 400f },
			],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var positions = TableLayoutEngine.ComputeCellPositions(layout);

		// 3 unique cells: restart (spans 2 rows), r0c1, r1c1
		positions.Should().HaveCount(3);

		var pRestart = positions.First(p => p.Cell == restart);
		pRestart.X.Should().Be(0f);
		pRestart.Y.Should().Be(0f);
		pRestart.Width.Should().Be(2000f);
		pRestart.Height.Should().Be(700f); // 300 + 400
	}

	[Fact]
	public void ComputeCellPositions_CombinedMerge_SpansRowsAndColumns()
	{
		// 3x2 grid: top-left cell spans 2 columns and 2 rows
		var big = MakeCell(gridSpan: 2, verticalMerge: VerticalMergeState.Restart);
		var bigCont = MakeCell(gridSpan: 2, verticalMerge: VerticalMergeState.Continue);
		var r0c2 = MakeCell();
		var r1c2 = MakeCell();
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1000f), new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement { Cells = [big, r0c2], HeightTwips = 500f },
				new TableRowElement { Cells = [bigCont, r1c2], HeightTwips = 500f },
			],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var positions = TableLayoutEngine.ComputeCellPositions(layout);

		// 3 unique cells: big (spans 2×2), r0c2, r1c2
		positions.Should().HaveCount(3);

		var pBig = positions.First(p => p.Cell == big);
		pBig.X.Should().Be(0f);
		pBig.Y.Should().Be(0f);
		pBig.Width.Should().Be(2000f);
		pBig.Height.Should().Be(1000f);
	}

	[Fact]
	public void ComputeCellPositions_FewerCellsThanColumns_SomeCellsNull()
	{
		var cell = MakeCell();
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(2000f), new TableGridColumn(3000f)],
			Rows = [new TableRowElement { Cells = [cell], HeightTwips = 400f }],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var positions = TableLayoutEngine.ComputeCellPositions(layout);

		// Only 1 cell positioned (other columns are null in the grid)
		positions.Should().HaveCount(1);
		positions[0].Cell.Should().BeSameAs(cell);
	}

	[Fact]
	public void ComputeCellPositions_ThreeRows_YOffsetsAccumulate()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(4800f)],
			Rows =
			[
				new TableRowElement { Cells = [MakeCell()], HeightTwips = 300f },
				new TableRowElement { Cells = [MakeCell()], HeightTwips = 400f },
				new TableRowElement { Cells = [MakeCell()], HeightTwips = 500f },
			],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var positions = TableLayoutEngine.ComputeCellPositions(layout);

		positions.Should().HaveCount(3);
		positions[0].Y.Should().Be(0f);
		positions[1].Y.Should().Be(300f);
		positions[2].Y.Should().Be(700f);
	}

	// ---- Horizontal merges (4.4.1) ----

	[Fact]
	public void ComputeHorizontalMergeRegions_NullLayout_ThrowsArgumentNullException()
	{
		var act = () => TableLayoutEngine.ComputeHorizontalMergeRegions(null!);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("layout");
	}

	[Fact]
	public void ComputeHorizontalMergeRegions_EmptyTable_ReturnsEmpty()
	{
		var table = new TableElement { GridColumns = [], Rows = [] };
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var regions = TableLayoutEngine.ComputeHorizontalMergeRegions(layout);

		regions.Should().BeEmpty();
	}

	[Fact]
	public void ComputeHorizontalMergeRegions_NoSpans_ReturnsEmpty()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(2000f), new TableGridColumn(2000f)],
			Rows = [new TableRowElement { Cells = [MakeCell(), MakeCell()] }],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var regions = TableLayoutEngine.ComputeHorizontalMergeRegions(layout);

		regions.Should().BeEmpty();
	}

	[Fact]
	public void ComputeHorizontalMergeRegions_SingleSpan_ReturnsRegionWithGeometry()
	{
		var merged = MakeCell(gridSpan: 2);
		var regular = MakeCell();
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1500f), new TableGridColumn(2000f)],
			Rows = [new TableRowElement { Cells = [merged, regular], HeightTwips = 300f }],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var regions = TableLayoutEngine.ComputeHorizontalMergeRegions(layout);

		regions.Should().HaveCount(1);
		regions[0].RowIndex.Should().Be(0);
		regions[0].StartColumnIndex.Should().Be(0);
		regions[0].ColumnSpan.Should().Be(2);
		regions[0].X.Should().Be(0f);
		regions[0].Y.Should().Be(0f);
		regions[0].Width.Should().Be(2500f);
		regions[0].Height.Should().Be(300f);
		regions[0].Cell.Should().BeSameAs(merged);
	}

	[Fact]
	public void ComputeHorizontalMergeRegions_MultipleRows_ReturnsRegionsInOrder()
	{
		var r0 = MakeCell(gridSpan: 2);
		var r1 = MakeCell(gridSpan: 2);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1000f), new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement { Cells = [r0, MakeCell()] },
				new TableRowElement { Cells = [MakeCell(), r1] },
			],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var regions = TableLayoutEngine.ComputeHorizontalMergeRegions(layout);

		regions.Should().HaveCount(2);
		regions[0].RowIndex.Should().Be(0);
		regions[0].StartColumnIndex.Should().Be(0);
		regions[1].RowIndex.Should().Be(1);
		regions[1].StartColumnIndex.Should().Be(1);
	}

	[Fact]
	public void ComputeHorizontalMergeRegions_ClipsSpanAtGridEnd()
	{
		var oversized = MakeCell(gridSpan: 5);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1000f), new TableGridColumn(1000f)],
			Rows = [new TableRowElement { Cells = [MakeCell(), oversized] }],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var regions = TableLayoutEngine.ComputeHorizontalMergeRegions(layout);

		regions.Should().HaveCount(1);
		regions[0].StartColumnIndex.Should().Be(1);
		regions[0].ColumnSpan.Should().Be(2);
	}

	// ---- Vertical merges (4.4.2) ----

	[Fact]
	public void ComputeVerticalMergeRegions_NullLayout_ThrowsArgumentNullException()
	{
		var act = () => TableLayoutEngine.ComputeVerticalMergeRegions(null!);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("layout");
	}

	[Fact]
	public void ComputeVerticalMergeRegions_EmptyTable_ReturnsEmpty()
	{
		var table = new TableElement { GridColumns = [], Rows = [] };
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var regions = TableLayoutEngine.ComputeVerticalMergeRegions(layout);

		regions.Should().BeEmpty();
	}

	[Fact]
	public void ComputeVerticalMergeRegions_NoVerticalMerges_ReturnsEmpty()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(2000f)],
			Rows =
			[
				new TableRowElement { Cells = [MakeCell()], HeightTwips = 300f },
				new TableRowElement { Cells = [MakeCell()], HeightTwips = 400f },
			],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var regions = TableLayoutEngine.ComputeVerticalMergeRegions(layout);

		regions.Should().BeEmpty();
	}

	[Fact]
	public void ComputeVerticalMergeRegions_SingleVerticalMerge_ReturnsRegionWithGeometry()
	{
		var restart = MakeCell(verticalMerge: VerticalMergeState.Restart);
		var cont = MakeCell(verticalMerge: VerticalMergeState.Continue);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1200f), new TableGridColumn(1800f)],
			Rows =
			[
				new TableRowElement { Cells = [restart, MakeCell()], HeightTwips = 300f },
				new TableRowElement { Cells = [cont, MakeCell()], HeightTwips = 500f },
			],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var regions = TableLayoutEngine.ComputeVerticalMergeRegions(layout);

		regions.Should().HaveCount(1);
		regions[0].StartRowIndex.Should().Be(0);
		regions[0].ColumnIndex.Should().Be(0);
		regions[0].RowSpan.Should().Be(2);
		regions[0].X.Should().Be(0f);
		regions[0].Y.Should().Be(0f);
		regions[0].Width.Should().Be(1200f);
		regions[0].Height.Should().Be(800f);
		regions[0].Cell.Should().BeSameAs(restart);
	}
}
