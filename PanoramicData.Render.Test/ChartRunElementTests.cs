namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;

public sealed class ChartRunElementTests
{
	// -------------------------------------------------------------------------
	// 5.5.1 — Detect chart element in inline drawing
	// -------------------------------------------------------------------------

	[Fact]
	public void Parse_InlineDrawingWithChart_ReturnsChartRunElement()
	{
		var drawing = CreateInlineChartDrawing("rId5", 1800000L, 900000L);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);

		elements.Should().ContainSingle()
			.Which.Should().BeOfType<ChartRunElement>();
	}

	[Fact]
	public void Parse_InlineDrawingWithChart_PreservesRelationshipId()
	{
		var drawing = CreateInlineChartDrawing("rIdChart1", 914400L, 457200L);
		var run = new Run(drawing);

		var chart = (ChartRunElement)RunElementParser.Parse(run)[0];

		chart.RelationshipId.Should().Be("rIdChart1");
	}

	[Fact]
	public void Parse_InlineDrawingWithChart_PreservesExtents()
	{
		var drawing = CreateInlineChartDrawing("rId1", 3200000L, 2400000L);
		var run = new Run(drawing);

		var chart = (ChartRunElement)RunElementParser.Parse(run)[0];

		chart.WidthEmu.Should().Be(3200000L);
		chart.HeightEmu.Should().Be(2400000L);
	}

	// -------------------------------------------------------------------------
	// 5.5.1 — Detect chart element in anchor drawing
	// -------------------------------------------------------------------------

	[Fact]
	public void Parse_AnchorDrawingWithChart_ReturnsChartRunElement()
	{
		var drawing = CreateAnchorChartDrawing("rIdAnchorChart", 2000000L, 1500000L);
		var run = new Run(drawing);

		var chart = RunElementParser.Parse(run).Should().ContainSingle()
			.Which.Should().BeOfType<ChartRunElement>()
			.Subject;

		chart.RelationshipId.Should().Be("rIdAnchorChart");
		chart.WidthEmu.Should().Be(2000000L);
		chart.HeightEmu.Should().Be(1500000L);
	}

	// -------------------------------------------------------------------------
	// 5.5.2 — Fallback image detection
	// -------------------------------------------------------------------------

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

	// -------------------------------------------------------------------------
	// 5.5.3 — No fallback: placeholder model
	// -------------------------------------------------------------------------

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

	// -------------------------------------------------------------------------
	// Helpers
	// -------------------------------------------------------------------------

	private static Drawing CreateInlineChartDrawing(string relId, long widthEmu, long heightEmu)
	{
		var chartRef = CreateChartElement(relId);
		var graphicData = new A.GraphicData(chartRef)
		{
			Uri = "http://schemas.openxmlformats.org/drawingml/2006/chart"
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

	private static Drawing CreateAnchorChartDrawing(string relId, long widthEmu, long heightEmu)
	{
		var chartRef = CreateChartElement(relId);
		var graphicData = new A.GraphicData(chartRef)
		{
			Uri = "http://schemas.openxmlformats.org/drawingml/2006/chart"
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
			new DW.DocProperties { Id = 1U, Name = "Chart" },
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

	/// <summary>
	/// Creates a <c>c:chart</c> unknown element with an r:id attribute — matching real DOCX structure.
	/// </summary>
	private static OpenXmlUnknownElement CreateChartElement(string relId)
	{
		var chart = new OpenXmlUnknownElement("c:chart");
		chart.SetAttribute(new OpenXmlAttribute("r", "id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships", relId));
		return chart;
	}
}
