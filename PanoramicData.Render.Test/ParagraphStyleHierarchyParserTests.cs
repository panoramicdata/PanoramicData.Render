using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

namespace PanoramicData.Render.Test;

public class ParagraphStyleHierarchyParserTests
{
	[Fact]
	public void Parse_WithNullStylesPart_ReturnsEmptyHierarchy()
	{
		var hierarchy = ParagraphStyleHierarchyParser.Parse(null);

		hierarchy.Styles.Should().BeEmpty();
		hierarchy.GetInheritanceChain("Normal").Should().BeEmpty();
	}

	[Fact]
	public void Parse_WithStylesWithoutParagraphStyles_ReturnsEmptyHierarchy()
	{
		var styles = new Styles(
			new Style { Type = StyleValues.Character, StyleId = "Emphasis" },
			new Style { Type = StyleValues.Table, StyleId = "GridTable" });

		using var stream = TestDocxBuilder.CreateDocxWithStyles(styles);
		using var doc = DocxDocument.Load(stream);

		var hierarchy = ParagraphStyleHierarchyParser.Parse(doc.StylesPart);

		hierarchy.Styles.Should().BeEmpty();
	}

	[Fact]
	public void Parse_WithSingleParagraphStyle_ParsesDefinition()
	{
		var style = new Style(
			new Name { Val = "Normal" },
			new StyleParagraphProperties(new Justification { Val = JustificationValues.Both }))
		{
			Type = StyleValues.Paragraph,
			StyleId = "Normal",
			Default = true
		};

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(style));
		using var doc = DocxDocument.Load(stream);

		var hierarchy = ParagraphStyleHierarchyParser.Parse(doc.StylesPart);

		hierarchy.Styles.Should().ContainKey("Normal");
		var parsed = hierarchy.Styles["Normal"];
		parsed.StyleId.Should().Be("Normal");
		parsed.Name.Should().Be("Normal");
		parsed.BasedOnStyleId.Should().BeNull();
		parsed.IsDefault.Should().BeTrue();
		parsed.Properties.GetFirstChild<Justification>()?.Val?.Value.Should().Be(JustificationValues.Both);
		hierarchy.GetInheritanceChain("Normal").Should().Equal("Normal");
	}

	[Fact]
	public void Parse_WithBasedOnChain_ResolvesAncestorsInOrder()
	{
		var baseStyle = new Style(
			new Name { Val = "Base" },
			new StyleParagraphProperties(new SpacingBetweenLines { Before = "120" }))
		{
			Type = StyleValues.Paragraph,
			StyleId = "Base"
		};

		var headingStyle = new Style(
			new Name { Val = "Heading" },
			new BasedOn { Val = "Base" },
			new StyleParagraphProperties(new Justification { Val = JustificationValues.Center }))
		{
			Type = StyleValues.Paragraph,
			StyleId = "Heading"
		};

		var heading2Style = new Style(
			new Name { Val = "Heading2" },
			new BasedOn { Val = "Heading" })
		{
			Type = StyleValues.Paragraph,
			StyleId = "Heading2"
		};

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(baseStyle, headingStyle, heading2Style));
		using var doc = DocxDocument.Load(stream);

		var hierarchy = ParagraphStyleHierarchyParser.Parse(doc.StylesPart);

		hierarchy.GetInheritanceChain("Heading2").Should().Equal("Heading2", "Heading", "Base");
		hierarchy.GetInheritanceChain("Heading").Should().Equal("Heading", "Base");
	}

	[Fact]
	public void Parse_WithMissingBasedOnStyle_StopsChainAtMissingParent()
	{
		var style = new Style(
			new Name { Val = "Custom" },
			new BasedOn { Val = "MissingParent" })
		{
			Type = StyleValues.Paragraph,
			StyleId = "Custom"
		};

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(style));
		using var doc = DocxDocument.Load(stream);

		var hierarchy = ParagraphStyleHierarchyParser.Parse(doc.StylesPart);

		hierarchy.GetInheritanceChain("Custom").Should().Equal("Custom");
	}

	[Fact]
	public void Parse_WithCycle_DoesNotLoopAndReturnsDistinctChain()
	{
		var styleA = new Style(new BasedOn { Val = "StyleB" })
		{
			Type = StyleValues.Paragraph,
			StyleId = "StyleA"
		};

		var styleB = new Style(new BasedOn { Val = "StyleA" })
		{
			Type = StyleValues.Paragraph,
			StyleId = "StyleB"
		};

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(styleA, styleB));
		using var doc = DocxDocument.Load(stream);

		var hierarchy = ParagraphStyleHierarchyParser.Parse(doc.StylesPart);

		hierarchy.GetInheritanceChain("StyleA").Should().Equal("StyleA", "StyleB");
		hierarchy.GetInheritanceChain("StyleB").Should().Equal("StyleB", "StyleA");
	}

	[Fact]
	public void Parse_ClonesStyleParagraphProperties()
	{
		var style = new Style(
			new StyleParagraphProperties(new Justification { Val = JustificationValues.Right }))
		{
			Type = StyleValues.Paragraph,
			StyleId = "CloneCheck"
		};

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(style));
		using var doc = DocxDocument.Load(stream);

		var sourceProperties = doc.StylesPart!.Styles!.Elements<Style>().Single().StyleParagraphProperties!;
		var hierarchy = ParagraphStyleHierarchyParser.Parse(doc.StylesPart);

		ReferenceEquals(sourceProperties, hierarchy.Styles["CloneCheck"].Properties).Should().BeFalse();
	}

	[Fact]
	public void GetInheritanceChain_ForUnknownStyle_ReturnsEmpty()
	{
		var style = new Style { Type = StyleValues.Paragraph, StyleId = "Normal" };

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(style));
		using var doc = DocxDocument.Load(stream);

		var hierarchy = ParagraphStyleHierarchyParser.Parse(doc.StylesPart);

		hierarchy.GetInheritanceChain("MissingStyle").Should().BeEmpty();
	}

	[Fact]
	public void GetInheritanceChain_WithNullOrWhitespaceStyleId_ReturnsEmpty()
	{
		var style = new Style { Type = StyleValues.Paragraph, StyleId = "Normal" };

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(style));
		using var doc = DocxDocument.Load(stream);

		var hierarchy = ParagraphStyleHierarchyParser.Parse(doc.StylesPart);

		hierarchy.GetInheritanceChain(null!).Should().BeEmpty();
		hierarchy.GetInheritanceChain(string.Empty).Should().BeEmpty();
		hierarchy.GetInheritanceChain("   ").Should().BeEmpty();
	}

	[Fact]
	public void GetInheritanceChain_WithMissingOrEmptyStyleId_UsesDefaultStyleWhenPresent()
	{
		var normal = new Style(new Name { Val = "Normal" })
		{
			Type = StyleValues.Paragraph,
			StyleId = "Normal",
			Default = true
		};
		var heading = new Style(new Name { Val = "Heading1" }, new BasedOn { Val = "Normal" })
		{
			Type = StyleValues.Paragraph,
			StyleId = "Heading1"
		};

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(normal, heading));
		using var doc = DocxDocument.Load(stream);

		var hierarchy = ParagraphStyleHierarchyParser.Parse(doc.StylesPart);

		hierarchy.GetInheritanceChain(null!).Should().Equal("Normal");
		hierarchy.GetInheritanceChain(string.Empty).Should().Equal("Normal");
		hierarchy.GetInheritanceChain("MissingStyle").Should().Equal("Normal");
	}
}
