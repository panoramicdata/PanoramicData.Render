namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

/// <summary>
/// Integration tests for the table model — verifies that <see cref="TableParser"/>
/// and <see cref="TableGridResolver"/> work together correctly on realistic tables.
/// </summary>
public sealed class TableModelIntegrationTests
{
	[Fact]
	public void SimpleThreeByThreeTable_ParsesCorrectly()
	{
		var table = new Table(
			new TableGrid(
				new GridColumn { Width = "2400" },
				new GridColumn { Width = "2400" },
				new GridColumn { Width = "2400" }),
			new TableRow(
				new TableCell(new Paragraph(new Run(new Text("A1")))),
				new TableCell(new Paragraph(new Run(new Text("B1")))),
				new TableCell(new Paragraph(new Run(new Text("C1"))))),
			new TableRow(
				new TableCell(new Paragraph(new Run(new Text("A2")))),
				new TableCell(new Paragraph(new Run(new Text("B2")))),
				new TableCell(new Paragraph(new Run(new Text("C2"))))),
			new TableRow(
				new TableCell(new Paragraph(new Run(new Text("A3")))),
				new TableCell(new Paragraph(new Run(new Text("B3")))),
				new TableCell(new Paragraph(new Run(new Text("C3"))))));

		var result = TableParser.Parse(table);

		result.GridColumns.Should().HaveCount(3);
		result.Rows.Should().HaveCount(3);
		foreach (var row in result.Rows)
		{
			row.Cells.Should().HaveCount(3);
		}

		// All cells should have 1 block each
		foreach (var row in result.Rows)
		{
			foreach (var cell in row.Cells)
			{
				cell.Blocks.Should().HaveCount(1);
			}
		}
	}

	[Fact]
	public void TableWithHeaderRow_ParsesHeaderCorrectly()
	{
		var table = new Table(
			new TableGrid(
				new GridColumn { Width = "4800" },
				new GridColumn { Width = "4800" }),
			new TableRow(
				new TableRowProperties(
					new TableHeader()),
				new TableCell(new Paragraph(new Run(new Text("Name")))),
				new TableCell(new Paragraph(new Run(new Text("Value"))))),
			new TableRow(
				new TableCell(new Paragraph(new Run(new Text("Foo")))),
				new TableCell(new Paragraph(new Run(new Text("42"))))));

		var result = TableParser.Parse(table);

		result.Rows[0].IsHeaderRow.Should().BeTrue();
		result.Rows[1].IsHeaderRow.Should().BeFalse();
	}

	[Fact]
	public void TableWithHorizontalMerge_ResolvesGridCorrectly()
	{
		// Row 0: merged cell spans 2 columns, then 1 regular cell
		// Row 1: 3 regular cells
		var table = new Table(
			new TableGrid(
				new GridColumn { Width = "1600" },
				new GridColumn { Width = "1600" },
				new GridColumn { Width = "1600" }),
			new TableRow(
				new TableCell(
					new TableCellProperties(new GridSpan { Val = 2 }),
					new Paragraph(new Run(new Text("Merged")))),
				new TableCell(new Paragraph(new Run(new Text("C1"))))),
			new TableRow(
				new TableCell(new Paragraph(new Run(new Text("A2")))),
				new TableCell(new Paragraph(new Run(new Text("B2")))),
				new TableCell(new Paragraph(new Run(new Text("C2"))))));

		var result = TableParser.Parse(table);

		result.Rows[0].Cells.Should().HaveCount(2);
		result.Rows[0].Cells[0].GridSpan.Should().Be(2);
		result.Rows[1].Cells.Should().HaveCount(3);

		var grid = TableGridResolver.Resolve(result);
		grid.GetLength(0).Should().Be(2);
		grid.GetLength(1).Should().Be(3);

		// Merged cell occupies [0,0] and [0,1]
		grid[0, 0]!.Value.Cell.Should().BeSameAs(grid[0, 1]!.Value.Cell);
		// Regular cells in row 1 are distinct
		grid[1, 0]!.Value.Cell.Should().NotBeSameAs(grid[1, 1]!.Value.Cell);
	}

