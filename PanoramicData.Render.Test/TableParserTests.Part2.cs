namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed partial class TableParserTests
{
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
		var table = SingleCellTableWithMargins(
			new TopMargin { Width = "72", Type = TableWidthUnitValues.Dxa },
			new LeftMargin { Width = "108", Type = TableWidthUnitValues.Dxa },
			new BottomMargin { Width = "72", Type = TableWidthUnitValues.Dxa },
			new RightMargin { Width = "108", Type = TableWidthUnitValues.Dxa });

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
		var table = SingleCellTableWithMargins(new TopMargin { Width = "100", Type = TableWidthUnitValues.Dxa });

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
		var table = SingleCellTableWithMargins(new TopMargin());

		var result = TableParser.Parse(table);

		result.Rows[0].Cells[0].Margins.Top.Should().Be(0f);
	}
}
