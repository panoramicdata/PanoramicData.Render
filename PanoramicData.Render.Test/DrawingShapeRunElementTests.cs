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
		var shape = ParseInline("rect", 914400L, 457200L);

		shape.WidthEmu.Should().Be(914400L);
		shape.HeightEmu.Should().Be(457200L);
		shape.RawPresetName.Should().Be("rect");
		shape.PresetKind.ToString().Should().Be("Rectangle");
	}

	[Theory]
	[InlineData("ellipse", "Ellipse")]
	[InlineData("futureMysticShape", "Unknown")]
	public void Parse_InlineDrawingWithPresetName_ReturnsExpectedKind(string presetName, string expectedKind)
	{
		var shape = ParseInline(presetName, 500000L, 300000L);

		shape.PresetKind.ToString().Should().Be(expectedKind);
		shape.RawPresetName.Should().Be(presetName);
	}

	// -------------------------------------------------------------------------
	// RunElementParser: anchor shape detection
	// -------------------------------------------------------------------------

	[Theory]
	[InlineData("triangle", "Triangle", 800000L, 600000L)]
	[InlineData("diamond", "Diamond", 400000L, 300000L)]
	public void Parse_AnchorDrawingWithPresetShape_ReturnsDrawingShapeRunElement(string presetName, string expectedKind, long widthEmu, long heightEmu)
	{
		var shape = ShapeDrawingBuilder.ParseAnchor<DrawingShapeRunElement>(PresetShapeProperties(presetName), "Shape", null, widthEmu, heightEmu);

		shape.PresetKind.ToString().Should().Be(expectedKind);
		shape.WidthEmu.Should().Be(widthEmu);
		shape.HeightEmu.Should().Be(heightEmu);
	}

	[Fact]
	public void Parse_InlineDrawingWithDrawingMlTextFrame_ExtractsText()
	{
		var textBody = new OpenXmlUnknownElement("txBody")
		{
			InnerXml = "<bodyPr/>" + LinesMarkup(["First line", "Second line"], "<p><r><t>{0}</t></r></p>")
		};

		var shape = ShapeDrawingBuilder.ParseInline<DrawingShapeRunElement>(PresetShapeProperties("rect"), textBody);

		shape.TextFrame.HasTextFrame.Should().BeTrue();
		shape.TextFrame.Text.Should().Be("First line\nSecond line");
	}

	[Fact]
	public void Parse_InlineDrawingWithWordTextBoxContent_ExtractsText()
	{
		var textBox = new OpenXmlUnknownElement("wps:txbx")
		{
			InnerXml = "<w:txbxContent xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main'>"
				+ LinesMarkup(["Box line one", "Box line two"], "<w:p><w:r><w:t>{0}</w:t></w:r></w:p>")
				+ "</w:txbxContent>"
		};

		var shape = ShapeDrawingBuilder.ParseInline<DrawingShapeRunElement>(PresetShapeProperties("rect"), textBox);

		shape.TextFrame.HasTextFrame.Should().BeTrue();
		shape.TextFrame.Text.Should().Be("Box line one\nBox line two");
	}

	// -------------------------------------------------------------------------
	// RunElementParser: AlternateContent (mc:Choice / mc:Fallback) unwrapping
	// -------------------------------------------------------------------------

	[Fact]
	public void Parse_AlternateContentChoiceWithAnchorShape_ExtractsShapeFromChoiceBranch()
	{
		var shape = ParseAlternateContent(
			new AlternateContentChoice(CreateAnchorShape("rect", 914400L, 457200L)) { Requires = "wps" },
			new AlternateContentFallback());

		shape.PresetKind.ToString().Should().Be("Rectangle");
		shape.WidthEmu.Should().Be(914400L);
		shape.HeightEmu.Should().Be(457200L);
	}

	[Fact]
	public void Parse_AlternateContentFallbackOnly_ExtractsShapeFromFallbackBranch()
	{
		var shape = ParseAlternateContent(
			new AlternateContentChoice { Requires = "wps" },
			new AlternateContentFallback(CreateInlineShape("ellipse", 500000L, 300000L)));

		shape.PresetKind.ToString().Should().Be("Ellipse");
	}

	[Fact]
	public void Parse_AlternateContentWithShapeInBothBranches_UsesChoiceOnly()
	{
		var shape = ParseAlternateContent(
			new AlternateContentChoice(CreateAnchorShape("rect", 914400L, 457200L)) { Requires = "wps" },
			new AlternateContentFallback(CreateInlineShape("ellipse", 500000L, 300000L)));

		shape.PresetKind.ToString().Should().Be("Rectangle");
	}

	// -------------------------------------------------------------------------
	// Helpers
	// -------------------------------------------------------------------------

	private static DrawingShapeRunElement ParseAlternateContent(AlternateContentChoice choice, AlternateContentFallback fallback)
		=> RunElementParser.Parse(new Run(new AlternateContent(choice, fallback)))
			.Should().ContainSingle().Which.Should().BeOfType<DrawingShapeRunElement>().Subject;

	private static DrawingShapeRunElement ParseInline(string presetName, long widthEmu, long heightEmu)
		=> ShapeDrawingBuilder.ParseInline<DrawingShapeRunElement>(PresetShapeProperties(presetName), null, widthEmu, heightEmu);

	private static Drawing CreateInlineShape(string presetName, long widthEmu, long heightEmu)
		=> ShapeDrawingBuilder.CreateInline(PresetShapeProperties(presetName), null, widthEmu, heightEmu);

	private static Drawing CreateAnchorShape(string presetName, long widthEmu, long heightEmu)
		=> ShapeDrawingBuilder.CreateAnchor(PresetShapeProperties(presetName), "Shape", null, widthEmu, heightEmu);

	private static string LinesMarkup(IEnumerable<string> lines, string template)
		=> string.Concat(lines.Select(line => template.Replace("{0}", System.Security.SecurityElement.Escape(line), StringComparison.Ordinal)));

	private static A.ShapeProperties PresetShapeProperties(string presetName)
	{
		var presetGeom = new A.PresetGeometry();
		presetGeom.SetAttribute(new OpenXmlAttribute("prst", string.Empty, presetName));
		return new A.ShapeProperties(presetGeom);
	}
}
