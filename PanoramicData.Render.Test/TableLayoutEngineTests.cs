namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using static PanoramicData.Render.Test.NestedTableBuilders;
using static PanoramicData.Render.Test.TableLayoutBuilders;

public sealed partial class TableLayoutEngineTests
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

	[Theory]
	[InlineData("Center", 4800f, 2400f)]
	[InlineData("Right", 4800f, 4800f)]
	[InlineData("Center", 12000f, 0f)]
	[InlineData("Right", 12000f, 0f)]
	public void Layout_AlignedTable_XOffsetFollowsAlignmentAndClampsToZero(string alignment, float columnWidth, float expectedOffset)
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(columnWidth)],
			Rows = [new TableRowElement { Cells = [MakeCell()] }],
			Alignment = Enum.Parse<TableAlignment>(alignment),
		};

		var result = TableLayoutEngine.Layout(table, 9600f);

		result.TableXOffset.Should().Be(expectedOffset);
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

	// ---- Auto-fit (4.3.1) ----

	[Fact]
	public void ComputeAutoFitColumnWidths_EmptyGrid_ReturnsEmpty()
	{
		var table = new TableElement { GridColumns = [], Rows = [] };

		var widths = TableLayoutEngine.ComputeAutoFitColumnWidths(table, 9600f);

		widths.Should().BeEmpty();
	}

	[Fact]
	public void MeasureColumnWidths_EmptyTable_ReturnsMinimums()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(0f), new TableGridColumn(0f)],
			Rows = [new TableRowElement { Cells = [MakeCell(), MakeCell()] }],
		};

		var measurements = TableLayoutEngine.MeasureColumnWidths(table);

		measurements.Should().HaveCount(2);
		// Empty cells → MinimumColumnWidthTwips is enforced for both preferred and minimum
		measurements[0].PreferredWidthTwips.Should().Be(TableLayoutEngine.MinimumColumnWidthTwips);
		measurements[0].MinimumWidthTwips.Should().Be(TableLayoutEngine.MinimumColumnWidthTwips);
	}

	[Fact]
	public void MeasureColumnWidths_CellsWithContent_UsesEstimates()
	{
		var cell0 = MakeCellWithParagraphs(1);
		var cell1 = MakeCellWithParagraphs(3);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(0f), new TableGridColumn(0f)],
			Rows = [new TableRowElement { Cells = [cell0, cell1] }],
		};

		var measurements = TableLayoutEngine.MeasureColumnWidths(table);

		// Empty paragraphs → preferred = DefaultBlockWidthTwips (widest block, not sum)
		measurements[0].PreferredWidthTwips.Should().Be(TableLayoutEngine.DefaultBlockWidthTwips);
		measurements[1].PreferredWidthTwips.Should().Be(TableLayoutEngine.DefaultBlockWidthTwips);
		// Minimums
		measurements[0].MinimumWidthTwips.Should().Be(TableLayoutEngine.MinimumColumnWidthTwips);
		measurements[1].MinimumWidthTwips.Should().Be(TableLayoutEngine.MinimumColumnWidthTwips);
	}

	[Fact]
	public void MeasureColumnWidths_SpannedCell_DistributesEvenly()
	{
		var spannedCell = MakeCellWithParagraphs(2);
		spannedCell = new TableCellElement
		{
			Blocks = spannedCell.Blocks,
			GridSpan = 2,
		};
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(0f), new TableGridColumn(0f)],
			Rows = [new TableRowElement { Cells = [spannedCell] }],
		};

		var measurements = TableLayoutEngine.MeasureColumnWidths(table);

		// Empty text paragraphs → max preferred = DefaultBlockWidthTwips (2400) / 2 = 1200
		// But clamped to MinimumColumnWidthTwips
		var expected = Math.Max(TableLayoutEngine.DefaultBlockWidthTwips / 2f, TableLayoutEngine.MinimumColumnWidthTwips);
		measurements[0].PreferredWidthTwips.Should().Be(expected);
		measurements[1].PreferredWidthTwips.Should().Be(expected);
	}

	[Fact]
	public void MeasureColumnWidths_ContinueCell_Skipped()
	{
		var restart = MakeCellWithParagraphs(3);
		var cont = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() },
					  new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() },
					  new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() },
					  new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() },
					  new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() }],
			VerticalMerge = VerticalMergeState.Continue,
		};
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(0f)],
			Rows =
			[
				new TableRowElement { Cells = [restart] },
				new TableRowElement { Cells = [cont] },
			],
		};

		var measurements = TableLayoutEngine.MeasureColumnWidths(table);

		// Only restart cell counted, empty text → preferred = DefaultBlockWidthTwips (widest block)
		measurements[0].PreferredWidthTwips.Should().Be(TableLayoutEngine.DefaultBlockWidthTwips);
	}

	[Fact]
	public void DistributeColumnWidths_Empty_ReturnsEmpty()
	{
		var widths = TableLayoutEngine.DistributeColumnWidths([], 9600f);

		widths.Should().BeEmpty();
	}

	[Fact]
	public void DistributeColumnWidths_PreferredFitsInAvailable_ScalesUp()
	{
		ColumnMeasurement[] measurements =
		[
			new(2000f, 500f),
			new(4000f, 500f),
		];

		var widths = TableLayoutEngine.DistributeColumnWidths(measurements, 9000f);

		// totalPreferred = 6000, available = 9000, remaining = 3000
		// col0: 2000 + (3000 × 2000/6000) = 2000 + 1000 = 3000
		// col1: 4000 + (3000 × 4000/6000) = 4000 + 2000 = 6000
		widths[0].Should().Be(3000f);
		widths[1].Should().Be(6000f);
	}

	[Fact]
	public void DistributeColumnWidths_PreferredExceedsAvailable_DistributesBetweenMinAndPref()
	{
		ColumnMeasurement[] measurements =
		[
			new(5000f, 1000f),
			new(5000f, 1000f),
		];

		var widths = TableLayoutEngine.DistributeColumnWidths(measurements, 6000f);

		// totalMin = 2000, totalPref = 10000, excess = 6000-2000 = 4000
		// each stretch = 5000-1000 = 4000, totalStretch = 8000
		// col0: 1000 + (4000 × 4000/8000) = 1000 + 2000 = 3000
		// col1: same = 3000
		widths[0].Should().Be(3000f);
		widths[1].Should().Be(3000f);
	}

	[Fact]
	public void DistributeColumnWidths_MinimumExceedsAvailable_UsesMinimums()
	{
		ColumnMeasurement[] measurements =
		{
			new(5000f, 3000f),
			new(5000f, 3000f),
		};

		var widths = TableLayoutEngine.DistributeColumnWidths(measurements, 4000f);

		// totalMin = 6000 > 4000, just use minimums
		widths[0].Should().Be(3000f);
		widths[1].Should().Be(3000f);
	}

	[Fact]
	public void EstimateCellPreferredWidth_EmptyBlocks_ReturnsMinimum()
	{
		var cell = MakeCell();

		var width = TableLayoutEngine.EstimateCellPreferredWidth(cell);

		width.Should().Be(TableLayoutEngine.MinimumColumnWidthTwips);
	}
}
