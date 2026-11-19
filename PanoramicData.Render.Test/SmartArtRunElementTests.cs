namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using A = DocumentFormat.OpenXml.Drawing;

public sealed class SmartArtRunElementTests
{
	private static SmartArtRunElement ParseInline(string dmRelId, long widthEmu, long heightEmu, bool withFallback = false)
		=> Single(ShapeDrawingBuilder.InlineOf(SmartArtGraphic(dmRelId, withFallback), widthEmu, heightEmu));

	private static SmartArtRunElement Single(Drawing drawing)
		=> RunElementParser.Parse(new Run(drawing)).Should().ContainSingle()
			.Which.Should().BeOfType<SmartArtRunElement>()
			.Subject;

	[Fact]
	public void Parse_InlineDrawingWithSmartArt_ReturnsSmartArtRunElement()
	{
		var smartArt = ParseInline("rIdDm1", 2000000L, 1500000L);

		smartArt.Should().NotBeNull();
	}

	[Fact]
	public void Parse_InlineDrawingWithSmartArt_PreservesRelationshipId()
	{
		var smartArt = ParseInline("rIdDm99", 914400L, 457200L);

		smartArt.RelationshipId.Should().Be("rIdDm99");
	}

	[Fact]
	public void Parse_InlineDrawingWithSmartArt_PreservesExtents()
	{
		var smartArt = ParseInline("rId1", 3200000L, 2400000L);

		smartArt.WidthEmu.Should().Be(3200000L);
		smartArt.HeightEmu.Should().Be(2400000L);
	}

	[Fact]
	public void Parse_AnchorDrawingWithSmartArt_ReturnsSmartArtRunElement()
	{
		var smartArt = Single(ShapeDrawingBuilder.AnchorOf(SmartArtGraphic("rIdAnchorDm", false), "SmartArt 1", 1800000L, 900000L));

		smartArt.RelationshipId.Should().Be("rIdAnchorDm");
		smartArt.WidthEmu.Should().Be(1800000L);
		smartArt.HeightEmu.Should().Be(900000L);
	}

	[Fact]
	public void Parse_SmartArtWithFallbackShapes_HasFallbackTrue()
	{
		var smartArt = ParseInline("rIdDm2", 1000000L, 1000000L, withFallback: true);

		smartArt.HasFallback.Should().BeTrue();
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void SmartArtRunElement_HasFallback_ReflectsInitialiser(bool hasFallback)
	{
		var smartArt = new SmartArtRunElement
		{
			RelationshipId = "rId1",
			HasFallback = hasFallback
		};

		smartArt.HasFallback.Should().Be(hasFallback);
	}

	private static A.Graphic SmartArtGraphic(string dmRelId, bool withFallback)
	{
		// UnknownElement avoids a dependency on the dgm namespace types.
		var relIds = new OpenXmlUnknownElement("dgm", "relIds", "http://schemas.openxmlformats.org/drawingml/2006/diagram");
		relIds.SetAttribute(new OpenXmlAttribute("r", "dm", "http://schemas.openxmlformats.org/officeDocument/2006/relationships", dmRelId));

		var graphicData = new A.GraphicData(relIds)
		{
			Uri = "http://schemas.openxmlformats.org/drawingml/2006/diagram"
		};
		if (withFallback)
		{
			graphicData.Append(new A.ShapeProperties());
		}

		return new A.Graphic(graphicData);
	}
}
