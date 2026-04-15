namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
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

	[Fact]
	public void Parse_RowWithExactHeight_ParsesHeightAndRule()
	{
		var table = new Table(
			new TableRow(
				new TableRowProperties(
					new TableRowHeight { Val = 720, HeightType = HeightRuleValues.Exact }),
				new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].HeightTwips.Should().Be(720f);
		result.Rows[0].HeightRule.Should().Be(RowHeightRule.Exact);
	}

	[Fact]
	public void Parse_RowWithAtLeastHeight_ParsesRule()
	{
		var table = new Table(
			new TableRow(
				new TableRowProperties(
					new TableRowHeight { Val = 360, HeightType = HeightRuleValues.AtLeast }),
				new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].HeightTwips.Should().Be(360f);
		result.Rows[0].HeightRule.Should().Be(RowHeightRule.AtLeast);
	}

	[Fact]
	public void Parse_RowWithAutoHeight_ParsesRule()
	{
		var table = new Table(
			new TableRow(
				new TableRowProperties(
					new TableRowHeight { Val = 400, HeightType = HeightRuleValues.Auto }),
				new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].HeightRule.Should().Be(RowHeightRule.Auto);
	}

	[Fact]
	public void Parse_RowWithHeightNoType_DefaultsToAuto()
	{
		var table = new Table(
			new TableRow(
				new TableRowProperties(
					new TableRowHeight { Val = 500 }),
				new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].HeightTwips.Should().Be(500f);
		result.Rows[0].HeightRule.Should().Be(RowHeightRule.Auto);
	}

	[Fact]
	public void Parse_RowIsHeaderRow_ParsesTrue()
	{
		var table = new Table(
			new TableRow(
				new TableRowProperties(new TableHeader()),
				new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].IsHeaderRow.Should().BeTrue();
	}

	[Fact]
	public void Parse_RowHeaderOff_ParsesFalse()
	{
		var table = new Table(
			new TableRow(
				new TableRowProperties(new TableHeader { Val = OnOffOnlyValues.Off }),
				new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].IsHeaderRow.Should().BeFalse();
	}

	[Fact]
	public void Parse_RowCantSplit_ParsesTrue()
	{
		var table = new Table(
			new TableRow(
				new TableRowProperties(new CantSplit()),
				new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].CantSplit.Should().BeTrue();
	}

	[Fact]
	public void Parse_RowCantSplitOff_ParsesFalse()
	{
		var table = new Table(
			new TableRow(
				new TableRowProperties(new CantSplit { Val = OnOffOnlyValues.Off }),
				new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].CantSplit.Should().BeFalse();
	}

	[Fact]
	public void Parse_RowWithAllProperties_ParsesAll()
	{
		var table = new Table(
			new TableRow(
				new TableRowProperties(
					new TableRowHeight { Val = 720, HeightType = HeightRuleValues.Exact },
					new TableHeader(),
					new CantSplit()),
				new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		var row = result.Rows[0];
		row.HeightTwips.Should().Be(720f);
		row.HeightRule.Should().Be(RowHeightRule.Exact);
		row.IsHeaderRow.Should().BeTrue();
		row.CantSplit.Should().BeTrue();
	}

	[Fact]
	public void ParseRowHeightRule_NullProperties_ReturnsAuto()
	{
		var result = TableParser.ParseRowHeightRule(null);

		result.Should().Be(RowHeightRule.Auto);
	}

	[Fact]
	public void RowHeightRule_EnumValues_AreCorrect()
	{
		((int)RowHeightRule.Auto).Should().Be(0);
		((int)RowHeightRule.AtLeast).Should().Be(1);
		((int)RowHeightRule.Exact).Should().Be(2);
	}

	// ---- Cell properties (4.1.4) ----

	[Fact]
	public void Parse_CellWithWidth_ParsesCellWidth()
	{
		var table = new Table(
			new TableRow(
				new TableCell(
					new TableCellProperties(
						new TableCellWidth { Width = "2400", Type = TableWidthUnitValues.Dxa }),
					new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].Cells[0].Width.Type.Should().Be(TableWidthUnit.Dxa);
		result.Rows[0].Cells[0].Width.Value.Should().Be(2400f);
	}

	[Fact]
	public void Parse_CellWithNoWidth_DefaultsToAuto()
	{
		var table = new Table(
			new TableRow(
				new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].Cells[0].Width.Should().Be(TableWidthValue.Auto);
	}

	[Fact]
	public void Parse_CellWithPercentageWidth_ParsesPct()
	{
		var table = new Table(
			new TableRow(
				new TableCell(
					new TableCellProperties(
						new TableCellWidth { Width = "2500", Type = TableWidthUnitValues.Pct }),
					new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].Cells[0].Width.Type.Should().Be(TableWidthUnit.Pct);
		result.Rows[0].Cells[0].Width.Value.Should().Be(2500f);
	}

	[Fact]
	public void Parse_CellVerticalAlignmentCenter_Parses()
	{
		var table = new Table(
			new TableRow(
				new TableCell(
					new TableCellProperties(
						new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }),
					new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].Cells[0].VerticalAlignment.Should().Be(CellVerticalAlignment.Center);
	}

	[Fact]
	public void Parse_CellVerticalAlignmentBottom_Parses()
	{
		var table = new Table(
			new TableRow(
				new TableCell(
					new TableCellProperties(
						new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Bottom }),
					new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].Cells[0].VerticalAlignment.Should().Be(CellVerticalAlignment.Bottom);
	}

	[Fact]
	public void Parse_CellVerticalAlignmentDefault_IsTop()
	{
		var table = new Table(
			new TableRow(
				new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].Cells[0].VerticalAlignment.Should().Be(CellVerticalAlignment.Top);
	}

	[Fact]
	public void Parse_CellVerticalAlignmentExplicitTop_IsTop()
	{
		var table = new Table(
			new TableRow(
				new TableCell(
					new TableCellProperties(
						new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Top }),
					new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].Cells[0].VerticalAlignment.Should().Be(CellVerticalAlignment.Top);
	}

	[Fact]
	public void Parse_CellTextDirectionTbRl_Parses()
	{
		var table = new Table(
			new TableRow(
				new TableCell(
					new TableCellProperties(
						new TextDirection { Val = TextDirectionValues.TopToBottomRightToLeft }),
					new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].Cells[0].TextDirection.Should().Be(CellTextDirection.TopToBottomRightToLeft);
	}

	[Fact]
	public void Parse_CellTextDirectionTbRlRotated_Parses()
	{
		var table = new Table(
			new TableRow(
				new TableCell(
					new TableCellProperties(
						new TextDirection { Val = TextDirectionValues.TopToBottomRightToLeftRotated }),
					new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].Cells[0].TextDirection.Should().Be(CellTextDirection.TopToBottomRightToLeft);
	}

	[Fact]
	public void Parse_CellTextDirectionBtLr_Parses()
	{
		var table = new Table(
			new TableRow(
				new TableCell(
					new TableCellProperties(
						new TextDirection { Val = TextDirectionValues.BottomToTopLeftToRight }),
					new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].Cells[0].TextDirection.Should().Be(CellTextDirection.BottomToTopLeftToRight);
	}

	[Fact]
	public void Parse_CellTextDirectionBtLr2010_Parses()
	{
		var table = new Table(
			new TableRow(
				new TableCell(
					new TableCellProperties(
						new TextDirection { Val = TextDirectionValues.BottomToTopLeftToRight2010 }),
					new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].Cells[0].TextDirection.Should().Be(CellTextDirection.BottomToTopLeftToRight);
	}

	[Fact]
	public void Parse_CellTextDirectionDefault_IsLrTb()
	{
		var table = new Table(
			new TableRow(
				new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].Cells[0].TextDirection.Should().Be(CellTextDirection.LeftToRightTopToBottom);
	}

	[Fact]
	public void Parse_CellTextDirectionLrTb_IsDefault()
	{
		var table = new Table(
			new TableRow(
				new TableCell(
					new TableCellProperties(
						new TextDirection { Val = TextDirectionValues.LefToRightTopToBottom }),
					new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].Cells[0].TextDirection.Should().Be(CellTextDirection.LeftToRightTopToBottom);
	}

	[Fact]
	public void Parse_CellWithMargins_ParsesAll()
	{
		var table = new Table(
			new TableRow(
				new TableCell(
					new TableCellProperties(
						new TableCellMargin(
							new TopMargin { Width = "72", Type = TableWidthUnitValues.Dxa },
							new LeftMargin { Width = "108", Type = TableWidthUnitValues.Dxa },
							new BottomMargin { Width = "72", Type = TableWidthUnitValues.Dxa },
							new RightMargin { Width = "108", Type = TableWidthUnitValues.Dxa })),
					new Paragraph())));

		var result = TableParser.Parse(table);

		var margins = result.Rows[0].Cells[0].Margins;
		margins.Top.Should().Be(72f);
		margins.Right.Should().Be(108f);
		margins.Bottom.Should().Be(72f);
		margins.Left.Should().Be(108f);
	}

	[Fact]
	public void Parse_CellWithNoMarginsAndNoStyle_UsesWordDefault108TwipLeftRightMargins()
	{
		var table = new Table(
			new TableRow(
				new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		// Word's built-in TableNormal style defines 108-twip L/R, 0 T/B as the default.
		// ResolveDefaultCellMargins() applies this fallback when no explicit margins are defined.
		result.Rows[0].Cells[0].Margins.Left.Should().Be(108f);
		result.Rows[0].Cells[0].Margins.Right.Should().Be(108f);
		result.Rows[0].Cells[0].Margins.Top.Should().Be(0f);
		result.Rows[0].Cells[0].Margins.Bottom.Should().Be(0f);
	}

	[Fact]
	public void Parse_CellWithPartialMargins_ZeroForMissing()
	{
		var table = new Table(
			new TableRow(
				new TableCell(
					new TableCellProperties(
						new TableCellMargin(
							new TopMargin { Width = "100", Type = TableWidthUnitValues.Dxa })),
					new Paragraph())));

		var result = TableParser.Parse(table);

		var margins = result.Rows[0].Cells[0].Margins;
		margins.Top.Should().Be(100f);
		margins.Right.Should().Be(0f);
		margins.Bottom.Should().Be(0f);
		margins.Left.Should().Be(0f);
	}

	[Fact]
	public void Parse_CellWithMarginNoWidthValue_ZeroForThat()
	{
		var table = new Table(
			new TableRow(
				new TableCell(
					new TableCellProperties(
						new TableCellMargin(
							new TopMargin())),
					new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].Cells[0].Margins.Top.Should().Be(0f);
	}

	[Fact]
	public void ParseCellVerticalAlignment_Null_ReturnsTop()
	{
		TableParser.ParseCellVerticalAlignment(null).Should().Be(CellVerticalAlignment.Top);
	}

	[Fact]
	public void ParseCellTextDirection_Null_ReturnsLrTb()
	{
		TableParser.ParseCellTextDirection(null).Should().Be(CellTextDirection.LeftToRightTopToBottom);
	}

	[Fact]
	public void ParseCellMargins_Null_ReturnsNone()
	{
		TableParser.ParseCellMargins(null).Should().Be(CellMargins.None);
	}

	[Fact]
	public void CellVerticalAlignment_EnumValues_AreCorrect()
	{
		((int)CellVerticalAlignment.Top).Should().Be(0);
		((int)CellVerticalAlignment.Center).Should().Be(1);
		((int)CellVerticalAlignment.Bottom).Should().Be(2);
	}

	[Fact]
	public void CellTextDirection_EnumValues_AreCorrect()
	{
		((int)CellTextDirection.LeftToRightTopToBottom).Should().Be(0);
		((int)CellTextDirection.TopToBottomRightToLeft).Should().Be(1);
		((int)CellTextDirection.BottomToTopLeftToRight).Should().Be(2);
	}

	[Fact]
	public void CellMargins_None_HasAllZeros()
	{
		var none = CellMargins.None;
		none.Top.Should().Be(0f);
		none.Right.Should().Be(0f);
		none.Bottom.Should().Be(0f);
		none.Left.Should().Be(0f);
	}

	// ---- Border definitions (4.5.1) ----

	[Fact]
	public void Parse_TableWithBorders_ParsesBorderDefinitions()
	{
		var table = new Table(
			new TableProperties(
				new TableBorders(
					new TopBorder { Val = BorderValues.Single, Size = 8U, Color = "FF0000" },
					new BottomBorder { Val = BorderValues.Double, Size = 12U, Color = "00FF00" },
					new InsideHorizontalBorder { Val = BorderValues.Dotted, Size = 4U, Color = "0000FF" })),
			new TableRow(new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.Borders.Top.Should().NotBeNull();
		result.Borders.Top!.Value.Style.Should().Be(BorderStyle.Single);
		result.Borders.Top!.Value.WidthEighthsOfPoint.Should().Be(8);
		result.Borders.Top!.Value.Color.Should().Be("FF0000");

		result.Borders.Bottom.Should().NotBeNull();
		result.Borders.Bottom!.Value.Style.Should().Be(BorderStyle.Double);
		result.Borders.InsideHorizontal.Should().NotBeNull();
		result.Borders.InsideHorizontal!.Value.Style.Should().Be(BorderStyle.Dotted);
	}

	[Fact]
	public void Parse_TableWithNoBorders_DefaultsToNone()
	{
		var table = new Table(
			new TableProperties(),
			new TableRow(new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.Borders.Should().Be(TableBorderSet.None);
		result.Borders.HasAnyVisibleBorder.Should().BeFalse();
		result.BorderSpacingTwips.Should().Be(0f);
	}

	[Fact]
	public void Parse_CellWithBorders_ParsesBorderDefinitions()
	{
		var table = new Table(
			new TableRow(
				new TableCell(
					new TableCellProperties(
						new TableCellBorders(
							new LeftBorder { Val = BorderValues.Dashed, Size = 6U, Color = "ABCDEF" },
							new RightBorder { Val = BorderValues.Thick, Size = 10U, Color = "123456" })),
					new Paragraph())));

		var result = TableParser.Parse(table);

		var borders = result.Rows[0].Cells[0].Borders;
		borders.Left.Should().NotBeNull();
		borders.Left!.Value.Style.Should().Be(BorderStyle.Dashed);
		borders.Left!.Value.WidthEighthsOfPoint.Should().Be(6);
		borders.Left!.Value.Color.Should().Be("ABCDEF");
		borders.Right.Should().NotBeNull();
		borders.Right!.Value.Style.Should().Be(BorderStyle.Thick);
	}

	[Fact]
	public void Parse_CellWithNoBorders_DefaultsToNone()
	{
		var table = new Table(
			new TableRow(new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].Cells[0].Borders.Should().Be(TableBorderSet.None);
	}

	[Fact]
	public void Parse_RowWithTablePropertyExceptionBorders_ParsesRowBorders()
	{
		var table = new Table(
			new TableRow(
				new TableRowProperties(
					new TablePropertyExceptions(
						new TableBorders(
							new TopBorder { Val = BorderValues.Double, Size = 7U, Color = "112233" }))),
				new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.Rows[0].Borders.Top.Should().NotBeNull();
		result.Rows[0].Borders.Top!.Value.Style.Should().Be(BorderStyle.Double);
		result.Rows[0].Borders.Top!.Value.WidthEighthsOfPoint.Should().Be(7);
		result.Rows[0].Borders.Top!.Value.Color.Should().Be("112233");
	}

	[Fact]
	public void ParseBorderStyle_NullAndNone_ReturnNone()
	{
		TableParser.ParseBorderStyle(null).Should().Be(BorderStyle.None);
		TableParser.ParseBorderStyle(BorderValues.None).Should().Be(BorderStyle.None);
	}

	[Fact]
	public void ParseBorderStyle_KnownValues_MapCorrectly()
	{
		TableParser.ParseBorderStyle(BorderValues.Single).Should().Be(BorderStyle.Single);
		TableParser.ParseBorderStyle(BorderValues.Double).Should().Be(BorderStyle.Double);
		TableParser.ParseBorderStyle(BorderValues.Dotted).Should().Be(BorderStyle.Dotted);
		TableParser.ParseBorderStyle(BorderValues.Dashed).Should().Be(BorderStyle.Dashed);
		TableParser.ParseBorderStyle(BorderValues.DotDash).Should().Be(BorderStyle.DotDash);
		TableParser.ParseBorderStyle(BorderValues.DotDotDash).Should().Be(BorderStyle.DotDotDash);
		TableParser.ParseBorderStyle(BorderValues.Triple).Should().Be(BorderStyle.Triple);
		TableParser.ParseBorderStyle(BorderValues.Thick).Should().Be(BorderStyle.Thick);
		TableParser.ParseBorderStyle(BorderValues.ThinThickSmallGap).Should().Be(BorderStyle.ThinThickSmallGap);
		TableParser.ParseBorderStyle(BorderValues.ThickThinSmallGap).Should().Be(BorderStyle.ThickThinSmallGap);
		TableParser.ParseBorderStyle(BorderValues.ThinThickThinSmallGap).Should().Be(BorderStyle.ThinThickThinSmallGap);
		TableParser.ParseBorderStyle(BorderValues.Wave).Should().Be(BorderStyle.Wave);
		TableParser.ParseBorderStyle(BorderValues.DoubleWave).Should().Be(BorderStyle.DoubleWave);
		TableParser.ParseBorderStyle(BorderValues.ThreeDEmboss).Should().Be(BorderStyle.ThreeDEmboss);
		TableParser.ParseBorderStyle(BorderValues.ThreeDEngrave).Should().Be(BorderStyle.ThreeDEngrave);
		TableParser.ParseBorderStyle(BorderValues.Nil).Should().Be(BorderStyle.None);
	}

	[Fact]
	public void ParseBorderStyle_UnmappedValue_ReturnsNone()
	{
		TableParser.ParseBorderStyle(BorderValues.Apples).Should().Be(BorderStyle.None);
	}

	[Fact]
	public void ParseBorderDefinition_AllSupportedElements_Parse()
	{
		TableParser.ParseBorderDefinition(new TopBorder { Val = BorderValues.Single, Size = 4U, Color = "FF0000" })
			.Should().Be(new TableBorderDefinition(BorderStyle.Single, 4, "FF0000"));
		TableParser.ParseBorderDefinition(new BottomBorder { Val = BorderValues.Double, Size = 6U, Color = "00FF00" })
			.Should().Be(new TableBorderDefinition(BorderStyle.Double, 6, "00FF00"));
		TableParser.ParseBorderDefinition(new LeftBorder { Val = BorderValues.Dotted, Size = 2U, Color = "0000FF" })
			.Should().Be(new TableBorderDefinition(BorderStyle.Dotted, 2, "0000FF"));
		TableParser.ParseBorderDefinition(new RightBorder { Val = BorderValues.Dashed, Size = 8U, Color = "ABCDEF" })
			.Should().Be(new TableBorderDefinition(BorderStyle.Dashed, 8, "ABCDEF"));
		TableParser.ParseBorderDefinition(new InsideHorizontalBorder { Val = BorderValues.DotDash, Size = 3U, Color = "112233" })
			.Should().Be(new TableBorderDefinition(BorderStyle.DotDash, 3, "112233"));
		TableParser.ParseBorderDefinition(new InsideVerticalBorder { Val = BorderValues.DotDotDash, Size = 5U, Color = "445566" })
			.Should().Be(new TableBorderDefinition(BorderStyle.DotDotDash, 5, "445566"));
	}

	[Fact]
	public void ParseBorderDefinition_UnsupportedOrNull_ReturnsNull()
	{
		TableParser.ParseBorderDefinition(null).Should().BeNull();
		TableParser.ParseBorderDefinition(new GridSpan { Val = 2 }).Should().BeNull();
	}

	[Fact]
	public void TableBorderDefinition_WidthConversion_Works()
	{
		var border = new TableBorderDefinition(BorderStyle.Single, 8, "FF0000");

		border.IsVisible.Should().BeTrue();
		border.GetWidthTwips().Should().Be(20f);
		TableBorderDefinition.None.IsVisible.Should().BeFalse();
	}

	[Fact]
	public void TableBorderSet_HasAnyVisibleBorder_Works()
	{
		var none = TableBorderSet.None;
		none.HasAnyVisibleBorder.Should().BeFalse();

		var borders = new TableBorderSet(Top: new TableBorderDefinition(BorderStyle.Single, 4));
		borders.HasAnyVisibleBorder.Should().BeTrue();
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
	public void Parse_TableWithCellSpacing_ParsesBorderSpacingTwips()
	{
		var table = new Table(
			new TableProperties(
				new TableCellSpacing { Width = "120", Type = TableWidthUnitValues.Dxa }),
			new TableRow(new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.BorderSpacingTwips.Should().Be(120f);
	}

	[Fact]
	public void Parse_TableWithPctCellSpacing_DefaultsToZero()
	{
		var table = new Table(
			new TableProperties(
				new TableCellSpacing { Width = "500", Type = TableWidthUnitValues.Pct }),
			new TableRow(new TableCell(new Paragraph())));

		var result = TableParser.Parse(table);

		result.BorderSpacingTwips.Should().Be(0f);
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
	public void ParseTableCellSpacing_Null_ReturnsZero()
	{
		var result = TableParser.ParseTableCellSpacing(null);

		result.Should().Be(0f);
	}

	[Fact]
	public void ParseTableCellSpacing_Dxa_ReturnsValue()
	{
		var spacing = new TableCellSpacing { Width = "96", Type = TableWidthUnitValues.Dxa };

		var result = TableParser.ParseTableCellSpacing(spacing);

		result.Should().Be(96f);
	}

	[Fact]
	public void ParseTableCellSpacing_InvalidWidth_ReturnsZero()
	{
		var spacing = new TableCellSpacing { Width = "invalid", Type = TableWidthUnitValues.Dxa };

		var result = TableParser.ParseTableCellSpacing(spacing);

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

	[Fact]
	public void Parse_TableWithBiDiVisual_SetsIsBiDi()
	{
		var table = new Table(
			new TableProperties(new BiDiVisual()),
			new TableGrid(new GridColumn { Width = "2000" }),
			new TableRow(new TableCell(new Paragraph(new Run(new Text("A"))))));

		var result = TableParser.Parse(table);

		result.IsBiDi.Should().BeTrue();
	}

	[Fact]
	public void Parse_TableWithoutBiDiVisual_IsBiDiIsFalse()
	{
		var table = new Table(
			new TableProperties(),
			new TableGrid(new GridColumn { Width = "2000" }),
			new TableRow(new TableCell(new Paragraph(new Run(new Text("A"))))));

		var result = TableParser.Parse(table);

		result.IsBiDi.Should().BeFalse();
	}

	// ---- SDT (content control) support ----

	[Fact]
	public void Parse_SdtRowWrappingTableRow_UnwrapsRow()
	{
		var table = new Table(
			new TableGrid(new GridColumn { Width = "2400" }),
			new SdtRow(
				new SdtContentRow(
					new TableRow(
						new TableCell(
							new TableCellProperties(new TableCellWidth { Width = "2400", Type = TableWidthUnitValues.Dxa }),
							new Paragraph(new Run(new Text("SDT Row"))))))));

		var result = TableParser.Parse(table);

		result.Rows.Should().ContainSingle();
		result.Rows[0].Cells.Should().ContainSingle();
	}

	[Fact]
	public void Parse_SdtCellWrappingTableCell_UnwrapsCell()
	{
		var table = new Table(
			new TableGrid(new GridColumn { Width = "2400" }),
			new TableRow(
				new SdtCell(
					new SdtContentCell(
						new TableCell(
							new TableCellProperties(new TableCellWidth { Width = "2400", Type = TableWidthUnitValues.Dxa }),
							new Paragraph(new Run(new Text("SDT Cell"))))))));

		var result = TableParser.Parse(table);

		result.Rows.Should().ContainSingle();
		result.Rows[0].Cells.Should().ContainSingle();
	}

	[Fact]
	public void Parse_MixedSdtRowAndNormalRow_ParsesBoth()
	{
		var table = new Table(
			new TableGrid(new GridColumn { Width = "2400" }),
			new TableRow(
				new TableCell(
					new TableCellProperties(new TableCellWidth { Width = "2400", Type = TableWidthUnitValues.Dxa }),
					new Paragraph(new Run(new Text("Normal"))))),
			new SdtRow(
				new SdtContentRow(
					new TableRow(
						new TableCell(
							new TableCellProperties(new TableCellWidth { Width = "2400", Type = TableWidthUnitValues.Dxa }),
							new Paragraph(new Run(new Text("SDT"))))))));

		var result = TableParser.Parse(table);

		result.Rows.Should().HaveCount(2);
	}

	[Fact]
	public void Parse_MixedSdtCellAndNormalCell_ParsesBoth()
	{
		var table = new Table(
			new TableGrid(
				new GridColumn { Width = "2400" },
				new GridColumn { Width = "2400" }),
			new TableRow(
				new TableCell(
					new TableCellProperties(new TableCellWidth { Width = "2400", Type = TableWidthUnitValues.Dxa }),
					new Paragraph(new Run(new Text("Normal")))),
				new SdtCell(
					new SdtContentCell(
						new TableCell(
							new TableCellProperties(new TableCellWidth { Width = "2400", Type = TableWidthUnitValues.Dxa }),
							new Paragraph(new Run(new Text("SDT"))))))));

		var result = TableParser.Parse(table);

		result.Rows.Should().ContainSingle();
		result.Rows[0].Cells.Should().HaveCount(2);
	}
}
