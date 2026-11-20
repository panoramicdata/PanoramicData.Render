namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using static PanoramicData.Render.Test.TableModelBuilders;

/// <summary>
/// Integration tests for the table model: verifies that <see cref="TableParser"/>
/// and <see cref="TableGridResolver"/> work together correctly on realistic tables.
/// </summary>
public sealed class TableModelIntegrationTests
{
	[Fact]
	public void SimpleThreeByThreeTable_ParsesCorrectly()
	{
		var table = new Table(
			GridOf(2400, 2400, 2400),
			RowOfTexts("A1", "B1", "C1"),
			RowOfTexts("A2", "B2", "C2"),
			RowOfTexts("A3", "B3", "C3"));

		var result = TableParser.Parse(table);

		result.GridColumns.Should().HaveCount(3);
		result.Rows.Should().HaveCount(3);
		result.Rows.Should().AllSatisfy(row =>
		{
			row.Cells.Should().HaveCount(3);
			row.Cells.Should().AllSatisfy(cell => cell.Blocks.Should().HaveCount(1));
		});
	}

	[Fact]
	public void TableWithHeaderRow_ParsesHeaderCorrectly()
	{
		var headerRow = RowOfTexts("Name", "Value");
		headerRow.InsertAt(new TableRowProperties(new TableHeader()), 0);
		var table = new Table(GridOf(4800, 4800), headerRow, RowOfTexts("Foo", "42"));

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
			GridOf(1600, 1600, 1600),
			new TableRow(CellWithText("Merged", new GridSpan { Val = 2 }), CellWithText("C1")),
			RowOfTexts("A2", "B2", "C2"));

		var result = TableParser.Parse(table);

		result.Rows[0].Cells.Should().HaveCount(2);
		result.Rows[0].Cells[0].GridSpan.Should().Be(2);
		result.Rows[1].Cells.Should().HaveCount(3);

		var grid = TableGridResolver.Resolve(result);
		grid.GetLength(0).Should().Be(2);
		grid.GetLength(1).Should().Be(3);

