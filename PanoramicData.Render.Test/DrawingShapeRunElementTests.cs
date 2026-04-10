namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;

public sealed class DrawingShapeRunElementTests
{
	// -------------------------------------------------------------------------
	// PresetGeometryParser: known names → expected kind name
	// (Uses string params to avoid CS0051 accessibility error for internal enum.)
	// -------------------------------------------------------------------------

	[Theory]
	[InlineData("rect", "Rectangle")]
	[InlineData("roundRect", "RoundedRectangle")]
	[InlineData("ellipse", "Ellipse")]
	[InlineData("triangle", "Triangle")]
	[InlineData("rtTriangle", "RightTriangle")]
	[InlineData("diamond", "Diamond")]
	[InlineData("parallelogram", "Parallelogram")]
	[InlineData("trapezoid", "Trapezoid")]
	[InlineData("pentagon", "Pentagon")]
	[InlineData("hexagon", "Hexagon")]
	[InlineData("octagon", "Octagon")]
	[InlineData("cross", "Cross")]
	[InlineData("foldedCorner", "FoldedCorner")]
	[InlineData("star4", "Star4")]
	[InlineData("star5", "Star5")]
	[InlineData("rightArrow", "RightArrow")]
	[InlineData("leftArrow", "LeftArrow")]
	[InlineData("upArrow", "UpArrow")]
	[InlineData("downArrow", "DownArrow")]
	[InlineData("wedgeRectCallout", "WedgeRectCallout")]
	[InlineData("wedgeEllipseCallout", "WedgeEllipseCallout")]
	[InlineData("line", "Line")]
	public void PresetGeometryParser_KnownName_ReturnsExpectedKind(string rawName, string expectedKindName)
	{
		PresetGeometryParser.Parse(rawName).ToString().Should().Be(expectedKindName);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("unknownShape")]
	[InlineData("RECT")]  // case-sensitive: uppercase is not recognised
	public void PresetGeometryParser_UnknownOrNullName_ReturnsUnknown(string? rawName)
	{
		PresetGeometryParser.Parse(rawName).ToString().Should().Be("Unknown");
	}

	// -------------------------------------------------------------------------
	// RunElementParser: inline shape detection
	// -------------------------------------------------------------------------

	[Fact]
	public void Parse_InlineDrawingWithPresetShape_ReturnsDrawingShapeRunElement()
	{
		var drawing = CreateInlineShape("rect", 914400L, 457200L);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<DrawingShapeRunElement>();
		var shape = (DrawingShapeRunElement)elements[0];
		shape.WidthEmu.Should().Be(914400L);
		shape.HeightEmu.Should().Be(457200L);
		shape.RawPresetName.Should().Be("rect");
		shape.PresetKind.ToString().Should().Be("Rectangle");
	}

	[Fact]
	public void Parse_InlineDrawingWithEllipseShape_ReturnsEllipseKind()
	{
		var drawing = CreateInlineShape("ellipse", 500000L, 300000L);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);

