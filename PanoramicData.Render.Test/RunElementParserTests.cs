namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;

public sealed partial class RunElementParserTests
{
	private static T Single<T>(Run run)
		where T : RunElement
		=> RunElementParser.Parse(run).Should().ContainSingle().Which.Should().BeOfType<T>().Subject;

	private static T Single<T>(params OpenXmlElement[] runChildren)
		where T : RunElement
		=> Single<T>(new Run(runChildren));

	private static void AssertCrop(ImageRunElement image, int left, int top, int right, int bottom)
	{
		image.CropLeft.Should().Be(left);
		image.CropTop.Should().Be(top);
		image.CropRight.Should().Be(right);
		image.CropBottom.Should().Be(bottom);
	}

	[Fact]
	public void Parse_SimpleTextRun_ReturnsTextElement()
	{
		Single<TextRunElement>(new Text("Hello")).Text.Should().Be("Hello");
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

	[Theory]
	[InlineData("line", "Line")]
	[InlineData("page", "Page")]
	[InlineData("column", "Column")]
	[InlineData("textWrapping", "Line")]
	public void Parse_Break_ReturnsBreakElementOfExpectedType(string breakKind, string expectedType)
	{
		var lineBreak = breakKind switch
		{
			"page" => new Break { Type = BreakValues.Page },
			"column" => new Break { Type = BreakValues.Column },
			"textWrapping" => new Break { Type = BreakValues.TextWrapping },
			_ => new Break()
		};

		Single<BreakRunElement>(lineBreak).BreakType.ToString().Should().Be(expectedType);
	}

	[Fact]
	public void Parse_TabCharacter_ReturnsTabElement()
	{
		Single<TabRunElement>(new TabChar()).Should().NotBeNull();
	}

	[Theory]
	[InlineData(false, "rId1", 914400, 457200)]
	[InlineData(true, "rIdAnchor", 111, 222)]
	public void Parse_Drawing_ReturnsImageElementWithoutCrop(bool anchored, string relationshipId, long widthEmu, long heightEmu)
	{
		var image = ParseImage(anchored, relationshipId, widthEmu, heightEmu);

		image.RelationshipId.Should().Be(relationshipId);
		image.WidthEmu.Should().Be(widthEmu);
		image.HeightEmu.Should().Be(heightEmu);
		AssertCrop(image, 0, 0, 0, 0);
	}

	[Theory]
	[InlineData(false, 5000, 10000, 15000, 20000)]
	[InlineData(true, 123, 456, 789, 321)]
	public void Parse_DrawingWithSourceRect_ParsesCropValues(bool anchored, int left, int top, int right, int bottom)
	{
		var image = ParseImage(anchored, "rId1", 914400, 457200, (left, top, right, bottom));

		AssertCrop(image, left, top, right, bottom);
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
		RunElementParser.Parse(new Run()).Should().BeEmpty();
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
		RunElementParser.ParseRun(new Run(new Text("plain"))).StyleId.Should().BeNull();
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
		RunElementParser.ParseParagraphRuns(new Paragraph()).Should().BeEmpty();
	}

	[Theory]
	[InlineData("run")]
	[InlineData("parseRun")]
	[InlineData("paragraph")]
	public void Parse_NullInput_ThrowsArgumentNullException(string entryPoint)
	{
		Action act = entryPoint switch
		{
			"run" => () => RunElementParser.Parse(null!),
			"parseRun" => () => RunElementParser.ParseRun(null!),
			_ => () => RunElementParser.ParseParagraphRuns(null!)
		};

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
		// Drawing with no inline or anchor child is skipped
		RunElementParser.Parse(new Run(new Drawing())).Should().BeEmpty();
	}

	[Fact]
	public void Parse_InlineDrawingWithoutBlip_ReturnsElementWithEmptyRelId()
	{
		var drawing = new Drawing(new DW.Inline(new DW.Extent { Cx = 100, Cy = 200 }));

		var img = Single<InlineImageRunElement>(drawing);

		img.RelationshipId.Should().BeEmpty();
		img.WidthEmu.Should().Be(100);
		img.HeightEmu.Should().Be(200);
	}

	[Fact]
	public void Parse_AnchorDrawingWithoutBlip_ReturnsElementWithEmptyRelId()
	{
		var img = Single<AnchorImageRunElement>(new Drawing(CreateAnchor(300, 400)));

		img.RelationshipId.Should().BeEmpty();
		img.WidthEmu.Should().Be(300);
		img.HeightEmu.Should().Be(400);
	}

	[Fact]
	public void Parse_AnchorDrawingWithPositionOffsets_ParsesRelativeReferencesAndOffsets()
	{
		var img = ParseAnchor(new AnchorOptions { HorizontalRelativeFrom = DW.HorizontalRelativePositionValues.Column, VerticalRelativeFrom = DW.VerticalRelativePositionValues.Paragraph, HorizontalOffset = "12345", VerticalOffset = "67890" });

		AssertPlacement(img, AnchorRelativeFrom.Column, AnchorRelativeFrom.Paragraph, 12345, 67890);
	}

	[Fact]
	public void Parse_AnchorDrawingWithAlignment_ParsesAlignmentKeywords()
	{
		var img = ParseAnchor(new AnchorOptions { HorizontalAlign = "center", VerticalAlign = "bottom" });

		img.Placement.HorizontalAlignment.Should().Be(AnchorAlignment.Center);
		img.Placement.VerticalAlignment.Should().Be(AnchorAlignment.Bottom);
		img.Placement.HorizontalOffsetEmu.Should().Be(0);
		img.Placement.VerticalOffsetEmu.Should().Be(0);
	}
}
