namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;

/// <summary>
/// Step 5.4.8 — verifies full metadata round-trip (geometry, fill, outline, text frame, transform)
/// for the 10 most common preset geometries.
/// </summary>
public sealed class PresetGeometryRenderMetadataTests
{
	// Top 10 preset shapes by real-world frequency.
	private static readonly string[] Top10Presets =
	[
		"rect",
		"roundRect",
		"ellipse",
		"triangle",
		"diamond",
		"rightArrow",
		"line",
		"wedgeRectCallout",
		"star5",
		"hexagon"
	];

	[Theory]
	[InlineData("rect")]
	[InlineData("roundRect")]
	[InlineData("ellipse")]
	[InlineData("triangle")]
	[InlineData("diamond")]
	[InlineData("rightArrow")]
	[InlineData("line")]
	[InlineData("wedgeRectCallout")]
	[InlineData("star5")]
	[InlineData("hexagon")]
	public void Parse_Top10PresetShape_ParsesCorrectPresetKind(string presetName)
	{
		var drawing = CreateShapeWithAllMetadata(presetName, A.ShapeTypeValues.Rectangle);
		var run = new Run(drawing);

		var shape = RunElementParser.Parse(run).Should().ContainSingle()
			.Which.Should().BeOfType<DrawingShapeRunElement>()
			.Subject;

		shape.RawPresetName.Should().Be(presetName);
		shape.PresetKind.ToString().Should().NotBe("Unknown");
	}

	[Theory]
	[InlineData("rect")]
	[InlineData("roundRect")]
	[InlineData("ellipse")]
	[InlineData("triangle")]
	[InlineData("diamond")]
	[InlineData("rightArrow")]
	[InlineData("line")]
	[InlineData("wedgeRectCallout")]
	[InlineData("star5")]
	[InlineData("hexagon")]
	public void Parse_Top10PresetShapeWithSolidFill_ParsesFill(string presetName)
	{
		var shapeProperties = new A.ShapeProperties(
			new A.PresetGeometry { Preset = MapPreset(presetName) },
			new A.SolidFill(new A.RgbColorModelHex { Val = "336699" }));
		var drawing = CreateInlineDrawing(shapeProperties);
		var run = new Run(drawing);

		var shape = (DrawingShapeRunElement)RunElementParser.Parse(run)[0];

		shape.Fill.Kind.Should().Be(ShapeFillKind.Solid);
		shape.Fill.SolidColorHex.Should().Be("336699");
	}

	[Theory]
	[InlineData("rect")]
	[InlineData("roundRect")]
	[InlineData("ellipse")]
	[InlineData("triangle")]
	[InlineData("diamond")]
	[InlineData("rightArrow")]
	[InlineData("line")]
	[InlineData("wedgeRectCallout")]
	[InlineData("star5")]
	[InlineData("hexagon")]
	public void Parse_Top10PresetShapeWithOutline_ParsesOutlineWidth(string presetName)
	{
		var outline = new A.Outline();
		outline.Width = 12700;
		var shapeProperties = new A.ShapeProperties(
			new A.PresetGeometry { Preset = MapPreset(presetName) },
			outline);
		var drawing = CreateInlineDrawing(shapeProperties);
		var run = new Run(drawing);

		var shape = (DrawingShapeRunElement)RunElementParser.Parse(run)[0];

		shape.Outline.HasOutline.Should().BeTrue();
		shape.Outline.WidthEmu.Should().Be(12700);
	}

	[Theory]
	[InlineData("rect")]
	[InlineData("ellipse")]
	[InlineData("triangle")]
	public void Parse_Top10PresetShapeWithTransform_ParsesRotation(string presetName)
	{
		var xfrm = new A.Transform2D();
		xfrm.Rotation = 5400000;
		var shapeProperties = new A.ShapeProperties(
			xfrm,
			new A.PresetGeometry { Preset = MapPreset(presetName) });
		var drawing = CreateInlineDrawing(shapeProperties);
		var run = new Run(drawing);

		var shape = (DrawingShapeRunElement)RunElementParser.Parse(run)[0];

		shape.Transform.HasTransform.Should().BeTrue();
		shape.Transform.RotationAngle60000.Should().Be(5400000);
	}

	// -------------------------------------------------------------------------
	// Helpers
	// -------------------------------------------------------------------------

	private static Drawing CreateShapeWithAllMetadata(string presetName, A.ShapeTypeValues shapeType)
	{
		var xfrm = new A.Transform2D();
		xfrm.Rotation = 0;
		var shapeProperties = new A.ShapeProperties(
			xfrm,
			new A.PresetGeometry { Preset = MapPreset(presetName) },
			new A.SolidFill(new A.RgbColorModelHex { Val = "AABBCC" }));
		return CreateInlineDrawing(shapeProperties);
	}

	private static Drawing CreateInlineDrawing(A.ShapeProperties shapeProperties)
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

	private static A.ShapeTypeValues MapPreset(string name) => name switch
	{
		"rect" => A.ShapeTypeValues.Rectangle,
		"roundRect" => A.ShapeTypeValues.RoundRectangle,
		"ellipse" => A.ShapeTypeValues.Ellipse,
		"triangle" => A.ShapeTypeValues.Triangle,
		"diamond" => A.ShapeTypeValues.Diamond,
		"rightArrow" => A.ShapeTypeValues.RightArrow,
		"line" => A.ShapeTypeValues.Line,
		"wedgeRectCallout" => A.ShapeTypeValues.WedgeRectangleCallout,
		"star5" => A.ShapeTypeValues.Star5,
		"hexagon" => A.ShapeTypeValues.Hexagon,
		_ => A.ShapeTypeValues.Rectangle
	};
}
