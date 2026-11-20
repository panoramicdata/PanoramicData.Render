namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed partial class TableLayoutEngineTests
{
	[Fact]
	public void ComputeRowHeights_WithNoColumnWidths_FallsBackToDefaultComputeRowHeights()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(0f)],
			Rows = [new TableRowElement { Cells = [MakeCellWithParagraphs(2)] }],
		};

		var baseHeights = TableLayoutEngine.ComputeRowHeights(table);
		var fallbackHeights = TableLayoutEngine.ComputeRowHeights(table, []);

		fallbackHeights.Should().BeEquivalentTo(baseHeights);
	}

	[Fact]
	public void ComputeRowHeights_WithColumnWidths_MoreCellsThanColumns_IgnoresExtraCells()
	{
		var first = MakeCellWithParagraphs(1);
		var extra = MakeCellWithParagraphs(10);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(0f)],
			Rows = [new TableRowElement { Cells = [first, extra] }],
		};

		var heights = TableLayoutEngine.ComputeRowHeights(table, [1200f]);

		heights.Should().HaveCount(1);
		heights[0].Should().Be(TableLayoutEngine.DefaultRowHeightTwips);
	}

	// ---- Auto-fit content-pattern tests (4.3.8) ----

	[Fact]
	public void ComputeAutoFitColumnWidths_UniformContent_ColumnsAreNearEqual()
	{
		var table = AutoFitTable(2, RowOfText("alpha beta gamma", "alpha beta gamma"));

		var widths = TableLayoutEngine.ComputeAutoFitColumnWidths(table, 9600f);

		widths.Should().HaveCount(2);
		(widths[0] + widths[1]).Should().BeApproximately(9600f, 0.01f);
		MathF.Abs(widths[0] - widths[1]).Should().BeLessThan(1f);
	}

	[Fact]
	public void ComputeAutoFitColumnWidths_LongTextColumn_GetsMoreWidthThanShortColumn()
	{
		var table = AutoFitTable(2, RowOfText("lorem ipsum dolor sit amet consectetur adipiscing elit", "x"));

		var widths = TableLayoutEngine.ComputeAutoFitColumnWidths(table, 9600f);

		widths[0].Should().BeGreaterThan(widths[1]);
	}

	[Fact]
	public void ComputeAutoFitColumnWidths_LongUnbreakableWord_RespectsMinimumWordWidth()
	{
		// 20 * 140 = 2800 twips minimum
		var table = AutoFitTable(2, RowOfText(new string('a', 20), "x"));

		var widths = TableLayoutEngine.ComputeAutoFitColumnWidths(table, 2000f);

		widths[0].Should().Be(2800f);
		widths[1].Should().Be(TableLayoutEngine.MinimumColumnWidthTwips);
	}

	[Fact]
	public void ComputeAutoFitColumnWidths_SpannedCellPattern_DistributesAcrossSpannedColumns()
	{
		var table = AutoFitTable(
			2,
			RowOf(TextCell("span cell content across both columns", gridSpan: 2)),
			RowOfText("left", "right"));

		var widths = TableLayoutEngine.ComputeAutoFitColumnWidths(table, 9600f);

		widths.Should().HaveCount(2);
		MathF.Abs(widths[0] - widths[1]).Should().BeLessThan(300f);
	}

	[Fact]
	public void ComputeAutoFitColumnWidths_ThreeColumnMixedPattern_MonotonicByContentWeight()
	{
		var table = AutoFitTable(3, RowOfText("a", "medium sized text", "this is the longest content column in this row"));

		var widths = TableLayoutEngine.ComputeAutoFitColumnWidths(table, 12000f);

		widths[2].Should().BeGreaterThan(widths[1]);
		widths[1].Should().BeGreaterThan(widths[0]);
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

	[Fact]
	public void ComputeColumnOffsets_BiDi_ThreeColumns_MirroredOffsets()
	{
		// Total width = 1000 + 2000 + 3000 = 6000
		// Column 0 (width 1000): offset = 6000 - 0 - 1000 = 5000
		// Column 1 (width 2000): offset = 6000 - 1000 - 2000 = 3000
		// Column 2 (width 3000): offset = 6000 - 3000 - 3000 = 0
		var offsets = TableLayoutEngine.ComputeColumnOffsets([1000f, 2000f, 3000f], isBiDi: true);

		offsets.Should().HaveCount(3);
		offsets[0].Should().Be(5000f);
		offsets[1].Should().Be(3000f);
		offsets[2].Should().Be(0f);
	}

	[Fact]
	public void ComputeColumnOffsets_BiDi_SingleColumn_StartsAtZero()
	{
		var offsets = TableLayoutEngine.ComputeColumnOffsets([4800f], isBiDi: true);

		offsets.Should().HaveCount(1);
		offsets[0].Should().Be(0f);
	}

	[Fact]
	public void ComputeColumnOffsets_BiDi_Empty_ReturnsEmpty()
	{
		var offsets = TableLayoutEngine.ComputeColumnOffsets([], isBiDi: true);

		offsets.Should().BeEmpty();
	}

	// ---- ComputeRowHeights ----

	[Fact]
	public void ComputeRowHeights_NoRows_ReturnsEmpty()
	{
		var heights = TableLayoutEngine.ComputeRowHeights(EmptyTable());

		heights.Should().BeEmpty();
	}

	[Fact]
	public void ComputeRowHeights_ExplicitHeight_UsesExplicit()
	{
		var heights = TableLayoutEngine.ComputeRowHeights(SingleColumnTable(HeightRow(500f)));

		heights[0].Should().Be(500f);
	}

	[Fact]
	public void ComputeRowHeights_NoHeight_UsesDefault()
	{
		var heights = TableLayoutEngine.ComputeRowHeights(SingleColumnTable(HeightRow(0f)));

		heights[0].Should().Be(TableLayoutEngine.DefaultRowHeightTwips);
	}

	[Fact]
	public void ComputeRowHeights_MixedRows_CorrectHeights()
	{
		var table = SingleColumnTable(HeightRow(600f), HeightRow(0f), HeightRow(400f));

		var heights = TableLayoutEngine.ComputeRowHeights(table);

		heights[0].Should().Be(600f);
		heights[1].Should().Be(TableLayoutEngine.DefaultRowHeightTwips);
		heights[2].Should().Be(400f);
	}

	[Fact]
	public void Layout_TotalHeight_SumsRowHeights()
	{
		var table = SingleColumnTable(HeightRow(300f), HeightRow(400f));

		var result = TableLayoutEngine.Layout(table, 9600f);

		result.TotalHeightTwips.Should().Be(700f);
	}
	// ---- ComputeTableXOffset ----

	[Theory]
	[InlineData("Left", 360f, 4800f, 9600f, 360f)]
	[InlineData("Center", 0f, 4000f, 10000f, 3000f)]
	[InlineData("Right", 0f, 4000f, 10000f, 6000f)]
	public void ComputeTableXOffset_ByAlignment_PositionsTable(string alignment, float indentation, float tableWidth, float availableWidth, float expected)
	{
		var table = EmptyTable(Enum.Parse<TableAlignment>(alignment), indentation);

		var offset = TableLayoutEngine.ComputeTableXOffset(table, tableWidth, availableWidth);

		offset.Should().Be(expected);
	}
	// ---- Integration: Layout produces LayoutBlock ----
}
