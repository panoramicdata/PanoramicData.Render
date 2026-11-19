namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using A = DocumentFormat.OpenXml.Drawing;

public sealed class ChartRunElementTests
{
	private static ChartRunElement Single(Drawing drawing)
		=> RunElementParser.Parse(new Run(drawing)).Should().ContainSingle()
			.Which.Should().BeOfType<ChartRunElement>()
			.Subject;

	private static ChartRunElement ParseInline(string relId, long widthEmu, long heightEmu)
		=> Single(ShapeDrawingBuilder.InlineOf(ChartGraphic(relId), widthEmu, heightEmu));

	[Fact]
	public void Parse_InlineDrawingWithChart_ReturnsChartRunElement()
	{
		var chart = ParseInline("rId5", 1800000L, 900000L);

		chart.Should().NotBeNull();
	}

	[Fact]
	public void Parse_InlineDrawingWithChart_PreservesRelationshipId()
	{
		var chart = ParseInline("rIdChart1", 914400L, 457200L);

		chart.RelationshipId.Should().Be("rIdChart1");
	}

	[Fact]
	public void Parse_InlineDrawingWithChart_PreservesExtents()
	{
		var chart = ParseInline("rId1", 3200000L, 2400000L);

		chart.WidthEmu.Should().Be(3200000L);
		chart.HeightEmu.Should().Be(2400000L);
	}

	[Fact]
	public void Parse_AnchorDrawingWithChart_ReturnsChartRunElement()
	{
		var chart = Single(ShapeDrawingBuilder.AnchorOf(ChartGraphic("rIdAnchorChart"), "Chart", 2000000L, 1500000L));

		chart.RelationshipId.Should().Be("rIdAnchorChart");
		chart.WidthEmu.Should().Be(2000000L);
		chart.HeightEmu.Should().Be(1500000L);
	}

	[Fact]
	public void ChartRunElement_WithFallbackImage_HasFallbackImageTrue()
	{
		var chart = new ChartRunElement
		{
			RelationshipId = "rId1",
			FallbackImageRelationshipId = "rIdFallback"
		};

		chart.HasFallbackImage.Should().BeTrue();
		chart.FallbackImageRelationshipId.Should().Be("rIdFallback");
	}

	[Fact]
	public void ChartRunElement_WithoutFallback_HasFallbackImageFalse()
	{
		var chart = new ChartRunElement
		{
			RelationshipId = "rId1"
		};

		chart.HasFallbackImage.Should().BeFalse();
		chart.FallbackImageRelationshipId.Should().BeEmpty();
	}

	private static A.Graphic ChartGraphic(string relId)
	{
		var chart = new OpenXmlUnknownElement("c:chart");
		chart.SetAttribute(new OpenXmlAttribute("r", "id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships", relId));
		return new A.Graphic(new A.GraphicData(chart)
		{
			Uri = "http://schemas.openxmlformats.org/drawingml/2006/chart"
		});
	}
}
