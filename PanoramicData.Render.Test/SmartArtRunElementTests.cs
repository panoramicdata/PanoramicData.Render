namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;

public sealed class SmartArtRunElementTests
{
	// -------------------------------------------------------------------------
	// 5.6.1 — Detect SmartArt element in inline drawing
	// -------------------------------------------------------------------------

	[Fact]
	public void Parse_InlineDrawingWithSmartArt_ReturnsSmartArtRunElement()
	{
		var drawing = CreateInlineSmartArtDrawing("rIdDm1", 2000000L, 1500000L);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<SmartArtRunElement>();
	}

	[Fact]
	public void Parse_InlineDrawingWithSmartArt_PreservesRelationshipId()
	{
		var drawing = CreateInlineSmartArtDrawing("rIdDm99", 914400L, 457200L);
		var run = new Run(drawing);

		var smartArt = (SmartArtRunElement)RunElementParser.Parse(run)[0];

		smartArt.RelationshipId.Should().Be("rIdDm99");
	}

	[Fact]
	public void Parse_InlineDrawingWithSmartArt_PreservesExtents()
	{
		var drawing = CreateInlineSmartArtDrawing("rId1", 3200000L, 2400000L);
		var run = new Run(drawing);

		var smartArt = (SmartArtRunElement)RunElementParser.Parse(run)[0];

		smartArt.WidthEmu.Should().Be(3200000L);
		smartArt.HeightEmu.Should().Be(2400000L);
	}

	// -------------------------------------------------------------------------
	// 5.6.1 — Detect SmartArt in anchor drawing
	// -------------------------------------------------------------------------

	[Fact]
	public void Parse_AnchorDrawingWithSmartArt_ReturnsSmartArtRunElement()
	{
		var drawing = CreateAnchorSmartArtDrawing("rIdAnchorDm", 1800000L, 900000L);
		var run = new Run(drawing);

		var smartArt = RunElementParser.Parse(run).Should().ContainSingle()
			.Which.Should().BeOfType<SmartArtRunElement>()
			.Subject;

		smartArt.RelationshipId.Should().Be("rIdAnchorDm");
		smartArt.WidthEmu.Should().Be(1800000L);
		smartArt.HeightEmu.Should().Be(900000L);
	}

	// -------------------------------------------------------------------------
	// 5.6.2 — Fallback detection
	// -------------------------------------------------------------------------

	[Fact]
	public void Parse_SmartArtWithFallbackShapes_HasFallbackTrue()
	{
		var drawing = CreateInlineSmartArtDrawingWithFallback("rIdDm2", 1000000L, 1000000L);
		var run = new Run(drawing);

		var smartArt = (SmartArtRunElement)RunElementParser.Parse(run)[0];

		smartArt.HasFallback.Should().BeTrue();
	}

	// -------------------------------------------------------------------------
	// 5.6.3 — No fallback: placeholder model
	// -------------------------------------------------------------------------

	[Fact]
	public void SmartArtRunElement_WithoutFallback_HasFallbackFalse()
	{
		var smartArt = new SmartArtRunElement
		{
			RelationshipId = "rId1",
			HasFallback = false
		};

		smartArt.HasFallback.Should().BeFalse();
	}

	[Fact]
	public void SmartArtRunElement_WithFallback_HasFallbackTrue()
	{
		var smartArt = new SmartArtRunElement
		{
			RelationshipId = "rId1",
			HasFallback = true
		};

		smartArt.HasFallback.Should().BeTrue();
	}

	// -------------------------------------------------------------------------
	// Helpers
	// -------------------------------------------------------------------------

	private static Drawing CreateInlineSmartArtDrawing(string dmRelId, long widthEmu, long heightEmu)
	{
		var relIdsElem = CreateRelIdsElement(dmRelId);
		var graphicData = new A.GraphicData(relIdsElem)
		{
			Uri = "http://schemas.openxmlformats.org/drawingml/2006/diagram"
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

	private static Drawing CreateInlineSmartArtDrawingWithFallback(string dmRelId, long widthEmu, long heightEmu)
	{
		var relIdsElem = CreateRelIdsElement(dmRelId);
		// Add a ShapeProperties child — the parser uses this to detect fallback shapes.
		var spPr = new A.ShapeProperties();
		var graphicData = new A.GraphicData(relIdsElem, spPr)
		{
			Uri = "http://schemas.openxmlformats.org/drawingml/2006/diagram"
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

	private static Drawing CreateAnchorSmartArtDrawing(string dmRelId, long widthEmu, long heightEmu)
	{
		var relIdsElem = CreateRelIdsElement(dmRelId);
		var graphicData = new A.GraphicData(relIdsElem)
		{
			Uri = "http://schemas.openxmlformats.org/drawingml/2006/diagram"
		};
		var graphic = new A.Graphic(graphicData);
		var anchor = new DW.Anchor(
			new DW.SimplePosition { X = 0L, Y = 0L },
			new DW.HorizontalPosition(new DW.PositionOffset("0"))
			{
				RelativeFrom = DW.HorizontalRelativePositionValues.Column
			},
			new DW.VerticalPosition(new DW.PositionOffset("0"))
			{
				RelativeFrom = DW.VerticalRelativePositionValues.Paragraph
			},
			new DW.Extent { Cx = widthEmu, Cy = heightEmu },
			new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
			new DW.WrapNone(),
			new DW.DocProperties { Id = 1U, Name = "SmartArt 1" },
			new DW.NonVisualGraphicFrameDrawingProperties(),
			graphic)
		{
			DistanceFromTop = 0U,
			DistanceFromBottom = 0U,
			DistanceFromLeft = 0U,
			DistanceFromRight = 0U,
			SimplePos = false,
			RelativeHeight = 1U,
			BehindDoc = false,
			Locked = false,
			LayoutInCell = true,
			AllowOverlap = true
		};
		return new Drawing(anchor);
	}

	private static OpenXmlElement CreateRelIdsElement(string dmRelId)
	{
		// dgm:relIds r:dm="..." — use UnknownElement to avoid requiring dgm namespace dependency.
		var relIds = new OpenXmlUnknownElement("dgm", "relIds", "http://schemas.openxmlformats.org/drawingml/2006/diagram");
		relIds.SetAttribute(new OpenXmlAttribute("r", "dm", "http://schemas.openxmlformats.org/officeDocument/2006/relationships", dmRelId));
		return relIds;
	}
}