	[Fact]
	public void TableWithVerticalMerge_ResolvesGridCorrectly()
	{
		// Col 0 merges rows 0 and 1. Col 1 is regular in both rows.
		var table = new Table(
			new TableGrid(
				new GridColumn { Width = "2400" },
				new GridColumn { Width = "2400" }),
			new TableRow(
				new TableCell(
					new TableCellProperties(new VerticalMerge { Val = MergedCellValues.Restart }),
					new Paragraph(new Run(new Text("Merged")))),
				new TableCell(new Paragraph(new Run(new Text("B1"))))),
			new TableRow(
				new TableCell(
					new TableCellProperties(new VerticalMerge()),
					new Paragraph()),
				new TableCell(new Paragraph(new Run(new Text("B2"))))));

		var result = TableParser.Parse(table);
		var grid = TableGridResolver.Resolve(result);

		// Both rows in col 0 point to the same restart cell
		grid[0, 0]!.Value.Cell.Should().BeSameAs(grid[1, 0]!.Value.Cell);
		grid[0, 0]!.Value.OwnerRowIndex.Should().Be(0);
		grid[1, 0]!.Value.OwnerRowIndex.Should().Be(0);
		// Col 1 cells are distinct
		grid[0, 1]!.Value.Cell.Should().NotBeSameAs(grid[1, 1]!.Value.Cell);
	}

	[Fact]
	public void TableWithMixedMerging_ResolvesCorrectly()
	{
		// 4 columns × 3 rows
		// [0,0..1] merged (gridSpan=2, vMerge restart), [0,2] regular, [0,3] regular
		// [1,0..1] continue, [1,2] regular, [1,3] regular
		// [2,0] regular, [2,1] regular, [2,2..3] merged (gridSpan=2)
		var table = new Table(
			new TableGrid(
				new GridColumn { Width = "1200" },
				new GridColumn { Width = "1200" },
				new GridColumn { Width = "1200" },
				new GridColumn { Width = "1200" }),
			new TableRow(
				new TableCell(
					new TableCellProperties(
						new GridSpan { Val = 2 },
						new VerticalMerge { Val = MergedCellValues.Restart }),
					new Paragraph(new Run(new Text("Big cell")))),
				new TableCell(new Paragraph(new Run(new Text("C1")))),
				new TableCell(new Paragraph(new Run(new Text("D1"))))),
			new TableRow(
				new TableCell(
					new TableCellProperties(
						new GridSpan { Val = 2 },
						new VerticalMerge()),
					new Paragraph()),
				new TableCell(new Paragraph(new Run(new Text("C2")))),
				new TableCell(new Paragraph(new Run(new Text("D2"))))),
			new TableRow(
				new TableCell(new Paragraph(new Run(new Text("A3")))),
				new TableCell(new Paragraph(new Run(new Text("B3")))),
				new TableCell(
					new TableCellProperties(new GridSpan { Val = 2 }),
					new Paragraph(new Run(new Text("CD3"))))));

		var result = TableParser.Parse(table);
		var grid = TableGridResolver.Resolve(result);

		grid.GetLength(0).Should().Be(3);
		grid.GetLength(1).Should().Be(4);

		// Row 0: big cell in [0,0] and [0,1]
		grid[0, 0]!.Value.Cell.Should().BeSameAs(grid[0, 1]!.Value.Cell);
		// Row 1: continue in [1,0] and [1,1] → same owner as row 0
		grid[1, 0]!.Value.Cell.Should().BeSameAs(grid[0, 0]!.Value.Cell);
		grid[1, 1]!.Value.Cell.Should().BeSameAs(grid[0, 0]!.Value.Cell);
		// Row 2: [2,2] and [2,3] merged
		grid[2, 2]!.Value.Cell.Should().BeSameAs(grid[2, 3]!.Value.Cell);
		// Row 2: [2,0] and [2,1] are distinct
		grid[2, 0]!.Value.Cell.Should().NotBeSameAs(grid[2, 1]!.Value.Cell);
	}

	[Fact]
	public void TableWithCellProperties_PreservesAllProperties()
	{
		var table = new Table(
			new TableProperties(
				new TableWidth { Width = "9600", Type = TableWidthUnitValues.Dxa },
				new TableJustification { Val = TableRowAlignmentValues.Center }),
			new TableGrid(
				new GridColumn { Width = "4800" },
				new GridColumn { Width = "4800" }),
			new TableRow(
				new TableRowProperties(
					new TableRowHeight { Val = 500, HeightType = HeightRuleValues.AtLeast }),
				new TableCell(
					new TableCellProperties(
						new TableCellWidth { Width = "4800", Type = TableWidthUnitValues.Dxa },
						new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center },
						new TableCellMargin(
							new TopMargin { Width = "72", Type = TableWidthUnitValues.Dxa },
							new BottomMargin { Width = "72", Type = TableWidthUnitValues.Dxa })),
					new Paragraph(new Run(new Text("Centered content")))),
				new TableCell(new Paragraph(new Run(new Text("Default"))))));

