namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using Xunit;
using A = DocumentFormat.OpenXml.Drawing;

public sealed class ShapeOutlineParserTests
{
	private const string Ns = "xmlns:a='http://schemas.openxmlformats.org/drawingml/2006/main'";

	private static OpenXmlUnknownElement Outline(string width, string colorHex, string dash, string join)
	{
		var joinXml = join switch
		{
			"round" => $"<a:round {Ns}/>",
			"bevel" => $"<a:bevel {Ns}/>",
			_ => $"<a:miter {Ns} lim='800000'/>"
		};

		var line = new OpenXmlUnknownElement("a:ln")
		{
			InnerXml = $"<a:solidFill {Ns}><a:srgbClr val='{colorHex}'/></a:solidFill><a:prstDash {Ns} val='{dash}'/>{joinXml}"
		};
		line.SetAttribute(new OpenXmlAttribute("w", string.Empty, width));
		return line;
	}

	private static void AssertOutline(ShapeOutlineInfo outline, long widthEmu, string colorHex, string dash, ShapeLineJoinKind join)
	{
		outline.HasOutline.Should().BeTrue();
		outline.WidthEmu.Should().Be(widthEmu);
		outline.ColorHex.Should().Be(colorHex);
		outline.DashStyle.Should().Be(dash);
		outline.JoinStyle.Should().Be(join);
	}

	[Fact]
	public void Parse_InlinePresetShapeWithOutline_ParsesWidthColorDashAndJoin()
	{
		var shape = ShapeDrawingBuilder.ParseInline<DrawingShapeRunElement>(ShapeDrawingBuilder.Props(
			ShapeDrawingBuilder.Preset(A.ShapeTypeValues.Rectangle),
			Outline("12700", "00AA11", "dash", "round")));

		AssertOutline(shape.Outline, 12700, "00AA11", "dash", ShapeLineJoinKind.Round);
	}

	[Fact]
	public void Parse_CustomGeometryWithOutline_ParsesBevelJoin()
	{
		var shape = ShapeDrawingBuilder.ParseInline<DrawingCustomGeometryRunElement>(ShapeDrawingBuilder.Props(
			ShapeDrawingBuilder.CustomGeometry(),
			Outline("6350", "112233", "dot", "bevel")));

		AssertOutline(shape.Outline, 6350, "112233", "dot", ShapeLineJoinKind.Bevel);
	}

	[Fact]
	public void Parse_InlineShapeWithoutOutline_HasNoOutline()
	{
		var shape = ShapeDrawingBuilder.ParseInline<DrawingShapeRunElement>(
			ShapeDrawingBuilder.Props(ShapeDrawingBuilder.Preset(A.ShapeTypeValues.Ellipse)));

		shape.Outline.HasOutline.Should().BeFalse();
		shape.Outline.WidthEmu.Should().Be(0);
		shape.Outline.JoinStyle.Should().Be(ShapeLineJoinKind.None);
	}
}
