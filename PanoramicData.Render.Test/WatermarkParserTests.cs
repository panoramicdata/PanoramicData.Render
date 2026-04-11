namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Vml;
using Xunit;

public sealed class WatermarkParserTests
{
	[Fact]
	public void TryParseWatermarkShape_TextWatermark_ReturnsTextWatermarkInfo()
	{
		var shape = new Shape
		{
			Id = "PowerPlusWaterMarkObject357922611",
			Type = "#_x0000_t136",
			Style = "position:absolute;width:527.85pt;height:131.95pt;rotation:315;z-index:-251658752;mso-position-horizontal:center;mso-position-vertical:center;mso-position-horizontal-relative:margin;mso-position-vertical-relative:margin",
			FillColor = "silver"
		};
		shape.AppendChild(new Fill { Opacity = ".5" });
		shape.AppendChild(new TextPath
		{
			Style = "font-family:\"Calibri\";font-size:1pt",
			String = "DRAFT"
		});

		var result = WatermarkParser.TryParseWatermarkShape(shape);

		result.Should().NotBeNull();
		result!.Kind.Should().Be(WatermarkKind.Text);
		result.Text.Should().Be("DRAFT");
		result.FontFamily.Should().Be("Calibri");
		result.FillColor.Should().Be("silver");
		result.Opacity.Should().BeApproximately(0.5f, 0.01f);
		result.RotationDegrees.Should().Be(315f);
		result.WidthTwips.Should().BeApproximately(527.85f * 20f, 0.1f);
		result.HeightTwips.Should().BeApproximately(131.95f * 20f, 0.1f);
		result.IsHorizontallyCentered.Should().BeTrue();
		result.IsVerticallyCentered.Should().BeTrue();
	}

	[Fact]
	public void TryParseWatermarkShape_ImageWatermark_ReturnsImageWatermarkInfo()
	{
		var shape = new Shape
		{
			Id = "PowerPlusWaterMarkObject12345",
			Type = "#_x0000_t75",
			Style = "position:absolute;width:400pt;height:300pt;rotation:0;mso-position-horizontal:center;mso-position-vertical:center"
		};
		shape.AppendChild(new DocumentFormat.OpenXml.Vml.ImageData
		{
			RelationshipId = "rId1"
		});

		var result = WatermarkParser.TryParseWatermarkShape(shape);

		result.Should().NotBeNull();
		result!.Kind.Should().Be(WatermarkKind.Image);
		result.ImageRelationshipId.Should().Be("rId1");
		result.WidthTwips.Should().BeApproximately(400f * 20f, 0.1f);
		result.HeightTwips.Should().BeApproximately(300f * 20f, 0.1f);
		result.IsHorizontallyCentered.Should().BeTrue();
		result.IsVerticallyCentered.Should().BeTrue();
	}

	[Fact]
	public void TryParseWatermarkShape_NonWatermarkShape_ReturnsNull()
	{
		var shape = new Shape
		{
			Id = "SomeOtherShape",
			Style = "width:100pt;height:50pt"
		};

		var result = WatermarkParser.TryParseWatermarkShape(shape);

		result.Should().BeNull();
	}

	[Fact]
	public void TryParseWatermarkShape_WatermarkIdWithoutType_Detected()
	{
		var shape = new Shape
		{
			Id = "PowerPlusWaterMarkObject999",
			Style = "width:300pt;height:100pt"
		};
		shape.AppendChild(new TextPath
		{
			Style = "font-family:\"Arial\"",
			String = "CONFIDENTIAL"
		});

		var result = WatermarkParser.TryParseWatermarkShape(shape);

		result.Should().NotBeNull();
		result!.Kind.Should().Be(WatermarkKind.Text);
		result.Text.Should().Be("CONFIDENTIAL");
	}

	[Fact]
	public void TryParseWatermarkShape_ShapeTypeWithoutAbsolutePosition_ReturnsNull()
	{
		var shape = new Shape
		{
			Id = "RegularShape123",
			Type = "#_x0000_t136",
			Style = "width:100pt;height:50pt"
		};
		shape.AppendChild(new TextPath { String = "Not a watermark" });

		var result = WatermarkParser.TryParseWatermarkShape(shape);

		result.Should().BeNull();
	}

	[Fact]
	public void TryParseWatermarkShape_OpacityAsFixedPoint_ParsedCorrectly()
	{
		var shape = new Shape
		{
			Id = "PowerPlusWaterMarkObject100",
			Style = "position:absolute;width:100pt;height:50pt"
		};
		shape.AppendChild(new Fill { Opacity = "32768f" });
		shape.AppendChild(new TextPath { String = "HALF", Style = "font-family:\"Calibri\"" });

		var result = WatermarkParser.TryParseWatermarkShape(shape);

		result.Should().NotBeNull();
		result!.Opacity.Should().BeApproximately(0.5f, 0.01f);
	}

	[Fact]
	public void TryParseWatermarkShape_NoTextPathOrImageData_ReturnsNull()
	{
		var shape = new Shape
		{
			Id = "PowerPlusWaterMarkObject555",
			Style = "position:absolute;width:100pt;height:50pt"
		};
		// No TextPath or ImageData children

		var result = WatermarkParser.TryParseWatermarkShape(shape);

		result.Should().BeNull();
	}

	[Fact]
	public void TryParseWatermarkShape_NotCentered_ReportsNotCentered()
	{
		var shape = new Shape
		{
			Id = "PowerPlusWaterMarkObject777",
			Style = "position:absolute;width:200pt;height:80pt;mso-position-horizontal:left;mso-position-vertical:top"
		};
		shape.AppendChild(new TextPath { String = "OFF-CENTER", Style = "font-family:\"Arial\"" });

		var result = WatermarkParser.TryParseWatermarkShape(shape);

		result.Should().NotBeNull();
		result!.IsHorizontallyCentered.Should().BeFalse();
		result.IsVerticallyCentered.Should().BeFalse();
	}

	[Fact]
	public void TryParseWatermarkShape_MissingOpacity_DefaultsToHalf()
	{
		var shape = new Shape
		{
			Id = "PowerPlusWaterMarkObject888",
			Style = "position:absolute;width:100pt;height:50pt"
		};
		shape.AppendChild(new TextPath { String = "DEFAULT", Style = "font-family:\"Calibri\"" });

		var result = WatermarkParser.TryParseWatermarkShape(shape);

		result.Should().NotBeNull();
		result!.Opacity.Should().Be(0.5f);
	}

	[Fact]
	public void ParseWatermarks_NullDocument_ThrowsArgumentNullException()
	{
		var action = () => WatermarkParser.ParseWatermarks(null!);

		action.Should().Throw<ArgumentNullException>();
	}
}
