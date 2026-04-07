namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;

public sealed class RunElementParserTests
{
	[Fact]
	public void Parse_SimpleTextRun_ReturnsTextElement()
	{
		var run = new Run(new Text("Hello"));

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<TextRunElement>()
			.Which.Text.Should().Be("Hello");
	}

	[Fact]
	public void Parse_MultipleTextNodes_ReturnsSeparateElements()
	{
		var run = new Run(
			new Text("Hello ") { Space = SpaceProcessingModeValues.Preserve },
			new Text("World"));

		var elements = RunElementParser.Parse(run);

		elements.Should().HaveCount(2);
		elements[0].Should().BeOfType<TextRunElement>()
			.Which.Text.Should().Be("Hello ");
		elements[1].Should().BeOfType<TextRunElement>()
			.Which.Text.Should().Be("World");
	}

	[Fact]
	public void Parse_LineBreak_ReturnsBreakElement()
	{
		var run = new Run(new Break());

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<BreakRunElement>()
			.Which.BreakType.Should().Be(RunBreakType.Line);
	}

	[Fact]
	public void Parse_PageBreak_ReturnsPageBreakElement()
	{
		var run = new Run(new Break { Type = BreakValues.Page });

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<BreakRunElement>()
			.Which.BreakType.Should().Be(RunBreakType.Page);
	}

	[Fact]
	public void Parse_ColumnBreak_ReturnsColumnBreakElement()
	{
		var run = new Run(new Break { Type = BreakValues.Column });

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<BreakRunElement>()
			.Which.BreakType.Should().Be(RunBreakType.Column);
	}

	[Fact]
	public void Parse_TabCharacter_ReturnsTabElement()
	{
		var run = new Run(new TabChar());

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<TabRunElement>();
	}

	[Fact]
	public void Parse_InlineDrawing_ReturnsInlineImageElement()
	{
		var drawing = CreateInlineDrawing("rId1", 914400, 457200);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<InlineImageRunElement>();
		var img = (InlineImageRunElement)elements[0];
		img.RelationshipId.Should().Be("rId1");
		img.WidthEmu.Should().Be(914400);
		img.HeightEmu.Should().Be(457200);
	}

	[Fact]
	public void Parse_MixedContent_PreservesOrder()
	{
		var run = new Run(
			new Text("Before"),
			new Break(),
			new Text("After"));

		var elements = RunElementParser.Parse(run);

		elements.Should().HaveCount(3);
		elements[0].Should().BeOfType<TextRunElement>();
		elements[1].Should().BeOfType<BreakRunElement>();
		elements[2].Should().BeOfType<TextRunElement>();
	}

	[Fact]
	public void Parse_EmptyRun_ReturnsEmptyList()
	{
		var run = new Run();

		var elements = RunElementParser.Parse(run);

		elements.Should().BeEmpty();
	}

	[Fact]
	public void Parse_RunWithStyleId_CapturesStyleId()
	{
		var run = new Run(
			new RunProperties(new RunStyle { Val = "Emphasis" }),
			new Text("styled"));

		var result = RunElementParser.ParseRun(run);

		result.StyleId.Should().Be("Emphasis");
		result.Elements.Should().ContainSingle();
	}

	[Fact]
	public void Parse_RunWithoutStyle_HasNullStyleId()
	{
		var run = new Run(new Text("plain"));

		var result = RunElementParser.ParseRun(run);

		result.StyleId.Should().BeNull();
	}

	[Fact]
	public void ParseParagraphRuns_MultiplRuns_ReturnsAll()
	{
		var paragraph = new Paragraph(
			new Run(new Text("First")),
			new Run(new Text(" ")),
			new Run(new Text("Second")));

		var runs = RunElementParser.ParseParagraphRuns(paragraph);

		runs.Should().HaveCount(3);
		runs[0].Elements.Should().ContainSingle()
			.Which.Should().BeOfType<TextRunElement>()
			.Which.Text.Should().Be("First");
	}

