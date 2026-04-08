namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class TableLayoutEngineTests
{
	// ---- Layout (4.2.1) ----

	[Fact]
	public void Layout_NullTable_ThrowsArgumentNullException()
	{
		var act = () => TableLayoutEngine.Layout(null!, 9600f);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("table");
	}

	[Fact]
	public void Layout_EmptyTable_ReturnsZeroWidthAndHeight()
	{
		var table = new TableElement
		{
			GridColumns = [],
			Rows = [],
		};

		var result = TableLayoutEngine.Layout(table, 9600f);

		result.TableWidthTwips.Should().Be(0f);
		result.TotalHeightTwips.Should().Be(0f);
		result.ColumnOffsets.Should().BeEmpty();
		result.ColumnWidths.Should().BeEmpty();
		result.RowHeights.Should().BeEmpty();
		result.Table.Should().BeSameAs(table);
	}

	[Fact]
	public void Layout_SingleColumn_WidthEqualsGridColumn()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(4800f)],
			Rows = [new TableRowElement { Cells = [MakeCell()] }],
		};

		var result = TableLayoutEngine.Layout(table, 9600f);

		result.ColumnWidths.Should().HaveCount(1);
		result.ColumnWidths[0].Should().Be(4800f);
		result.TableWidthTwips.Should().Be(4800f);
	}

	[Fact]
	public void Layout_ThreeColumns_WidthsSumToTableWidth()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(2000f), new TableGridColumn(3000f), new TableGridColumn(4000f)],
			Rows = [new TableRowElement { Cells = [MakeCell(), MakeCell(), MakeCell()] }],
		};

		var result = TableLayoutEngine.Layout(table, 9600f);

		result.ColumnWidths.Should().HaveCount(3);
		result.ColumnWidths[0].Should().Be(2000f);
		result.ColumnWidths[1].Should().Be(3000f);
		result.ColumnWidths[2].Should().Be(4000f);
		result.TableWidthTwips.Should().Be(9000f);
	}

	[Fact]
	public void Layout_LeftAligned_XOffsetIsIndentation()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(4800f)],
			Rows = [new TableRowElement { Cells = [MakeCell()] }],
			IndentationTwips = 720f,
		};

		var result = TableLayoutEngine.Layout(table, 9600f);

		result.TableXOffset.Should().Be(720f);
	}

	[Fact]
	public void Layout_CenterAligned_XOffsetCenters()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(4800f)],
			Rows = [new TableRowElement { Cells = [MakeCell()] }],
			Alignment = TableAlignment.Center,
		};

		var result = TableLayoutEngine.Layout(table, 9600f);

		// (9600 - 4800) / 2 = 2400
		result.TableXOffset.Should().Be(2400f);
	}

	[Fact]
	public void Layout_RightAligned_XOffsetAlignsRight()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(4800f)],
			Rows = [new TableRowElement { Cells = [MakeCell()] }],
			Alignment = TableAlignment.Right,
		};

		var result = TableLayoutEngine.Layout(table, 9600f);

		// 9600 - 4800 = 4800
		result.TableXOffset.Should().Be(4800f);
	}

	[Fact]
	public void Layout_CenterAligned_WiderThanAvailable_XOffsetClampsToZero()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(12000f)],
			Rows = [new TableRowElement { Cells = [MakeCell()] }],
			Alignment = TableAlignment.Center,
		};

		var result = TableLayoutEngine.Layout(table, 9600f);

		result.TableXOffset.Should().Be(0f);
	}

	[Fact]
	public void Layout_RightAligned_WiderThanAvailable_XOffsetClampsToZero()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(12000f)],
			Rows = [new TableRowElement { Cells = [MakeCell()] }],
			Alignment = TableAlignment.Right,
		};

		var result = TableLayoutEngine.Layout(table, 9600f);

		result.TableXOffset.Should().Be(0f);
	}

	// ---- ComputeFixedColumnWidths ----

	[Fact]
	public void ComputeFixedColumnWidths_NoColumns_ReturnsEmpty()
	{
		var table = new TableElement
		{
			GridColumns = [],
			Rows = [],
		};

		var widths = TableLayoutEngine.ComputeFixedColumnWidths(table, 9600f);

		widths.Should().BeEmpty();
	}

	[Fact]
	public void ComputeFixedColumnWidths_AllExplicit_UsesGridWidths()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1200f), new TableGridColumn(2400f)],
			Rows = [],
		};

		var widths = TableLayoutEngine.ComputeFixedColumnWidths(table, 9600f);

		widths[0].Should().Be(1200f);
		widths[1].Should().Be(2400f);
	}

	[Fact]
	public void ComputeFixedColumnWidths_AllZero_DistributesEqually()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(0f), new TableGridColumn(0f), new TableGridColumn(0f)],
			Rows = [],
		};

		var widths = TableLayoutEngine.ComputeFixedColumnWidths(table, 9000f);

		widths[0].Should().Be(3000f);
		widths[1].Should().Be(3000f);
		widths[2].Should().Be(3000f);
	}

	[Fact]
	public void ComputeFixedColumnWidths_MixedExplicitAndZero_DistributesRemainder()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(2000f), new TableGridColumn(0f), new TableGridColumn(3000f)],
			Rows = [],
		};

		var widths = TableLayoutEngine.ComputeFixedColumnWidths(table, 9000f);

		widths[0].Should().Be(2000f);
		widths[1].Should().Be(4000f); // 9000 - 2000 - 3000 = 4000
		widths[2].Should().Be(3000f);
	}

	[Fact]
	public void ComputeFixedColumnWidths_ExplicitExceedsAvailable_ZeroColumnsGetZero()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(5000f), new TableGridColumn(5000f), new TableGridColumn(0f)],
			Rows = [],
		};

		var widths = TableLayoutEngine.ComputeFixedColumnWidths(table, 9000f);

		widths[0].Should().Be(5000f);
		widths[1].Should().Be(5000f);
		widths[2].Should().Be(0f); // max(0, 9000 - 10000) / 1 = 0
	}

	// ---- ComputeColumnOffsets ----

	[Fact]
	public void ComputeColumnOffsets_Empty_ReturnsEmpty()
	{
		var offsets = TableLayoutEngine.ComputeColumnOffsets([]);

		offsets.Should().BeEmpty();
	}

	[Fact]
	public void ComputeColumnOffsets_SingleColumn_StartsAtZero()
	{
		var offsets = TableLayoutEngine.ComputeColumnOffsets([4800f]);

		offsets.Should().HaveCount(1);
		offsets[0].Should().Be(0f);
	}

	[Fact]
	public void ComputeColumnOffsets_ThreeColumns_CumulativeOffsets()
	{
		var offsets = TableLayoutEngine.ComputeColumnOffsets([1000f, 2000f, 3000f]);

		offsets.Should().HaveCount(3);
		offsets[0].Should().Be(0f);
		offsets[1].Should().Be(1000f);
		offsets[2].Should().Be(3000f);
	}

	// ---- ComputeRowHeights ----

	[Fact]
	public void ComputeRowHeights_NoRows_ReturnsEmpty()
	{
		var table = new TableElement
		{
			GridColumns = [],
			Rows = [],
		};

		var heights = TableLayoutEngine.ComputeRowHeights(table);

		heights.Should().BeEmpty();
	}

	[Fact]
	public void ComputeRowHeights_ExplicitHeight_UsesExplicit()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(4800f)],
			Rows = [new TableRowElement { Cells = [MakeCell()], HeightTwips = 500f }],
		};

		var heights = TableLayoutEngine.ComputeRowHeights(table);

		heights[0].Should().Be(500f);
	}

	[Fact]
	public void ComputeRowHeights_NoHeight_UsesDefault()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(4800f)],
			Rows = [new TableRowElement { Cells = [MakeCell()] }],
		};

		var heights = TableLayoutEngine.ComputeRowHeights(table);

		heights[0].Should().Be(TableLayoutEngine.DefaultRowHeightTwips);
	}

	[Fact]
	public void ComputeRowHeights_MixedRows_CorrectHeights()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(4800f)],
			Rows =
			[
				new TableRowElement { Cells = [MakeCell()], HeightTwips = 600f },
				new TableRowElement { Cells = [MakeCell()] },
				new TableRowElement { Cells = [MakeCell()], HeightTwips = 400f },
			],
		};

		var heights = TableLayoutEngine.ComputeRowHeights(table);

		heights[0].Should().Be(600f);
		heights[1].Should().Be(TableLayoutEngine.DefaultRowHeightTwips);
		heights[2].Should().Be(400f);
	}

	[Fact]
	public void Layout_TotalHeight_SumsRowHeights()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(4800f)],
			Rows =
			[
				new TableRowElement { Cells = [MakeCell()], HeightTwips = 300f },
				new TableRowElement { Cells = [MakeCell()], HeightTwips = 400f },
			],
		};

		var result = TableLayoutEngine.Layout(table, 9600f);

		result.TotalHeightTwips.Should().Be(700f);
	}

	// ---- ComputeTableXOffset ----

	[Fact]
	public void ComputeTableXOffset_LeftAlignment_ReturnsIndentation()
	{
		var table = new TableElement
		{
			GridColumns = [],
			Rows = [],
			IndentationTwips = 360f,
		};

		var offset = TableLayoutEngine.ComputeTableXOffset(table, 4800f, 9600f);

		offset.Should().Be(360f);
	}

	[Fact]
	public void ComputeTableXOffset_Center_CentersTable()
	{
		var table = new TableElement
		{
			GridColumns = [],
			Rows = [],
			Alignment = TableAlignment.Center,
		};

		var offset = TableLayoutEngine.ComputeTableXOffset(table, 4000f, 10000f);

		offset.Should().Be(3000f);
	}

	[Fact]
	public void ComputeTableXOffset_Right_AlignsRight()
	{
		var table = new TableElement
		{
			GridColumns = [],
			Rows = [],
			Alignment = TableAlignment.Right,
		};

		var offset = TableLayoutEngine.ComputeTableXOffset(table, 4000f, 10000f);

		offset.Should().Be(6000f);
	}

	// ---- Integration: Layout produces LayoutBlock ----

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

	private static TableCellElement MakeCell() => new()
	{
		Blocks = [],
	};
}
