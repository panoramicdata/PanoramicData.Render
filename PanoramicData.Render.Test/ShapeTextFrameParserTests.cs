namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using Xunit;
using A = DocumentFormat.OpenXml.Drawing;

public sealed class ShapeTextFrameParserTests
{
	private static DrawingShapeRunElement ParseRectangle(OpenXmlElement? content)
		=> ShapeDrawingBuilder.ParseInline<DrawingShapeRunElement>(
			ShapeDrawingBuilder.Props(ShapeDrawingBuilder.Preset(A.ShapeTypeValues.Rectangle)),
			content);

	private static OpenXmlUnknownElement TextBody(string bodyPrAttributes, string autoFitMarkup, params string[] paragraphs)
	{
		var paragraphMarkup = string.Concat(paragraphs.Select(text => $"<p><r><t>{text}</t></r></p>"));
		return new OpenXmlUnknownElement("txBody")
		{
			InnerXml = $"<bodyPr {bodyPrAttributes}>{autoFitMarkup}</bodyPr><lstStyle/>{paragraphMarkup}"
		};
	}

	private static OpenXmlUnknownElement TextBox(string innerContent)
		=> new("wps:txbx")
		{
			InnerXml = $"<w:txbxContent xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main'>{innerContent}</w:txbxContent>"
		};

	[Fact]
	public void Parse_InlinePresetShapeWithTextFrame_ParsesTextInsetsAndAutoFit()
	{
		var shape = ParseRectangle(TextBody(
			"lIns='1000' tIns='2000' rIns='3000' bIns='4000'",
			"<normAutofit fontScale='90000' lnSpcReduction='10000'/>",
			"Hello",
			"World"));

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
		var shape = ShapeDrawingBuilder.ParseAnchor<DrawingCustomGeometryRunElement>(
			ShapeDrawingBuilder.Props(ShapeDrawingBuilder.CustomGeometry()),
			"ShapeWithText",
			TextBody("lIns='500' tIns='600' rIns='700' bIns='800'", "<noAutofit/>", "Inside shape"));

		shape.TextFrame.HasTextFrame.Should().BeTrue();
		shape.TextFrame.Text.Should().Be("Inside shape");
		shape.TextFrame.AutoFitMode.Should().Be(ShapeTextAutoFitMode.NoAutoFit);
	}

	[Fact]
	public void Parse_InlinePresetShapeWithShapeAutoFit_ParsesShapeAutoFitMode()
	{
		var shape = ParseRectangle(TextBody("lIns='0' tIns='0' rIns='0' bIns='0'", "<spAutoFit/>", "Auto size me"));

		shape.TextFrame.AutoFitMode.Should().Be(ShapeTextAutoFitMode.ShapeAutoFit);
	}

	[Fact]
	public void Parse_InlinePresetShapeWithWordTextBoxContent_PreservesBlockStructure()
	{
		var shape = ParseRectangle(TextBox(
			"<w:p><w:r><w:t>Before table</w:t></w:r></w:p>"
			+ "<w:tbl><w:tblGrid><w:gridCol w:w='2400'/></w:tblGrid><w:tr><w:tc><w:p><w:r><w:t>Cell</w:t></w:r></w:p></w:tc></w:tr></w:tbl>"
			+ "<w:p><w:r><w:t>After table</w:t></w:r></w:p>"));

		shape.TextFrame.HasTextFrame.Should().BeTrue();
		shape.TextFrame.Text.Should().Be("Before table\nAfter table");
		shape.TextFrame.Blocks.Should().HaveCount(3);
		shape.TextFrame.Blocks[0].Should().BeOfType<ParagraphBlock>();
		shape.TextFrame.Blocks[1].Should().BeOfType<TablePlaceholderBlock>();
		shape.TextFrame.Blocks[2].Should().BeOfType<ParagraphBlock>();
	}

	[Fact]
	public void Parse_PresetShapeWithoutTextFrame_ReturnsNone()
	{
		var shape = ShapeDrawingBuilder.ParseInline<DrawingShapeRunElement>(
			ShapeDrawingBuilder.Props(ShapeDrawingBuilder.Preset(A.ShapeTypeValues.Ellipse)));

		shape.TextFrame.HasTextFrame.Should().BeFalse();
		shape.TextFrame.Text.Should().BeEmpty();
		shape.TextFrame.AutoFitMode.Should().Be(ShapeTextAutoFitMode.None);
	}
}
