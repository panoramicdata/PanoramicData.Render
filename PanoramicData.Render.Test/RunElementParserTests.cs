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
		img.CropLeft.Should().Be(0);
		img.CropTop.Should().Be(0);
		img.CropRight.Should().Be(0);
		img.CropBottom.Should().Be(0);
	}

	[Fact]
	public void Parse_InlineDrawingWithSourceRect_ParsesCropValues()
	{
		var drawing = CreateInlineDrawing("rId1", 914400, 457200, leftCrop: 5000, topCrop: 10000, rightCrop: 15000, bottomCrop: 20000);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<InlineImageRunElement>();
		var img = (InlineImageRunElement)elements[0];
		img.CropLeft.Should().Be(5000);
		img.CropTop.Should().Be(10000);
		img.CropRight.Should().Be(15000);
		img.CropBottom.Should().Be(20000);
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
		// Drawing with no inline or anchor child is skipped
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

	[Fact]
	public void Parse_AnchorDrawing_ReturnsAnchorImageElement()
	{
		var drawing = CreateAnchorDrawing("rIdAnchor", 111, 222);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<AnchorImageRunElement>();
		var img = (AnchorImageRunElement)elements[0];
		img.RelationshipId.Should().Be("rIdAnchor");
		img.WidthEmu.Should().Be(111);
		img.HeightEmu.Should().Be(222);
		img.CropLeft.Should().Be(0);
		img.CropTop.Should().Be(0);
		img.CropRight.Should().Be(0);
		img.CropBottom.Should().Be(0);
	}

	[Fact]
	public void Parse_AnchorDrawingWithSourceRect_ParsesCropValues()
	{
		var drawing = CreateAnchorDrawing("rIdAnchor", 111, 222, leftCrop: 123, topCrop: 456, rightCrop: 789, bottomCrop: 321);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<AnchorImageRunElement>();
		var img = (AnchorImageRunElement)elements[0];
		img.CropLeft.Should().Be(123);
		img.CropTop.Should().Be(456);
		img.CropRight.Should().Be(789);
		img.CropBottom.Should().Be(321);
	}

	[Fact]
	public void Parse_AnchorDrawingWithoutBlip_ReturnsElementWithEmptyRelId()
	{
		var anchor = new DW.Anchor(
			new DW.SimplePosition { X = 0, Y = 0 },
			new DW.HorizontalPosition(new DW.PositionOffset("0"))
			{
				RelativeFrom = DW.HorizontalRelativePositionValues.Page
			},
			new DW.VerticalPosition(new DW.PositionOffset("0"))
			{
				RelativeFrom = DW.VerticalRelativePositionValues.Page
			},
			new DW.Extent { Cx = 300, Cy = 400 },
			new DW.EffectExtent(),
			new DW.WrapNone(),
			new DW.DocProperties { Id = 1U, Name = "AnchorImage" },
			new DW.NonVisualGraphicFrameDrawingProperties(),
			new A.Graphic())
		{
			DistanceFromTop = 0U,
			DistanceFromBottom = 0U,
			DistanceFromLeft = 0U,
			DistanceFromRight = 0U,
			SimplePos = false,
			RelativeHeight = 0U,
			BehindDoc = false,
			Locked = false,
			LayoutInCell = true,
			AllowOverlap = true
		};
		var drawing = new Drawing(anchor);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<AnchorImageRunElement>();
		var img = (AnchorImageRunElement)elements[0];
		img.RelationshipId.Should().BeEmpty();
		img.WidthEmu.Should().Be(300);
		img.HeightEmu.Should().Be(400);
	}

	[Fact]
	public void Parse_AnchorDrawingWithPositionOffsets_ParsesRelativeReferencesAndOffsets()
	{
		var anchor = CreateAnchor("rIdAnchor", 100, 200,
			horizontalRelativeFrom: DW.HorizontalRelativePositionValues.Column,
			verticalRelativeFrom: DW.VerticalRelativePositionValues.Paragraph,
			horizontalOffset: "12345",
			verticalOffset: "67890");
		var drawing = new Drawing(anchor);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<AnchorImageRunElement>();
		var img = (AnchorImageRunElement)elements[0];
		img.HorizontalRelativeFrom.Should().Be(AnchorRelativeFrom.Column);
		img.VerticalRelativeFrom.Should().Be(AnchorRelativeFrom.Paragraph);
		img.HorizontalOffsetEmu.Should().Be(12345);
		img.VerticalOffsetEmu.Should().Be(67890);
	}

	[Fact]
	public void Parse_AnchorDrawingWithAlignment_ParsesAlignmentKeywords()
	{
		var anchor = CreateAnchor("rIdAnchor", 100, 200,
			horizontalAlign: "center",
			verticalAlign: "bottom");
		var drawing = new Drawing(anchor);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<AnchorImageRunElement>();
		var img = (AnchorImageRunElement)elements[0];
		img.HorizontalAlignment.Should().Be(AnchorAlignment.Center);
		img.VerticalAlignment.Should().Be(AnchorAlignment.Bottom);
		img.HorizontalOffsetEmu.Should().Be(0);
		img.VerticalOffsetEmu.Should().Be(0);
	}

	[Theory]
	[InlineData("left", "top", 1, 6)]
	[InlineData("right", "center", 3, 2)]
	[InlineData("inside", "outside", 4, 5)]
	public void Parse_AnchorDrawingWithDifferentAlignmentKeywords_ParsesExpectedValues(
		string horizontalAlign,
		string verticalAlign,
		int expectedHorizontal,
		int expectedVertical)
	{
		var anchor = CreateAnchor("rIdAnchor", 100, 200,
			horizontalAlign: horizontalAlign,
			verticalAlign: verticalAlign);
		var drawing = new Drawing(anchor);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<AnchorImageRunElement>();
		var img = (AnchorImageRunElement)elements[0];
		img.HorizontalAlignment.Should().Be((AnchorAlignment)expectedHorizontal);
		img.VerticalAlignment.Should().Be((AnchorAlignment)expectedVertical);
	}

	private static Drawing CreateInlineDrawing(
		string relationshipId,
		long widthEmu,
		long heightEmu,
		int? leftCrop = null,
		int? topCrop = null,
		int? rightCrop = null,
		int? bottomCrop = null)
	{
		var blip = new A.Blip { Embed = relationshipId };
		var blipFill = new A.Pictures.BlipFill(blip);
		if (leftCrop.HasValue || topCrop.HasValue || rightCrop.HasValue || bottomCrop.HasValue)
		{
			blipFill.SourceRectangle = new A.SourceRectangle
			{
				Left = leftCrop,
				Top = topCrop,
				Right = rightCrop,
				Bottom = bottomCrop
			};
		}

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

	private static Drawing CreateAnchorDrawing(
		string relationshipId,
		long widthEmu,
		long heightEmu,
		int? leftCrop = null,
		int? topCrop = null,
		int? rightCrop = null,
		int? bottomCrop = null)
	{
		var blip = new A.Blip { Embed = relationshipId };
		var blipFill = new A.Pictures.BlipFill(blip);
		if (leftCrop.HasValue || topCrop.HasValue || rightCrop.HasValue || bottomCrop.HasValue)
		{
			blipFill.SourceRectangle = new A.SourceRectangle
			{
				Left = leftCrop,
				Top = topCrop,
				Right = rightCrop,
				Bottom = bottomCrop
			};
		}

		var pic = new A.Pictures.Picture(
			new A.Pictures.NonVisualPictureProperties(
				new A.Pictures.NonVisualDrawingProperties { Id = 1, Name = "anchor-test" },
				new A.Pictures.NonVisualPictureDrawingProperties()),
			blipFill,
			new A.Pictures.ShapeProperties());
		var graphicData = new A.GraphicData(pic)
		{
			Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture"
		};
		var graphic = new A.Graphic(graphicData);

		var anchor = CreateAnchor(relationshipId, widthEmu, heightEmu, graphic);

		return new Drawing(anchor);
	}

	private static DW.Anchor CreateAnchor(
		string relationshipId,
		long widthEmu,
		long heightEmu,
		A.Graphic? graphic = null,
		DW.HorizontalRelativePositionValues? horizontalRelativeFrom = null,
		DW.VerticalRelativePositionValues? verticalRelativeFrom = null,
		string horizontalOffset = "0",
		string verticalOffset = "0",
		string? horizontalAlign = null,
		string? verticalAlign = null)
	{
		var horizontalPosition = horizontalAlign is null
			? new DW.HorizontalPosition(new DW.PositionOffset(horizontalOffset))
			: new DW.HorizontalPosition(new DW.HorizontalAlignment(horizontalAlign));
		horizontalPosition.RelativeFrom = horizontalRelativeFrom ?? DW.HorizontalRelativePositionValues.Page;

		var verticalPosition = verticalAlign is null
			? new DW.VerticalPosition(new DW.PositionOffset(verticalOffset))
			: new DW.VerticalPosition(new DW.VerticalAlignment(verticalAlign));
		verticalPosition.RelativeFrom = verticalRelativeFrom ?? DW.VerticalRelativePositionValues.Page;

		var anchor = new DW.Anchor(
			new DW.SimplePosition { X = 0, Y = 0 },
			horizontalPosition,
			verticalPosition,
			new DW.Extent { Cx = widthEmu, Cy = heightEmu },
			new DW.EffectExtent(),
			new DW.WrapNone(),
			new DW.DocProperties { Id = 1U, Name = "AnchorImage" },
			new DW.NonVisualGraphicFrameDrawingProperties(),
			graphic ?? new A.Graphic())
		{
			DistanceFromTop = 0U,
			DistanceFromBottom = 0U,
			DistanceFromLeft = 0U,
			DistanceFromRight = 0U,
			SimplePos = false,
			RelativeHeight = 0U,
			BehindDoc = false,
			Locked = false,
			LayoutInCell = true,
			AllowOverlap = true
		};

		return anchor;
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

	// --- Footnote/Endnote reference tests (step 3.4.1) ---

	[Fact]
	public void Parse_FootnoteReference_ReturnsFootnoteReferenceElement()
	{
		var run = new Run(new FootnoteReference { Id = 1 });

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<FootnoteReferenceRunElement>()
			.Which.FootnoteId.Should().Be(1);
	}

	[Fact]
	public void Parse_FootnoteReference_WithLargeId()
	{
		var run = new Run(new FootnoteReference { Id = 42 });

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<FootnoteReferenceRunElement>()
			.Which.FootnoteId.Should().Be(42);
	}

	[Fact]
	public void Parse_FootnoteReference_NullId_DefaultsToZero()
	{
		var run = new Run(new FootnoteReference());

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<FootnoteReferenceRunElement>()
			.Which.FootnoteId.Should().Be(0);
	}

	[Fact]
	public void Parse_EndnoteReference_ReturnsEndnoteReferenceElement()
	{
		var run = new Run(new EndnoteReference { Id = 1 });

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<EndnoteReferenceRunElement>()
			.Which.EndnoteId.Should().Be(1);
	}

	[Fact]
	public void Parse_EndnoteReference_WithLargeId()
	{
		var run = new Run(new EndnoteReference { Id = 99 });

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<EndnoteReferenceRunElement>()
			.Which.EndnoteId.Should().Be(99);
	}

	[Fact]
	public void Parse_EndnoteReference_NullId_DefaultsToZero()
	{
		var run = new Run(new EndnoteReference());

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<EndnoteReferenceRunElement>()
			.Which.EndnoteId.Should().Be(0);
	}

	[Fact]
	public void Parse_TextWithFootnoteReference_BothParsed()
	{
		var run = new Run(
			new Text("See"),
			new FootnoteReference { Id = 3 });

		var elements = RunElementParser.Parse(run);

		elements.Should().HaveCount(2);
		elements[0].Should().BeOfType<TextRunElement>()
			.Which.Text.Should().Be("See");
		elements[1].Should().BeOfType<FootnoteReferenceRunElement>()
			.Which.FootnoteId.Should().Be(3);
	}

	[Fact]
	public void Parse_TextWithEndnoteReference_BothParsed()
	{
		var run = new Run(
			new Text("See"),
			new EndnoteReference { Id = 5 });

		var elements = RunElementParser.Parse(run);

		elements.Should().HaveCount(2);
		elements[0].Should().BeOfType<TextRunElement>()
			.Which.Text.Should().Be("See");
		elements[1].Should().BeOfType<EndnoteReferenceRunElement>()
			.Which.EndnoteId.Should().Be(5);
	}
}
