namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public class CharacterStyleHierarchyParserTests
{
	[Fact]
	public void Parse_WithNullStylesPart_ReturnsEmptyHierarchy()
	{
		var hierarchy = CharacterStyleHierarchyParser.Parse(null);

		hierarchy.Styles.Should().BeEmpty();
		hierarchy.GetInheritanceChain("Emphasis").Should().BeEmpty();
	}

	[Fact]
	public void Parse_WithStylesWithoutCharacterStyles_ReturnsEmptyHierarchy()
	{
		var styles = new Styles(
			new Style { Type = StyleValues.Paragraph, StyleId = "Normal" },
			new Style { Type = StyleValues.Table, StyleId = "GridTable" });

		using var stream = TestDocxBuilder.CreateDocxWithStyles(styles);
		using var doc = DocxDocument.Load(stream);

		var hierarchy = CharacterStyleHierarchyParser.Parse(doc.StylesPart);

		hierarchy.Styles.Should().BeEmpty();
	}

	[Fact]
	public void Parse_WithSingleCharacterStyle_ParsesDefinition()
	{
		var style = new Style(
			new Name { Val = "Emphasis" },
			new StyleRunProperties(new Italic(), new Color { Val = "FF0000" }))
		{
			Type = StyleValues.Character,
			StyleId = "Emphasis",
			Default = true
		};

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(style));
		using var doc = DocxDocument.Load(stream);

		var hierarchy = CharacterStyleHierarchyParser.Parse(doc.StylesPart);

		hierarchy.Styles.Should().ContainKey("Emphasis");
		var parsed = hierarchy.Styles["Emphasis"];
		parsed.StyleId.Should().Be("Emphasis");
		parsed.Name.Should().Be("Emphasis");
		parsed.BasedOnStyleId.Should().BeNull();
		parsed.IsDefault.Should().BeTrue();
		parsed.Properties.GetFirstChild<Italic>().Should().NotBeNull();
		parsed.Properties.GetFirstChild<Color>()?.Val?.Value.Should().Be("FF0000");
		hierarchy.GetInheritanceChain("Emphasis").Should().Equal("Emphasis");
	}

	[Fact]
	public void Parse_WithBasedOnChain_ResolvesAncestorsInOrder()
	{
		var baseStyle = new Style(
			new Name { Val = "BaseChar" },
			new StyleRunProperties(new Color { Val = "0000FF" }))
		{
			Type = StyleValues.Character,
			StyleId = "BaseChar"
		};

		var strongStyle = new Style(
			new Name { Val = "Strong" },
			new BasedOn { Val = "BaseChar" },
			new StyleRunProperties(new Bold()))
		{
			Type = StyleValues.Character,
			StyleId = "Strong"
		};

		var strongEmStyle = new Style(
			new Name { Val = "StrongEm" },
			new BasedOn { Val = "Strong" })
		{
			Type = StyleValues.Character,
			StyleId = "StrongEm"
		};

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(baseStyle, strongStyle, strongEmStyle));
		using var doc = DocxDocument.Load(stream);

		var hierarchy = CharacterStyleHierarchyParser.Parse(doc.StylesPart);

		hierarchy.GetInheritanceChain("StrongEm").Should().Equal("StrongEm", "Strong", "BaseChar");
		hierarchy.GetInheritanceChain("Strong").Should().Equal("Strong", "BaseChar");
	}

	[Fact]
	public void Parse_WithMissingBasedOnStyle_StopsChainAtMissingParent()
	{
		var style = new Style(
			new Name { Val = "CustomChar" },
			new BasedOn { Val = "MissingParent" })
		{
			Type = StyleValues.Character,
			StyleId = "CustomChar"
		};

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(style));
		using var doc = DocxDocument.Load(stream);

		var hierarchy = CharacterStyleHierarchyParser.Parse(doc.StylesPart);

		hierarchy.GetInheritanceChain("CustomChar").Should().Equal("CustomChar");
	}

	[Fact]
	public void Parse_WithCycle_DoesNotLoopAndReturnsDistinctChain()
	{
		var styleA = new Style(new BasedOn { Val = "StyleB" })
		{
			Type = StyleValues.Character,
			StyleId = "StyleA"
		};

		var styleB = new Style(new BasedOn { Val = "StyleA" })
		{
			Type = StyleValues.Character,
			StyleId = "StyleB"
		};

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(styleA, styleB));
		using var doc = DocxDocument.Load(stream);

		var hierarchy = CharacterStyleHierarchyParser.Parse(doc.StylesPart);

		hierarchy.GetInheritanceChain("StyleA").Should().Equal("StyleA", "StyleB");
		hierarchy.GetInheritanceChain("StyleB").Should().Equal("StyleB", "StyleA");
	}

	[Fact]
	public void Parse_ClonesStyleRunProperties()
	{
		var style = new Style(
			new StyleRunProperties(new Bold()))
		{
			Type = StyleValues.Character,
			StyleId = "CloneCheck"
		};

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(style));
		using var doc = DocxDocument.Load(stream);

		var sourceProperties = doc.StylesPart!.Styles!.Elements<Style>().Single().StyleRunProperties!;
		var hierarchy = CharacterStyleHierarchyParser.Parse(doc.StylesPart);

		ReferenceEquals(sourceProperties, hierarchy.Styles["CloneCheck"].Properties).Should().BeFalse();
	}

	[Fact]
	public void GetInheritanceChain_ForUnknownOrWhitespaceStyle_ReturnsEmpty()
	{
		var style = new Style { Type = StyleValues.Character, StyleId = "Emphasis" };

		using var stream = TestDocxBuilder.CreateDocxWithStyles(new Styles(style));
		using var doc = DocxDocument.Load(stream);

		var hierarchy = CharacterStyleHierarchyParser.Parse(doc.StylesPart);

		hierarchy.GetInheritanceChain("Missing").Should().BeEmpty();
		hierarchy.GetInheritanceChain(string.Empty).Should().BeEmpty();
		hierarchy.GetInheritanceChain(" ").Should().BeEmpty();
		hierarchy.GetInheritanceChain(null!).Should().BeEmpty();
	}
}
