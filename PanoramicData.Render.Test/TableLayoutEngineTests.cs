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
		var measurements = new ColumnMeasurement[]
		{
			new(2000f, 500f),
			new(4000f, 500f),
		};

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
		var measurements = new ColumnMeasurement[]
		{
			new(5000f, 1000f),
			new(5000f, 1000f),
		};

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
		var measurements = new ColumnMeasurement[]
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

	[Fact]
	public void EstimateCellPreferredWidth_WithExplicitWidth_UsesExplicit()
	{
		var cell = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() }],
			Width = new TableWidthValue(3500f, TableWidthUnit.Dxa),
		};

		var width = TableLayoutEngine.EstimateCellPreferredWidth(cell);

		width.Should().Be(3500f);
	}

	[Fact]
	public void EstimateCellPreferredWidth_WithMargins_IncludesMargins()
	{
		var cell = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() }],
			Margins = new CellMargins(0f, 100f, 0f, 100f),
		};

		var width = TableLayoutEngine.EstimateCellPreferredWidth(cell);

		// 1×2400 + 100 + 100 = 2600
		width.Should().Be(2600f);
	}

	[Fact]
	public void EstimateCellMinimumWidth_EmptyBlocks_ReturnsZeroPlusMargins()
	{
		var cell = new TableCellElement
		{
			Blocks = [],
			Margins = new CellMargins(0f, 50f, 0f, 50f),
		};

		var width = TableLayoutEngine.EstimateCellMinimumWidth(cell);

		width.Should().Be(100f); // 0 + 50 + 50
	}

	[Fact]
	public void EstimateCellMinimumWidth_WithBlocks_ReturnsMinPlusMargins()
	{
		var cell = MakeCellWithParagraphs(3, new CellMargins(0f, 80f, 0f, 80f));

		var width = TableLayoutEngine.EstimateCellMinimumWidth(cell);

		// MinimumColumnWidthTwips + 80 + 80 = 360 + 160 = 520
		width.Should().Be(520f);
	}

	[Fact]
	public void EstimateBlockPreferredWidth_EmptyParagraph_ReturnsDefault()
	{
		var block = new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() };

		var width = TableLayoutEngine.EstimateBlockPreferredWidth(block);

		width.Should().Be(TableLayoutEngine.DefaultBlockWidthTwips);
	}

	[Fact]
	public void EstimateBlockPreferredWidth_ParagraphWithText_ReturnsLengthTimesCharWidth()
	{
		var para = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
			new DocumentFormat.OpenXml.Wordprocessing.Run(
				new DocumentFormat.OpenXml.Wordprocessing.Text("Hello World")));
		var block = new ParagraphBlock { SourceElement = para };

		var width = TableLayoutEngine.EstimateBlockPreferredWidth(block);

		// "Hello World" = 11 chars × 140 = 1540
		width.Should().Be(11f * TableLayoutEngine.AverageCharWidthTwips);
	}

	[Fact]
	public void EstimateBlockPreferredWidth_NonParagraphBlock_ReturnsDefault()
	{
		var block = new TablePlaceholderBlock { TableElement = new DocumentFormat.OpenXml.Wordprocessing.Table() };

		var width = TableLayoutEngine.EstimateBlockPreferredWidth(block);

		width.Should().Be(TableLayoutEngine.DefaultBlockWidthTwips);
	}

	[Fact]
	public void EstimateBlockPreferredWidth_NestedTable_UsesNestedTablePreferredWidth()
	{
		var nested = new DocumentFormat.OpenXml.Wordprocessing.Table(
			new DocumentFormat.OpenXml.Wordprocessing.TableGrid(
				new DocumentFormat.OpenXml.Wordprocessing.GridColumn { Width = "0" },
				new DocumentFormat.OpenXml.Wordprocessing.GridColumn { Width = "0" }),
			new DocumentFormat.OpenXml.Wordprocessing.TableRow(
				new DocumentFormat.OpenXml.Wordprocessing.TableCell(
					new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
						new DocumentFormat.OpenXml.Wordprocessing.Run(
							new DocumentFormat.OpenXml.Wordprocessing.Text("nested nested nested")))),
				new DocumentFormat.OpenXml.Wordprocessing.TableCell(
					new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
						new DocumentFormat.OpenXml.Wordprocessing.Run(
							new DocumentFormat.OpenXml.Wordprocessing.Text("x"))))));
		var block = new TablePlaceholderBlock { TableElement = nested };

		var width = TableLayoutEngine.EstimateBlockPreferredWidth(block);

		width.Should().BeGreaterThan(TableLayoutEngine.DefaultBlockWidthTwips);
	}

	[Fact]
	public void EstimateBlockMinimumWidth_EmptyParagraph_ReturnsMinimum()
	{
		var block = new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() };

		var width = TableLayoutEngine.EstimateBlockMinimumWidth(block);

		width.Should().Be(TableLayoutEngine.MinimumColumnWidthTwips);
	}

	[Fact]
	public void EstimateBlockMinimumWidth_ParagraphWithText_ReturnsLongestWordWidth()
	{
		var para = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
			new DocumentFormat.OpenXml.Wordprocessing.Run(
				new DocumentFormat.OpenXml.Wordprocessing.Text("Hello wonderful World")));
		var block = new ParagraphBlock { SourceElement = para };

		var width = TableLayoutEngine.EstimateBlockMinimumWidth(block);

		// "wonderful" = 9 chars × 140 = 1260
		width.Should().Be(9f * TableLayoutEngine.AverageCharWidthTwips);
	}

	[Fact]
	public void EstimateBlockMinimumWidth_NonParagraphBlock_ReturnsMinimum()
	{
		var block = new TablePlaceholderBlock { TableElement = new DocumentFormat.OpenXml.Wordprocessing.Table() };

		var width = TableLayoutEngine.EstimateBlockMinimumWidth(block);

		width.Should().Be(TableLayoutEngine.MinimumColumnWidthTwips);
	}

	[Fact]
	public void EstimateBlockMinimumWidth_NestedTable_UsesNestedTableMinimumWidth()
	{
		var nested = new DocumentFormat.OpenXml.Wordprocessing.Table(
			new DocumentFormat.OpenXml.Wordprocessing.TableGrid(
				new DocumentFormat.OpenXml.Wordprocessing.GridColumn { Width = "0" },
				new DocumentFormat.OpenXml.Wordprocessing.GridColumn { Width = "0" }),
			new DocumentFormat.OpenXml.Wordprocessing.TableRow(
				new DocumentFormat.OpenXml.Wordprocessing.TableCell(
					new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
						new DocumentFormat.OpenXml.Wordprocessing.Run(
							new DocumentFormat.OpenXml.Wordprocessing.Text("averyveryverylongtoken")))),
				new DocumentFormat.OpenXml.Wordprocessing.TableCell(
					new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
						new DocumentFormat.OpenXml.Wordprocessing.Run(
							new DocumentFormat.OpenXml.Wordprocessing.Text("short"))))));
		var block = new TablePlaceholderBlock { TableElement = nested };

		var width = TableLayoutEngine.EstimateBlockMinimumWidth(block);

		width.Should().BeGreaterThan(TableLayoutEngine.MinimumColumnWidthTwips);
	}

	[Fact]
	public void EstimateCellPreferredWidth_WithTextContent_UsesTextLength()
	{
		var para = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
			new DocumentFormat.OpenXml.Wordprocessing.Run(
				new DocumentFormat.OpenXml.Wordprocessing.Text("Short")));
		var cell = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = para }],
		};

		var width = TableLayoutEngine.EstimateCellPreferredWidth(cell);

		// "Short" = 5 chars × 140 = 700, + 0 margins = 700
		width.Should().Be(700f);
	}

	[Fact]
	public void EstimateCellMinimumWidth_WithTextContent_UsesLongestWord()
	{
		var para = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
			new DocumentFormat.OpenXml.Wordprocessing.Run(
				new DocumentFormat.OpenXml.Wordprocessing.Text("A longword here")));
		var cell = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = para }],
		};

		var width = TableLayoutEngine.EstimateCellMinimumWidth(cell);

		// "longword" = 8 chars × 140 = 1120
		width.Should().Be(8f * TableLayoutEngine.AverageCharWidthTwips);
	}

	[Fact]
	public void ComputeAutoFitColumnWidths_TwoColumnsWithText_ProducesReasonableWidths()
	{
		var shortPara = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
			new DocumentFormat.OpenXml.Wordprocessing.Run(
				new DocumentFormat.OpenXml.Wordprocessing.Text("Short")));
		var longPara = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
			new DocumentFormat.OpenXml.Wordprocessing.Run(
				new DocumentFormat.OpenXml.Wordprocessing.Text("This is a much longer paragraph of text")));
		var cell0 = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = shortPara }],
		};
		var cell1 = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = longPara }],
		};
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(0f), new TableGridColumn(0f)],
			Rows = [new TableRowElement { Cells = [cell0, cell1] }],
		};

		var widths = TableLayoutEngine.ComputeAutoFitColumnWidths(table, 9600f);

		widths.Should().HaveCount(2);
		// Total should equal available width (since preferred < available, it scales up)
		var total = widths[0] + widths[1];
		total.Should().BeApproximately(9600f, 0.01f);
		// The longer text column should get proportionally more space
		widths[1].Should().BeGreaterThan(widths[0]);
	}

	[Fact]
	public void ComputeAutoFitColumnWidths_PercentageWidths_ResolvedProportionally()
	{
		// 30% and 70% columns (value in fiftieths of a percent: 30% = 1500, 70% = 3500)
		var cell0 = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() }],
			Width = new TableWidthValue(1500f, TableWidthUnit.Pct),
		};
		var cell1 = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() }],
			Width = new TableWidthValue(3500f, TableWidthUnit.Pct),
		};
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(0f), new TableGridColumn(0f)],
			Rows = [new TableRowElement { Cells = [cell0, cell1] }],
		};

		var widths = TableLayoutEngine.ComputeAutoFitColumnWidths(table, 10000f);

		widths.Should().HaveCount(2);
		// 30% of 10000 = 3000, 70% of 10000 = 7000
		widths[0].Should().Be(3000f);
		widths[1].Should().Be(7000f);
	}

	[Fact]
	public void ComputeAutoFitColumnWidths_MixedFixedAndAuto_FixedColumnsKeptAutoDistributed()
	{
		var fixedCell = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() }],
			Width = new TableWidthValue(3000f, TableWidthUnit.Dxa),
		};
		var autoCell = MakeCell();
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(0f), new TableGridColumn(0f)],
			Rows = [new TableRowElement { Cells = [fixedCell, autoCell] }],
		};

		var widths = TableLayoutEngine.ComputeAutoFitColumnWidths(table, 9000f);

		widths.Should().HaveCount(2);
		// First column fixed at 3000
		widths[0].Should().Be(3000f);
		// Second column gets remaining 6000
		widths[1].Should().Be(6000f);
	}

	[Fact]
	public void ComputeAutoFitColumnWidths_NestedTableCell_IncreasesOwningColumnWidth()
	{
		var nested = new DocumentFormat.OpenXml.Wordprocessing.Table(
			new DocumentFormat.OpenXml.Wordprocessing.TableGrid(
				new DocumentFormat.OpenXml.Wordprocessing.GridColumn { Width = "0" },
				new DocumentFormat.OpenXml.Wordprocessing.GridColumn { Width = "0" }),
			new DocumentFormat.OpenXml.Wordprocessing.TableRow(
				new DocumentFormat.OpenXml.Wordprocessing.TableCell(
					new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
						new DocumentFormat.OpenXml.Wordprocessing.Run(
							new DocumentFormat.OpenXml.Wordprocessing.Text("nested nested nested nested")))),
				new DocumentFormat.OpenXml.Wordprocessing.TableCell(
					new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
						new DocumentFormat.OpenXml.Wordprocessing.Run(
							new DocumentFormat.OpenXml.Wordprocessing.Text("nested"))))));

		var nestedCell = new TableCellElement
		{
			Blocks = [new TablePlaceholderBlock { TableElement = nested }],
		};
		var textCell = new TableCellElement
		{
			Blocks =
			[
				new ParagraphBlock
				{
					SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
						new DocumentFormat.OpenXml.Wordprocessing.Run(
							new DocumentFormat.OpenXml.Wordprocessing.Text("x"))),
				},
			],
		};

		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(0f), new TableGridColumn(0f)],
			Rows = [new TableRowElement { Cells = [nestedCell, textCell] }],
		};

		var widths = TableLayoutEngine.ComputeAutoFitColumnWidths(table, 10000f);

		widths.Should().HaveCount(2);
		widths[0].Should().BeGreaterThan(widths[1]);
	}

	[Fact]
	public void ResolveExplicitColumnWidths_NoExplicit_AllNull()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(0f), new TableGridColumn(0f)],
			Rows = [new TableRowElement { Cells = [MakeCell(), MakeCell()] }],
		};
		var fixedWidths = new float?[2];

		TableLayoutEngine.ResolveExplicitColumnWidths(table, 10000f, fixedWidths);

		fixedWidths[0].Should().BeNull();
		fixedWidths[1].Should().BeNull();
	}

	[Fact]
	public void ResolveExplicitColumnWidths_PercentageWidth_ConvertedToTwips()
	{
		var cell = new TableCellElement
		{
			Blocks = [],
			Width = new TableWidthValue(2500f, TableWidthUnit.Pct), // 50%
		};
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(0f)],
			Rows = [new TableRowElement { Cells = [cell] }],
		};
		var fixedWidths = new float?[1];

		TableLayoutEngine.ResolveExplicitColumnWidths(table, 10000f, fixedWidths);

		fixedWidths[0].Should().Be(5000f);
	}

	[Fact]
	public void ResolveExplicitColumnWidths_SkipsContinueCells()
	{
		var cont = new TableCellElement
		{
			Blocks = [],
			Width = new TableWidthValue(3000f, TableWidthUnit.Dxa),
			VerticalMerge = VerticalMergeState.Continue,
		};
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(0f)],
			Rows = [new TableRowElement { Cells = [cont] }],
		};
		var fixedWidths = new float?[1];

		TableLayoutEngine.ResolveExplicitColumnWidths(table, 10000f, fixedWidths);

		fixedWidths[0].Should().BeNull();
	}

	[Fact]
	public void ResolveExplicitColumnWidths_SkipsSpannedCells()
	{
		var spanned = new TableCellElement
		{
			Blocks = [],
			Width = new TableWidthValue(3000f, TableWidthUnit.Dxa),
			GridSpan = 2,
		};
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(0f), new TableGridColumn(0f)],
			Rows = [new TableRowElement { Cells = [spanned] }],
		};
		var fixedWidths = new float?[2];

		TableLayoutEngine.ResolveExplicitColumnWidths(table, 10000f, fixedWidths);

		fixedWidths[0].Should().BeNull();
		fixedWidths[1].Should().BeNull();
	}

	[Fact]
	public void ResolveExplicitColumnWidths_MoreCellsThanColumns_Stops()
	{
		var cell0 = new TableCellElement
		{
			Blocks = [],
			Width = new TableWidthValue(2000f, TableWidthUnit.Dxa),
		};
		var extra = new TableCellElement
		{
			Blocks = [],
			Width = new TableWidthValue(5000f, TableWidthUnit.Dxa),
		};
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(0f)],
			Rows = [new TableRowElement { Cells = [cell0, extra] }],
		};
		var fixedWidths = new float?[1];

		TableLayoutEngine.ResolveExplicitColumnWidths(table, 10000f, fixedWidths);

		fixedWidths[0].Should().Be(2000f);
	}

	[Fact]
	public void DistributeWithFixedColumns_NoFixed_DelegatesToDistribute()
	{
		var measurements = new ColumnMeasurement[] { new(2000f, 500f), new(3000f, 500f) };
		var fixedWidths = new float?[2];

		var widths = TableLayoutEngine.DistributeWithFixedColumns(measurements, fixedWidths, 10000f);

		widths.Should().HaveCount(2);
		(widths[0] + widths[1]).Should().BeApproximately(10000f, 0.01f);
	}

	[Fact]
	public void DistributeWithFixedColumns_AllFixed_UsesFixedWidths()
	{
		var measurements = new ColumnMeasurement[] { new(2000f, 500f), new(3000f, 500f) };
		var fixedWidths = new float?[] { 4000f, 5000f };

		var widths = TableLayoutEngine.DistributeWithFixedColumns(measurements, fixedWidths, 10000f);

		widths[0].Should().Be(4000f);
		widths[1].Should().Be(5000f);
	}

	[Fact]
	public void DistributeWithFixedColumns_Empty_ReturnsEmpty()
	{
		var widths = TableLayoutEngine.DistributeWithFixedColumns([], [], 10000f);

		widths.Should().BeEmpty();
	}

	[Fact]
	public void DistributeColumnWidths_MinimumEqualsPreferred_UsesMinimums()
	{
		var measurements = new ColumnMeasurement[]
		{
			new(1000f, 1000f),
			new(2000f, 2000f),
		};

		// totalMin = 3000, totalPref = 3000, totalStretch = 0
		// Available exceeds preferred, so preferred path is taken
		var widths = TableLayoutEngine.DistributeColumnWidths(measurements, 1500f);

		// totalMin (3000) >= available (1500), so use minimums
		widths[0].Should().Be(1000f);
		widths[1].Should().Be(2000f);
	}

	[Fact]
	public void MeasureColumnWidths_MoreCellsThanColumns_IgnoresExtraCells()
	{
		var cell0 = MakeCellWithParagraphs(1);
		var cell1 = MakeCellWithParagraphs(2);
		var extraCell = MakeCellWithParagraphs(5);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(0f)], // Only 1 column
			Rows = [new TableRowElement { Cells = [cell0, cell1, extraCell] }],
		};

		var measurements = TableLayoutEngine.MeasureColumnWidths(table);

		measurements.Should().HaveCount(1);
		// Only cell0 counted, empty text → preferred = DefaultBlockWidthTwips
		measurements[0].PreferredWidthTwips.Should().Be(TableLayoutEngine.DefaultBlockWidthTwips);
	}

	[Fact]
	public void MeasureColumnWidths_WithBorderSpacing_IncludesSpacingInset()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(0f)],
			Rows = [new TableRowElement { Cells = [MakeCellWithParagraphs(1)] }],
			BorderSpacingTwips = 100f,
		};

		var measurements = TableLayoutEngine.MeasureColumnWidths(table);

		measurements.Should().HaveCount(1);
		measurements[0].PreferredWidthTwips.Should().Be(TableLayoutEngine.DefaultBlockWidthTwips + 200f);
		measurements[0].MinimumWidthTwips.Should().Be(TableLayoutEngine.MinimumColumnWidthTwips + 200f);
	}

	// ---- Auto-fit re-layout (4.3.7) ----

	[Fact]
	public void LayoutAutoFit_NullTable_ThrowsArgumentNullException()
	{
		var act = () => TableLayoutEngine.LayoutAutoFit(null!, 9600f);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("table");
	}

	[Fact]
	public void LayoutAutoFit_UsesAutoFitWidths_AndWidthAwareRowHeights()
	{
		var longText = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
			new DocumentFormat.OpenXml.Wordprocessing.Run(
				new DocumentFormat.OpenXml.Wordprocessing.Text("word word word word word word word word word word")));
		var shortText = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
			new DocumentFormat.OpenXml.Wordprocessing.Run(
				new DocumentFormat.OpenXml.Wordprocessing.Text("x")));

		var longCell = new TableCellElement { Blocks = [new ParagraphBlock { SourceElement = longText }] };
		var shortCell = new TableCellElement { Blocks = [new ParagraphBlock { SourceElement = shortText }] };

		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(0f), new TableGridColumn(0f)],
			Rows = [new TableRowElement { Cells = [longCell, shortCell] }],
		};

		var layout = TableLayoutEngine.LayoutAutoFit(table, 2000f);

		layout.ColumnWidths.Should().HaveCount(2);
		(layout.ColumnWidths[0] + layout.ColumnWidths[1]).Should().BeApproximately(2000f, 0.01f);
		layout.ColumnWidths[0].Should().BeGreaterThan(layout.ColumnWidths[1]);

		// Long text should wrap under the final width, so row height should exceed one line.
		layout.RowHeights[0].Should().BeGreaterThan(TableLayoutEngine.DefaultRowHeightTwips);
	}

	[Fact]
	public void MeasureCellContentHeight_ForWidth_NarrowerWidthIncreasesHeight()
	{
		var text = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
			new DocumentFormat.OpenXml.Wordprocessing.Run(
				new DocumentFormat.OpenXml.Wordprocessing.Text("aaaaaaaaaaaaaaaaaaaa")));
		var cell = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = text }],
		};

		var wide = TableLayoutEngine.MeasureCellContentHeight(cell, 4000f);
		var narrow = TableLayoutEngine.MeasureCellContentHeight(cell, 700f);

		narrow.Should().BeGreaterThan(wide);
	}

	[Fact]
	public void MeasureCellContentHeight_ForWidth_ZeroContentWidthFallsBackToSingleLine()
	{
		var text = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
			new DocumentFormat.OpenXml.Wordprocessing.Run(
				new DocumentFormat.OpenXml.Wordprocessing.Text("text")));
		var cell = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = text }],
			Margins = new CellMargins(10f, 600f, 20f, 600f),
		};

		// cellWidth 1000 with left+right margins 1200 => content width = 0
		var height = TableLayoutEngine.MeasureCellContentHeight(cell, 1000f);

		height.Should().Be(TableLayoutEngine.DefaultRowHeightTwips + 30f);
	}

	[Fact]
	public void MeasureCellContentHeight_ForWidth_EmptyParagraphUsesSingleLineHeight()
	{
		var cell = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() }],
		};

		var height = TableLayoutEngine.MeasureCellContentHeight(cell, 200f);

		height.Should().Be(TableLayoutEngine.DefaultRowHeightTwips);
	}

	[Fact]
	public void MeasureCellContentHeight_ForWidth_NonParagraphUsesDefaultHeight()
	{
		var cell = new TableCellElement
		{
			Blocks = [new TablePlaceholderBlock { TableElement = new DocumentFormat.OpenXml.Wordprocessing.Table() }],
		};

		var height = TableLayoutEngine.MeasureCellContentHeight(cell, 200f);

		height.Should().Be(TableLayoutEngine.DefaultRowHeightTwips);
	}

	[Fact]
	public void MeasureCellContentHeight_ForWidth_NestedTableUsesRecursiveHeight()
	{
		var nestedTable = new DocumentFormat.OpenXml.Wordprocessing.Table(
			new DocumentFormat.OpenXml.Wordprocessing.TableGrid(
				new DocumentFormat.OpenXml.Wordprocessing.GridColumn { Width = "1000" }),
			new DocumentFormat.OpenXml.Wordprocessing.TableRow(
				new DocumentFormat.OpenXml.Wordprocessing.TableCell(new DocumentFormat.OpenXml.Wordprocessing.Paragraph())),
			new DocumentFormat.OpenXml.Wordprocessing.TableRow(
				new DocumentFormat.OpenXml.Wordprocessing.TableCell(new DocumentFormat.OpenXml.Wordprocessing.Paragraph())),
			new DocumentFormat.OpenXml.Wordprocessing.TableRow(
				new DocumentFormat.OpenXml.Wordprocessing.TableCell(new DocumentFormat.OpenXml.Wordprocessing.Paragraph())));

		var cell = new TableCellElement
		{
			Blocks = [new TablePlaceholderBlock { TableElement = nestedTable }],
		};

		var height = TableLayoutEngine.MeasureCellContentHeight(cell, 2000f);

		height.Should().Be(3f * TableLayoutEngine.DefaultRowHeightTwips);
	}

	[Fact]
	public void LayoutCellContent_NestedTableBlock_UsesRecursiveHeightInLayoutBlocks()
	{
		var nestedTable = new DocumentFormat.OpenXml.Wordprocessing.Table(
			new DocumentFormat.OpenXml.Wordprocessing.TableGrid(
				new DocumentFormat.OpenXml.Wordprocessing.GridColumn { Width = "1000" }),
			new DocumentFormat.OpenXml.Wordprocessing.TableRow(
				new DocumentFormat.OpenXml.Wordprocessing.TableCell(new DocumentFormat.OpenXml.Wordprocessing.Paragraph())),
			new DocumentFormat.OpenXml.Wordprocessing.TableRow(
				new DocumentFormat.OpenXml.Wordprocessing.TableCell(new DocumentFormat.OpenXml.Wordprocessing.Paragraph())));

		var cell = new TableCellElement
		{
			Blocks = [new TablePlaceholderBlock { TableElement = nestedTable }],
		};

		var (blocks, totalHeight) = TableLayoutEngine.LayoutCellContent(cell);

		blocks.Should().ContainSingle();
		blocks[0].HeightTwips.Should().Be(2f * TableLayoutEngine.DefaultRowHeightTwips);
		totalHeight.Should().Be(2f * TableLayoutEngine.DefaultRowHeightTwips);
	}

	[Fact]
	public void Layout_NestedTableCell_UsesNestedTableHeightForRow()
	{
		var nestedTable = new DocumentFormat.OpenXml.Wordprocessing.Table(
			new DocumentFormat.OpenXml.Wordprocessing.TableGrid(
				new DocumentFormat.OpenXml.Wordprocessing.GridColumn { Width = "1200" }),
			new DocumentFormat.OpenXml.Wordprocessing.TableRow(
				new DocumentFormat.OpenXml.Wordprocessing.TableCell(new DocumentFormat.OpenXml.Wordprocessing.Paragraph())),
			new DocumentFormat.OpenXml.Wordprocessing.TableRow(
				new DocumentFormat.OpenXml.Wordprocessing.TableCell(new DocumentFormat.OpenXml.Wordprocessing.Paragraph())));

		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(2400f)],
			Rows =
			[
				new TableRowElement
				{
					Cells =
					[
						new TableCellElement
						{
							Blocks = [new TablePlaceholderBlock { TableElement = nestedTable }],
						},
					],
				},
			],
		};

		var layout = TableLayoutEngine.Layout(table, 2400f);

		layout.RowHeights.Should().ContainSingle();
		layout.RowHeights[0].Should().Be(2f * TableLayoutEngine.DefaultRowHeightTwips);
	}

	[Fact]
	public void LayoutAutoFit_NestedTableCell_PropagatesNestedTableHeight()
	{
		var nestedTable = new DocumentFormat.OpenXml.Wordprocessing.Table(
			new DocumentFormat.OpenXml.Wordprocessing.TableGrid(
				new DocumentFormat.OpenXml.Wordprocessing.GridColumn { Width = "0" },
				new DocumentFormat.OpenXml.Wordprocessing.GridColumn { Width = "0" }),
			new DocumentFormat.OpenXml.Wordprocessing.TableRow(
				new DocumentFormat.OpenXml.Wordprocessing.TableCell(new DocumentFormat.OpenXml.Wordprocessing.Paragraph()),
				new DocumentFormat.OpenXml.Wordprocessing.TableCell(new DocumentFormat.OpenXml.Wordprocessing.Paragraph())),
			new DocumentFormat.OpenXml.Wordprocessing.TableRow(
				new DocumentFormat.OpenXml.Wordprocessing.TableCell(new DocumentFormat.OpenXml.Wordprocessing.Paragraph()),
				new DocumentFormat.OpenXml.Wordprocessing.TableCell(new DocumentFormat.OpenXml.Wordprocessing.Paragraph())));

		var nestedCell = new TableCellElement
		{
			Blocks = [new TablePlaceholderBlock { TableElement = nestedTable }],
		};
		var shortTextCell = new TableCellElement
		{
			Blocks =
			[
				new ParagraphBlock
				{
					SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
						new DocumentFormat.OpenXml.Wordprocessing.Run(
							new DocumentFormat.OpenXml.Wordprocessing.Text("x"))),
				},
			],
		};

		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(0f), new TableGridColumn(0f)],
			Rows = [new TableRowElement { Cells = [nestedCell, shortTextCell] }],
		};

		var layout = TableLayoutEngine.LayoutAutoFit(table, 4000f);

		layout.ColumnWidths[0].Should().BeGreaterThan(layout.ColumnWidths[1]);
		layout.RowHeights[0].Should().Be(2f * TableLayoutEngine.DefaultRowHeightTwips);
	}

	[Fact]
	public void LayoutAutoFit_NestedTableWithWrappedText_IncreasesOuterRowHeight()
	{
		var longParagraph = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
			new DocumentFormat.OpenXml.Wordprocessing.Run(
				new DocumentFormat.OpenXml.Wordprocessing.Text("nested nested nested nested nested nested nested nested")));
		var nestedTable = new DocumentFormat.OpenXml.Wordprocessing.Table(
			new DocumentFormat.OpenXml.Wordprocessing.TableGrid(
				new DocumentFormat.OpenXml.Wordprocessing.GridColumn { Width = "0" }),
			new DocumentFormat.OpenXml.Wordprocessing.TableRow(
				new DocumentFormat.OpenXml.Wordprocessing.TableCell(longParagraph)));

		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(0f)],
			Rows =
			[
				new TableRowElement
				{
					Cells =
					[
						new TableCellElement
						{
							Blocks = [new TablePlaceholderBlock { TableElement = nestedTable }],
						},
					],
				},
			],
		};

		var layout = TableLayoutEngine.LayoutAutoFit(table, 1000f);

		layout.RowHeights.Should().ContainSingle();
		layout.RowHeights[0].Should().BeGreaterThan(TableLayoutEngine.DefaultRowHeightTwips);
	}

	[Fact]
	public void ComputeRowHeights_WithColumnWidths_ExactRuleStillWins()
	{
		var text = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
			new DocumentFormat.OpenXml.Wordprocessing.Run(
				new DocumentFormat.OpenXml.Wordprocessing.Text("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")));
		var cell = new TableCellElement { Blocks = [new ParagraphBlock { SourceElement = text }] };
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(0f)],
			Rows =
			[
				new TableRowElement
				{
					Cells = [cell],
					HeightRule = RowHeightRule.Exact,
					HeightTwips = 300f,
				},
			],
		};

		var heights = TableLayoutEngine.ComputeRowHeights(table, [360f]);

		heights[0].Should().Be(300f);
	}

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
		var p0 = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
			new DocumentFormat.OpenXml.Wordprocessing.Run(
				new DocumentFormat.OpenXml.Wordprocessing.Text("alpha beta gamma")));
		var p1 = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
			new DocumentFormat.OpenXml.Wordprocessing.Run(
				new DocumentFormat.OpenXml.Wordprocessing.Text("alpha beta gamma")));

		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(0f), new TableGridColumn(0f)],
			Rows =
			[
				new TableRowElement
				{
					Cells =
					[
						new TableCellElement { Blocks = [new ParagraphBlock { SourceElement = p0 }] },
						new TableCellElement { Blocks = [new ParagraphBlock { SourceElement = p1 }] },
					],
				},
			],
		};

		var widths = TableLayoutEngine.ComputeAutoFitColumnWidths(table, 9600f);

		widths.Should().HaveCount(2);
		(widths[0] + widths[1]).Should().BeApproximately(9600f, 0.01f);
		MathF.Abs(widths[0] - widths[1]).Should().BeLessThan(1f);
	}

	[Fact]
	public void ComputeAutoFitColumnWidths_LongTextColumn_GetsMoreWidthThanShortColumn()
	{
		var longPara = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
			new DocumentFormat.OpenXml.Wordprocessing.Run(
				new DocumentFormat.OpenXml.Wordprocessing.Text("lorem ipsum dolor sit amet consectetur adipiscing elit")));
		var shortPara = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
			new DocumentFormat.OpenXml.Wordprocessing.Run(
				new DocumentFormat.OpenXml.Wordprocessing.Text("x")));

		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(0f), new TableGridColumn(0f)],
			Rows =
			[
				new TableRowElement
				{
					Cells =
					[
						new TableCellElement { Blocks = [new ParagraphBlock { SourceElement = longPara }] },
						new TableCellElement { Blocks = [new ParagraphBlock { SourceElement = shortPara }] },
					],
				},
			],
		};

		var widths = TableLayoutEngine.ComputeAutoFitColumnWidths(table, 9600f);

		widths[0].Should().BeGreaterThan(widths[1]);
	}

	[Fact]
	public void ComputeAutoFitColumnWidths_LongUnbreakableWord_RespectsMinimumWordWidth()
	{
		var longWord = new string('a', 20); // 20 * 140 = 2800 twips minimum
		var longPara = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
			new DocumentFormat.OpenXml.Wordprocessing.Run(
				new DocumentFormat.OpenXml.Wordprocessing.Text(longWord)));
		var shortPara = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
			new DocumentFormat.OpenXml.Wordprocessing.Run(
				new DocumentFormat.OpenXml.Wordprocessing.Text("x")));

		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(0f), new TableGridColumn(0f)],
			Rows =
			[
				new TableRowElement
				{
					Cells =
					[
						new TableCellElement { Blocks = [new ParagraphBlock { SourceElement = longPara }] },
						new TableCellElement { Blocks = [new ParagraphBlock { SourceElement = shortPara }] },
					],
				},
			],
		};

		var widths = TableLayoutEngine.ComputeAutoFitColumnWidths(table, 2000f);

		widths[0].Should().Be(2800f);
		widths[1].Should().Be(TableLayoutEngine.MinimumColumnWidthTwips);
	}

	[Fact]
	public void ComputeAutoFitColumnWidths_SpannedCellPattern_DistributesAcrossSpannedColumns()
	{
		var spannedPara = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
			new DocumentFormat.OpenXml.Wordprocessing.Run(
				new DocumentFormat.OpenXml.Wordprocessing.Text("span cell content across both columns")));
		var row2c0 = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
			new DocumentFormat.OpenXml.Wordprocessing.Run(
				new DocumentFormat.OpenXml.Wordprocessing.Text("left")));
		var row2c1 = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
			new DocumentFormat.OpenXml.Wordprocessing.Run(
				new DocumentFormat.OpenXml.Wordprocessing.Text("right")));

		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(0f), new TableGridColumn(0f)],
			Rows =
			[
				new TableRowElement
				{
					Cells = [new TableCellElement { Blocks = [new ParagraphBlock { SourceElement = spannedPara }], GridSpan = 2 }],
				},
				new TableRowElement
				{
					Cells =
					[
						new TableCellElement { Blocks = [new ParagraphBlock { SourceElement = row2c0 }] },
						new TableCellElement { Blocks = [new ParagraphBlock { SourceElement = row2c1 }] },
					],
				},
			],
		};

		var widths = TableLayoutEngine.ComputeAutoFitColumnWidths(table, 9600f);

		widths.Should().HaveCount(2);
		MathF.Abs(widths[0] - widths[1]).Should().BeLessThan(300f);
	}

	[Fact]
	public void ComputeAutoFitColumnWidths_ThreeColumnMixedPattern_MonotonicByContentWeight()
	{
		var shortPara = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
			new DocumentFormat.OpenXml.Wordprocessing.Run(
				new DocumentFormat.OpenXml.Wordprocessing.Text("a")));
		var mediumPara = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
			new DocumentFormat.OpenXml.Wordprocessing.Run(
				new DocumentFormat.OpenXml.Wordprocessing.Text("medium sized text")));
		var longPara = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
			new DocumentFormat.OpenXml.Wordprocessing.Run(
				new DocumentFormat.OpenXml.Wordprocessing.Text("this is the longest content column in this row")));

		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(0f), new TableGridColumn(0f), new TableGridColumn(0f)],
			Rows =
			[
				new TableRowElement
				{
					Cells =
					[
						new TableCellElement { Blocks = [new ParagraphBlock { SourceElement = shortPara }] },
						new TableCellElement { Blocks = [new ParagraphBlock { SourceElement = mediumPara }] },
						new TableCellElement { Blocks = [new ParagraphBlock { SourceElement = longPara }] },
					],
				},
			],
		};

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

	[Fact]
	public void ComputeVerticalMergeRegions_MultipleRegions_ReturnsInOrder()
	{
		var r0c0 = MakeCell(verticalMerge: VerticalMergeState.Restart);
		var r1c0 = MakeCell(verticalMerge: VerticalMergeState.Continue);
		var r0c1 = MakeCell(verticalMerge: VerticalMergeState.Restart);
		var r1c1 = MakeCell(verticalMerge: VerticalMergeState.Continue);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement { Cells = [r0c0, r0c1], HeightTwips = 300f },
				new TableRowElement { Cells = [r1c0, r1c1], HeightTwips = 300f },
			],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var regions = TableLayoutEngine.ComputeVerticalMergeRegions(layout);

		regions.Should().HaveCount(2);
		regions[0].StartRowIndex.Should().Be(0);
		regions[0].ColumnIndex.Should().Be(0);
		regions[1].StartRowIndex.Should().Be(0);
		regions[1].ColumnIndex.Should().Be(1);
	}

	[Fact]
	public void ComputeVerticalMergeRegions_ContinueWithoutRestart_IsIgnored()
	{
		var orphanContinue = MakeCell(verticalMerge: VerticalMergeState.Continue);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f)],
			Rows = [new TableRowElement { Cells = [orphanContinue], HeightTwips = 300f }],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var regions = TableLayoutEngine.ComputeVerticalMergeRegions(layout);

		regions.Should().BeEmpty();
	}

	[Fact]
	public void ComputeVerticalMergeRegions_WithHorizontalAndVerticalMerge_UsesOwnerGeometry()
	{
		var restart = MakeCell(gridSpan: 2, verticalMerge: VerticalMergeState.Restart);
		var cont = MakeCell(gridSpan: 2, verticalMerge: VerticalMergeState.Continue);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1500f), new TableGridColumn(2000f)],
			Rows =
			[
				new TableRowElement { Cells = [restart, MakeCell()], HeightTwips = 400f },
				new TableRowElement { Cells = [cont, MakeCell()], HeightTwips = 600f },
			],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var regions = TableLayoutEngine.ComputeVerticalMergeRegions(layout);

		regions.Should().HaveCount(1);
		regions[0].ColumnIndex.Should().Be(0);
		regions[0].RowSpan.Should().Be(2);
		regions[0].Width.Should().Be(2500f);
		regions[0].Height.Should().Be(1000f);
		regions[0].Cell.Should().BeSameAs(restart);
	}

	// ---- Combined merge regions (4.4.3) ----

	[Fact]
	public void ComputeMergedCellRegions_NullLayout_ThrowsArgumentNullException()
	{
		var act = () => TableLayoutEngine.ComputeMergedCellRegions(null!);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("layout");
	}

	[Fact]
	public void ComputeMergedCellRegions_EmptyTable_ReturnsEmpty()
	{
		var table = new TableElement { GridColumns = [], Rows = [] };
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var regions = TableLayoutEngine.ComputeMergedCellRegions(layout);

		regions.Should().BeEmpty();
	}

	[Fact]
	public void ComputeMergedCellRegions_NoMerges_ReturnsEmpty()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement { Cells = [MakeCell(), MakeCell()] },
				new TableRowElement { Cells = [MakeCell(), MakeCell()] },
			],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var regions = TableLayoutEngine.ComputeMergedCellRegions(layout);

		regions.Should().BeEmpty();
	}

	[Fact]
	public void ComputeMergedCellRegions_HorizontalOnly_IncludesRegion()
	{
		var merged = MakeCell(gridSpan: 2);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1200f), new TableGridColumn(800f), new TableGridColumn(1000f)],
			Rows = [new TableRowElement { Cells = [merged, MakeCell()], HeightTwips = 300f }],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var regions = TableLayoutEngine.ComputeMergedCellRegions(layout);

		regions.Should().HaveCount(1);
		regions[0].RowSpan.Should().Be(1);
		regions[0].ColumnSpan.Should().Be(2);
		regions[0].Width.Should().Be(2000f);
		regions[0].Height.Should().Be(300f);
	}

	[Fact]
	public void ComputeMergedCellRegions_VerticalOnly_IncludesRegion()
	{
		var restart = MakeCell(verticalMerge: VerticalMergeState.Restart);
		var cont = MakeCell(verticalMerge: VerticalMergeState.Continue);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement { Cells = [restart], HeightTwips = 250f },
				new TableRowElement { Cells = [cont], HeightTwips = 350f },
			],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var regions = TableLayoutEngine.ComputeMergedCellRegions(layout);

		regions.Should().HaveCount(1);
		regions[0].RowSpan.Should().Be(2);
		regions[0].ColumnSpan.Should().Be(1);
		regions[0].Width.Should().Be(1000f);
		regions[0].Height.Should().Be(600f);
	}

	[Fact]
	public void ComputeMergedCellRegions_CombinedHorizontalAndVertical_IncludesRectangularRegion()
	{
		var restart = MakeCell(gridSpan: 2, verticalMerge: VerticalMergeState.Restart);
		var cont = MakeCell(gridSpan: 2, verticalMerge: VerticalMergeState.Continue);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1500f), new TableGridColumn(500f)],
			Rows =
			[
				new TableRowElement { Cells = [restart, MakeCell()], HeightTwips = 400f },
				new TableRowElement { Cells = [cont, MakeCell()], HeightTwips = 600f },
			],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var regions = TableLayoutEngine.ComputeMergedCellRegions(layout);

		regions.Should().HaveCount(1);
		regions[0].StartRowIndex.Should().Be(0);
		regions[0].StartColumnIndex.Should().Be(0);
		regions[0].RowSpan.Should().Be(2);
		regions[0].ColumnSpan.Should().Be(2);
		regions[0].Width.Should().Be(2500f);
		regions[0].Height.Should().Be(1000f);
		regions[0].Cell.Should().BeSameAs(restart);
	}

	[Fact]
	public void ComputeMergedCellRegions_MixedMergedCells_ReturnsInReadingOrder()
	{
		var r0c0 = MakeCell(gridSpan: 2);
		var r1c0 = MakeCell(verticalMerge: VerticalMergeState.Restart);
		var r2c0 = MakeCell(verticalMerge: VerticalMergeState.Continue);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1000f), new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement { Cells = [r0c0, MakeCell()], HeightTwips = 300f },
				new TableRowElement { Cells = [r1c0, MakeCell(), MakeCell()], HeightTwips = 300f },
				new TableRowElement { Cells = [r2c0, MakeCell(), MakeCell()], HeightTwips = 300f },
			],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var regions = TableLayoutEngine.ComputeMergedCellRegions(layout);

		regions.Should().HaveCount(2);
		regions[0].StartRowIndex.Should().Be(0);
		regions[0].StartColumnIndex.Should().Be(0);
		regions[1].StartRowIndex.Should().Be(1);
		regions[1].StartColumnIndex.Should().Be(0);
	}

	// ---- Merged content layout (4.4.4) ----

	[Fact]
	public void ComputeMergedCellContentLayouts_NullLayout_ThrowsArgumentNullException()
	{
		var act = () => TableLayoutEngine.ComputeMergedCellContentLayouts(null!);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("layout");
	}

	[Fact]
	public void ComputeMergedCellContentLayouts_NoMergedCells_ReturnsEmpty()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f)],
			Rows = [new TableRowElement { Cells = [MakeCell()] }],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var contentLayouts = TableLayoutEngine.ComputeMergedCellContentLayouts(layout);

		contentLayouts.Should().BeEmpty();
	}

	[Fact]
	public void ComputeMergedCellContentLayouts_HorizontalMerge_AdjustsContentAreaForMargins()
	{
		var mergedCell = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() }],
			GridSpan = 2,
			Margins = new CellMargins(20f, 50f, 30f, 40f),
		};
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1500f), new TableGridColumn(500f)],
			Rows = [new TableRowElement { Cells = [mergedCell, MakeCell()], HeightTwips = 400f }],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var contentLayouts = TableLayoutEngine.ComputeMergedCellContentLayouts(layout);

		contentLayouts.Should().HaveCount(1);
		contentLayouts[0].CellWidth.Should().Be(2500f);
		contentLayouts[0].ContentX.Should().Be(40f); // left margin
		contentLayouts[0].ContentWidth.Should().Be(2410f); // 2500 - 40 - 50
		contentLayouts[0].ContentHeight.Should().Be(240f); // one paragraph block
		contentLayouts[0].Blocks.Should().HaveCount(1);
	}

	[Fact]
	public void ComputeMergedCellContentLayouts_VerticalMerge_CenterAlignmentAdjustsY()
	{
		var restartCell = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() }],
			VerticalMerge = VerticalMergeState.Restart,
			VerticalAlignment = CellVerticalAlignment.Center,
		};
		var continueCell = new TableCellElement
		{
			Blocks = [],
			VerticalMerge = VerticalMergeState.Continue,
		};
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1200f)],
			Rows =
			[
				new TableRowElement { Cells = [restartCell], HeightTwips = 300f },
				new TableRowElement { Cells = [continueCell], HeightTwips = 500f },
			],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var contentLayouts = TableLayoutEngine.ComputeMergedCellContentLayouts(layout);

		contentLayouts.Should().HaveCount(1);
		contentLayouts[0].CellHeight.Should().Be(800f);
		contentLayouts[0].ContentY.Should().Be(280f); // (800 - 240) / 2
	}

	[Fact]
	public void ComputeMergedCellContentLayouts_CombinedMerge_UsesRectangularRegionGeometry()
	{
		var restart = new TableCellElement
		{
			Blocks =
			[
				new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() },
				new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() },
			],
			GridSpan = 2,
			VerticalMerge = VerticalMergeState.Restart,
			Margins = new CellMargins(10f, 20f, 10f, 20f),
		};
		var cont = new TableCellElement
		{
			Blocks = [],
			GridSpan = 2,
			VerticalMerge = VerticalMergeState.Continue,
		};
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1500f), new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement { Cells = [restart, MakeCell()], HeightTwips = 400f },
				new TableRowElement { Cells = [cont, MakeCell()], HeightTwips = 600f },
			],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var contentLayouts = TableLayoutEngine.ComputeMergedCellContentLayouts(layout);

		contentLayouts.Should().HaveCount(1);
		contentLayouts[0].RowSpan.Should().Be(2);
		contentLayouts[0].ColumnSpan.Should().Be(2);
		contentLayouts[0].CellWidth.Should().Be(2500f);
		contentLayouts[0].CellHeight.Should().Be(1100f);
		contentLayouts[0].ContentWidth.Should().Be(2460f);
		contentLayouts[0].ContentHeight.Should().Be(480f); // 2 paragraph blocks
		contentLayouts[0].Blocks.Should().HaveCount(2);
	}

	[Fact]
	public void ComputeMergedCellContentLayouts_MultipleMergedCells_ReturnsInReadingOrder()
	{
		var horizontal = MakeCell(gridSpan: 2);
		var verticalStart = MakeCell(verticalMerge: VerticalMergeState.Restart);
		var verticalCont = MakeCell(verticalMerge: VerticalMergeState.Continue);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1000f), new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement { Cells = [horizontal, MakeCell()] },
				new TableRowElement { Cells = [verticalStart, MakeCell(), MakeCell()] },
				new TableRowElement { Cells = [verticalCont, MakeCell(), MakeCell()] },
			],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var contentLayouts = TableLayoutEngine.ComputeMergedCellContentLayouts(layout);

		contentLayouts.Should().HaveCount(2);
		contentLayouts[0].StartRowIndex.Should().Be(0);
		contentLayouts[0].StartColumnIndex.Should().Be(0);
		contentLayouts[1].StartRowIndex.Should().Be(1);
		contentLayouts[1].StartColumnIndex.Should().Be(0);
	}

	// ---- Table cell backgrounds (4.8.2) ----

	[Fact]
	public void ComputeCellBackgrounds_NullLayout_ThrowsArgumentNullException()
	{
		var act = () => TableLayoutEngine.ComputeCellBackgrounds(null!);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("layout");
	}

	[Fact]
	public void ComputeCellBackgrounds_NoVisibleShading_ReturnsEmpty()
	{
		var layout = TableLayoutEngine.Layout(
			new TableElement
			{
				GridColumns = [new TableGridColumn(1200f)],
				Rows = [new TableRowElement { Cells = [MakeCell()] }],
			},
			1200f);

		var backgrounds = TableLayoutEngine.ComputeCellBackgrounds(layout);

		backgrounds.Should().BeEmpty();
	}

	[Fact]
	public void ComputeCellBackgrounds_ShadedCells_ReturnsReadingOrderRectangles()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1500f)],
			Rows =
			[
				new TableRowElement
				{
					Cells =
					[
						new TableCellElement
						{
							Blocks = [],
							Shading = new ParagraphShading(ShadingPattern.Clear, FillColor: "FFFF00"),
						},
						new TableCellElement
						{
							Blocks = [],
							Shading = new ParagraphShading(ShadingPattern.Solid, PatternColor: "00FF00"),
						},
					],
				},
			],
		};

		var layout = TableLayoutEngine.Layout(table, 2500f);

		var backgrounds = TableLayoutEngine.ComputeCellBackgrounds(layout);

		backgrounds.Should().HaveCount(2);

		backgrounds[0].RowIndex.Should().Be(0);
		backgrounds[0].ColumnIndex.Should().Be(0);
		backgrounds[0].X.Should().Be(0f);
		backgrounds[0].Y.Should().Be(0f);
		backgrounds[0].Width.Should().Be(1000f);
		backgrounds[0].Height.Should().Be(240f);
		backgrounds[0].Shading.FillColor.Should().Be("FFFF00");

		backgrounds[1].RowIndex.Should().Be(0);
		backgrounds[1].ColumnIndex.Should().Be(1);
		backgrounds[1].X.Should().Be(1000f);
		backgrounds[1].Width.Should().Be(1500f);
		backgrounds[1].Shading.Pattern.Should().Be(ShadingPattern.Solid);
		backgrounds[1].Shading.PatternColor.Should().Be("00FF00");
	}

	[Fact]
	public void ComputeCellBackgrounds_MergedCell_UsesMergedCellGeometry()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement
				{
					Cells =
					[
						new TableCellElement
						{
							Blocks = [],
							GridSpan = 2,
							Shading = new ParagraphShading(ShadingPattern.Clear, FillColor: "ABCDEF"),
						},
					],
				},
				new TableRowElement { Cells = [MakeCell(), MakeCell()] },
			],
		};

		var layout = TableLayoutEngine.Layout(table, 2000f);

		var backgrounds = TableLayoutEngine.ComputeCellBackgrounds(layout);

		backgrounds.Should().ContainSingle();
		backgrounds[0].X.Should().Be(0f);
		backgrounds[0].Y.Should().Be(0f);
		backgrounds[0].Width.Should().Be(2000f);
		backgrounds[0].Height.Should().Be(240f);
	}

	// ---- Cell content layout (4.2.3) ----

	[Fact]
	public void LayoutCellContent_NullCell_ThrowsArgumentNullException()
	{
		var act = () => TableLayoutEngine.LayoutCellContent(null!);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("cell");
	}

	[Fact]
	public void LayoutCellContent_EmptyBlocks_ReturnsEmpty()
	{
		var cell = MakeCell();

		var (blocks, totalHeight) = TableLayoutEngine.LayoutCellContent(cell);

		blocks.Should().BeEmpty();
		totalHeight.Should().Be(0f);
	}

	[Fact]
	public void LayoutCellContent_SingleParagraph_ReturnsOneBlock()
	{
		var cell = MakeCellWithParagraphs(1);

		var (blocks, totalHeight) = TableLayoutEngine.LayoutCellContent(cell);

		blocks.Should().HaveCount(1);
		blocks[0].Block.Should().BeOfType<ParagraphBlock>();
		totalHeight.Should().Be(TableLayoutEngine.DefaultRowHeightTwips);
	}

	[Fact]
	public void LayoutCellContent_ThreeParagraphs_HeightIsSumOfEstimates()
	{
		var cell = MakeCellWithParagraphs(3);

		var (blocks, totalHeight) = TableLayoutEngine.LayoutCellContent(cell);

		blocks.Should().HaveCount(3);
		totalHeight.Should().Be(TableLayoutEngine.DefaultRowHeightTwips * 3f);
	}

	[Fact]
	public void LayoutCellContent_TablePlaceholderBlock_UsesDefaultHeight()
	{
		var cell = new TableCellElement
		{
			Blocks = [new TablePlaceholderBlock { TableElement = new DocumentFormat.OpenXml.Wordprocessing.Table() }],
		};

		var (blocks, totalHeight) = TableLayoutEngine.LayoutCellContent(cell);

		blocks.Should().HaveCount(1);
		totalHeight.Should().Be(TableLayoutEngine.DefaultRowHeightTwips);
	}

	[Fact]
	public void MeasureCellContentHeight_EmptyCell_ReturnsZero()
	{
		var cell = MakeCell();

		var height = TableLayoutEngine.MeasureCellContentHeight(cell);

		height.Should().Be(0f);
	}

	[Fact]
	public void MeasureCellContentHeight_TwoParagraphs_ReturnsSumOfEstimates()
	{
		var cell = MakeCellWithParagraphs(2);

		var height = TableLayoutEngine.MeasureCellContentHeight(cell);

		height.Should().Be(TableLayoutEngine.DefaultRowHeightTwips * 2f);
	}

	[Fact]
	public void ComputeRowHeights_ExactHeightRule_IgnoresContent()
	{
		var cell = MakeCellWithParagraphs(5);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(4800f)],
			Rows = [new TableRowElement
			{
				Cells = [cell],
				HeightTwips = 300f,
				HeightRule = RowHeightRule.Exact,
			}],
		};

		var heights = TableLayoutEngine.ComputeRowHeights(table);

		heights[0].Should().Be(300f); // Exact ignores content
	}

	[Fact]
	public void ComputeRowHeights_AtLeastHeightRule_UsesMaxOfSpecifiedAndContent()
	{
		var cell = MakeCellWithParagraphs(5); // 5 × 240 = 1200
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(4800f)],
			Rows = [new TableRowElement
			{
				Cells = [cell],
				HeightTwips = 300f,
				HeightRule = RowHeightRule.AtLeast,
			}],
		};

		var heights = TableLayoutEngine.ComputeRowHeights(table);

		heights[0].Should().Be(1200f); // Content (1200) > specified (300)
	}

	[Fact]
	public void ComputeRowHeights_AutoWithContent_UsesContentHeight()
	{
		var cell = MakeCellWithParagraphs(3); // 3 × 240 = 720
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(4800f)],
			Rows = [new TableRowElement
			{
				Cells = [cell],
				HeightTwips = 200f,
			}],
		};

		var heights = TableLayoutEngine.ComputeRowHeights(table);

		heights[0].Should().Be(720f); // Content (720) > specified (200)
	}

	[Fact]
	public void ComputeRowHeights_ContinueCell_IgnoredForContentHeight()
	{
		// A continue cell's content shouldn't affect the row height
		var contCell = new TableCellElement
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
			GridColumns = [new TableGridColumn(4800f)],
			Rows = [new TableRowElement { Cells = [contCell] }],
		};

		var heights = TableLayoutEngine.ComputeRowHeights(table);

		// Continue cell content ignored, no other cells → DefaultRowHeightTwips
		heights[0].Should().Be(TableLayoutEngine.DefaultRowHeightTwips);
	}

	private static TableCellElement MakeCellWithParagraphs(int count, CellMargins? margins = null)
	{
		var blocks = new List<DocumentBlock>();
		for (var i = 0; i < count; i++)
		{
			blocks.Add(new ParagraphBlock
			{
				SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph()
			});
		}

		return new TableCellElement { Blocks = blocks, Margins = margins ?? CellMargins.None };
	}

	private static TableCellElement MakeCell(
		int gridSpan = 1,
		VerticalMergeState verticalMerge = VerticalMergeState.None) => new()
	{
		Blocks = [],
		GridSpan = gridSpan,
		VerticalMerge = verticalMerge,
	};

	// ---- Cell margins (4.2.4) ----

	[Fact]
	public void ComputeContentWidth_NoMargins_ReturnsCellWidth()
	{
		var width = TableLayoutEngine.ComputeContentWidth(4800f, CellMargins.None);

		width.Should().Be(4800f);
	}

	[Fact]
	public void ComputeContentWidth_WithMargins_SubtractsLeftRight()
	{
		var margins = new CellMargins(0f, 100f, 0f, 150f);

		var width = TableLayoutEngine.ComputeContentWidth(4800f, margins);

		width.Should().Be(4550f);
	}

	[Fact]
	public void ComputeContentWidth_WithBorderSpacing_SubtractsBothSides()
	{
		var width = TableLayoutEngine.ComputeContentWidth(4800f, CellMargins.None, 50f);

		width.Should().Be(4700f);
	}

	[Fact]
	public void ComputeContentWidth_MarginsExceedWidth_ReturnsZero()
	{
		var margins = new CellMargins(0f, 3000f, 0f, 2000f);

		var width = TableLayoutEngine.ComputeContentWidth(4800f, margins);

		width.Should().Be(0f);
	}

	[Fact]
	public void MeasureCellContentHeight_WithMargins_IncludesTopBottom()
	{
		var margins = new CellMargins(50f, 0f, 75f, 0f);
		var cell = MakeCellWithParagraphs(2, margins);

		var height = TableLayoutEngine.MeasureCellContentHeight(cell);

		// 2 × 240 + 50 + 75 = 605
		height.Should().Be(605f);
	}

	[Fact]
	public void MeasureCellContentHeight_EmptyCellWithMargins_ReturnsMarginSum()
	{
		var cell = new TableCellElement
		{
			Blocks = [],
			Margins = new CellMargins(100f, 0f, 100f, 0f),
		};

		var height = TableLayoutEngine.MeasureCellContentHeight(cell);

		height.Should().Be(200f);
	}

	[Fact]
	public void LayoutCellContent_EmptyCellWithMargins_ReturnsMarginHeight()
	{
		var cell = new TableCellElement
		{
			Blocks = [],
			Margins = new CellMargins(60f, 0f, 40f, 0f),
		};

		var (blocks, totalHeight) = TableLayoutEngine.LayoutCellContent(cell);

		blocks.Should().BeEmpty();
		totalHeight.Should().Be(100f);
	}

	[Fact]
	public void LayoutCellContent_WithMargins_HeightIncludesMargins()
	{
		var margins = new CellMargins(30f, 0f, 20f, 0f);
		var cell = MakeCellWithParagraphs(1, margins);

		var (blocks, totalHeight) = TableLayoutEngine.LayoutCellContent(cell);

		blocks.Should().HaveCount(1);
		// 30 (top) + 240 (content) + 20 (bottom) = 290
		totalHeight.Should().Be(290f);
	}

	[Fact]
	public void LayoutCellContent_WithBorderSpacing_HeightIncludesSpacing()
	{
		var cell = MakeCellWithParagraphs(1);

		var (blocks, totalHeight) = TableLayoutEngine.LayoutCellContent(cell, 15f);

		blocks.Should().HaveCount(1);
		// 15 (top spacing) + 240 (content) + 15 (bottom spacing) = 270
		totalHeight.Should().Be(270f);
	}

	[Fact]
	public void ComputeRowHeights_CellMarginsAffectRowHeight()
	{
		var margins = new CellMargins(100f, 0f, 100f, 0f);
		var cell = MakeCellWithParagraphs(1, margins);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(4800f)],
			Rows = [new TableRowElement { Cells = [cell] }],
		};

		var heights = TableLayoutEngine.ComputeRowHeights(table);

		// 100 (top) + 240 (content) + 100 (bottom) = 440
		heights[0].Should().Be(440f);
	}

	[Fact]
	public void ComputeRowHeights_BorderSpacingAffectsRowHeight()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(4800f)],
			Rows = [new TableRowElement { Cells = [MakeCellWithParagraphs(1)] }],
			BorderSpacingTwips = 10f,
		};

		var heights = TableLayoutEngine.ComputeRowHeights(table);

		heights.Should().ContainSingle();
		heights[0].Should().Be(260f);
	}

	// ---- Table border rendering segments (4.5.5) ----

	[Fact]
	public void ComputeBorderSegments_NullLayout_ThrowsArgumentNullException()
	{
		var act = () => TableLayoutEngine.ComputeBorderSegments(null!);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("layout");
	}

	[Fact]
	public void ComputeBorderSegments_EmptyTable_ReturnsEmpty()
	{
		var layout = TableLayoutEngine.Layout(new TableElement { GridColumns = [], Rows = [] }, 9600f);

		var segments = TableLayoutEngine.ComputeBorderSegments(layout);

		segments.Should().BeEmpty();
	}

	[Fact]
	public void ComputeBorderSegments_SingleCell_ProducesOuterSegmentsWithStyleWidthColor()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f)],
			Rows = [new TableRowElement { Cells = [MakeCell()] }],
			Borders = new TableBorderSet(
				Top: new TableBorderDefinition(BorderStyle.Single, 8, "ff0000"),
				Bottom: new TableBorderDefinition(BorderStyle.Dashed, 6, "00ff00"),
				Left: new TableBorderDefinition(BorderStyle.Dotted, 4, "0000ff"),
				Right: new TableBorderDefinition(BorderStyle.Double, 10, "auto")),
		};

		var layout = TableLayoutEngine.Layout(table, 1000f);

		var segments = TableLayoutEngine.ComputeBorderSegments(layout);

		segments.Should().HaveCount(4);

		segments.Should().ContainSingle(s => s.Style == BorderStyle.Single && s.Y1 == 0f && s.Y2 == 0f);
		segments.Should().ContainSingle(s => s.Style == BorderStyle.Dashed && s.Y1 == 240f && s.Y2 == 240f);
		segments.Should().ContainSingle(s => s.Style == BorderStyle.Dotted && s.X1 == 0f && s.X2 == 0f);
		segments.Should().ContainSingle(s => s.Style == BorderStyle.Double && s.X1 == 1000f && s.X2 == 1000f);

		var top = segments.Single(s => s.Style == BorderStyle.Single);
		top.WidthTwips.Should().Be(20f);
		top.ColorHex.Should().Be("FF0000");
		top.DashPatternTwips.Should().BeNull();

		var bottom = segments.Single(s => s.Style == BorderStyle.Dashed);
		bottom.WidthTwips.Should().Be(15f);
		bottom.ColorHex.Should().Be("00FF00");
		bottom.DashPatternTwips.Should().NotBeNull();

		var right = segments.Single(s => s.Style == BorderStyle.Double);
		right.ColorHex.Should().Be("000000");
	}

	[Fact]
	public void ComputeBorderSegments_InnerEdges_UseInsideBorders()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement { Cells = [MakeCell(), MakeCell()] },
				new TableRowElement { Cells = [MakeCell(), MakeCell()] },
			],
			Borders = new TableBorderSet(
				Top: new TableBorderDefinition(BorderStyle.Single, 4, "111111"),
				Bottom: new TableBorderDefinition(BorderStyle.Single, 4, "111111"),
				Left: new TableBorderDefinition(BorderStyle.Single, 4, "111111"),
				Right: new TableBorderDefinition(BorderStyle.Single, 4, "111111"),
				InsideHorizontal: new TableBorderDefinition(BorderStyle.Dotted, 8, "222222"),
				InsideVertical: new TableBorderDefinition(BorderStyle.Dashed, 6, "333333")),
		};

		var layout = TableLayoutEngine.Layout(table, 2000f);

		var segments = TableLayoutEngine.ComputeBorderSegments(layout);

		segments.Should().NotBeEmpty();

		segments.Should().ContainSingle(s =>
			s.Style == BorderStyle.Dashed
			&& s.X1 == 1000f
			&& s.X2 == 1000f
			&& s.Y1 == 0f
			&& s.Y2 == 240f);

		segments.Should().ContainSingle(s =>
			s.Style == BorderStyle.Dotted
			&& s.Y1 == 240f
			&& s.Y2 == 240f
			&& s.X1 == 0f
			&& s.X2 == 1000f);
	}

	[Fact]
	public void ComputeBorderSegments_RowAndCellPrecedence_OverridesInsideAndTableBorders()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement
				{
					Cells =
					[
						new TableCellElement
						{
							Blocks = [],
							Borders = new TableBorderSet(Right: new TableBorderDefinition(BorderStyle.Double, 10, "ABCDEF")),
						},
						MakeCell(),
					],
					Borders = new TableBorderSet(Bottom: new TableBorderDefinition(BorderStyle.Thick, 8, "123123")),
				},
			],
			Borders = new TableBorderSet(
				Top: new TableBorderDefinition(BorderStyle.Single, 4, "111111"),
				Bottom: new TableBorderDefinition(BorderStyle.Single, 4, "111111"),
				Left: new TableBorderDefinition(BorderStyle.Single, 4, "111111"),
				Right: new TableBorderDefinition(BorderStyle.Single, 4, "111111"),
				InsideVertical: new TableBorderDefinition(BorderStyle.Dashed, 6, "222222")),
		};

		var layout = TableLayoutEngine.Layout(table, 2000f);

		var segments = TableLayoutEngine.ComputeBorderSegments(layout);

		segments.Should().ContainSingle(s =>
			s.Style == BorderStyle.Double
			&& s.ColorHex == "ABCDEF"
			&& s.X1 == 1000f
			&& s.X2 == 1000f);

		segments.Should().Contain(s =>
			s.Style == BorderStyle.Thick
			&& s.ColorHex == "123123"
			&& s.Y1 == 240f
			&& s.Y2 == 240f);
	}

	[Fact]
	public void ComputeBorderSegments_MergedCell_UsesMergedGeometryForSegments()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement
				{
					Cells =
					[
						new TableCellElement
						{
							Blocks = [],
							GridSpan = 2,
						},
					],
				},
				new TableRowElement { Cells = [MakeCell(), MakeCell()] },
			],
			Borders = new TableBorderSet(
				Top: new TableBorderDefinition(BorderStyle.Single, 4, "111111"),
				Bottom: new TableBorderDefinition(BorderStyle.Single, 4, "111111"),
				Left: new TableBorderDefinition(BorderStyle.Single, 4, "111111"),
				Right: new TableBorderDefinition(BorderStyle.Single, 4, "111111"),
				InsideVertical: new TableBorderDefinition(BorderStyle.Dashed, 6, "333333")),
		};

		var layout = TableLayoutEngine.Layout(table, 2000f);

		var segments = TableLayoutEngine.ComputeBorderSegments(layout);

		segments.Should().ContainSingle(s =>
			s.Style == BorderStyle.Single
			&& s.Y1 == 0f
			&& s.Y2 == 0f
			&& s.X1 == 0f
			&& s.X2 == 2000f);

		segments.Should().NotContain(s =>
			s.X1 == 1000f
			&& s.X2 == 1000f
			&& s.Y1 == 0f
			&& s.Y2 == 240f);
	}

	[Fact]
	public void ComputeBorderSegments_DotDashAndDotDotDash_EmitExpectedDashPatterns()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement
				{
					Cells =
					[
						new TableCellElement
						{
							Blocks = [],
							Borders = new TableBorderSet(
								Top: new TableBorderDefinition(BorderStyle.DotDash, 4, "AAAAAA"),
								Bottom: new TableBorderDefinition(BorderStyle.DotDotDash, 4, "BBBBBB")),
						},
					],
				},
			],
		};

		var layout = TableLayoutEngine.Layout(table, 1000f);

		var segments = TableLayoutEngine.ComputeBorderSegments(layout);

		var dotDash = segments.Single(s => s.Style == BorderStyle.DotDash);
		dotDash.DashPatternTwips.Should().Equal(7.5f, 5f, 2.5f, 5f);

		var dotDotDash = segments.Single(s => s.Style == BorderStyle.DotDotDash);
		dotDotDash.DashPatternTwips.Should().Equal(7.5f, 5f, 2.5f, 5f, 2.5f, 5f);
	}

	// ---- Vertical alignment (4.2.5) ----

	[Fact]
	public void ComputeVerticalContentOffset_TopAlignment_ReturnsZero()
	{
		var offset = TableLayoutEngine.ComputeVerticalContentOffset(500f, 240f, CellVerticalAlignment.Top);

		offset.Should().Be(0f);
	}

	[Fact]
	public void ComputeVerticalContentOffset_CenterAlignment_CentersContent()
	{
		var offset = TableLayoutEngine.ComputeVerticalContentOffset(500f, 240f, CellVerticalAlignment.Center);

		// (500 - 240) / 2 = 130
		offset.Should().Be(130f);
	}

	[Fact]
	public void ComputeVerticalContentOffset_BottomAlignment_PushesContentDown()
	{
		var offset = TableLayoutEngine.ComputeVerticalContentOffset(500f, 240f, CellVerticalAlignment.Bottom);

		// 500 - 240 = 260
		offset.Should().Be(260f);
	}

	[Fact]
	public void ComputeVerticalContentOffset_ContentTallerThanCell_ReturnsZero()
	{
		// Center: (300 - 500) / 2 = -100 → clamped to 0
		var offset = TableLayoutEngine.ComputeVerticalContentOffset(300f, 500f, CellVerticalAlignment.Center);

		offset.Should().Be(0f);
	}

	[Fact]
	public void ComputeVerticalContentOffset_BottomWithContentTallerThanCell_ReturnsZero()
	{
		var offset = TableLayoutEngine.ComputeVerticalContentOffset(300f, 500f, CellVerticalAlignment.Bottom);

		offset.Should().Be(0f);
	}

	[Fact]
	public void ComputeVerticalContentOffset_CenterWithEqualHeights_ReturnsZero()
	{
		var offset = TableLayoutEngine.ComputeVerticalContentOffset(240f, 240f, CellVerticalAlignment.Center);

		offset.Should().Be(0f);
	}

	// ---- Fixed layout integration (4.2.6) ----

	[Fact]
	public void Layout_FullPipeline_CellPositionsWithMarginsAndContent()
	{
		var margins = new CellMargins(50f, 100f, 50f, 100f);
		var cell00 = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() }],
			Margins = margins,
		};
		var cell01 = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() },
					  new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() }],
			Margins = margins,
		};
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(3000f), new TableGridColumn(5000f)],
			Rows = [new TableRowElement { Cells = [cell00, cell01] }],
		};

		var layout = TableLayoutEngine.Layout(table, 9600f);
		var positions = TableLayoutEngine.ComputeCellPositions(layout);

		// cell00: 1 para + margins = 50 + 240 + 50 = 340
		// cell01: 2 para + margins = 50 + 480 + 50 = 580
		// Row height = max(340, 580) = 580
		positions.Should().HaveCount(2);
		positions[0].Height.Should().Be(580f);
		positions[1].Height.Should().Be(580f);

		// Content widths
		var cw0 = TableLayoutEngine.ComputeContentWidth(positions[0].Width, margins);
		cw0.Should().Be(2800f); // 3000 - 100 - 100

		var cw1 = TableLayoutEngine.ComputeContentWidth(positions[1].Width, margins);
		cw1.Should().Be(4800f); // 5000 - 100 - 100
	}

	[Fact]
	public void Layout_FullPipeline_VerticalAlignmentIntegration()
	{
		var cell = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() }],
			VerticalAlignment = CellVerticalAlignment.Center,
		};
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(4800f)],
			Rows = [new TableRowElement { Cells = [cell], HeightTwips = 600f }],
		};

		var layout = TableLayoutEngine.Layout(table, 9600f);
		var positions = TableLayoutEngine.ComputeCellPositions(layout);

		// Row height: max(600, 240) = 600. Content height: 240. Offset: (600-240)/2 = 180
		positions.Should().HaveCount(1);
		positions[0].Height.Should().Be(600f);

		var (_, contentHeight) = TableLayoutEngine.LayoutCellContent(cell);
		var vOffset = TableLayoutEngine.ComputeVerticalContentOffset(600f, contentHeight, cell.VerticalAlignment);
		vOffset.Should().Be(180f);
	}

	[Fact]
	public void Layout_FullPipeline_MultiRowWithMarginsAndAlignment()
	{
		var topMargins = new CellMargins(100f, 50f, 100f, 50f);
		var row0Cell = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() }],
			Margins = topMargins,
			VerticalAlignment = CellVerticalAlignment.Bottom,
		};
		var row1Cell = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() },
					  new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() }],
		};
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(4800f)],
			Rows =
			[
				new TableRowElement { Cells = [row0Cell], HeightTwips = 600f },
				new TableRowElement { Cells = [row1Cell] },
			],
		};

		var layout = TableLayoutEngine.Layout(table, 9600f);
		var positions = TableLayoutEngine.ComputeCellPositions(layout);

		positions.Should().HaveCount(2);

		// Row 0: max(600, 100+240+100=440) = 600
		positions[0].Y.Should().Be(0f);
		positions[0].Height.Should().Be(600f);

		// Row 1: 2×240 = 480 (no margins), max(0, 480) = 480
		positions[1].Y.Should().Be(600f);
		positions[1].Height.Should().Be(480f);

		// Vertical offset for row0 cell (bottom alignment): 600 - 440 = 160
		var (_, ch) = TableLayoutEngine.LayoutCellContent(row0Cell);
		var vOff = TableLayoutEngine.ComputeVerticalContentOffset(600f, ch, row0Cell.VerticalAlignment);
		vOff.Should().Be(160f);
	}

	[Fact]
	public void Layout_FullPipeline_ExactHeightIgnoresMarginsAndContent()
	{
		var margins = new CellMargins(200f, 50f, 200f, 50f);
		var cell = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() },
					  new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() },
					  new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() }],
			Margins = margins,
		};
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(4800f)],
			Rows = [new TableRowElement
			{
				Cells = [cell],
				HeightTwips = 300f,
				HeightRule = RowHeightRule.Exact,
			}],
		};

		var layout = TableLayoutEngine.Layout(table, 9600f);

		// Exact ignores content (200+720+200=1120) and uses specified 300
		layout.RowHeights[0].Should().Be(300f);
		layout.TotalHeightTwips.Should().Be(300f);
	}
}
