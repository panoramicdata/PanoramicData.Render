namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;

public sealed class ShapeTransformParserTests
{
	[Fact]
	public void Parse_InlinePresetShapeWithTransform_ParsesRotationAndFlips()
	{
		var transform = new A.Transform2D();
		transform.SetAttribute(new OpenXmlAttribute("rot", string.Empty, "5400000"));
		transform.SetAttribute(new OpenXmlAttribute("flipH", string.Empty, "1"));
		transform.SetAttribute(new OpenXmlAttribute("flipV", string.Empty, "true"));

		var shapeProperties = new A.ShapeProperties(
			transform,
			new A.PresetGeometry { Preset = A.ShapeTypeValues.Rectangle });
		var drawing = CreateInlineShapeDrawing(shapeProperties);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);
		var shape = (DrawingShapeRunElement)elements[0];

		shape.Transform.HasTransform.Should().BeTrue();
		shape.Transform.RotationAngle60000.Should().Be(5400000);
		shape.Transform.FlipHorizontal.Should().BeTrue();
		shape.Transform.FlipVertical.Should().BeTrue();
	}

	[Fact]
	public void Parse_AnchorCustomShapeWithTransform_ParsesRotationAndFlips()
	{
		var transform = new A.Transform2D();
		transform.SetAttribute(new OpenXmlAttribute("rot", string.Empty, "2700000"));
		transform.SetAttribute(new OpenXmlAttribute("flipH", string.Empty, "false"));
		transform.SetAttribute(new OpenXmlAttribute("flipV", string.Empty, "1"));

		var shapeProperties = new A.ShapeProperties(transform);
		shapeProperties.Append(CreateCustomGeometryElement());
		var drawing = CreateAnchorShapeDrawing(shapeProperties);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);
		var shape = (DrawingCustomGeometryRunElement)elements[0];

		shape.Transform.HasTransform.Should().BeTrue();
		shape.Transform.RotationAngle60000.Should().Be(2700000);
		shape.Transform.FlipHorizontal.Should().BeFalse();
		shape.Transform.FlipVertical.Should().BeTrue();
	}

	[Fact]
	public void Parse_PresetShapeWithoutTransform_ReturnsNone()
	{
		var shapeProperties = new A.ShapeProperties(
			new A.PresetGeometry { Preset = A.ShapeTypeValues.Ellipse });
		var drawing = CreateInlineShapeDrawing(shapeProperties);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);
		var shape = (DrawingShapeRunElement)elements[0];

		shape.Transform.HasTransform.Should().BeFalse();
		shape.Transform.RotationAngle60000.Should().Be(0);
		shape.Transform.FlipHorizontal.Should().BeFalse();
		shape.Transform.FlipVertical.Should().BeFalse();
	}

	private static Drawing CreateInlineShapeDrawing(A.ShapeProperties shapeProperties)
	{
		var graphicData = new A.GraphicData(shapeProperties)
		{
			Uri = "http://schemas.openxmlformats.org/drawingml/2006/main"
		};
		var graphic = new A.Graphic(graphicData);
		var inline = new DW.Inline(
			new DW.Extent { Cx = 914400, Cy = 457200 },
			graphic)
		{
			DistanceFromTop = 0,
			DistanceFromBottom = 0,
			DistanceFromLeft = 0,
			DistanceFromRight = 0
		};
		return new Drawing(inline);
	}

	private static Drawing CreateAnchorShapeDrawing(A.ShapeProperties shapeProperties)
	{
		var graphicData = new A.GraphicData(shapeProperties)
		{
			Uri = "http://schemas.openxmlformats.org/drawingml/2006/main"
		};
		var graphic = new A.Graphic(graphicData);
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
			new DW.Extent { Cx = 914400, Cy = 457200 },
			new DW.EffectExtent(),
			new DW.WrapNone(),
			new DW.DocProperties { Id = 1U, Name = "ShapeTransform" },
			new DW.NonVisualGraphicFrameDrawingProperties(),
			graphic)
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
		return new Drawing(anchor);
	}

	private static OpenXmlUnknownElement CreateCustomGeometryElement()
	{
		const string innerXml = "<a:pathLst xmlns:a='http://schemas.openxmlformats.org/drawingml/2006/main'><a:path><a:moveTo><a:pt x='0' y='0'/></a:moveTo><a:close/></a:path></a:pathLst>";
		return new OpenXmlUnknownElement("a:custGeom")
		{
			InnerXml = innerXml
		};
	}
}
