namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;

public sealed class ShapeOutlineParserTests
{
	[Fact]
	public void Parse_InlinePresetShapeWithOutline_ParsesWidthColorDashAndJoin()
	{
		var shapeProperties = new A.ShapeProperties(new A.PresetGeometry { Preset = A.ShapeTypeValues.Rectangle });
		shapeProperties.Append(CreateOutlineElement("12700", "00AA11", "dash", "round"));

		var drawing = CreateInlineShapeDrawing(shapeProperties);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);
		var shape = (DrawingShapeRunElement)elements[0];

		shape.Outline.HasOutline.Should().BeTrue();
		shape.Outline.WidthEmu.Should().Be(12700);
		shape.Outline.ColorHex.Should().Be("00AA11");
		shape.Outline.DashStyle.Should().Be("dash");
		shape.Outline.JoinStyle.Should().Be(ShapeLineJoinKind.Round);
	}

	[Fact]
	public void Parse_CustomGeometryWithOutline_ParsesBevelJoin()
	{
		var shapeProperties = new A.ShapeProperties();
		shapeProperties.Append(CreateCustomGeometryElement());
		shapeProperties.Append(CreateOutlineElement("6350", "112233", "dot", "bevel"));

		var drawing = CreateInlineShapeDrawing(shapeProperties);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);
		var shape = (DrawingCustomGeometryRunElement)elements[0];

		shape.Outline.HasOutline.Should().BeTrue();
		shape.Outline.WidthEmu.Should().Be(6350);
		shape.Outline.ColorHex.Should().Be("112233");
		shape.Outline.DashStyle.Should().Be("dot");
		shape.Outline.JoinStyle.Should().Be(ShapeLineJoinKind.Bevel);
	}

	[Fact]
	public void Parse_InlineShapeWithoutOutline_HasNoOutline()
	{
		var shapeProperties = new A.ShapeProperties(new A.PresetGeometry { Preset = A.ShapeTypeValues.Ellipse });
		var drawing = CreateInlineShapeDrawing(shapeProperties);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);
		var shape = (DrawingShapeRunElement)elements[0];

		shape.Outline.HasOutline.Should().BeFalse();
		shape.Outline.WidthEmu.Should().Be(0);
		shape.Outline.JoinStyle.Should().Be(ShapeLineJoinKind.None);
	}

	private static OpenXmlUnknownElement CreateOutlineElement(string width, string colorHex, string dash, string join)
	{
		var joinXml = join switch
		{
			"round" => "<a:round xmlns:a='http://schemas.openxmlformats.org/drawingml/2006/main'/>",
			"bevel" => "<a:bevel xmlns:a='http://schemas.openxmlformats.org/drawingml/2006/main'/>",
			_ => "<a:miter xmlns:a='http://schemas.openxmlformats.org/drawingml/2006/main' lim='800000'/>"
		};

		var line = new OpenXmlUnknownElement("a:ln")
		{
			InnerXml = "<a:solidFill xmlns:a='http://schemas.openxmlformats.org/drawingml/2006/main'><a:srgbClr val='" + colorHex + "'/></a:solidFill>"
				+ "<a:prstDash xmlns:a='http://schemas.openxmlformats.org/drawingml/2006/main' val='" + dash + "'/>"
				+ joinXml
		};
		line.SetAttribute(new OpenXmlAttribute("w", string.Empty, width));
		return line;
	}

	private static OpenXmlUnknownElement CreateCustomGeometryElement()
	{
		const string innerXml = "<a:pathLst xmlns:a='http://schemas.openxmlformats.org/drawingml/2006/main'><a:path>"
			+ "<a:moveTo><a:pt x='0' y='0'/></a:moveTo>"
			+ "<a:close/>"
			+ "</a:path></a:pathLst>";

		var customGeometry = new OpenXmlUnknownElement("a:custGeom")
		{
			InnerXml = innerXml
		};
		return customGeometry;
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
}
