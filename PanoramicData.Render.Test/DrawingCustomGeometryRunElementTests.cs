namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;

public sealed class DrawingCustomGeometryRunElementTests
{
	[Fact]
	public void Parse_InlineDrawingWithCustomGeometry_ReturnsCustomGeometryElement()
	{
		var drawing = CreateInlineCustomGeometryDrawing(900000L, 600000L);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);

		var shape = elements.Should().ContainSingle()
			.Which.Should().BeOfType<DrawingCustomGeometryRunElement>()
			.Subject;
		shape.WidthEmu.Should().Be(900000L);
		shape.HeightEmu.Should().Be(600000L);
		shape.Commands.Should().HaveCount(5);
		shape.Commands[0].Kind.Should().Be(CustomGeometryCommandKind.MoveTo);
		shape.Commands[1].Kind.Should().Be(CustomGeometryCommandKind.LineTo);
		shape.Commands[2].Kind.Should().Be(CustomGeometryCommandKind.CubicBezierTo);
		shape.Commands[3].Kind.Should().Be(CustomGeometryCommandKind.ArcTo);
		shape.Commands[4].Kind.Should().Be(CustomGeometryCommandKind.Close);
	}

	[Fact]
	public void Parse_AnchorDrawingWithCustomGeometry_ReturnsCustomGeometryElement()
	{
		var drawing = CreateAnchorCustomGeometryDrawing(800000L, 500000L);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);

		var shape = elements.Should().ContainSingle()
			.Which.Should().BeOfType<DrawingCustomGeometryRunElement>()
			.Subject;
		shape.WidthEmu.Should().Be(800000L);
		shape.HeightEmu.Should().Be(500000L);
		shape.Commands.Should().HaveCount(5);
	}

	[Fact]
	public void Parse_InlineCustomGeometry_ParsesCommandData()
	{
		var drawing = CreateInlineCustomGeometryDrawing(900000L, 600000L);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);
		var shape = (DrawingCustomGeometryRunElement)elements[0];

		shape.Commands[0].Points.Should().ContainSingle();
		shape.Commands[0].Points[0].XEmu.Should().Be(0);
		shape.Commands[0].Points[0].YEmu.Should().Be(0);

		shape.Commands[2].Points.Should().HaveCount(3);
		shape.Commands[2].Points[2].XEmu.Should().Be(40000);
		shape.Commands[2].Points[2].YEmu.Should().Be(50000);

		shape.Commands[3].ArcWidthRadius.Should().Be(20000);
		shape.Commands[3].ArcHeightRadius.Should().Be(30000);
		shape.Commands[3].ArcStartAngle.Should().Be(0);
		shape.Commands[3].ArcSweepAngle.Should().Be(5400000);
	}

	private static Drawing CreateInlineCustomGeometryDrawing(long widthEmu, long heightEmu)
	{
		var customGeometry = CreateCustomGeometryElement();
		var shapeProperties = new A.ShapeProperties();
		shapeProperties.Append(customGeometry);

		var graphicData = new A.GraphicData(shapeProperties)
		{
			Uri = "http://schemas.openxmlformats.org/drawingml/2006/main"
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

	private static Drawing CreateAnchorCustomGeometryDrawing(long widthEmu, long heightEmu)
	{
		var customGeometry = CreateCustomGeometryElement();
		var shapeProperties = new A.ShapeProperties();
		shapeProperties.Append(customGeometry);
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
			new DW.Extent { Cx = widthEmu, Cy = heightEmu },
			new DW.EffectExtent(),
			new DW.WrapNone(),
			new DW.DocProperties { Id = 1U, Name = "CustomShape" },
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
		const string innerXml = "<a:pathLst xmlns:a='http://schemas.openxmlformats.org/drawingml/2006/main'><a:path>"
			+ "<a:moveTo><a:pt x='0' y='0'/></a:moveTo>"
			+ "<a:lnTo><a:pt x='10000' y='10000'/></a:lnTo>"
			+ "<a:cubicBezTo><a:pt x='20000' y='30000'/><a:pt x='30000' y='40000'/><a:pt x='40000' y='50000'/></a:cubicBezTo>"
			+ "<a:arcTo wR='20000' hR='30000' stAng='0' swAng='5400000'/>"
			+ "<a:close/>"
			+ "</a:path></a:pathLst>";

		var customGeometry = new OpenXmlUnknownElement("a:custGeom")
		{
			InnerXml = innerXml
		};
		return customGeometry;
	}
}
