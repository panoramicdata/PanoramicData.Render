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
		var style = TableStyle("CloneTable", null, new StyleTableProperties(new TableJustification { Val = TableRowAlignmentValues.Center }));

		using var doc = new StyleDocument(new Styles(style));
		var source = doc.Styles!.Elements<Style>().Single().StyleTableProperties!;

		var result = TableStyleResolver.Resolve(doc.Styles, "CloneTable", null);

		ReferenceEquals(source, result!.TableProperties).Should().BeFalse();
	}

	[Fact]
	public void Resolve_WithNullOrWhitespaceStyleId_ReturnsNull()
	{
		using var doc = new StyleDocument(new Styles());

		TableStyleResolver.Resolve(doc.Styles, null!, null).Should().BeNull();
		TableStyleResolver.Resolve(doc.Styles, string.Empty, null).Should().BeNull();
		TableStyleResolver.Resolve(doc.Styles, " ", null).Should().BeNull();
	}

	[Fact]
	public void ResolveCellShading_FirstRowOverridesBandedRowShading()
	{
		var style = TableStyle("ConditionalShade", null,
			Conditional(TableStyleOverrideValues.Band1Horizontal, "ffff00"),
			Conditional(TableStyleOverrideValues.FirstRow, "00ff00"));
		using var doc = new StyleDocument(new Styles(style));
		var table = StyledTwoRowTable("ConditionalShade", new TableLookOptions(ApplyFirstRow: true, ApplyBandedRows: true));

		AssertRowFills(doc, table, "00FF00", "FFFF00");
	}

	[Fact]
	public void ResolveCellShading_BandedColumnsExcludeFirstColumnAndAlternate()
	{
		var style = TableStyle("ColumnBands", null,
			Conditional(TableStyleOverrideValues.Band1Vertical, "fff000"),
			Conditional(TableStyleOverrideValues.Band2Vertical, "00aaff"),
			Conditional(TableStyleOverrideValues.FirstColumn, "11bb33"));
		using var doc = new StyleDocument(new Styles(style));
		var table = GridTable("ColumnBands", new TableLookOptions(ApplyFirstColumn: true, ApplyBandedColumns: true), 1, 3);

		FillAt(doc, table, 0, 0).Should().Be("11BB33");
		FillAt(doc, table, 0, 1).Should().Be("FFF000");
		FillAt(doc, table, 0, 2).Should().Be("00AAFF");
	}

	[Fact]
	public void Resolve_WalksBasedOnChain_FindsConditionalInParentStyle()
	{
		var parentStyle = TableStyle("ParentTable", null, Conditional(TableStyleOverrideValues.Band1Horizontal, "aabb00"));
		var childStyle = TableStyle("ChildTable", "ParentTable", new StyleRunProperties(new Bold()));

		using var doc = new StyleDocument(new Styles(parentStyle, childStyle));

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
		var baseStyle = TableStyle("BaseGrid", null,
			Conditional(TableStyleOverrideValues.Band1Horizontal, "cc1122"),
			Conditional(TableStyleOverrideValues.FirstRow, "00dd88"));
		var derivedStyle = TableStyle("DerivedGrid", "BaseGrid");

		using var doc = new StyleDocument(new Styles(baseStyle, derivedStyle));
		var table = StyledTwoRowTable("DerivedGrid", new TableLookOptions(ApplyFirstRow: true, ApplyBandedRows: true));

		AssertRowFills(doc, table, "00DD88", "CC1122");
	}

	[Fact]
	public void ResolveCellShading_UsesBand1Fallback_WhenBand2HorizontalIsMissing()
	{
		var style = TableStyle("SingleBandStyle", null, Conditional(TableStyleOverrideValues.Band1Horizontal, "F7CAAC"));
		using var doc = new StyleDocument(new Styles(style));
		var table = GridTable("SingleBandStyle", new TableLookOptions(ApplyFirstRow: true, ApplyBandedRows: true), 3, 1);

		FillAt(doc, table, 1, 0).Should().Be("F7CAAC");
		FillAt(doc, table, 2, 0).Should().Be("F7CAAC");
	}

	[Fact]
	public void Resolve_WalksBasedOnChain_CollectsBaseTcPrFromParentStyle()
	{
		// The parent style carries a whole-table tcPr with a background colour (simulating FBE4D5
		// from GridTable5Dark-Accent2).  The child style has no tcPr of its own.
		// After the fix, Resolve() should inherit the parent's whole-table tcPr.
		var parentStyle = TableStyle("ParentWithTcPr", null, new TableCellProperties(ClearShading("FBE4D5")));
		var childStyle = TableStyle("ChildTable", "ParentWithTcPr", new StyleRunProperties(new Bold()));

		using var doc = new StyleDocument(new Styles(parentStyle, childStyle));

		var result = TableStyleResolver.Resolve(doc.Styles, "ChildTable", null);

		result.Should().NotBeNull();
		result!.TableCellProperties.Should().NotBeNull();
		result.TableCellProperties!.GetFirstChild<Shading>()?.Fill?.Value.Should().Be("FBE4D5");
	}
}