	[Fact]
	public void ParseParagraphRuns_NoRuns_ReturnsEmptyList()
	{
		var paragraph = new Paragraph();

		var runs = RunElementParser.ParseParagraphRuns(paragraph);

		runs.Should().BeEmpty();
	}

	[Fact]
	public void Parse_TextWrappingBreak_TreatedAsLineBreak()
	{
		var run = new Run(new Break { Type = BreakValues.TextWrapping });

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<BreakRunElement>()
			.Which.BreakType.Should().Be(RunBreakType.Line);
	}

	[Fact]
	public void Parse_NullRun_ThrowsArgumentNullException()
	{
		Action act = () => RunElementParser.Parse(null!);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void ParseRun_NullRun_ThrowsArgumentNullException()
	{
		Action act = () => RunElementParser.ParseRun(null!);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void ParseParagraphRuns_NullParagraph_ThrowsArgumentNullException()
	{
		Action act = () => RunElementParser.ParseParagraphRuns(null!);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void Parse_UnrecognizedElement_IsIgnored()
	{
		// FieldChar is a run child that we don't handle yet
		var run = new Run(
			new Text("Before"),
			new FieldChar { FieldCharType = FieldCharValues.Begin },
			new Text("After"));

		var elements = RunElementParser.Parse(run);

		elements.Should().HaveCount(2);
		elements[0].Should().BeOfType<TextRunElement>()
			.Which.Text.Should().Be("Before");
		elements[1].Should().BeOfType<TextRunElement>()
			.Which.Text.Should().Be("After");
	}

	[Fact]
	public void Parse_DrawingWithoutInline_IsIgnored()
	{
		// Drawing with no Inline child (e.g. anchor-only) is skipped
		var drawing = new Drawing();
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);

		elements.Should().BeEmpty();
	}

	[Fact]
	public void Parse_InlineDrawingWithoutBlip_ReturnsElementWithEmptyRelId()
	{
		// Inline drawing that has no blip (no embedded image)
		var inline = new DW.Inline(
			new DW.Extent { Cx = 100, Cy = 200 });
		var drawing = new Drawing(inline);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<InlineImageRunElement>();
		var img = (InlineImageRunElement)elements[0];
		img.RelationshipId.Should().BeEmpty();
		img.WidthEmu.Should().Be(100);
		img.HeightEmu.Should().Be(200);
	}

	private static Drawing CreateInlineDrawing(string relationshipId, long widthEmu, long heightEmu)
	{
		var blip = new A.Blip { Embed = relationshipId };
		var blipFill = new A.Pictures.BlipFill(blip);
		var pic = new A.Pictures.Picture(
			new A.Pictures.NonVisualPictureProperties(
				new A.Pictures.NonVisualDrawingProperties { Id = 1, Name = "test" },
				new A.Pictures.NonVisualPictureDrawingProperties()),
			blipFill,
			new A.Pictures.ShapeProperties());
		var graphicData = new A.GraphicData(pic)
		{
			Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture"
		};
		var graphic = new A.Graphic(graphicData);
		var inline = new DW.Inline(
			new DW.Extent { Cx = widthEmu, Cy = heightEmu },
			graphic)
		{
			DistanceFromTop = 0,
			DistanceFromBottom = 0,
			DistanceFromLeft = 0,
			DistanceFromRight = 0
		};
		return new Drawing(inline);
	}

	[Fact]
	public void Parse_NoBreakHyphen_ReturnsNonBreakingHyphenElement()
	{
		var run = new Run(new NoBreakHyphen());

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<NonBreakingHyphenRunElement>();
	}

	[Fact]
	public void Parse_NoBreakHyphenBetweenText_PreservesOrder()
	{
		var run = new Run(
			new Text("well"),
			new NoBreakHyphen(),
			new Text("known"));

		var elements = RunElementParser.Parse(run);

		elements.Should().HaveCount(3);
		elements[0].Should().BeOfType<TextRunElement>()
			.Which.Text.Should().Be("well");
		elements[1].Should().BeOfType<NonBreakingHyphenRunElement>();
		elements[2].Should().BeOfType<TextRunElement>()
			.Which.Text.Should().Be("known");
	}
}
