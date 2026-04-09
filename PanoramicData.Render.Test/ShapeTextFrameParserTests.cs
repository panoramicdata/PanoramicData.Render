namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;

public sealed class ShapeTextFrameParserTests
{
	[Fact]
	public void Parse_InlinePresetShapeWithTextFrame_ParsesTextInsetsAndAutoFit()
	{
		var shapeProperties = new A.ShapeProperties(
			new A.PresetGeometry { Preset = A.ShapeTypeValues.Rectangle });
		var txBody = CreateTextBodyElement(
			"lIns='1000' tIns='2000' rIns='3000' bIns='4000'",
			"<normAutofit fontScale='90000' lnSpcReduction='10000'/>",
			new[] { "Hello", "World" });
		var drawing = CreateInlineShapeDrawing(shapeProperties, txBody);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);

		var shape = elements.Should().ContainSingle()
			.Which.Should().BeOfType<DrawingShapeRunElement>()
			.Subject;
		shape.TextFrame.HasTextFrame.Should().BeTrue();
		shape.TextFrame.Text.Should().Be("Hello\nWorld");
		shape.TextFrame.LeftInsetEmu.Should().Be(1000);
		shape.TextFrame.TopInsetEmu.Should().Be(2000);
		shape.TextFrame.RightInsetEmu.Should().Be(3000);
		shape.TextFrame.BottomInsetEmu.Should().Be(4000);
		shape.TextFrame.AutoFitMode.Should().Be(ShapeTextAutoFitMode.NormalAutoFit);
	}

	[Fact]
	public void Parse_AnchorCustomShapeWithTextFrame_ParsesNoAutoFit()
	{
		var shapeProperties = new A.ShapeProperties();
		shapeProperties.Append(CreateCustomGeometryElement());
		var txBody = CreateTextBodyElement(
			"lIns='500' tIns='600' rIns='700' bIns='800'",
			"<noAutofit/>",
			new[] { "Inside shape" });
		var drawing = CreateAnchorShapeDrawing(shapeProperties, txBody);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);

		var shape = elements.Should().ContainSingle()
			.Which.Should().BeOfType<DrawingCustomGeometryRunElement>()
			.Subject;
		shape.TextFrame.HasTextFrame.Should().BeTrue();
		shape.TextFrame.Text.Should().Be("Inside shape");
		shape.TextFrame.AutoFitMode.Should().Be(ShapeTextAutoFitMode.NoAutoFit);
	}

	[Fact]
	public void Parse_PresetShapeWithoutTextFrame_ReturnsNone()
	{
		var shapeProperties = new A.ShapeProperties(
			new A.PresetGeometry { Preset = A.ShapeTypeValues.Ellipse });
		var drawing = CreateInlineShapeDrawing(shapeProperties, null);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);
		var shape = (DrawingShapeRunElement)elements[0];

		shape.TextFrame.HasTextFrame.Should().BeFalse();
		shape.TextFrame.Text.Should().BeEmpty();
		shape.TextFrame.AutoFitMode.Should().Be(ShapeTextAutoFitMode.None);
	}

	private static Drawing CreateInlineShapeDrawing(A.ShapeProperties shapeProperties, OpenXmlUnknownElement? txBody)
	{
		var graphicData = new A.GraphicData(shapeProperties)
		{
			Uri = "http://schemas.openxmlformats.org/drawingml/2006/main"
		};
		if (txBody is not null)
		{
			graphicData.Append(txBody);
		}

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

	private static Drawing CreateAnchorShapeDrawing(A.ShapeProperties shapeProperties, OpenXmlUnknownElement? txBody)
	{
		var graphicData = new A.GraphicData(shapeProperties)
		{
			Uri = "http://schemas.openxmlformats.org/drawingml/2006/main"
		};
		if (txBody is not null)
		{
			graphicData.Append(txBody);
		}

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
			new DW.DocProperties { Id = 1U, Name = "ShapeWithText" },
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

	private static OpenXmlUnknownElement CreateTextBodyElement(string bodyPrAttributes, string autoFitMarkup, IEnumerable<string> paragraphs)
	{
		var paragraphMarkup = string.Join(
			string.Empty,
			paragraphs.Select(text => $"<p><r><t>{text}</t></r></p>"));
		var txBody = new OpenXmlUnknownElement("txBody")
		{
			InnerXml = $"<bodyPr {bodyPrAttributes}>{autoFitMarkup}</bodyPr><lstStyle/>{paragraphMarkup}"
		};
		return txBody;
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