		// Merged cell occupies [0,0] and [0,1]
		CellAt(grid, 0, 0).Should().BeSameAs(CellAt(grid, 0, 1));
		// Regular cells in row 1 are distinct
		CellAt(grid, 1, 0).Should().NotBeSameAs(CellAt(grid, 1, 1));
	}

	[Fact]
	public void TableWithVerticalMerge_ResolvesGridCorrectly()
	{
		// Col 0 merges rows 0 and 1. Col 1 is regular in both rows.
		var table = new Table(
			GridOf(2400, 2400),
			new TableRow(CellWithText("Merged", new VerticalMerge { Val = MergedCellValues.Restart }), CellWithText("B1")),
			new TableRow(BlankCell(new VerticalMerge()), CellWithText("B2")));

		var grid = TableGridResolver.Resolve(TableParser.Parse(table));

		// Both rows in col 0 point to the same restart cell
		CellAt(grid, 0, 0).Should().BeSameAs(CellAt(grid, 1, 0));
		grid[0, 0]!.Value.OwnerRowIndex.Should().Be(0);
		grid[1, 0]!.Value.OwnerRowIndex.Should().Be(0);
		// Col 1 cells are distinct
		CellAt(grid, 0, 1).Should().NotBeSameAs(CellAt(grid, 1, 1));
	}

	[Fact]
	public void TableWithMixedMerging_ResolvesCorrectly()
	{
		// 4 columns x 3 rows
		// [0,0..1] merged (gridSpan=2, vMerge restart), [0,2] regular, [0,3] regular
		// [1,0..1] continue, [1,2] regular, [1,3] regular
		// [2,0] regular, [2,1] regular, [2,2..3] merged (gridSpan=2)
		var table = new Table(
			GridOf(1200, 1200, 1200, 1200),
			new TableRow(
				CellWithText("Big cell", new GridSpan { Val = 2 }, new VerticalMerge { Val = MergedCellValues.Restart }),
				CellWithText("C1"),
				CellWithText("D1")),
			new TableRow(
				BlankCell(new GridSpan { Val = 2 }, new VerticalMerge()),
				CellWithText("C2"),
				CellWithText("D2")),
			new TableRow(
				CellWithText("A3"),
				CellWithText("B3"),
				CellWithText("CD3", new GridSpan { Val = 2 })));

		var grid = TableGridResolver.Resolve(TableParser.Parse(table));

		grid.GetLength(0).Should().Be(3);
		grid.GetLength(1).Should().Be(4);

		// Row 0: big cell in [0,0] and [0,1]
		CellAt(grid, 0, 0).Should().BeSameAs(CellAt(grid, 0, 1));
		// Row 1: continue in [1,0] and [1,1] share the row 0 owner
		CellAt(grid, 1, 0).Should().BeSameAs(CellAt(grid, 0, 0));
		CellAt(grid, 1, 1).Should().BeSameAs(CellAt(grid, 0, 0));
		// Row 2: [2,2] and [2,3] merged
		CellAt(grid, 2, 2).Should().BeSameAs(CellAt(grid, 2, 3));
		// Row 2: [2,0] and [2,1] are distinct
		CellAt(grid, 2, 0).Should().NotBeSameAs(CellAt(grid, 2, 1));
	}

	[Fact]
	public void TableWithCellProperties_PreservesAllProperties()
	{
		var table = new Table(
			new TableProperties(
				new TableWidth { Width = "9600", Type = TableWidthUnitValues.Dxa },
				new TableJustification { Val = TableRowAlignmentValues.Center }),
			GridOf(4800, 4800),
			new TableRow(
				new TableRowProperties(
					new TableRowHeight { Val = 500, HeightType = HeightRuleValues.AtLeast }),
				CellWithText(
					"Centered content",
					new TableCellWidth { Width = "4800", Type = TableWidthUnitValues.Dxa },
					new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center },
					new TableCellMargin(
						new TopMargin { Width = "72", Type = TableWidthUnitValues.Dxa },
						new BottomMargin { Width = "72", Type = TableWidthUnitValues.Dxa })),
				CellWithText("Default")));

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

		// Second cell: no explicit margins, falls back to the 108-twip Word default (L/R)
		var cell1 = result.Rows[0].Cells[1];
		cell1.Width.Should().Be(TableWidthValue.Auto);
		cell1.VerticalAlignment.Should().Be(CellVerticalAlignment.Top);
		cell1.Margins.Left.Should().Be(108f);
		cell1.Margins.Right.Should().Be(108f);
		cell1.Margins.Top.Should().Be(0f);
		cell1.Margins.Bottom.Should().Be(0f);
	}

	[Fact]
	public void TableWithStyleId_ParsesStyleId()
	{
		ParseTableWith(new TableStyle { Val = "TableGrid" }).StyleId.Should().Be("TableGrid");
	}

	[Fact]
	public void TableWithIndentation_ParsesIndent()
	{
		ParseTableWith(new TableIndentation { Width = 720, Type = TableWidthUnitValues.Dxa }).IndentationTwips.Should().Be(720f);
	}

	[Fact]
	public void TableWithNestedTable_ParsesPlaceholder()
	{
		var innerTable = new Table(GridOf(2400), RowOfTexts("Inner"));
		var table = new Table(
			GridOf(4800),
			new TableRow(
				new TableCell(
					new Paragraph(new Run(new Text("Before"))),
					innerTable,
					new Paragraph(new Run(new Text("After"))))));

		var blocks = TableParser.Parse(table).Rows[0].Cells[0].Blocks;

		blocks.Should().HaveCount(3);
		blocks[0].Should().BeOfType<ParagraphBlock>();
		blocks[1].Should().BeOfType<TablePlaceholderBlock>();
		blocks[2].Should().BeOfType<ParagraphBlock>();
	}

	[Fact]
	public void TableWithCantSplit_ParsesCantSplit()
	{
		var row = BlankRow(1);
		row.InsertAt(new TableRowProperties(new CantSplit()), 0);

		TableParser.Parse(new Table(GridOf(4800), row)).Rows[0].CantSplit.Should().BeTrue();
	}

	[Fact]
	public void FullTableRoundtrip_GridColumnsMatchWidths()
	{
		var result = TableParser.Parse(new Table(GridOf(1000, 2000, 3000), BlankRow(3)));

		result.GridColumns.Select(column => column.WidthTwips).Should().Equal(1000f, 2000f, 3000f);
	}

	[Fact]
	public void TableWithTextDirection_Parses()
	{
		var result = ParseSingleBlankCell(new TextDirection { Val = TextDirectionValues.TopToBottomRightToLeft });

		result.Rows[0].Cells[0].TextDirection.Should().Be(CellTextDirection.TopToBottomRightToLeft);
	}
}
