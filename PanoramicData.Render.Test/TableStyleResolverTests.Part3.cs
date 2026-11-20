namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public partial class TableStyleResolverTests
{
	private const string WordNamespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

	[Fact]
	public void ResolveCellShading_Band2Row_UsesBaseShading_WhenStyleChainHasBaseTcPr()
	{
		// When the style chain provides a base whole-table tcPr (e.g. FBE4D5 background) and there is
		// no explicit Band2 conditional, band2 rows should return the base shading, not the Band1
		// fallback.  This produces true alternation: band1 F7CAAC, band2 FBE4D5.
		var parentStyle = TableStyle("BaseGridStyle", null, new TableCellProperties(ClearShading("FBE4D5")));
		var derivedStyle = TableStyle("DerivedBandStyle", "BaseGridStyle", Conditional(TableStyleOverrideValues.Band1Horizontal, "F7CAAC"));

		using var doc = new StyleDocument(new Styles(parentStyle, derivedStyle));
		var table = StyledTwoRowTable("DerivedBandStyle", new TableLookOptions(ApplyBandedRows: true));

		AssertRowFills(doc, table, "F7CAAC", "FBE4D5");
	}

	[Fact]
	public void ResolveDefaultCellMargins_WithNullStyleId_ReturnsWordDefault108TwipMargins()
	{
		AssertWordDefaultMargins(TableStyleResolver.ResolveDefaultCellMargins(null, null));
	}

	[Fact]
	public void ResolveDefaultCellMargins_WithNoMarginsInStyleChain_ReturnsWordDefault108TwipMargins()
	{
		using var doc = new StyleDocument(new Styles(TableStyle("NoMarginTable", null, new StyleTableProperties())));

		AssertWordDefaultMargins(TableStyleResolver.ResolveDefaultCellMargins(doc.Styles, "NoMarginTable"));
	}

	[Fact]
	public void ResolveDefaultCellMargins_WalksStyleChain_ReturnsMarginsDefinedInAncestorStyle()
	{
		// Two-level style chain: child has no tblCellMar, parent defines 72-twip L/R.
		var parentStyle = TableStyle("ParentWithMargins", null,
			new StyleTableProperties(
				new TableCellMarginDefault(
					new TableCellLeftMargin { Width = 72, Type = TableWidthValues.Dxa },
					new TableCellRightMargin { Width = 72, Type = TableWidthValues.Dxa })));
		var childStyle = TableStyle("ChildNoMargins", "ParentWithMargins", new StyleRunProperties(new Bold()));

		using var doc = new StyleDocument(new Styles(parentStyle, childStyle));

		var margins = TableStyleResolver.ResolveDefaultCellMargins(doc.Styles, "ChildNoMargins");

		margins.Left.Should().Be(72f);
		margins.Right.Should().Be(72f);
	}

	private static void AssertWordDefaultMargins(CellMargins margins)
	{
		margins.Left.Should().Be(108f);
		margins.Right.Should().Be(108f);
		margins.Top.Should().Be(0f);
		margins.Bottom.Should().Be(0f);
	}

	private static Style TableStyle(string styleId, string? basedOn, params OpenXmlElement[] children)
	{
		var style = new Style(children)
		{
			Type = StyleValues.Table,
			StyleId = styleId
		};
		if (basedOn is not null)
		{
			style.BasedOn = new BasedOn { Val = basedOn };
		}

		return style;
	}

	private static TableStyleProperties Conditional(TableStyleOverrideValues type, string fill)
		=> new(new TableCellProperties(ClearShading(fill))) { Type = type };

	private static Shading ClearShading(string fill)
	{
		var shading = new Shading { Fill = fill };
		shading.SetAttribute(new OpenXmlAttribute("w", "val", WordNamespace, "clear"));
		return shading;
	}

	private static string? FillAt(StyleDocument doc, TableElement table, int row, int column)
		=> TableStyleResolver.ResolveCellShading(doc.Styles, table, row, column, 1, 1, table.Rows.Count, table.GridColumns.Count).FillColor;

	private static void AssertRowFills(StyleDocument doc, TableElement table, params string[] expectedFills)
	{
		for (var row = 0; row < expectedFills.Length; row++)
		{
			FillAt(doc, table, row, 0).Should().Be(expectedFills[row]);
		}
	}

	private static TableElement GridTable(string styleId, TableLookOptions look, int rows, int columns) => new()
	{
		GridColumns = [.. Enumerable.Repeat(new TableGridColumn(1000f), columns)],
		Rows = [.. Enumerable.Range(0, rows).Select(_ => new TableRowElement { Cells = [.. Enumerable.Range(0, columns).Select(_ => new TableCellElement { Blocks = [] })] })],
		StyleId = styleId,
		Look = look,
	};

	private static TableElement StyledTwoRowTable(string styleId, TableLookOptions look) => GridTable(styleId, look, 2, 1);
}
