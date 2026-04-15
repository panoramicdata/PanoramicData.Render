namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public class TableStyleResolverTests
{
	[Fact]
	public void Resolve_WithNullStylesPart_ReturnsNull()
	{
		var result = TableStyleResolver.Resolve(null, "TableGrid", null);

		result.Should().BeNull();
	}

	[Fact]
	public void Resolve_WithMissingStyleId_ReturnsNull()
	{
		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles());
		using var doc = DocxDocument.Load(stream);

		var result = TableStyleResolver.Resolve(doc.StylesPart?.Styles, "Missing", null);

		result.Should().BeNull();
	}

	[Fact]
	public void Resolve_WithNonTableStyle_ReturnsNull()
	{
		var styles = new Styles(
			new Style { Type = StyleValues.Paragraph, StyleId = "Normal" });
		using var stream = TestDocxBuilder.CreateDocxWithStyles(styles);
		using var doc = DocxDocument.Load(stream);

		var result = TableStyleResolver.Resolve(doc.StylesPart?.Styles, "Normal", null);

		result.Should().BeNull();
	}

	[Fact]
	public void Resolve_WithBaseTableStyle_ReturnsBaseFragments()
	{
		var style = new Style(
			new StyleTableProperties(new TableJustification { Val = TableRowAlignmentValues.Center }),
			new StyleRunProperties(new Bold()))
		{
			Type = StyleValues.Table,
			StyleId = "FancyTable"
		};

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(style));
		using var doc = DocxDocument.Load(stream);

		var result = TableStyleResolver.Resolve(doc.StylesPart?.Styles, "FancyTable", null);

		result.Should().NotBeNull();
		result!.StyleId.Should().Be("FancyTable");
		result.AppliedConditionals.Should().BeEmpty();
		result.TableProperties.Should().NotBeNull();
		result.TableProperties!.GetFirstChild<TableJustification>()?.Val?.Value
			.Should().Be(TableRowAlignmentValues.Center);
		result.RunProperties.Should().NotBeNull();
		result.RunProperties!.GetFirstChild<Bold>().Should().NotBeNull();
	}

	[Fact]
	public void Resolve_WithFirstRowConditional_AppliesConditionalFragments()
	{
		var style = new Style(
			new StyleTableProperties(new TableJustification { Val = TableRowAlignmentValues.Left }),
			new TableStyleProperties(
				new TableRowProperties(new CantSplit()),
				new TableCellProperties(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }))
			{ Type = TableStyleOverrideValues.FirstRow })
		{
			Type = StyleValues.Table,
			StyleId = "ConditionalTable"
		};

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(style));
		using var doc = DocxDocument.Load(stream);

		var result = TableStyleResolver.Resolve(doc.StylesPart?.Styles, "ConditionalTable", [TableStyleOverrideValues.FirstRow]);

		result.Should().NotBeNull();
		result!.AppliedConditionals.Should().Equal(TableStyleOverrideValues.FirstRow);
		result.TableRowProperties!.GetFirstChild<CantSplit>().Should().NotBeNull();
		result.TableCellProperties!.GetFirstChild<TableCellVerticalAlignment>()?.Val?.Value
			.Should().Be(TableVerticalAlignmentValues.Center);
	}

	[Fact]
	public void Resolve_WithMultipleConditionals_AppliesInProvidedOrder()
	{
		var style = new Style(
			new StyleRunProperties(new Bold()),
			new TableStyleProperties(new RunProperties(new Italic()))
			{ Type = TableStyleOverrideValues.FirstRow },
			new TableStyleProperties(new RunProperties(new Underline { Val = UnderlineValues.Single }))
			{ Type = TableStyleOverrideValues.Band1Horizontal })
		{
			Type = StyleValues.Table,
			StyleId = "OrderTable"
		};

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(style));
		using var doc = DocxDocument.Load(stream);

		var result = TableStyleResolver.Resolve(
			doc.StylesPart?.Styles,
			"OrderTable",
			[TableStyleOverrideValues.FirstRow, TableStyleOverrideValues.Band1Horizontal]);

		result.Should().NotBeNull();
		result!.RunProperties!.GetFirstChild<Underline>()?.Val?.Value.Should().Be(UnderlineValues.Single);
		result.RunProperties!.GetFirstChild<Italic>().Should().BeNull();
	}

	[Fact]
	public void Resolve_WithUnknownConditional_IgnoresConditional()
	{
		var style = new Style(new StyleTableProperties(new TableJustification { Val = TableRowAlignmentValues.Right }))
		{
			Type = StyleValues.Table,
			StyleId = "NoConditionalTable"
		};

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(style));
		using var doc = DocxDocument.Load(stream);

		var result = TableStyleResolver.Resolve(doc.StylesPart?.Styles, "NoConditionalTable", [TableStyleOverrideValues.FirstRow]);

		result.Should().NotBeNull();
		result!.AppliedConditionals.Should().BeEmpty();
		result.TableProperties!.GetFirstChild<TableJustification>()?.Val?.Value
			.Should().Be(TableRowAlignmentValues.Right);
	}

	[Fact]
	public void Resolve_WithConditionalTableAndParagraphProperties_AppliesBothFragments()
	{
		var style = new Style(
			new StyleTableProperties(new TableJustification { Val = TableRowAlignmentValues.Left }),
			new StyleParagraphProperties(new Justification { Val = JustificationValues.Left }),
			new TableStyleProperties(
				new TableProperties(new TableJustification { Val = TableRowAlignmentValues.Center }),
				new ParagraphProperties(new Justification { Val = JustificationValues.Right }))
			{ Type = TableStyleOverrideValues.Band1Vertical })
		{
			Type = StyleValues.Table,
			StyleId = "TblAndPprConditional"
		};

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(style));
		using var doc = DocxDocument.Load(stream);

		var result = TableStyleResolver.Resolve(doc.StylesPart?.Styles, "TblAndPprConditional", [TableStyleOverrideValues.Band1Vertical]);

		result.Should().NotBeNull();
		result!.TableProperties!.GetFirstChild<TableJustification>()?.Val?.Value
			.Should().Be(TableRowAlignmentValues.Center);
		result.ParagraphProperties!.GetFirstChild<Justification>()?.Val?.Value
			.Should().Be(JustificationValues.Right);
	}

	[Fact]
	public void Resolve_ClonesBaseFragments()
	{
		var style = new Style(new StyleTableProperties(new TableJustification { Val = TableRowAlignmentValues.Center }))
		{
			Type = StyleValues.Table,
			StyleId = "CloneTable"
		};

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(style));
		using var doc = DocxDocument.Load(stream);
		var source = doc.StylesPart?.Styles!.Elements<Style>().Single().StyleTableProperties!;

		var result = TableStyleResolver.Resolve(doc.StylesPart?.Styles, "CloneTable", null);

		ReferenceEquals(source, result!.TableProperties).Should().BeFalse();
	}

	[Fact]
	public void Resolve_WithNullOrWhitespaceStyleId_ReturnsNull()
	{
		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles());
		using var doc = DocxDocument.Load(stream);

		TableStyleResolver.Resolve(doc.StylesPart?.Styles, null!, null).Should().BeNull();
		TableStyleResolver.Resolve(doc.StylesPart?.Styles, string.Empty, null).Should().BeNull();
		TableStyleResolver.Resolve(doc.StylesPart?.Styles, " ", null).Should().BeNull();
	}

	[Fact]
	public void ResolveCellShading_FirstRowOverridesBandedRowShading()
	{
		var band1Shading = new Shading { Fill = "ffff00" };
		band1Shading.SetAttribute(new OpenXmlAttribute("w", "val", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "clear"));
		var firstRowShading = new Shading { Fill = "00ff00" };
		firstRowShading.SetAttribute(new OpenXmlAttribute("w", "val", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "clear"));

		var style = new Style(
			new TableStyleProperties(new TableCellProperties(band1Shading)) { Type = TableStyleOverrideValues.Band1Horizontal },
			new TableStyleProperties(new TableCellProperties(firstRowShading)) { Type = TableStyleOverrideValues.FirstRow })
		{
			Type = StyleValues.Table,
			StyleId = "ConditionalShade"
		};

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(style));
		using var doc = DocxDocument.Load(stream);

		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement { Cells = [new TableCellElement { Blocks = [] }] },
				new TableRowElement { Cells = [new TableCellElement { Blocks = [] }] },
			],
			StyleId = "ConditionalShade",
			Look = new TableLookOptions(ApplyFirstRow: true, ApplyBandedRows: true),
		};

		var firstRow = TableStyleResolver.ResolveCellShading(doc.StylesPart?.Styles, table, 0, 0, 1, 1, 2, 1);
		var secondRow = TableStyleResolver.ResolveCellShading(doc.StylesPart?.Styles, table, 1, 0, 1, 1, 2, 1);

		firstRow.FillColor.Should().Be("00FF00");
		secondRow.FillColor.Should().Be("FFFF00");
	}

	[Fact]
	public void ResolveCellShading_BandedColumnsExcludeFirstColumnAndAlternate()
	{
		var band1Shading = new Shading { Fill = "fff000" };
		band1Shading.SetAttribute(new OpenXmlAttribute("w", "val", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "clear"));
		var band2Shading = new Shading { Fill = "00aaff" };
		band2Shading.SetAttribute(new OpenXmlAttribute("w", "val", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "clear"));
		var firstColumnShading = new Shading { Fill = "11bb33" };
		firstColumnShading.SetAttribute(new OpenXmlAttribute("w", "val", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "clear"));

		var style = new Style(
			new TableStyleProperties(new TableCellProperties(band1Shading)) { Type = TableStyleOverrideValues.Band1Vertical },
			new TableStyleProperties(new TableCellProperties(band2Shading)) { Type = TableStyleOverrideValues.Band2Vertical },
			new TableStyleProperties(new TableCellProperties(firstColumnShading)) { Type = TableStyleOverrideValues.FirstColumn })
		{
			Type = StyleValues.Table,
			StyleId = "ColumnBands"
		};

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(style));
		using var doc = DocxDocument.Load(stream);

		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1000f), new TableGridColumn(1000f)],
			Rows = [new TableRowElement { Cells = [new TableCellElement { Blocks = [] }, new TableCellElement { Blocks = [] }, new TableCellElement { Blocks = [] }] }],
			StyleId = "ColumnBands",
			Look = new TableLookOptions(ApplyFirstColumn: true, ApplyBandedColumns: true),
		};

		TableStyleResolver.ResolveCellShading(doc.StylesPart?.Styles, table, 0, 0, 1, 1, 1, 3).FillColor.Should().Be("11BB33");
		TableStyleResolver.ResolveCellShading(doc.StylesPart?.Styles, table, 0, 1, 1, 1, 1, 3).FillColor.Should().Be("FFF000");
		TableStyleResolver.ResolveCellShading(doc.StylesPart?.Styles, table, 0, 2, 1, 1, 1, 3).FillColor.Should().Be("00AAFF");
	}

	[Fact]
	public void Resolve_WalksBasedOnChain_FindsConditionalInParentStyle()
	{
		var bandShading = new Shading { Fill = "aabb00" };
		bandShading.SetAttribute(new OpenXmlAttribute("w", "val", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "clear"));

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

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(parentStyle, childStyle));
		using var doc = DocxDocument.Load(stream);

		var result = TableStyleResolver.Resolve(
			doc.StylesPart?.Styles,
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
		var band1Shading = new Shading { Fill = "cc1122" };
		band1Shading.SetAttribute(new OpenXmlAttribute("w", "val", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "clear"));
		var firstRowShading = new Shading { Fill = "00dd88" };
		firstRowShading.SetAttribute(new OpenXmlAttribute("w", "val", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "clear"));

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

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(baseStyle, derivedStyle));
		using var doc = DocxDocument.Load(stream);

		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement { Cells = [new TableCellElement { Blocks = [] }] },
				new TableRowElement { Cells = [new TableCellElement { Blocks = [] }] },
			],
			StyleId = "DerivedGrid",
			Look = new TableLookOptions(ApplyFirstRow: true, ApplyBandedRows: true),
		};

		var firstRow = TableStyleResolver.ResolveCellShading(doc.StylesPart?.Styles, table, 0, 0, 1, 1, 2, 1);
		var secondRow = TableStyleResolver.ResolveCellShading(doc.StylesPart?.Styles, table, 1, 0, 1, 1, 2, 1);

		firstRow.FillColor.Should().Be("00DD88");
		secondRow.FillColor.Should().Be("CC1122");
	}

	[Fact]
	public void ResolveCellShading_UsesBand1Fallback_WhenBand2HorizontalIsMissing()
	{
		var band1Shading = new Shading { Fill = "F7CAAC" };
		band1Shading.SetAttribute(new OpenXmlAttribute("w", "val", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "clear"));

		var style = new Style(
			new TableStyleProperties(new TableCellProperties(band1Shading))
			{ Type = TableStyleOverrideValues.Band1Horizontal })
		{
			Type = StyleValues.Table,
			StyleId = "SingleBandStyle"
		};

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(style));
		using var doc = DocxDocument.Load(stream);

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

		var secondRow = TableStyleResolver.ResolveCellShading(doc.StylesPart?.Styles, table, 1, 0, 1, 1, 3, 1);
		var thirdRow = TableStyleResolver.ResolveCellShading(doc.StylesPart?.Styles, table, 2, 0, 1, 1, 3, 1);

		secondRow.FillColor.Should().Be("F7CAAC");
		thirdRow.FillColor.Should().Be("F7CAAC");
	}

	[Fact]
	public void Resolve_WalksBasedOnChain_CollectsBaseTcPrFromParentStyle()
	{
		// The parent style carries a whole-table tcPr with a background colour (simulating FBE4D5
		// from GridTable5Dark-Accent2).  The child style has no tcPr of its own.
		// After the fix, Resolve() should inherit the parent's whole-table tcPr.
		var baseShading = new Shading { Fill = "FBE4D5" };
		baseShading.SetAttribute(new OpenXmlAttribute("w", "val", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "clear"));

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

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(parentStyle, childStyle));
		using var doc = DocxDocument.Load(stream);

		var result = TableStyleResolver.Resolve(doc.StylesPart?.Styles, "ChildTable", null);

		result.Should().NotBeNull();
		result!.TableCellProperties.Should().NotBeNull();
		result.TableCellProperties!.GetFirstChild<Shading>()?.Fill?.Value.Should().Be("FBE4D5");
	}

	[Fact]
	public void ResolveCellShading_Band2Row_UsesBaseShading_WhenStyleChainHasBaseTcPr()
	{
		// When the style chain provides a base whole-table tcPr (e.g. FBE4D5 background) and there is
		// no explicit Band2 conditional, band2 rows should return the base shading — not the Band1
		// fallback.  This produces true alternation: band1→F7CAAC, band2→FBE4D5.
		var baseShading = new Shading { Fill = "FBE4D5" };
		baseShading.SetAttribute(new OpenXmlAttribute("w", "val", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "clear"));
		var band1Shading = new Shading { Fill = "F7CAAC" };
		band1Shading.SetAttribute(new OpenXmlAttribute("w", "val", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "clear"));

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

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(parentStyle, derivedStyle));
		using var doc = DocxDocument.Load(stream);

		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement { Cells = [new TableCellElement { Blocks = [] }] },
				new TableRowElement { Cells = [new TableCellElement { Blocks = [] }] },
			],
			StyleId = "DerivedBandStyle",
			Look = new TableLookOptions(ApplyBandedRows: true),
		};

		// Row 0 → Band1Horizontal → F7CAAC
		// Row 1 → Band2Horizontal → no explicit Band2, but base tcPr has FBE4D5 (not Band1 fallback)
		var band1Row = TableStyleResolver.ResolveCellShading(doc.StylesPart?.Styles, table, 0, 0, 1, 1, 2, 1);
		var band2Row = TableStyleResolver.ResolveCellShading(doc.StylesPart?.Styles, table, 1, 0, 1, 1, 2, 1);

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

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(style));
		using var doc = DocxDocument.Load(stream);

		var margins = TableStyleResolver.ResolveDefaultCellMargins(doc.StylesPart?.Styles, "NoMarginTable");

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

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(parentStyle, childStyle));
		using var doc = DocxDocument.Load(stream);

		var margins = TableStyleResolver.ResolveDefaultCellMargins(doc.StylesPart?.Styles, "ChildNoMargins");

		margins.Left.Should().Be(72f);
		margins.Right.Should().Be(72f);
	}
}
