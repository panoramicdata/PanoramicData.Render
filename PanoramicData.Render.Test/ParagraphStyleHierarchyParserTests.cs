using AwesomeAssertions;
using DocumentFormat.OpenXml;
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
		var hierarchy = ParseHierarchy(
			new Style { Type = StyleValues.Character, StyleId = "Emphasis" },
			new Style { Type = StyleValues.Table, StyleId = "GridTable" });

		hierarchy.Styles.Should().BeEmpty();
	}

	[Fact]
	public void Parse_WithSingleParagraphStyle_ParsesDefinition()
	{
		var style = ParagraphStyle("Normal", new Name { Val = "Normal" }, new StyleParagraphProperties(new Justification { Val = JustificationValues.Both }));
		style.Default = true;

		var hierarchy = ParseHierarchy(style);

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
		var hierarchy = ParseHierarchy(
			ParagraphStyle("Base", new Name { Val = "Base" }, new StyleParagraphProperties(new SpacingBetweenLines { Before = "120" })),
			ParagraphStyle("Heading", new Name { Val = "Heading" }, new BasedOn { Val = "Base" }, new StyleParagraphProperties(new Justification { Val = JustificationValues.Center })),
			ParagraphStyle("Heading2", new Name { Val = "Heading2" }, new BasedOn { Val = "Heading" }));

		hierarchy.GetInheritanceChain("Heading2").Should().Equal("Heading2", "Heading", "Base");
		hierarchy.GetInheritanceChain("Heading").Should().Equal("Heading", "Base");
	}

	[Fact]
	public void Parse_WithMissingBasedOnStyle_StopsChainAtMissingParent()
	{
		var hierarchy = ParseHierarchy(ParagraphStyle("Custom", new Name { Val = "Custom" }, new BasedOn { Val = "MissingParent" }));

		hierarchy.GetInheritanceChain("Custom").Should().Equal("Custom");
	}

	[Fact]
	public void Parse_WithCycle_DoesNotLoopAndReturnsDistinctChain()
	{
		var hierarchy = ParseHierarchy(
			ParagraphStyle("StyleA", new BasedOn { Val = "StyleB" }),
			ParagraphStyle("StyleB", new BasedOn { Val = "StyleA" }));

		hierarchy.GetInheritanceChain("StyleA").Should().Equal("StyleA", "StyleB");
		hierarchy.GetInheritanceChain("StyleB").Should().Equal("StyleB", "StyleA");
	}

	[Fact]
	public void Parse_ClonesStyleParagraphProperties()
	{
		var style = ParagraphStyle("CloneCheck", new StyleParagraphProperties(new Justification { Val = JustificationValues.Right }));

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(style));
		using var doc = DocxDocument.Load(stream);

		var sourceProperties = doc.StylesPart!.Styles!.Elements<Style>().Single().StyleParagraphProperties!;
		var hierarchy = ParagraphStyleHierarchyParser.Parse(doc.StylesPart);

		ReferenceEquals(sourceProperties, hierarchy.Styles["CloneCheck"].Properties).Should().BeFalse();
	}

	[Fact]
	public void GetInheritanceChain_ForUnknownStyle_ReturnsEmpty()
	{
		ParseHierarchy(ParagraphStyle("Normal")).GetInheritanceChain("MissingStyle").Should().BeEmpty();
	}

	[Fact]
	public void GetInheritanceChain_WithNullOrWhitespaceStyleId_ReturnsEmpty()
	{
		var hierarchy = ParseHierarchy(ParagraphStyle("Normal"));

		hierarchy.GetInheritanceChain(null!).Should().BeEmpty();
		hierarchy.GetInheritanceChain(string.Empty).Should().BeEmpty();
		hierarchy.GetInheritanceChain("   ").Should().BeEmpty();
	}

	[Fact]
	public void GetInheritanceChain_WithMissingOrEmptyStyleId_UsesDefaultStyleWhenPresent()
	{
		var normal = ParagraphStyle("Normal", new Name { Val = "Normal" });
		normal.Default = true;
		var hierarchy = ParseHierarchy(normal, ParagraphStyle("Heading1", new Name { Val = "Heading1" }, new BasedOn { Val = "Normal" }));

		hierarchy.GetInheritanceChain(null!).Should().Equal("Normal");
		hierarchy.GetInheritanceChain(string.Empty).Should().Equal("Normal");
		hierarchy.GetInheritanceChain("MissingStyle").Should().Equal("Normal");
	}

	private static Style ParagraphStyle(string styleId, params OpenXmlElement[] children)
		=> new(children)
		{
			Type = StyleValues.Paragraph,
			StyleId = styleId
		};

	private static ParagraphStyleHierarchy ParseHierarchy(params Style[] styles)
	{
		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(styles));
		using var doc = DocxDocument.Load(stream);

		return ParagraphStyleHierarchyParser.Parse(doc.StylesPart);
	}
}