		var result = TableParser.Parse(table);

		// Table-level properties
		result.Width.Type.Should().Be(TableWidthUnit.Dxa);
		result.Width.Value.Should().Be(9600f);
		result.Alignment.Should().Be(TableAlignment.Center);

		// Row properties
		result.Rows[0].HeightTwips.Should().Be(500f);
		result.Rows[0].HeightRule.Should().Be(RowHeightRule.AtLeast);

		// First cell properties
		var cell0 = result.Rows[0].Cells[0];
		cell0.Width.Type.Should().Be(TableWidthUnit.Dxa);
		cell0.Width.Value.Should().Be(4800f);
		cell0.VerticalAlignment.Should().Be(CellVerticalAlignment.Center);
		cell0.Margins.Top.Should().Be(72f);
		cell0.Margins.Bottom.Should().Be(72f);

		// Second cell defaults
		var cell1 = result.Rows[0].Cells[1];
		cell1.Width.Should().Be(TableWidthValue.Auto);
		cell1.VerticalAlignment.Should().Be(CellVerticalAlignment.Top);
		cell1.Margins.Should().Be(CellMargins.None);
	}

	[Fact]
	public void TableWithStyleId_ParsesStyleId()
	{
		var table = new Table(
			new TableProperties(
				new TableStyle { Val = "TableGrid" }),
			new TableGrid(
				new GridColumn { Width = "4800" }),
			new TableRow(
				new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.StyleId.Should().Be("TableGrid");
	}

	[Fact]
	public void TableWithIndentation_ParsesIndent()
	{
		var table = new Table(
			new TableProperties(
				new TableIndentation { Width = 720, Type = TableWidthUnitValues.Dxa }),
			new TableGrid(
				new GridColumn { Width = "4800" }),
			new TableRow(
				new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.IndentationTwips.Should().Be(720f);
	}

	[Fact]
	public void TableWithNestedTable_ParsesPlaceholder()
	{
		var innerTable = new Table(
			new TableGrid(new GridColumn { Width = "2400" }),
			new TableRow(new TableCell(new Paragraph(new Run(new Text("Inner"))))));

		var table = new Table(
			new TableGrid(new GridColumn { Width = "4800" }),
			new TableRow(
				new TableCell(
					new Paragraph(new Run(new Text("Before"))),
					innerTable,
					new Paragraph(new Run(new Text("After"))))));

		var result = TableParser.Parse(table);

		var blocks = result.Rows[0].Cells[0].Blocks;
		blocks.Should().HaveCount(3);
		blocks[0].Should().BeOfType<ParagraphBlock>();
		blocks[1].Should().BeOfType<TablePlaceholderBlock>();
		blocks[2].Should().BeOfType<ParagraphBlock>();
	}

	[Fact]
	public void TableWithCantSplit_ParsesCantSplit()
	{
		var table = new Table(
			new TableGrid(new GridColumn { Width = "4800" }),
			new TableRow(
				new TableRowProperties(new CantSplit()),
				new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].CantSplit.Should().BeTrue();
	}

	[Fact]
	public void FullTableRoundtrip_GridColumnsMatchWidths()
	{
		var table = new Table(
			new TableGrid(
				new GridColumn { Width = "1000" },
				new GridColumn { Width = "2000" },
				new GridColumn { Width = "3000" }),
			new TableRow(
				new TableCell(new Paragraph()),
				new TableCell(new Paragraph()),
				new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.GridColumns.Should().HaveCount(3);
		result.GridColumns[0].WidthTwips.Should().Be(1000f);
		result.GridColumns[1].WidthTwips.Should().Be(2000f);
		result.GridColumns[2].WidthTwips.Should().Be(3000f);
	}

	[Fact]
	public void TableWithTextDirection_Parses()
	{
		var table = new Table(
			new TableGrid(new GridColumn { Width = "4800" }),
			new TableRow(
				new TableCell(
					new TableCellProperties(
						new TextDirection { Val = TextDirectionValues.TopToBottomRightToLeft }),
					new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].Cells[0].TextDirection.Should().Be(CellTextDirection.TopToBottomRightToLeft);
	}
}
