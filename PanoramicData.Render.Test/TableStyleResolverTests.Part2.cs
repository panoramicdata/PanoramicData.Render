namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public partial class TableStyleResolverTests
{

	[Fact]
	public void Resolve_ClonesBaseFragments()
	{
		var style = new Style(new StyleTableProperties(new TableJustification { Val = TableRowAlignmentValues.Center }))
		{
			Type = StyleValues.Table,
			StyleId = "CloneTable"
		};

		using var doc = StyleDocument.Create(new Styles(style));
		var source = doc.Styles!.Elements<Style>().Single().StyleTableProperties!;

		var result = TableStyleResolver.Resolve(doc.Styles, "CloneTable", null);

		ReferenceEquals(source, result!.TableProperties).Should().BeFalse();
	}

	[Fact]
	public void Resolve_WithNullOrWhitespaceStyleId_ReturnsNull()
	{
		using var doc = StyleDocument.Create(new Styles());

		TableStyleResolver.Resolve(doc.Styles, null!, null).Should().BeNull();
		TableStyleResolver.Resolve(doc.Styles, string.Empty, null).Should().BeNull();
		TableStyleResolver.Resolve(doc.Styles, " ", null).Should().BeNull();
	}

	[Fact]
	public void ResolveCellShading_FirstRowOverridesBandedRowShading()
	{
		var band1Shading = ClearShading("ffff00");
		var firstRowShading = ClearShading("00ff00");

		var style = new Style(
			new TableStyleProperties(new TableCellProperties(band1Shading)) { Type = TableStyleOverrideValues.Band1Horizontal },
			new TableStyleProperties(new TableCellProperties(firstRowShading)) { Type = TableStyleOverrideValues.FirstRow })
		{
			Type = StyleValues.Table,
			StyleId = "ConditionalShade"
		};

		using var doc = StyleDocument.Create(new Styles(style));

		var table = StyledTwoRowTable("ConditionalShade", new TableLookOptions(ApplyFirstRow: true, ApplyBandedRows: true));

		var firstRow = TableStyleResolver.ResolveCellShading(doc.Styles, table, 0, 0, 1, 1, 2, 1);
		var secondRow = TableStyleResolver.ResolveCellShading(doc.Styles, table, 1, 0, 1, 1, 2, 1);

		firstRow.FillColor.Should().Be("00FF00");
		secondRow.FillColor.Should().Be("FFFF00");
	}

	[Fact]
	public void ResolveCellShading_BandedColumnsExcludeFirstColumnAndAlternate()
	{
		var band1Shading = ClearShading("fff000");
		var band2Shading = ClearShading("00aaff");
		var firstColumnShading = ClearShading("11bb33");

		var style = new Style(
			new TableStyleProperties(new TableCellProperties(band1Shading)) { Type = TableStyleOverrideValues.Band1Vertical },
			new TableStyleProperties(new TableCellProperties(band2Shading)) { Type = TableStyleOverrideValues.Band2Vertical },
			new TableStyleProperties(new TableCellProperties(firstColumnShading)) { Type = TableStyleOverrideValues.FirstColumn })
		{
			Type = StyleValues.Table,
			StyleId = "ColumnBands"
		};

		using var doc = StyleDocument.Create(new Styles(style));

		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1000f), new TableGridColumn(1000f)],
			Rows = [new TableRowElement { Cells = [new TableCellElement { Blocks = [] }, new TableCellElement { Blocks = [] }, new TableCellElement { Blocks = [] }] }],
			StyleId = "ColumnBands",
			Look = new TableLookOptions(ApplyFirstColumn: true, ApplyBandedColumns: true),
		};

		TableStyleResolver.ResolveCellShading(doc.Styles, table, 0, 0, 1, 1, 1, 3).FillColor.Should().Be("11BB33");
		TableStyleResolver.ResolveCellShading(doc.Styles, table, 0, 1, 1, 1, 1, 3).FillColor.Should().Be("FFF000");
		TableStyleResolver.ResolveCellShading(doc.Styles, table, 0, 2, 1, 1, 1, 3).FillColor.Should().Be("00AAFF");
	}

	[Fact]
	public void Resolve_WalksBasedOnChain_FindsConditionalInParentStyle()
	{
		var bandShading = ClearShading("aabb00");

		var parentStyle = new Style(
			new TableStyleProperties(new TableCellProperties(bandShading))
			{ Type = TableStyleOverrideValues.Band1Horizontal })
		{
			Type = StyleValues.Table,
			StyleId = "ParentTable"
		};

		var childStyle = new Style(
			new StyleRunProperties(new Bold()))
		{
			Type = StyleValues.Table,
			StyleId = "ChildTable",
			BasedOn = new BasedOn { Val = "ParentTable" }
		};

		using var doc = StyleDocument.Create(new Styles(parentStyle, childStyle));

		var result = TableStyleResolver.Resolve(
			doc.Styles,
			"ChildTable",
			[TableStyleOverrideValues.Band1Horizontal]);

		result.Should().NotBeNull();
		result!.AppliedConditionals.Should().Equal(TableStyleOverrideValues.Band1Horizontal);
		result.TableCellProperties.Should().NotBeNull();
		result.TableCellProperties!.GetFirstChild<Shading>()?.Fill?.Value.Should().Be("aabb00");
		result.RunProperties.Should().NotBeNull();
		result.RunProperties!.GetFirstChild<Bold>().Should().NotBeNull();
	}

	[Fact]
	public void ResolveCellShading_WalksBasedOnChain_FindsBandShadingFromAncestorStyle()
	{
		var band1Shading = ClearShading("cc1122");
		var firstRowShading = ClearShading("00dd88");

		var baseStyle = new Style(
			new TableStyleProperties(new TableCellProperties(band1Shading))
			{ Type = TableStyleOverrideValues.Band1Horizontal },
			new TableStyleProperties(new TableCellProperties(firstRowShading))
			{ Type = TableStyleOverrideValues.FirstRow })
		{
			Type = StyleValues.Table,
			StyleId = "BaseGrid"
		};

		var derivedStyle = new Style
		{
			Type = StyleValues.Table,
			StyleId = "DerivedGrid",
			BasedOn = new BasedOn { Val = "BaseGrid" }
		};

		using var doc = StyleDocument.Create(new Styles(baseStyle, derivedStyle));

		var table = StyledTwoRowTable("DerivedGrid", new TableLookOptions(ApplyFirstRow: true, ApplyBandedRows: true));

		var firstRow = TableStyleResolver.ResolveCellShading(doc.Styles, table, 0, 0, 1, 1, 2, 1);
		var secondRow = TableStyleResolver.ResolveCellShading(doc.Styles, table, 1, 0, 1, 1, 2, 1);

		firstRow.FillColor.Should().Be("00DD88");
		secondRow.FillColor.Should().Be("CC1122");
	}

	[Fact]
	public void ResolveCellShading_UsesBand1Fallback_WhenBand2HorizontalIsMissing()
	{
		var band1Shading = ClearShading("F7CAAC");

		var style = new Style(
			new TableStyleProperties(new TableCellProperties(band1Shading))
			{ Type = TableStyleOverrideValues.Band1Horizontal })
		{
			Type = StyleValues.Table,
			StyleId = "SingleBandStyle"
		};

		using var doc = StyleDocument.Create(new Styles(style));

		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement { Cells = [new TableCellElement { Blocks = [] }] },
				new TableRowElement { Cells = [new TableCellElement { Blocks = [] }] },
				new TableRowElement { Cells = [new TableCellElement { Blocks = [] }] },
			],
			StyleId = "SingleBandStyle",
			Look = new TableLookOptions(ApplyFirstRow: true, ApplyBandedRows: true),
		};

		var secondRow = TableStyleResolver.ResolveCellShading(doc.Styles, table, 1, 0, 1, 1, 3, 1);
		var thirdRow = TableStyleResolver.ResolveCellShading(doc.Styles, table, 2, 0, 1, 1, 3, 1);

		secondRow.FillColor.Should().Be("F7CAAC");
		thirdRow.FillColor.Should().Be("F7CAAC");
	}

	[Fact]
	public void Resolve_WalksBasedOnChain_CollectsBaseTcPrFromParentStyle()
	{
		// The parent style carries a whole-table tcPr with a background colour (simulating FBE4D5
		// from GridTable5Dark-Accent2).  The child style has no tcPr of its own.
		// After the fix, Resolve() should inherit the parent's whole-table tcPr.
		var baseShading = ClearShading("FBE4D5");

		var parentStyle = new Style(
			new TableCellProperties(baseShading))
		{
			Type = StyleValues.Table,
			StyleId = "ParentWithTcPr"
		};

		var childStyle = new Style(
			new StyleRunProperties(new Bold()))
		{
			Type = StyleValues.Table,
			StyleId = "ChildTable",
			BasedOn = new BasedOn { Val = "ParentWithTcPr" }
		};

		using var doc = StyleDocument.Create(new Styles(parentStyle, childStyle));

		var result = TableStyleResolver.Resolve(doc.Styles, "ChildTable", null);

		result.Should().NotBeNull();
		result!.TableCellProperties.Should().NotBeNull();
		result.TableCellProperties!.GetFirstChild<Shading>()?.Fill?.Value.Should().Be("FBE4D5");
	}
}
