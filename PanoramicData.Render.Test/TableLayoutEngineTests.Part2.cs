namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed partial class TableLayoutEngineTests
{
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
}
