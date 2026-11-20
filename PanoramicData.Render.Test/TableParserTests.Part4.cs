namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed partial class TableParserTests
{
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
		var table = new Table(new TableGrid(new GridColumn { Width = "2400" }), SdtRowOf("SDT Row"));

		var result = TableParser.Parse(table);

		result.Rows.Should().ContainSingle();
		result.Rows[0].Cells.Should().ContainSingle();
	}

	[Fact]
	public void Parse_SdtCellWrappingTableCell_UnwrapsCell()
	{
		var table = new Table(new TableGrid(new GridColumn { Width = "2400" }), new TableRow(SdtCellOf("SDT Cell")));

		var result = TableParser.Parse(table);

		result.Rows.Should().ContainSingle();
		result.Rows[0].Cells.Should().ContainSingle();
	}

	[Fact]
	public void Parse_MixedSdtRowAndNormalRow_ParsesBoth()
	{
		var table = new Table(
			new TableGrid(new GridColumn { Width = "2400" }),
			new TableRow(WideTextCell("Normal")),
			SdtRowOf("SDT"));

		var result = TableParser.Parse(table);

		result.Rows.Should().HaveCount(2);
	}

	[Fact]
	public void Parse_MixedSdtCellAndNormalCell_ParsesBoth()
	{
		var table = new Table(
			new TableGrid(new GridColumn { Width = "2400" }, new GridColumn { Width = "2400" }),
			new TableRow(WideTextCell("Normal"), SdtCellOf("SDT")));

		var result = TableParser.Parse(table);

		result.Rows.Should().ContainSingle();
		result.Rows[0].Cells.Should().HaveCount(2);
	}
}