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
		var width = TableLayoutEngine.EstimateBlockPreferredWidth(TextBlock("Hello World"));

		// "Hello World" = 11 chars x 140 = 1540
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
		var width = TableLayoutEngine.EstimateBlockPreferredWidth(NestedTableBlock("nested nested nested", "x"));

		width.Should().BeGreaterThan(TableLayoutEngine.DefaultBlockWidthTwips);
	}

	[Fact]
	public void EstimateBlockMinimumWidth_EmptyParagraph_ReturnsMinimum()
	{
		var width = TableLayoutEngine.EstimateBlockMinimumWidth(EmptyBlock());

		width.Should().Be(TableLayoutEngine.MinimumColumnWidthTwips);
	}

	[Fact]
	public void EstimateBlockMinimumWidth_ParagraphWithText_ReturnsLongestWordWidth()
	{
		var width = TableLayoutEngine.EstimateBlockMinimumWidth(TextBlock("Hello wonderful World"));

		// "wonderful" = 9 chars x 140 = 1260
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
		var width = TableLayoutEngine.EstimateBlockMinimumWidth(NestedTableBlock("averyveryverylongtoken", "short"));

		width.Should().BeGreaterThan(TableLayoutEngine.MinimumColumnWidthTwips);
	}

	[Fact]
	public void EstimateCellPreferredWidth_WithTextContent_UsesTextLength()
	{
		var width = TableLayoutEngine.EstimateCellPreferredWidth(TextCell("Short"));

		// "Short" = 5 chars x 140 = 700, + 0 margins = 700
		width.Should().Be(700f);
	}

	[Fact]
	public void EstimateCellMinimumWidth_WithTextContent_UsesLongestWord()
	{
		var width = TableLayoutEngine.EstimateCellMinimumWidth(TextCell("A longword here"));

		// "longword" = 8 chars x 140 = 1120
		width.Should().Be(8f * TableLayoutEngine.AverageCharWidthTwips);
	}

	[Fact]
	public void ComputeAutoFitColumnWidths_TwoColumnsWithText_ProducesReasonableWidths()
	{
		var table = AutoFitTable(2, RowOfText("Short", "This is a much longer paragraph of text"));

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
		var table = AutoFitTable(2, RowOf(WithEmptyBlock(1500f, TableWidthUnit.Pct), WithEmptyBlock(3500f, TableWidthUnit.Pct)));

		var widths = TableLayoutEngine.ComputeAutoFitColumnWidths(table, 10000f);

		widths.Should().HaveCount(2);
		// 30% of 10000 = 3000, 70% of 10000 = 7000
		widths[0].Should().Be(3000f);
		widths[1].Should().Be(7000f);
	}

	[Fact]
	public void ComputeAutoFitColumnWidths_MixedFixedAndAuto_FixedColumnsKeptAutoDistributed()
	{
		var table = AutoFitTable(2, RowOf(WithEmptyBlock(3000f, TableWidthUnit.Dxa), MakeCell()));

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
		var nestedCell = new TableCellElement { Blocks = [NestedTableBlock("nested nested nested nested", "nested")] };
		var table = AutoFitTable(2, RowOf(nestedCell, TextCell("x")));

		var widths = TableLayoutEngine.ComputeAutoFitColumnWidths(table, 10000f);

		widths.Should().HaveCount(2);
		widths[0].Should().BeGreaterThan(widths[1]);
	}

	[Fact]
	public void ResolveExplicitColumnWidths_NoExplicit_AllNull()
	{
		var table = AutoFitTable(2, RowOf(MakeCell(), MakeCell()));

		var fixedWidths = ResolveFixed(table, 2);

		fixedWidths.Should().AllBeEquivalentTo((float?)null);
	}

	[Fact]
	public void ResolveExplicitColumnWidths_PercentageWidth_ConvertedToTwips()
	{
		// 2500 = 50%
		var table = AutoFitTable(1, RowOf(WidthCell(2500f, TableWidthUnit.Pct)));

		var fixedWidths = ResolveFixed(table, 1);

		fixedWidths[0].Should().Be(5000f);
	}

	[Fact]
	public void ResolveExplicitColumnWidths_SkipsContinueCells()
	{
		var table = AutoFitTable(1, RowOf(WidthCell(3000f, TableWidthUnit.Dxa, verticalMerge: VerticalMergeState.Continue)));

		var fixedWidths = ResolveFixed(table, 1);

		fixedWidths[0].Should().BeNull();
	}

	[Fact]
	public void ResolveExplicitColumnWidths_SkipsSpannedCells()
	{
		var table = AutoFitTable(2, RowOf(WidthCell(3000f, TableWidthUnit.Dxa, gridSpan: 2)));

		var fixedWidths = ResolveFixed(table, 2);

		fixedWidths.Should().AllBeEquivalentTo((float?)null);
	}

	private static TableCellElement WithEmptyBlock(float width, TableWidthUnit unit) => new()
	{
		Blocks = [EmptyBlock()],
		Width = new TableWidthValue(width, unit),
	};

	private static float?[] ResolveFixed(TableElement table, int columnCount)
	{
		var fixedWidths = new float?[columnCount];
		TableLayoutEngine.ResolveExplicitColumnWidths(table, 10000f, fixedWidths);
		return fixedWidths;
	}
}