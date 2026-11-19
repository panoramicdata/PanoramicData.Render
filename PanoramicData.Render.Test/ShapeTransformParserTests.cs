namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using Xunit;
using A = DocumentFormat.OpenXml.Drawing;

public sealed class ShapeTransformParserTests
{
	private static A.Transform2D Transform(string rotation, string flipH, string flipV)
	{
		var transform = new A.Transform2D();
		transform.SetAttribute(new OpenXmlAttribute("rot", string.Empty, rotation));
		transform.SetAttribute(new OpenXmlAttribute("flipH", string.Empty, flipH));
		transform.SetAttribute(new OpenXmlAttribute("flipV", string.Empty, flipV));
		return transform;
	}

	private static void AssertTransform(ShapeTransformInfo info, int rotation, bool flipH, bool flipV)
	{
		info.HasTransform.Should().BeTrue();
		info.RotationAngle60000.Should().Be(rotation);
		info.FlipHorizontal.Should().Be(flipH);
		info.FlipVertical.Should().Be(flipV);
	}

	[Fact]
	public void Parse_InlinePresetShapeWithTransform_ParsesRotationAndFlips()
	{
		var shape = ShapeDrawingBuilder.ParseInline<DrawingShapeRunElement>(ShapeDrawingBuilder.Props(
			Transform("5400000", "1", "true"),
			ShapeDrawingBuilder.Preset(A.ShapeTypeValues.Rectangle)));

		AssertTransform(shape.Transform, 5400000, true, true);
	}

	[Fact]
	public void Parse_AnchorCustomShapeWithTransform_ParsesRotationAndFlips()
	{
		var shape = ShapeDrawingBuilder.ParseAnchor<DrawingCustomGeometryRunElement>(
			ShapeDrawingBuilder.Props(Transform("2700000", "false", "1"), ShapeDrawingBuilder.CustomGeometry()),
			"ShapeTransform");

		AssertTransform(shape.Transform, 2700000, false, true);
	}

	[Fact]
	public void Parse_PresetShapeWithoutTransform_ReturnsNone()
	{
		var shape = ShapeDrawingBuilder.ParseInline<DrawingShapeRunElement>(
			ShapeDrawingBuilder.Props(ShapeDrawingBuilder.Preset(A.ShapeTypeValues.Ellipse)));

		shape.Transform.HasTransform.Should().BeFalse();
		shape.Transform.RotationAngle60000.Should().Be(0);
		shape.Transform.FlipHorizontal.Should().BeFalse();
		shape.Transform.FlipVertical.Should().BeFalse();
	}
}
