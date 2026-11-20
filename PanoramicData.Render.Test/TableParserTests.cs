namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using static PanoramicData.Render.Test.TableParserBuilders;

public sealed partial class TableParserTests
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
	public void Parse_CellWithFillShading_ParsesFillColor()
	{
		var table = new Table(
			new TableRow(
				new TableCell(
					new TableCellProperties(
						new Shading
						{
							Val = ShadingPatternValues.Clear,
							Fill = "ffff00"
						}),
					new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].Cells[0].Shading.Should().Be(new ParagraphShading(
			ShadingPattern.Clear,
			null,
			"FFFF00"));
	}

	[Fact]
	public void Parse_CellWithPatternedShading_ParsesPatternAndColors()
	{
		var shading = new Shading
		{
			Color = "112233",
			Fill = "aabbcc"
		};
		shading.SetAttribute(new OpenXmlAttribute("w", "val", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "horzStripe"));

		var table = new Table(
			new TableRow(
				new TableCell(
					new TableCellProperties(
						shading),
					new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].Cells[0].Shading.Should().Be(new ParagraphShading(
			ShadingPattern.HorizontalStripe,
			"112233",
			"AABBCC"));
	}

	[Fact]
	public void Parse_CellWithAutoShadingColors_TreatsAutoAsUnspecified()
	{
		var table = new Table(
			new TableRow(
				new TableCell(
					new TableCellProperties(
						new Shading
						{
							Val = ShadingPatternValues.Solid,
							Color = "auto",
							Fill = "nil"
						}),
					new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].Cells[0].Shading.Should().Be(new ParagraphShading(ShadingPattern.Solid, null, null));
	}

	[Fact]
	public void Parse_TableLook_ParsesConditionalFormattingFlags()
	{
		var look = new TableLook();
		look.SetAttribute(new OpenXmlAttribute("w", "firstRow", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "1"));
		look.SetAttribute(new OpenXmlAttribute("w", "lastColumn", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "1"));
		look.SetAttribute(new OpenXmlAttribute("w", "noHBand", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "1"));
		look.SetAttribute(new OpenXmlAttribute("w", "noVBand", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "1"));

		var table = new Table(
			new TableProperties(look),
			new TableRow(new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.Look.ApplyFirstRow.Should().BeTrue();
		result.Look.ApplyLastColumn.Should().BeTrue();
		result.Look.ApplyBandedRows.Should().BeFalse();
		result.Look.ApplyBandedColumns.Should().BeFalse();
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

	// ---- Row properties (4.1.3) ----

	[Fact]
	public void Parse_RowWithNoProperties_DefaultValues()
	{
		var table = new Table(
			new TableRow(new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		var row = result.Rows[0];
		row.HeightTwips.Should().Be(0f);
		row.HeightRule.Should().Be(RowHeightRule.Auto);
		row.IsHeaderRow.Should().BeFalse();
		row.CantSplit.Should().BeFalse();
	}
}
