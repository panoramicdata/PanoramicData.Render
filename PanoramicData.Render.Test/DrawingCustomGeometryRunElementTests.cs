namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;
using A = DocumentFormat.OpenXml.Drawing;

public sealed class DrawingCustomGeometryRunElementTests
{
	private const string PathList = "<a:pathLst xmlns:a='http://schemas.openxmlformats.org/drawingml/2006/main'><a:path>"
		+ "<a:moveTo><a:pt x='0' y='0'/></a:moveTo>"
		+ "<a:lnTo><a:pt x='10000' y='10000'/></a:lnTo>"
		+ "<a:cubicBezTo><a:pt x='20000' y='30000'/><a:pt x='30000' y='40000'/><a:pt x='40000' y='50000'/></a:cubicBezTo>"
		+ "<a:arcTo wR='20000' hR='30000' stAng='0' swAng='5400000'/>"
		+ "<a:close/>"
		+ "</a:path></a:pathLst>";

	private static A.ShapeProperties CustomShapeProperties()
		=> ShapeDrawingBuilder.Props(ShapeDrawingBuilder.CustomGeometry(PathList));

	private static DrawingCustomGeometryRunElement ParseInline(long widthEmu, long heightEmu)
		=> ShapeDrawingBuilder.ParseInline<DrawingCustomGeometryRunElement>(CustomShapeProperties(), null, widthEmu, heightEmu);

	[Fact]
	public void Parse_InlineDrawingWithCustomGeometry_ReturnsCustomGeometryElement()
	{
		var shape = ParseInline(900000L, 600000L);

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
		var shape = ShapeDrawingBuilder.ParseAnchor<DrawingCustomGeometryRunElement>(CustomShapeProperties(), "CustomShape", null, 800000L, 500000L);

		shape.WidthEmu.Should().Be(800000L);
		shape.HeightEmu.Should().Be(500000L);
		shape.Commands.Should().HaveCount(5);
	}

	[Fact]
	public void Parse_InlineCustomGeometry_ParsesCommandData()
	{
		var shape = ParseInline(900000L, 600000L);

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
}
