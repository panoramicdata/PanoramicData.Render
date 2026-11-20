namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using static PanoramicData.Render.Test.TableParserBuilders;

public sealed partial class TableParserTests
{
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
}
