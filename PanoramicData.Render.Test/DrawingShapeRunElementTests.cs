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
	// RunElementParser: AlternateContent (mc:Choice / mc:Fallback) unwrapping
	// -------------------------------------------------------------------------

	[Fact]
	public void Parse_AlternateContentChoiceWithAnchorShape_ExtractsShapeFromChoiceBranch()
	{
		var drawing = CreateAnchorShape("rect", 914400L, 457200L);
		var ac = new AlternateContent(new AlternateContentChoice(drawing) { Requires = "wps" }, new AlternateContentFallback());
		var run = new Run(ac);

		var elements = RunElementParser.Parse(run);

		var shape = elements.Should().ContainSingle()
			.Which.Should().BeOfType<DrawingShapeRunElement>()
			.Subject;
		shape.PresetKind.ToString().Should().Be("Rectangle");
		shape.WidthEmu.Should().Be(914400L);
		shape.HeightEmu.Should().Be(457200L);
	}

	[Fact]
	public void Parse_AlternateContentFallbackOnly_ExtractsShapeFromFallbackBranch()
	{
		var drawing = CreateInlineShape("ellipse", 500000L, 300000L);
		var ac = new AlternateContent(new AlternateContentChoice { Requires = "wps" }, new AlternateContentFallback(drawing));
		var run = new Run(ac);

		var elements = RunElementParser.Parse(run);

		var shape = elements.Should().ContainSingle()
			.Which.Should().BeOfType<DrawingShapeRunElement>()
			.Subject;
		shape.PresetKind.ToString().Should().Be("Ellipse");
	}

	[Fact]
	public void Parse_AlternateContentWithShapeInBothBranches_UsesChoiceOnly()
	{
		var ac = new AlternateContent(
			new AlternateContentChoice(CreateAnchorShape("rect", 914400L, 457200L)) { Requires = "wps" },
			new AlternateContentFallback(CreateInlineShape("ellipse", 500000L, 300000L)));
		var run = new Run(ac);

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<DrawingShapeRunElement>()
			.Which.PresetKind.ToString().Should().Be("Rectangle");
	}
	// -------------------------------------------------------------------------
	// Helpers
	// -------------------------------------------------------------------------

	private static Drawing CreateInlineShape(string presetName, long widthEmu, long heightEmu)
	{
		return ShapeDrawingBuilder.CreateInline(PresetShapeProperties(presetName), null, widthEmu, heightEmu);
	}

	private static Drawing CreateAnchorShape(string presetName, long widthEmu, long heightEmu)
	{
		return ShapeDrawingBuilder.CreateAnchor(PresetShapeProperties(presetName), "Shape", null, widthEmu, heightEmu);
	}

	private static Drawing CreateInlineShapeWithTextBody(string presetName, long widthEmu, long heightEmu, IReadOnlyList<string> lines)
	{
		var textBody = new OpenXmlUnknownElement("txBody")
		{
			InnerXml = "<bodyPr/>" + string.Concat(lines.Select(line => $"<p><r><t>{System.Security.SecurityElement.Escape(line)}</t></r></p>"))
		};
		return ShapeDrawingBuilder.CreateInline(PresetShapeProperties(presetName), textBody, widthEmu, heightEmu);
	}

	private static Drawing CreateInlineShapeWithTextBoxContent(string presetName, long widthEmu, long heightEmu, IReadOnlyList<string> lines)
	{
		var textBox = new OpenXmlUnknownElement("wps:txbx")
		{
			InnerXml = "<w:txbxContent xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">"
				+ string.Concat(lines.Select(line => $"<w:p><w:r><w:t>{System.Security.SecurityElement.Escape(line)}</w:t></w:r></w:p>"))
				+ "</w:txbxContent>"
		};
		return ShapeDrawingBuilder.CreateInline(PresetShapeProperties(presetName), textBox, widthEmu, heightEmu);
	}

	private static A.ShapeProperties PresetShapeProperties(string presetName)
	{
		var presetGeom = new A.PresetGeometry();
		presetGeom.SetAttribute(new OpenXmlAttribute("prst", string.Empty, presetName));
		return new A.ShapeProperties(presetGeom);
	}

}
