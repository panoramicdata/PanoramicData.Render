namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using static PanoramicData.Render.Test.TableParserBuilders;

public sealed partial class TableParserTests
{
	[Fact]
	public void Parse_RowWithExactHeight_ParsesHeightAndRule()
	{
		var row = ParsedRow(new TableRowHeight { Val = 720, HeightType = HeightRuleValues.Exact });

		row.HeightTwips.Should().Be(720f);
		row.HeightRule.Should().Be(RowHeightRule.Exact);
	}

	[Fact]
	public void Parse_RowWithAtLeastHeight_ParsesRule()
	{
		var row = ParsedRow(new TableRowHeight { Val = 360, HeightType = HeightRuleValues.AtLeast });

		row.HeightTwips.Should().Be(360f);
		row.HeightRule.Should().Be(RowHeightRule.AtLeast);
	}

	[Fact]
	public void Parse_RowWithAutoHeight_ParsesRule()
	{
		ParsedRow(new TableRowHeight { Val = 400, HeightType = HeightRuleValues.Auto }).HeightRule.Should().Be(RowHeightRule.Auto);
	}

	[Fact]
	public void Parse_RowWithHeightNoType_DefaultsToAuto()
	{
		var row = ParsedRow(new TableRowHeight { Val = 500 });

		row.HeightTwips.Should().Be(500f);
		row.HeightRule.Should().Be(RowHeightRule.Auto);
	}

	[Fact]
	public void Parse_RowIsHeaderRow_ParsesTrue()
	{
		ParsedRow(new TableHeader()).IsHeaderRow.Should().BeTrue();
	}

	[Fact]
	public void Parse_RowHeaderOff_ParsesFalse()
	{
		ParsedRow(new TableHeader { Val = OnOffOnlyValues.Off }).IsHeaderRow.Should().BeFalse();
	}

	[Fact]
	public void Parse_RowCantSplit_ParsesTrue()
	{
		ParsedRow(new CantSplit()).CantSplit.Should().BeTrue();
	}

	[Fact]
	public void Parse_RowCantSplitOff_ParsesFalse()
	{
		ParsedRow(new CantSplit { Val = OnOffOnlyValues.Off }).CantSplit.Should().BeFalse();
	}

	[Fact]
	public void Parse_RowWithAllProperties_ParsesAll()
	{
		var row = ParsedRow(
			new TableRowHeight { Val = 720, HeightType = HeightRuleValues.Exact },
			new TableHeader(),
			new CantSplit());

		row.HeightTwips.Should().Be(720f);
		row.HeightRule.Should().Be(RowHeightRule.Exact);
		row.IsHeaderRow.Should().BeTrue();
		row.CantSplit.Should().BeTrue();
	}

	[Fact]
	public void ParseRowHeightRule_NullProperties_ReturnsAuto()
	{
		TableParser.ParseRowHeightRule(null).Should().Be(RowHeightRule.Auto);
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
		var cell = ParsedCell(new TableCellWidth { Width = "2400", Type = TableWidthUnitValues.Dxa });

		cell.Width.Type.Should().Be(TableWidthUnit.Dxa);
		cell.Width.Value.Should().Be(2400f);
	}

	[Fact]
	public void Parse_CellWithNoWidth_DefaultsToAuto()
	{
		ParsedPlainCell().Width.Should().Be(TableWidthValue.Auto);
	}

	[Fact]
	public void Parse_CellWithPercentageWidth_ParsesPct()
	{
		var cell = ParsedCell(new TableCellWidth { Width = "2500", Type = TableWidthUnitValues.Pct });

		cell.Width.Type.Should().Be(TableWidthUnit.Pct);
		cell.Width.Value.Should().Be(2500f);
	}

