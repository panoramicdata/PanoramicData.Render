namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed partial class TableLayoutEngineTests
{
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
		ColumnMeasurement[] measurements = [new(2000f, 500f), new(3000f, 500f)];
		var fixedWidths = new float?[2];

		var widths = TableLayoutEngine.DistributeWithFixedColumns(measurements, fixedWidths, 10000f);

		widths.Should().HaveCount(2);
		(widths[0] + widths[1]).Should().BeApproximately(10000f, 0.01f);
	}

	[Fact]
	public void DistributeWithFixedColumns_AllFixed_UsesFixedWidths()
	{
		ColumnMeasurement[] measurements = { new(2000f, 500f), new(3000f, 500f) };
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
		ColumnMeasurement[] measurements =
		[
			new(1000f, 1000f),
			new(2000f, 2000f),
		];

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
				new DocumentFormat.OpenXml.Wordprocessing.Text("alpha beta gamma delta epsilon zeta")));
		var cell = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = text }],
		};

		var wide = TableLayoutEngine.MeasureCellContentHeight(cell, 4000f);
		var narrow = TableLayoutEngine.MeasureCellContentHeight(cell, 700f);

		narrow.Should().BeGreaterThan(wide);
	}

	[Fact]
	public void MeasureCellContentHeight_ForWidth_UsesRendererWrapEstimateForBorderlineParagraphs()
	{
		// A single unbreakable token (no spaces) cannot be wrapped by Knuth-Plass,
		// so it always occupies exactly one line regardless of the available width.
		var text = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
			new DocumentFormat.OpenXml.Wordprocessing.Run(
				new DocumentFormat.OpenXml.Wordprocessing.Text("aaaaaaaaaaaaaaaaaaaaa")));
		var cell = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = text }],
		};

		var height = TableLayoutEngine.MeasureCellContentHeight(cell, 2100f);

		height.Should().Be(TableLayoutEngine.DefaultRowHeightTwips * 1f);
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
}
