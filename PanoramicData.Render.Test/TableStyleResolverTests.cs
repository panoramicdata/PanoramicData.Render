namespace PanoramicData.Render.Test;

using AwesomeAssertions;
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

		var result = TableStyleResolver.Resolve(doc.StylesPart, "Missing", null);

		result.Should().BeNull();
	}

	[Fact]
	public void Resolve_WithNonTableStyle_ReturnsNull()
	{
		var styles = new Styles(
			new Style { Type = StyleValues.Paragraph, StyleId = "Normal" });
		using var stream = TestDocxBuilder.CreateDocxWithStyles(styles);
		using var doc = DocxDocument.Load(stream);

		var result = TableStyleResolver.Resolve(doc.StylesPart, "Normal", null);

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

		var result = TableStyleResolver.Resolve(doc.StylesPart, "FancyTable", null);

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

		var result = TableStyleResolver.Resolve(doc.StylesPart, "ConditionalTable", [TableStyleOverrideValues.FirstRow]);

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
			doc.StylesPart,
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

		var result = TableStyleResolver.Resolve(doc.StylesPart, "NoConditionalTable", [TableStyleOverrideValues.FirstRow]);

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

		var result = TableStyleResolver.Resolve(doc.StylesPart, "TblAndPprConditional", [TableStyleOverrideValues.Band1Vertical]);

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
		var source = doc.StylesPart!.Styles!.Elements<Style>().Single().StyleTableProperties!;

		var result = TableStyleResolver.Resolve(doc.StylesPart, "CloneTable", null);

		ReferenceEquals(source, result!.TableProperties).Should().BeFalse();
	}

	[Fact]
	public void Resolve_WithNullOrWhitespaceStyleId_ReturnsNull()
	{
		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles());
		using var doc = DocxDocument.Load(stream);

		TableStyleResolver.Resolve(doc.StylesPart, null!, null).Should().BeNull();
		TableStyleResolver.Resolve(doc.StylesPart, string.Empty, null).Should().BeNull();
		TableStyleResolver.Resolve(doc.StylesPart, " ", null).Should().BeNull();
	}
}
