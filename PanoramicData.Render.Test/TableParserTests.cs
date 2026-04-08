namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed class TableParserTests
{
	[Fact]
	public void Parse_NullTable_ThrowsArgumentNullException()
	{
		var act = () => TableParser.Parse(null!);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("table");
	}

	[Fact]
	public void Parse_EmptyTable_ReturnsEmptyGridAndRows()
	{
		var table = new Table();

		var result = TableParser.Parse(table);

		result.GridColumns.Should().BeEmpty();
		result.Rows.Should().BeEmpty();
		result.StyleId.Should().BeNull();
	}

	[Fact]
	public void Parse_TableWithGrid_ParsesGridColumns()
	{
		var table = new Table(
			new TableGrid(
				new GridColumn { Width = "2400" },
				new GridColumn { Width = "4800" }));

		var result = TableParser.Parse(table);

		result.GridColumns.Should().HaveCount(2);
		result.GridColumns[0].WidthTwips.Should().Be(2400f);
		result.GridColumns[1].WidthTwips.Should().Be(4800f);
	}

	[Fact]
	public void Parse_GridColumnWithNoWidth_ReturnsZero()
	{
		var table = new Table(
			new TableGrid(new GridColumn()));

		var result = TableParser.Parse(table);

		result.GridColumns.Should().ContainSingle();
		result.GridColumns[0].WidthTwips.Should().Be(0f);
	}

	[Fact]
	public void Parse_GridColumnWithInvalidWidth_ReturnsZero()
	{
		var table = new Table(
			new TableGrid(new GridColumn { Width = "abc" }));

		var result = TableParser.Parse(table);

		result.GridColumns.Should().ContainSingle();
		result.GridColumns[0].WidthTwips.Should().Be(0f);
	}

	[Fact]
	public void Parse_SingleRowSingleCell_ParsesStructure()
	{
		var table = new Table(
			new TableRow(
				new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows.Should().ContainSingle();
		result.Rows[0].Cells.Should().ContainSingle();
		result.Rows[0].Cells[0].Blocks.Should().ContainSingle();
		result.Rows[0].Cells[0].Blocks[0].Should().BeOfType<ParagraphBlock>();
	}

	[Fact]
	public void Parse_MultipleRowsAndCells_ParsesAll()
	{
		var table = new Table(
			new TableRow(
				new TableCell(new Paragraph()),
				new TableCell(new Paragraph())),
			new TableRow(
				new TableCell(new Paragraph()),
				new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows.Should().HaveCount(2);
		result.Rows[0].Cells.Should().HaveCount(2);
		result.Rows[1].Cells.Should().HaveCount(2);
	}

	[Fact]
	public void Parse_CellWithMultipleParagraphs_ParsesAllBlocks()
	{
		var table = new Table(
			new TableRow(
				new TableCell(
					new Paragraph(),
					new Paragraph(),
					new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].Cells[0].Blocks.Should().HaveCount(3);
	}

	[Fact]
	public void Parse_CellWithNestedTable_ParsesAsPlaceholder()
	{
		var table = new Table(
			new TableRow(
				new TableCell(
					new Paragraph(),
					new Table(new TableRow(new TableCell(new Paragraph()))))));

		var result = TableParser.Parse(table);

		var blocks = result.Rows[0].Cells[0].Blocks;
		blocks.Should().HaveCount(2);
		blocks[0].Should().BeOfType<ParagraphBlock>();
		blocks[1].Should().BeOfType<TablePlaceholderBlock>();
	}

	[Fact]
	public void Parse_CellDefaultGridSpan_IsOne()
	{
		var table = new Table(
			new TableRow(
				new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].Cells[0].GridSpan.Should().Be(1);
	}

	[Fact]
	public void Parse_CellWithGridSpan_ParsesValue()
	{
		var table = new Table(
			new TableRow(
				new TableCell(
					new TableCellProperties(new GridSpan { Val = 3 }),
					new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].Cells[0].GridSpan.Should().Be(3);
	}

	[Fact]
	public void Parse_CellDefaultVerticalMerge_IsNone()
	{
		var table = new Table(
			new TableRow(
				new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].Cells[0].VerticalMerge.Should().Be(VerticalMergeState.None);
	}

	[Fact]
	public void Parse_CellWithVerticalMergeRestart_ParsesState()
	{
		var table = new Table(
			new TableRow(
				new TableCell(
					new TableCellProperties(new VerticalMerge { Val = MergedCellValues.Restart }),
					new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].Cells[0].VerticalMerge.Should().Be(VerticalMergeState.Restart);
	}

	[Fact]
	public void Parse_CellWithVerticalMergeContinue_ParsesState()
	{
		var table = new Table(
			new TableRow(
				new TableCell(
					new TableCellProperties(new VerticalMerge()),
					new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].Cells[0].VerticalMerge.Should().Be(VerticalMergeState.Continue);
	}

	[Fact]
	public void Parse_CellWithVerticalMergeContinueExplicit_ParsesState()
	{
		var table = new Table(
			new TableRow(
				new TableCell(
					new TableCellProperties(new VerticalMerge { Val = MergedCellValues.Continue }),
					new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].Cells[0].VerticalMerge.Should().Be(VerticalMergeState.Continue);
	}

	[Fact]
	public void Parse_TableWithStyleId_ParsesStyleId()
	{
		var table = new Table(
			new TableProperties(new TableStyle { Val = "TableGrid" }),
			new TableRow(new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.StyleId.Should().Be("TableGrid");
	}

	[Fact]
	public void Parse_TableWithNoStyle_StyleIdIsNull()
	{
		var table = new Table(
			new TableProperties(),
			new TableRow(new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.StyleId.Should().BeNull();
	}

	[Fact]
	public void Parse_CompleteTable_ParsesGridAndContent()
	{
		var table = new Table(
			new TableProperties(new TableStyle { Val = "FancyTable" }),
			new TableGrid(
				new GridColumn { Width = "2400" },
				new GridColumn { Width = "4800" }),
			new TableRow(
				new TableCell(new Paragraph()),
				new TableCell(new Paragraph())),
			new TableRow(
				new TableCell(
					new TableCellProperties(new GridSpan { Val = 2 }),
					new Paragraph())));

		var result = TableParser.Parse(table);

		result.StyleId.Should().Be("FancyTable");
		result.GridColumns.Should().HaveCount(2);
		result.Rows.Should().HaveCount(2);
		result.Rows[0].Cells.Should().HaveCount(2);
		result.Rows[1].Cells.Should().ContainSingle();
		result.Rows[1].Cells[0].GridSpan.Should().Be(2);
	}

	[Fact]
	public void Parse_CellWithEmptyContent_ReturnsEmptyBlocks()
	{
		// A cell with only TableCellProperties (no paragraphs)
		var table = new Table(
			new TableRow(
				new TableCell(
					new TableCellProperties())));

		var result = TableParser.Parse(table);

		result.Rows[0].Cells[0].Blocks.Should().BeEmpty();
	}

	[Fact]
	public void VerticalMergeState_EnumValues_AreCorrect()
	{
		((int)VerticalMergeState.None).Should().Be(0);
		((int)VerticalMergeState.Restart).Should().Be(1);
		((int)VerticalMergeState.Continue).Should().Be(2);
	}

	[Fact]
	public void TableGridColumn_RecordStruct_StoresWidth()
	{
		var col = new TableGridColumn(1440f);

		col.WidthTwips.Should().Be(1440f);
	}

	[Fact]
	public void TableGridColumn_Default_HasZeroWidth()
	{
		var col = new TableGridColumn();

		col.WidthTwips.Should().Be(0f);
	}

	// ---- Table properties (4.1.2) ----

	[Fact]
	public void Parse_TableWithFixedWidth_ParsesWidthDxa()
	{
		var table = new Table(
			new TableProperties(
				new TableWidth { Width = "9360", Type = TableWidthUnitValues.Dxa }),
			new TableRow(new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.Width.Type.Should().Be(TableWidthUnit.Dxa);
		result.Width.Value.Should().Be(9360f);
	}

	[Fact]
	public void Parse_TableWithPercentageWidth_ParsesWidthPct()
	{
		var table = new Table(
			new TableProperties(
				new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct }),
			new TableRow(new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.Width.Type.Should().Be(TableWidthUnit.Pct);
		result.Width.Value.Should().Be(5000f); // 100% = 5000 fiftieths
	}

	[Fact]
	public void Parse_TableWithAutoWidth_ParsesWidthAuto()
	{
		var table = new Table(
			new TableProperties(
				new TableWidth { Width = "0", Type = TableWidthUnitValues.Auto }),
			new TableRow(new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.Width.Type.Should().Be(TableWidthUnit.Auto);
	}

	[Fact]
	public void Parse_TableWithNilWidth_ParsesWidthNil()
	{
		var table = new Table(
			new TableProperties(
				new TableWidth { Width = "0", Type = TableWidthUnitValues.Nil }),
			new TableRow(new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.Width.Type.Should().Be(TableWidthUnit.Nil);
	}

	[Fact]
	public void Parse_TableWithNoWidthElement_DefaultsToAuto()
	{
		var table = new Table(
			new TableProperties(),
			new TableRow(new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.Width.Should().Be(TableWidthValue.Auto);
	}

	[Fact]
	public void Parse_TableWithCenterAlignment_ParsesAlignment()
	{
		var table = new Table(
			new TableProperties(
				new TableJustification { Val = TableRowAlignmentValues.Center }),
			new TableRow(new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.Alignment.Should().Be(TableAlignment.Center);
	}

	[Fact]
	public void Parse_TableWithRightAlignment_ParsesAlignment()
	{
		var table = new Table(
			new TableProperties(
				new TableJustification { Val = TableRowAlignmentValues.Right }),
			new TableRow(new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.Alignment.Should().Be(TableAlignment.Right);
	}

	[Fact]
	public void Parse_TableWithLeftAlignment_ParsesAlignment()
	{
		var table = new Table(
			new TableProperties(
				new TableJustification { Val = TableRowAlignmentValues.Left }),
			new TableRow(new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.Alignment.Should().Be(TableAlignment.Left);
	}

	[Fact]
	public void Parse_TableWithNoAlignment_DefaultsToLeft()
	{
		var table = new Table(
			new TableProperties(),
			new TableRow(new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.Alignment.Should().Be(TableAlignment.Left);
	}

	[Fact]
	public void Parse_TableWithIndentation_ParsesIndent()
	{
		var table = new Table(
			new TableProperties(
				new TableIndentation { Width = 720 }),
			new TableRow(new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.IndentationTwips.Should().Be(720f);
	}

	[Fact]
	public void Parse_TableWithNoIndentation_DefaultsToZero()
	{
		var table = new Table(
			new TableProperties(),
			new TableRow(new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.IndentationTwips.Should().Be(0f);
	}

	[Fact]
	public void ParseTableWidth_NullTableWidth_ReturnsAuto()
	{
		var result = TableParser.ParseTableWidth(null);

		result.Should().Be(TableWidthValue.Auto);
	}

	[Fact]
	public void ParseTableWidth_InvalidWidthString_ReturnsZeroValue()
	{
		var tw = new TableWidth { Width = "invalid", Type = TableWidthUnitValues.Dxa };

		var result = TableParser.ParseTableWidth(tw);

		result.Type.Should().Be(TableWidthUnit.Dxa);
		result.Value.Should().Be(0f);
	}

	[Fact]
	public void ParseTableWidth_NoTypeAttribute_DefaultsToAuto()
	{
		var tw = new TableWidth { Width = "1000" };

		var result = TableParser.ParseTableWidth(tw);

		result.Type.Should().Be(TableWidthUnit.Auto);
	}

	[Fact]
	public void ParseAlignment_NullJustification_ReturnsLeft()
	{
		var result = TableParser.ParseAlignment(null);

		result.Should().Be(TableAlignment.Left);
	}

	[Fact]
	public void ParseIndentation_NullIndentation_ReturnsZero()
	{
		var result = TableParser.ParseIndentation(null);

		result.Should().Be(0f);
	}

	[Fact]
	public void TableWidthValue_Auto_HasCorrectDefaults()
	{
		TableWidthValue.Auto.Type.Should().Be(TableWidthUnit.Auto);
		TableWidthValue.Auto.Value.Should().Be(0f);
	}

	[Fact]
	public void TableWidthUnit_EnumValues_AreCorrect()
	{
		((int)TableWidthUnit.Auto).Should().Be(0);
		((int)TableWidthUnit.Dxa).Should().Be(1);
		((int)TableWidthUnit.Pct).Should().Be(2);
		((int)TableWidthUnit.Nil).Should().Be(3);
	}

	[Fact]
	public void TableAlignment_EnumValues_AreCorrect()
	{
		((int)TableAlignment.Left).Should().Be(0);
		((int)TableAlignment.Center).Should().Be(1);
		((int)TableAlignment.Right).Should().Be(2);
	}
}
