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
}
