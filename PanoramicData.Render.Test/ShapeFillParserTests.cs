namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;
using A = DocumentFormat.OpenXml.Drawing;

public sealed class ShapeFillParserTests
{
	private static DrawingShapeRunElement ParseWithFill(A.ShapeTypeValues preset, DocumentFormat.OpenXml.OpenXmlElement fill)
		=> ShapeDrawingBuilder.ParseInline<DrawingShapeRunElement>(
			ShapeDrawingBuilder.Props(ShapeDrawingBuilder.Preset(preset), fill));

	private static A.RgbColorModelHex Rgb(string hex) => new() { Val = hex };

	[Fact]
	public void Parse_InlinePresetShapeWithSolidFill_ParsesSolidColor()
	{
		var shape = ParseWithFill(A.ShapeTypeValues.Rectangle, new A.SolidFill(Rgb("FF0000")));

		shape.Fill.Kind.Should().Be(ShapeFillKind.Solid);
		shape.Fill.SolidColorHex.Should().Be("FF0000");
	}

	[Fact]
	public void Parse_InlinePresetShapeWithGradientFill_ParsesStops()
	{
		var shape = ParseWithFill(
			A.ShapeTypeValues.Ellipse,
			new A.GradientFill(
				new A.GradientStopList(
					new A.GradientStop { Position = 0, RgbColorModelHex = Rgb("000000") },
					new A.GradientStop { Position = 100000, RgbColorModelHex = Rgb("FFFFFF") }),
				new A.LinearGradientFill { Angle = 0, Scaled = true }));

		shape.Fill.Kind.Should().Be(ShapeFillKind.Gradient);
		shape.Fill.GradientStyle.Should().Be("linear");
		shape.Fill.GradientStops.Should().HaveCount(2);
		shape.Fill.GradientStops[0].ColorHex.Should().Be("000000");
		shape.Fill.GradientStops[1].ColorHex.Should().Be("FFFFFF");
	}

	[Fact]
	public void Parse_InlinePresetShapeWithPatternFill_ParsesPatternColors()
	{
		var shape = ParseWithFill(
			A.ShapeTypeValues.Diamond,
			new A.PatternFill
			{
				Preset = A.PresetPatternValues.Percent20,
				ForegroundColor = new A.ForegroundColor(Rgb("112233")),
				BackgroundColor = new A.BackgroundColor(Rgb("445566"))
			});

		shape.Fill.Kind.Should().Be(ShapeFillKind.Pattern);
		shape.Fill.PatternForegroundColorHex.Should().Be("112233");
		shape.Fill.PatternBackgroundColorHex.Should().Be("445566");
		shape.Fill.PatternPreset.Should().NotBeNullOrEmpty();
	}

	[Fact]
	public void Parse_InlinePresetShapeWithPictureFill_ParsesRelationshipId()
	{
		var shape = ParseWithFill(A.ShapeTypeValues.RoundRectangle, new A.BlipFill(new A.Blip { Embed = "rIdImage1" }));

		shape.Fill.Kind.Should().Be(ShapeFillKind.Picture);
		shape.Fill.PictureRelationshipId.Should().Be("rIdImage1");
	}
}
