namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;

public sealed partial class RunElementParserTests
{
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
		var anchor = CreateAnchor(100, 200, options: new AnchorOptions { HorizontalAlign = horizontalAlign, VerticalAlign = verticalAlign });
		var drawing = new Drawing(anchor);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<AnchorImageRunElement>();
		var img = (AnchorImageRunElement)elements[0];
		img.HorizontalAlignment.Should().Be((AnchorAlignment)expectedHorizontal);
		img.VerticalAlignment.Should().Be((AnchorAlignment)expectedVertical);
	}

	[Fact]
	public void Parse_AnchorDrawingWithNegativeOffsets_PreservesAbsoluteOffsetValues()
	{
		var anchor = CreateAnchor(100, 200, options: new AnchorOptions { HorizontalRelativeFrom = DW.HorizontalRelativePositionValues.Margin, VerticalRelativeFrom = DW.VerticalRelativePositionValues.Margin, HorizontalOffset = "-91440", VerticalOffset = "182880" });
		var drawing = new Drawing(anchor);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<AnchorImageRunElement>();
		var img = (AnchorImageRunElement)elements[0];
		img.HorizontalRelativeFrom.Should().Be(AnchorRelativeFrom.Margin);
		img.VerticalRelativeFrom.Should().Be(AnchorRelativeFrom.Margin);
		img.HorizontalOffsetEmu.Should().Be(-91440);
		img.VerticalOffsetEmu.Should().Be(182880);
	}

	[Fact]
	public void Parse_AnchorDrawingBehindDocument_ParsesZOrderFlag()
	{
		var anchor = CreateAnchor(100, 200, options: new AnchorOptions { BehindDocument = true });
		var drawing = new Drawing(anchor);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<AnchorImageRunElement>();
		var img = (AnchorImageRunElement)elements[0];
		img.BehindDocument.Should().BeTrue();
	}

	[Fact]
	public void Parse_AnchorDrawingDefaultZOrder_IsInFrontOfText()
	{
		var drawing = CreateAnchorDrawing("rIdAnchor", 100, 200);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<AnchorImageRunElement>();
		var img = (AnchorImageRunElement)elements[0];
		img.BehindDocument.Should().BeFalse();
	}

	[Fact]
	public void Parse_AnchorPresetShape_PreservesAnchorPlacementAndWrapMetadata()
	{
		var anchor = CreateAnchor(
			widthEmu: 300,
			heightEmu: 400,
			graphic: CreatePresetShapeGraphic("rect"), options: new AnchorOptions { HorizontalRelativeFrom = DW.HorizontalRelativePositionValues.Margin, VerticalRelativeFrom = DW.VerticalRelativePositionValues.Paragraph, HorizontalAlign = "center", VerticalOffset = "12700", BehindDocument = true, WrapElement = new DW.WrapSquare(), DistanceFromTop = 6350U, DistanceFromBottom = 12700U, DistanceFromLeft = 19050U, DistanceFromRight = 25400U });
		var run = new Run(new Drawing(anchor));

		var elements = RunElementParser.Parse(run);

		var shape = elements.Should().ContainSingle()
			.Which.Should().BeOfType<DrawingShapeRunElement>()
			.Subject;
		shape.AnchorPlacement.HorizontalRelativeFrom.Should().Be(AnchorRelativeFrom.Margin);
		shape.AnchorPlacement.VerticalRelativeFrom.Should().Be(AnchorRelativeFrom.Paragraph);
		shape.AnchorPlacement.HorizontalAlignment.Should().Be(AnchorAlignment.Center);
		shape.AnchorPlacement.VerticalOffsetEmu.Should().Be(12700);
		shape.AnchorPlacement.BehindDocument.Should().BeTrue();
		shape.AnchorPlacement.WrapStyle.Should().Be(AnchorWrapStyle.Square);
		shape.AnchorPlacement.DistanceTopEmu.Should().Be(6350);
		shape.AnchorPlacement.DistanceBottomEmu.Should().Be(12700);
		shape.AnchorPlacement.DistanceLeftEmu.Should().Be(19050);
		shape.AnchorPlacement.DistanceRightEmu.Should().Be(25400);
	}

	[Fact]
	public void Parse_AnchorCustomGeometry_PreservesAnchorPlacement()
	{
		var anchor = CreateAnchor(
			widthEmu: 500,
			heightEmu: 600,
			graphic: CreateCustomGeometryGraphic(), options: new AnchorOptions { HorizontalRelativeFrom = DW.HorizontalRelativePositionValues.Page, VerticalRelativeFrom = DW.VerticalRelativePositionValues.Margin, HorizontalOffset = "6350", VerticalAlign = "bottom", WrapElement = new DW.WrapTopBottom() });
		var run = new Run(new Drawing(anchor));

		var elements = RunElementParser.Parse(run);

		var shape = elements.Should().ContainSingle()
			.Which.Should().BeOfType<DrawingCustomGeometryRunElement>()
			.Subject;
		shape.AnchorPlacement.HorizontalRelativeFrom.Should().Be(AnchorRelativeFrom.Page);
		shape.AnchorPlacement.VerticalRelativeFrom.Should().Be(AnchorRelativeFrom.Margin);
		shape.AnchorPlacement.HorizontalOffsetEmu.Should().Be(6350);
		shape.AnchorPlacement.VerticalAlignment.Should().Be(AnchorAlignment.Bottom);
		shape.AnchorPlacement.WrapStyle.Should().Be(AnchorWrapStyle.TopAndBottom);
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

		var anchor = CreateAnchor(widthEmu, heightEmu, graphic);

		return new Drawing(anchor);
	}

	private sealed record AnchorOptions
	{
		public DW.HorizontalRelativePositionValues? HorizontalRelativeFrom { get; init; }

		public DW.VerticalRelativePositionValues? VerticalRelativeFrom { get; init; }

		public string HorizontalOffset { get; init; } = "0";

		public string VerticalOffset { get; init; } = "0";

		public string? HorizontalAlign { get; init; }

		public string? VerticalAlign { get; init; }

		public bool BehindDocument { get; init; }

		public OpenXmlElement? WrapElement { get; init; }

		public uint DistanceFromTop { get; init; }

		public uint DistanceFromBottom { get; init; }

		public uint DistanceFromLeft { get; init; }

		public uint DistanceFromRight { get; init; }
	}

	private static DW.Anchor CreateAnchor(long widthEmu, long heightEmu, A.Graphic? graphic = null, AnchorOptions? options = null)
	{
			options ??= new AnchorOptions();
	var horizontalPosition = options.HorizontalAlign is null
			? new DW.HorizontalPosition(new DW.PositionOffset(options.HorizontalOffset))
			: new DW.HorizontalPosition(new DW.HorizontalAlignment(options.HorizontalAlign));
		horizontalPosition.RelativeFrom = options.HorizontalRelativeFrom ?? DW.HorizontalRelativePositionValues.Page;

		var verticalPosition = options.VerticalAlign is null
			? new DW.VerticalPosition(new DW.PositionOffset(options.VerticalOffset))
			: new DW.VerticalPosition(new DW.VerticalAlignment(options.VerticalAlign));
		verticalPosition.RelativeFrom = options.VerticalRelativeFrom ?? DW.VerticalRelativePositionValues.Page;

		var anchor = new DW.Anchor(
			new DW.SimplePosition { X = 0, Y = 0 },
			horizontalPosition,
			verticalPosition,
			new DW.Extent { Cx = widthEmu, Cy = heightEmu },
			new DW.EffectExtent(),
			options.WrapElement ?? new DW.WrapNone(),
			new DW.DocProperties { Id = 1U, Name = "AnchorImage" },
			new DW.NonVisualGraphicFrameDrawingProperties(),
			graphic ?? new A.Graphic())
		{
			DistanceFromTop = options.DistanceFromTop,
			DistanceFromBottom = options.DistanceFromBottom,
			DistanceFromLeft = options.DistanceFromLeft,
			DistanceFromRight = options.DistanceFromRight,
			SimplePos = false,
			RelativeHeight = 0U,
			BehindDoc = options.BehindDocument,
			Locked = false,
			LayoutInCell = true,
			AllowOverlap = true
		};

		return anchor;
	}

	private static A.Graphic CreatePresetShapeGraphic(string presetName)
	{
		var presetGeometry = new A.PresetGeometry();
		presetGeometry.SetAttribute(new OpenXmlAttribute("prst", string.Empty, presetName));
		var shapeProperties = new A.ShapeProperties(presetGeometry);
		return new A.Graphic(
			new A.GraphicData(shapeProperties)
			{
				Uri = "http://schemas.openxmlformats.org/drawingml/2006/main"
			});
	}

	private static A.Graphic CreateCustomGeometryGraphic()
	{
		var customGeometry = new OpenXmlUnknownElement("a:custGeom")
		{
			InnerXml = "<a:pathLst xmlns:a='http://schemas.openxmlformats.org/drawingml/2006/main'><a:path><a:moveTo><a:pt x='0' y='0'/></a:moveTo><a:lnTo><a:pt x='1' y='1'/></a:lnTo><a:close/></a:path></a:pathLst>"
		};
		var shapeProperties = new A.ShapeProperties(customGeometry);
		return new A.Graphic(
			new A.GraphicData(shapeProperties)
			{
				Uri = "http://schemas.openxmlformats.org/drawingml/2006/main"
			});
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
}