		var shape = elements.Should().ContainSingle()
			.Which.Should().BeOfType<DrawingShapeRunElement>()
			.Subject;
		shape.PresetKind.ToString().Should().Be("Ellipse");
	}

	[Fact]
	public void Parse_InlineDrawingWithUnknownPresetShape_ReturnsUnknownKind()
	{
		var drawing = CreateInlineShape("futureMysticShape", 100L, 100L);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);

		var shape = elements.Should().ContainSingle()
			.Which.Should().BeOfType<DrawingShapeRunElement>()
			.Subject;
		shape.PresetKind.ToString().Should().Be("Unknown");
		shape.RawPresetName.Should().Be("futureMysticShape");
	}

	// -------------------------------------------------------------------------
	// RunElementParser: anchor shape detection
	// -------------------------------------------------------------------------

	[Fact]
	public void Parse_AnchorDrawingWithPresetShape_ReturnsDrawingShapeRunElement()
	{
		var drawing = CreateAnchorShape("triangle", 800000L, 600000L);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);

		var shape = elements.Should().ContainSingle()
			.Which.Should().BeOfType<DrawingShapeRunElement>()
			.Subject;
		shape.PresetKind.ToString().Should().Be("Triangle");
		shape.WidthEmu.Should().Be(800000L);
		shape.HeightEmu.Should().Be(600000L);
	}

	[Fact]
	public void Parse_AnchorDrawingWithDiamondShape_ReturnsDrawingShapeRunElement()
	{
		var drawing = CreateAnchorShape("diamond", 400000L, 300000L);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);

		var shape = elements.Should().ContainSingle()
			.Which.Should().BeOfType<DrawingShapeRunElement>()
			.Subject;
		shape.PresetKind.ToString().Should().Be("Diamond");
	}

	[Fact]
	public void Parse_InlineDrawingWithDrawingMlTextFrame_ExtractsText()
	{
		var drawing = CreateInlineShapeWithTextBody("rect", 914400L, 457200L, ["First line", "Second line"]);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);

		var shape = elements.Should().ContainSingle()
			.Which.Should().BeOfType<DrawingShapeRunElement>()
			.Subject;
		shape.TextFrame.HasTextFrame.Should().BeTrue();
		shape.TextFrame.Text.Should().Be("First line\nSecond line");
	}

	[Fact]
	public void Parse_InlineDrawingWithWordTextBoxContent_ExtractsText()
	{
		var drawing = CreateInlineShapeWithTextBoxContent("rect", 914400L, 457200L, ["Box line one", "Box line two"]);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);

		var shape = elements.Should().ContainSingle()
			.Which.Should().BeOfType<DrawingShapeRunElement>()
			.Subject;
		shape.TextFrame.HasTextFrame.Should().BeTrue();
		shape.TextFrame.Text.Should().Be("Box line one\nBox line two");
	}

	// -------------------------------------------------------------------------
	// Helpers
	// -------------------------------------------------------------------------

	private static Drawing CreateInlineShape(string presetName, long widthEmu, long heightEmu)
	{
		var presetGeom = new A.PresetGeometry();
		presetGeom.SetAttribute(new OpenXmlAttribute("prst", string.Empty, presetName));

		var spPr = new A.ShapeProperties(presetGeom);
		var graphicData = new A.GraphicData(spPr)
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

	private static Drawing CreateAnchorShape(string presetName, long widthEmu, long heightEmu)
	{
		var presetGeom = new A.PresetGeometry();
		presetGeom.SetAttribute(new OpenXmlAttribute("prst", string.Empty, presetName));

		var spPr = new A.ShapeProperties(presetGeom);
		var graphicData = new A.GraphicData(spPr)
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
			new DW.DocProperties { Id = 1U, Name = "Shape" },
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

	private static Drawing CreateInlineShapeWithTextBody(string presetName, long widthEmu, long heightEmu, IReadOnlyList<string> lines)
	{
		var presetGeom = new A.PresetGeometry();
		presetGeom.SetAttribute(new OpenXmlAttribute("prst", string.Empty, presetName));
		var textBody = new OpenXmlUnknownElement("txBody")
		{
			InnerXml = "<bodyPr/>" + string.Concat(lines.Select(line => $"<p><r><t>{System.Security.SecurityElement.Escape(line)}</t></r></p>"))
		};
		var spPr = new A.ShapeProperties(presetGeom);
		var graphicData = new A.GraphicData(spPr, textBody)
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

	private static Drawing CreateInlineShapeWithTextBoxContent(string presetName, long widthEmu, long heightEmu, IReadOnlyList<string> lines)
	{
		var presetGeom = new A.PresetGeometry();
		presetGeom.SetAttribute(new OpenXmlAttribute("prst", string.Empty, presetName));
		var textBox = new OpenXmlUnknownElement("wps:txbx")
		{
			InnerXml = "<w:txbxContent xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">"
				+ string.Concat(lines.Select(line => $"<w:p><w:r><w:t>{System.Security.SecurityElement.Escape(line)}</w:t></w:r></w:p>"))
				+ "</w:txbxContent>"
		};
		var spPr = new A.ShapeProperties(presetGeom);
		var graphicData = new A.GraphicData(spPr, textBox)
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

}
