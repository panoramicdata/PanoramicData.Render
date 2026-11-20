namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public class DocumentDefaultsParserTests
{
	[Fact]
	public void Parse_WithNullStylesPart_ReturnsEmptyDefaults()
	{
		var defaults = DocumentDefaultsParser.Parse(null);

		defaults.ParagraphProperties.Should().NotBeNull();
		defaults.RunProperties.Should().NotBeNull();
		AssertEmpty(defaults);
	}

	[Fact]
	public void Parse_WithStylesPartWithoutStyles_ReturnsEmptyDefaults()
		=> AssertEmpty(ParseStream(TestDocxBuilder.CreateDocxWithStylesPartWithoutStyles()));

	[Fact]
	public void Parse_WithStylesWithoutDocDefaults_ReturnsEmptyDefaults()
		=> AssertEmpty(ParseStream(TestDocxBuilder.CreateDocxWithStylesWithoutDocDefaults()));

	[Fact]
	public void Parse_WithParagraphDefaults_ParsesParagraphProperties()
	{
		var defaults = ParseDocDefaults(
			new ParagraphPropertiesBaseStyle(
				new Justification { Val = JustificationValues.Center },
				new SpacingBetweenLines { Before = "120", After = "240" }),
			null);

		defaults.ParagraphProperties.GetFirstChild<Justification>()?.Val?.Value
			.Should().Be(JustificationValues.Center);
		defaults.ParagraphProperties.GetFirstChild<SpacingBetweenLines>()?.Before?.Value
			.Should().Be("120");
		defaults.ParagraphProperties.GetFirstChild<SpacingBetweenLines>()?.After?.Value
			.Should().Be("240");
		defaults.RunProperties.ChildElements.Should().BeEmpty();
	}

	[Fact]
	public void Parse_WithRunDefaults_ParsesRunProperties()
	{
		var defaults = ParseDocDefaults(
			null,
			new RunPropertiesBaseStyle(
				new Bold(),
				new Italic(),
				new Color { Val = "FF0000" },
				new FontSize { Val = "24" }));

		defaults.RunProperties.GetFirstChild<Bold>().Should().NotBeNull();
		defaults.RunProperties.GetFirstChild<Italic>().Should().NotBeNull();
		defaults.RunProperties.GetFirstChild<Color>()?.Val?.Value.Should().Be("FF0000");
		defaults.RunProperties.GetFirstChild<FontSize>()?.Val?.Value.Should().Be("24");
		defaults.ParagraphProperties.ChildElements.Should().BeEmpty();
	}

	[Fact]
	public void Parse_WithBothDefaults_ParsesBoth()
	{
		var defaults = ParseDocDefaults(
			new ParagraphPropertiesBaseStyle(new Justification { Val = JustificationValues.Both }),
			new RunPropertiesBaseStyle(new Color { Val = "00AA00" }));

		defaults.ParagraphProperties.GetFirstChild<Justification>()?.Val?.Value
			.Should().Be(JustificationValues.Both);
		defaults.RunProperties.GetFirstChild<Color>()?.Val?.Value.Should().Be("00AA00");
	}

	[Fact]
	public void Parse_ReturnedPropertiesAreCloned()
	{
		using var stream = TestDocxBuilder.CreateDocxWithDocDefaults(
			new ParagraphPropertiesBaseStyle(new Justification { Val = JustificationValues.Left }),
			new RunPropertiesBaseStyle(new Color { Val = "112233" }));
		using var doc = DocxDocument.Load(stream);
		var sourceDocDefaults = doc.StylesPart!.Styles!.DocDefaults!;
		var sourceParagraph = sourceDocDefaults.GetFirstChild<ParagraphPropertiesDefault>()!
			.GetFirstChild<ParagraphPropertiesBaseStyle>()!;
		var sourceRun = sourceDocDefaults.GetFirstChild<RunPropertiesDefault>()!
			.GetFirstChild<RunPropertiesBaseStyle>()!;

		var defaults = DocumentDefaultsParser.Parse(doc.StylesPart);

		ReferenceEquals(defaults.ParagraphProperties, sourceParagraph).Should().BeFalse();
		ReferenceEquals(defaults.RunProperties, sourceRun).Should().BeFalse();
	}

	private static void AssertEmpty(DocumentDefaults defaults)
	{
		defaults.ParagraphProperties.ChildElements.Should().BeEmpty();
		defaults.RunProperties.ChildElements.Should().BeEmpty();
	}

	private static DocumentDefaults ParseDocDefaults(
		ParagraphPropertiesBaseStyle? paragraphDefaults,
		RunPropertiesBaseStyle? runDefaults)
		=> ParseStream(TestDocxBuilder.CreateDocxWithDocDefaults(paragraphDefaults, runDefaults));

	private static DocumentDefaults ParseStream(Stream stream)
	{
		using (stream)
		{
			using var doc = DocxDocument.Load(stream);
			return DocumentDefaultsParser.Parse(doc.StylesPart);
		}
	}
}