	[Fact]
	public void Parse_CellVerticalAlignmentCenter_Parses()
	{
		ParsedCell(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }).VerticalAlignment.Should().Be(CellVerticalAlignment.Center);
	}

	[Fact]
	public void Parse_CellVerticalAlignmentBottom_Parses()
	{
		ParsedCell(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Bottom }).VerticalAlignment.Should().Be(CellVerticalAlignment.Bottom);
	}

	[Fact]
	public void Parse_CellVerticalAlignmentDefault_IsTop()
	{
		ParsedPlainCell().VerticalAlignment.Should().Be(CellVerticalAlignment.Top);
	}

	[Fact]
	public void Parse_CellVerticalAlignmentExplicitTop_IsTop()
	{
		ParsedCell(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Top }).VerticalAlignment.Should().Be(CellVerticalAlignment.Top);
	}

	[Fact]
	public void Parse_CellTextDirectionTbRl_Parses()
	{
		AssertTextDirection(TextDirectionValues.TopToBottomRightToLeft, CellTextDirection.TopToBottomRightToLeft);
	}

	[Fact]
	public void Parse_CellTextDirectionTbRlRotated_Parses()
	{
		AssertTextDirection(TextDirectionValues.TopToBottomRightToLeftRotated, CellTextDirection.TopToBottomRightToLeft);
	}

	[Fact]
	public void Parse_CellTextDirectionBtLr_Parses()
	{
		AssertTextDirection(TextDirectionValues.BottomToTopLeftToRight, CellTextDirection.BottomToTopLeftToRight);
	}

	[Fact]
	public void Parse_CellTextDirectionBtLr2010_Parses()
	{
		AssertTextDirection(TextDirectionValues.BottomToTopLeftToRight2010, CellTextDirection.BottomToTopLeftToRight);
	}

	[Fact]
	public void Parse_CellTextDirectionDefault_IsLrTb()
	{
		ParsedPlainCell().TextDirection.Should().Be(CellTextDirection.LeftToRightTopToBottom);
	}

	[Fact]
	public void Parse_CellTextDirectionLrTb_IsDefault()
	{
		AssertTextDirection(TextDirectionValues.LefToRightTopToBottom, CellTextDirection.LeftToRightTopToBottom);
	}

	[Fact]
	public void Parse_CellWithMargins_ParsesAll()
	{
		var margins = ParsedMargins(
			new TopMargin { Width = "72", Type = TableWidthUnitValues.Dxa },
			new LeftMargin { Width = "108", Type = TableWidthUnitValues.Dxa },
			new BottomMargin { Width = "72", Type = TableWidthUnitValues.Dxa },
			new RightMargin { Width = "108", Type = TableWidthUnitValues.Dxa });

		AssertMargins(margins, top: 72f, right: 108f, bottom: 72f, left: 108f);
	}

	[Fact]
	public void Parse_CellWithNoMarginsAndNoStyle_UsesWordDefault108TwipLeftRightMargins()
	{
		// Word's built-in TableNormal style defines 108-twip L/R, 0 T/B as the default.
		// ResolveDefaultCellMargins() applies this fallback when no explicit margins are defined.
		AssertMargins(ParsedPlainCell().Margins, top: 0f, right: 108f, bottom: 0f, left: 108f);
	}

	[Fact]
	public void Parse_CellWithPartialMargins_ZeroForMissing()
	{
		AssertMargins(ParsedMargins(new TopMargin { Width = "100", Type = TableWidthUnitValues.Dxa }), top: 100f, right: 0f, bottom: 0f, left: 0f);
	}

	[Fact]
	public void Parse_CellWithMarginNoWidthValue_ZeroForThat()
	{
		ParsedMargins(new TopMargin()).Top.Should().Be(0f);
	}

	private static void AssertTextDirection(TextDirectionValues value, CellTextDirection expected)
	{
		ParsedCell(new TextDirection { Val = value }).TextDirection.Should().Be(expected);
	}

	private static void AssertMargins(CellMargins margins, float top, float right, float bottom, float left)
	{
		margins.Top.Should().Be(top);
		margins.Right.Should().Be(right);
		margins.Bottom.Should().Be(bottom);
		margins.Left.Should().Be(left);
	}
}
