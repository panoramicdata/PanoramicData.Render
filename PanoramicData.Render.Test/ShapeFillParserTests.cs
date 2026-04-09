namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;

public sealed class ShapeFillParserTests
{
	[Fact]
	public void Parse_InlinePresetShapeWithSolidFill_ParsesSolidColor()
	{
		var shapeProperties = new A.ShapeProperties(
			new A.PresetGeometry { Preset = A.ShapeTypeValues.Rectangle },
			new A.SolidFill(new A.RgbColorModelHex { Val = "FF0000" }));
		var drawing = CreateInlineShapeDrawing(shapeProperties);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);

		var shape = elements.Should().ContainSingle()
			.Which.Should().BeOfType<DrawingShapeRunElement>()
			.Subject;
		shape.Fill.Kind.Should().Be(ShapeFillKind.Solid);
		shape.Fill.SolidColorHex.Should().Be("FF0000");
	}

	[Fact]
	public void Parse_InlinePresetShapeWithGradientFill_ParsesStops()
	{
		var gradientFill = new A.GradientFill(
			new A.GradientStopList(
				new A.GradientStop { Position = 0, RgbColorModelHex = new A.RgbColorModelHex { Val = "000000" } },
				new A.GradientStop { Position = 100000, RgbColorModelHex = new A.RgbColorModelHex { Val = "FFFFFF" } }),
			new A.LinearGradientFill { Angle = 0, Scaled = true });

		var shapeProperties = new A.ShapeProperties(
			new A.PresetGeometry { Preset = A.ShapeTypeValues.Ellipse },
			gradientFill);
		var drawing = CreateInlineShapeDrawing(shapeProperties);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);
		var shape = (DrawingShapeRunElement)elements[0];

		shape.Fill.Kind.Should().Be(ShapeFillKind.Gradient);
		shape.Fill.GradientStyle.Should().Be("linear");
		shape.Fill.GradientStops.Should().HaveCount(2);
		shape.Fill.GradientStops[0].ColorHex.Should().Be("000000");
		shape.Fill.GradientStops[1].ColorHex.Should().Be("FFFFFF");
	}

	[Fact]
	public void Parse_InlinePresetShapeWithPatternFill_ParsesPatternColors()
	{
		var patternFill = new A.PatternFill
		{
			Preset = A.PresetPatternValues.Percent20,
			ForegroundColor = new A.ForegroundColor(new A.RgbColorModelHex { Val = "112233" }),
			BackgroundColor = new A.BackgroundColor(new A.RgbColorModelHex { Val = "445566" })
		};
		var shapeProperties = new A.ShapeProperties(
			new A.PresetGeometry { Preset = A.ShapeTypeValues.Diamond },
			patternFill);
		var drawing = CreateInlineShapeDrawing(shapeProperties);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);
		var shape = (DrawingShapeRunElement)elements[0];

		shape.Fill.Kind.Should().Be(ShapeFillKind.Pattern);
		shape.Fill.PatternForegroundColorHex.Should().Be("112233");
		shape.Fill.PatternBackgroundColorHex.Should().Be("445566");
		shape.Fill.PatternPreset.Should().NotBeNullOrEmpty();
	}

	[Fact]
	public void Parse_InlinePresetShapeWithPictureFill_ParsesRelationshipId()
	{
		var blipFill = new A.BlipFill(new A.Blip { Embed = "rIdImage1" });
		var shapeProperties = new A.ShapeProperties(
			new A.PresetGeometry { Preset = A.ShapeTypeValues.RoundRectangle },
			blipFill);
		var drawing = CreateInlineShapeDrawing(shapeProperties);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);
		var shape = (DrawingShapeRunElement)elements[0];

		shape.Fill.Kind.Should().Be(ShapeFillKind.Picture);
		shape.Fill.PictureRelationshipId.Should().Be("rIdImage1");
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
