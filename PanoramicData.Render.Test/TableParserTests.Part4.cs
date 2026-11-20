namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using static PanoramicData.Render.Test.TableParserBuilders;

public sealed partial class TableParserTests
{
	[Fact]
	public void ParseTableWidth_NoTypeAttribute_DefaultsToAuto()
	{
		TableParser.ParseTableWidth(new TableWidth { Width = "1000" }).Type.Should().Be(TableWidthUnit.Auto);
	}

	[Fact]
	public void ParseAlignment_NullJustification_ReturnsLeft()
	{
		TableParser.ParseAlignment(null).Should().Be(TableAlignment.Left);
	}

	[Fact]
	public void ParseIndentation_NullIndentation_ReturnsZero()
	{
		TableParser.ParseIndentation(null).Should().Be(0f);
	}

	[Fact]
	public void ParseTableCellSpacing_Null_ReturnsZero()
	{
		TableParser.ParseTableCellSpacing(null).Should().Be(0f);
	}

	[Fact]
	public void ParseTableCellSpacing_Dxa_ReturnsValue()
	{
		TableParser.ParseTableCellSpacing(new TableCellSpacing { Width = "96", Type = TableWidthUnitValues.Dxa }).Should().Be(96f);
	}

	[Fact]
	public void ParseTableCellSpacing_InvalidWidth_ReturnsZero()
	{
		TableParser.ParseTableCellSpacing(new TableCellSpacing { Width = "invalid", Type = TableWidthUnitValues.Dxa }).Should().Be(0f);
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
		ParsedTableWith(new BiDiVisual()).IsBiDi.Should().BeTrue();
	}

	[Fact]
	public void Parse_TableWithoutBiDiVisual_IsBiDiIsFalse()
	{
		ParsedTableWith().IsBiDi.Should().BeFalse();
	}

	// ---- SDT (content control) support ----

	[Fact]
	public void Parse_SdtRowWrappingTableRow_UnwrapsRow()
	{
		AssertSingleRowWithCells(ParseWithSingleColumn(SdtRowOf("SDT Row")), 1);
	}

	[Fact]
	public void Parse_SdtCellWrappingTableCell_UnwrapsCell()
	{
		AssertSingleRowWithCells(ParseWithSingleColumn(new TableRow(SdtCellOf("SDT Cell"))), 1);
	}

	[Fact]
	public void Parse_MixedSdtRowAndNormalRow_ParsesBoth()
	{
		ParseWithSingleColumn(new TableRow(WideTextCell("Normal")), SdtRowOf("SDT")).Rows.Should().HaveCount(2);
	}

	[Fact]
	public void Parse_MixedSdtCellAndNormalCell_ParsesBoth()
	{
		var table = new Table(
			new TableGrid(new GridColumn { Width = "2400" }, new GridColumn { Width = "2400" }),
			new TableRow(WideTextCell("Normal"), SdtCellOf("SDT")));

		AssertSingleRowWithCells(TableParser.Parse(table), 2);
	}

	private static TableElement ParseWithSingleColumn(params DocumentFormat.OpenXml.OpenXmlElement[] rows)
		=> TableParser.Parse(new Table(new DocumentFormat.OpenXml.OpenXmlElement[] { new TableGrid(new GridColumn { Width = "2400" }) }.Concat(rows)));

	private static void AssertSingleRowWithCells(TableElement result, int cellCount)
	{
		result.Rows.Should().ContainSingle();
		result.Rows[0].Cells.Should().HaveCount(cellCount);
	}
}
