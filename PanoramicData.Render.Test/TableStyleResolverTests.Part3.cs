namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public partial class TableStyleResolverTests
{

	[Fact]
	public void ResolveCellShading_Band2Row_UsesBaseShading_WhenStyleChainHasBaseTcPr()
	{
		// When the style chain provides a base whole-table tcPr (e.g. FBE4D5 background) and there is
		// no explicit Band2 conditional, band2 rows should return the base shading — not the Band1
		// fallback.  This produces true alternation: band1→F7CAAC, band2→FBE4D5.
		var baseShading = ClearShading("FBE4D5");
		var band1Shading = ClearShading("F7CAAC");

		var parentStyle = new Style(
			new TableCellProperties(baseShading))
		{
			Type = StyleValues.Table,
			StyleId = "BaseGridStyle"
		};

		var derivedStyle = new Style(
			new TableStyleProperties(new TableCellProperties(band1Shading))
			{ Type = TableStyleOverrideValues.Band1Horizontal })
		{
			Type = StyleValues.Table,
			StyleId = "DerivedBandStyle",
			BasedOn = new BasedOn { Val = "BaseGridStyle" }
		};

		using var doc = new StyleDocument(new Styles(parentStyle, derivedStyle));

		var table = StyledTwoRowTable("DerivedBandStyle", new TableLookOptions(ApplyBandedRows: true));

		// Row 0 → Band1Horizontal → F7CAAC
		// Row 1 → Band2Horizontal → no explicit Band2, but base tcPr has FBE4D5 (not Band1 fallback)
		var band1Row = TableStyleResolver.ResolveCellShading(doc.Styles, table, 0, 0, 1, 1, 2, 1);
		var band2Row = TableStyleResolver.ResolveCellShading(doc.Styles, table, 1, 0, 1, 1, 2, 1);

		band1Row.FillColor.Should().Be("F7CAAC");
		band2Row.FillColor.Should().Be("FBE4D5");
	}

	[Fact]
	public void ResolveDefaultCellMargins_WithNullStyleId_ReturnsWordDefault108TwipMargins()
	{
		var margins = TableStyleResolver.ResolveDefaultCellMargins(null, null);

		margins.Left.Should().Be(108f);
		margins.Right.Should().Be(108f);
		margins.Top.Should().Be(0f);
		margins.Bottom.Should().Be(0f);
	}

	[Fact]
	public void ResolveDefaultCellMargins_WithNoMarginsInStyleChain_ReturnsWordDefault108TwipMargins()
	{
		var style = new Style(new StyleTableProperties())
		{
			Type = StyleValues.Table,
			StyleId = "NoMarginTable"
		};

		using var doc = new StyleDocument(new Styles(style));

		var margins = TableStyleResolver.ResolveDefaultCellMargins(doc.Styles, "NoMarginTable");

		margins.Left.Should().Be(108f);
		margins.Right.Should().Be(108f);
		margins.Top.Should().Be(0f);
		margins.Bottom.Should().Be(0f);
	}

	[Fact]
	public void ResolveDefaultCellMargins_WalksStyleChain_ReturnsMarginsDefinedInAncestorStyle()
	{
		// Build a two-level style chain: child has no tblCellMar, parent defines 72-twip L/R.
		var topMargin = new TopMargin();
		topMargin.SetAttribute(new OpenXmlAttribute("w", "w", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "0"));
		topMargin.SetAttribute(new OpenXmlAttribute("w", "type", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "dxa"));
		var bottomMargin = new BottomMargin();
		bottomMargin.SetAttribute(new OpenXmlAttribute("w", "w", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "0"));
		bottomMargin.SetAttribute(new OpenXmlAttribute("w", "type", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "dxa"));

		var parentStyle = new Style(
			new StyleTableProperties(
				new TableCellMarginDefault(
					new TableCellLeftMargin { Width = 72, Type = TableWidthValues.Dxa },
					new TableCellRightMargin { Width = 72, Type = TableWidthValues.Dxa })))
		{
			Type = StyleValues.Table,
			StyleId = "ParentWithMargins"
		};

		var childStyle = new Style(new StyleRunProperties(new Bold()))
		{
			Type = StyleValues.Table,
			StyleId = "ChildNoMargins",
			BasedOn = new BasedOn { Val = "ParentWithMargins" }
		};

		using var doc = new StyleDocument(new Styles(parentStyle, childStyle));

		var margins = TableStyleResolver.ResolveDefaultCellMargins(doc.Styles, "ChildNoMargins");

		margins.Left.Should().Be(72f);
		margins.Right.Should().Be(72f);
	}

	private static Shading ClearShading(string fill)
	{
		var shading = new Shading { Fill = fill };
		shading.SetAttribute(new OpenXmlAttribute("w", "val", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "clear"));
		return shading;
	}

	private static TableElement StyledTwoRowTable(string styleId, TableLookOptions look) => new()
	{
		GridColumns = [new TableGridColumn(1000f)],
		Rows =
		[
			new TableRowElement { Cells = [new TableCellElement { Blocks = [] }] },
			new TableRowElement { Cells = [new TableCellElement { Blocks = [] }] },
		],
		StyleId = styleId,
		Look = look,
	};
}
