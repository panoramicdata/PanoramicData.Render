namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;

public sealed class DrawingGroupRunElementTests
{
	// -------------------------------------------------------------------------
	// Inline group: flat group with two preset-geometry children
	// -------------------------------------------------------------------------

	[Fact]
	public void Parse_InlineGroupWithTwoPresetChildren_ReturnsGroupWithBothChildren()
	{
		var drawing = CreateInlineGroupDrawing(1800000L, 900000L);
		var run = new Run(drawing);

		var elements = RunElementParser.Parse(run);

		var group = elements.Should().ContainSingle()
			.Which.Should().BeOfType<DrawingGroupRunElement>()
			.Subject;

		group.WidthEmu.Should().Be(1800000L);
		group.HeightEmu.Should().Be(900000L);
		group.Children.Should().HaveCount(2);
	}

	[Fact]
	public void Parse_InlineGroupChildren_HaveCorrectOffsetAndSize()
	{
		var drawing = CreateInlineGroupDrawing(1800000L, 900000L);
		var run = new Run(drawing);
		var group = (DrawingGroupRunElement)RunElementParser.Parse(run)[0];

		group.Children[0].OffsetXEmu.Should().Be(0);
		group.Children[0].OffsetYEmu.Should().Be(0);
		group.Children[0].WidthEmu.Should().Be(900000L);
		group.Children[0].HeightEmu.Should().Be(900000L);

		group.Children[1].OffsetXEmu.Should().Be(900000L);
		group.Children[1].OffsetYEmu.Should().Be(0);
		group.Children[1].WidthEmu.Should().Be(900000L);
		group.Children[1].HeightEmu.Should().Be(900000L);
	}

	[Fact]
	public void Parse_InlineGroupChildren_HaveCorrectPresetKinds()
	{
		var drawing = CreateInlineGroupDrawing(1800000L, 900000L);
		var run = new Run(drawing);
		var group = (DrawingGroupRunElement)RunElementParser.Parse(run)[0];

		group.Children[0].Shape.Should().BeOfType<DrawingShapeRunElement>()
			.Which.PresetKind.ToString().Should().Be("Rectangle");
		group.Children[1].Shape.Should().BeOfType<DrawingShapeRunElement>()
			.Which.PresetKind.ToString().Should().Be("Ellipse");
	}

	// -------------------------------------------------------------------------
	// Anchor group: single child
	// -------------------------------------------------------------------------

	[Fact]
	public void Parse_AnchorGroupWithOneChild_ReturnsGroupRunElement()
	{
		var drawing = CreateAnchorGroupDrawing(500000L, 500000L);
		var run = new Run(drawing);

		var group = RunElementParser.Parse(run).Should().ContainSingle()
			.Which.Should().BeOfType<DrawingGroupRunElement>()
			.Subject;

		group.Children.Should().HaveCount(1);
		group.Children[0].Shape.Should().BeOfType<DrawingShapeRunElement>()
			.Which.PresetKind.ToString().Should().Be("Diamond");
	}

	// -------------------------------------------------------------------------
	// Nested group
	// -------------------------------------------------------------------------

	[Fact]
	public void Parse_InlineNestedGroup_ReturnsGroupWithNestedGroupChild()
	{
		var drawing = CreateInlineNestedGroupDrawing(1800000L, 900000L);
		var run = new Run(drawing);

		var outer = RunElementParser.Parse(run).Should().ContainSingle()
			.Which.Should().BeOfType<DrawingGroupRunElement>()
			.Subject;

		outer.Children.Should().HaveCount(1);
		var inner = outer.Children[0].Shape.Should().BeOfType<DrawingGroupRunElement>().Subject;
		inner.Children.Should().HaveCount(1);
		inner.Children[0].Shape.Should().BeOfType<DrawingShapeRunElement>();
	}

	// -------------------------------------------------------------------------
	// Helpers
	// -------------------------------------------------------------------------

	private static Drawing CreateInlineGroupDrawing(long widthEmu, long heightEmu)
	{
		var wgp = CreateFlatGroup(widthEmu, heightEmu);
		var graphicData = new A.GraphicData(wgp)
		{
			Uri = "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingGroup"
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

	private static Drawing CreateAnchorGroupDrawing(long widthEmu, long heightEmu)
	{
		var wgp = CreateSingleChildGroup("diamond", widthEmu, heightEmu);
		var graphicData = new A.GraphicData(wgp)
		{
			Uri = "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingGroup"
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
			new DW.DocProperties { Id = 1U, Name = "Group" },
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

	private static Drawing CreateInlineNestedGroupDrawing(long widthEmu, long heightEmu)
	{
		// inner group contains one rect shape
		var innerWsp = CreateWsp("rect", 0, 0, widthEmu, heightEmu);
		var innerWgp = CreateWgpElement(widthEmu, heightEmu, new OpenXmlElement[] { innerWsp });

		// outer group contains the inner group
		var innerWgpItem = WrapInWgpGroupItem(innerWgp, 0, 0, widthEmu, heightEmu);
		var outerWgp = CreateWgpElement(widthEmu, heightEmu, new OpenXmlElement[] { innerWgpItem });

		var graphicData = new A.GraphicData(outerWgp)
		{
			Uri = "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingGroup"
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

	// Build a flat group with rect + ellipse children side by side.
	private static OpenXmlUnknownElement CreateFlatGroup(long widthEmu, long heightEmu)
	{
		var half = widthEmu / 2;
		var child1 = CreateWsp("rect", 0, 0, half, heightEmu);
		var child2 = CreateWsp("ellipse", half, 0, half, heightEmu);
		return CreateWgpElement(widthEmu, heightEmu, new OpenXmlElement[] { child1, child2 });
	}

	// Build a group with a single shape child.
	private static OpenXmlUnknownElement CreateSingleChildGroup(string presetName, long widthEmu, long heightEmu)
	{
		var child = CreateWsp(presetName, 0, 0, widthEmu, heightEmu);
		return CreateWgpElement(widthEmu, heightEmu, new OpenXmlElement[] { child });
	}

	// Construct a wgp element with grpSpPr and child elements.
	private static OpenXmlUnknownElement CreateWgpElement(long widthEmu, long heightEmu, IEnumerable<OpenXmlElement> children)
	{
		var grpSpPr = BuildGrpSpPr(0, 0, widthEmu, heightEmu);
		var wgp = new OpenXmlUnknownElement("wgp");
		wgp.Append(grpSpPr);
		foreach (var child in children)
		{
			wgp.Append(child);
		}

		return wgp;
	}

	// Wrap a wgp element in another wgp child (for nesting).
	private static OpenXmlUnknownElement WrapInWgpGroupItem(OpenXmlElement nestedWgp, long offsetX, long offsetY, long widthEmu, long heightEmu)
	{
		// For nested groups the wgp IS the item; GroupShapeParser detects "wgp" local name children.
		// Re-use the same element — just return it (already has grpSpPr with xfrm set by CreateWgpElement).
		// We need an additional xfrm on the grpSpPr for the child offset. Clone and patch.
		// Simplest: the nested element already has grpSpPr with correct extents; offsetX/Y are 0 here.
		return (OpenXmlUnknownElement)nestedWgp;
	}

	// Construct a wsp element containing a typed ShapeProperties with preset geometry.
	private static OpenXmlUnknownElement CreateWsp(string presetName, long offsetX, long offsetY, long widthEmu, long heightEmu)
	{
		var spPr = new A.ShapeProperties(
			BuildXfrm(offsetX, offsetY, widthEmu, heightEmu),
			new A.PresetGeometry { Preset = MapPreset(presetName) });

		var wsp = new OpenXmlUnknownElement("wsp");
		wsp.Append(BuildCnvSpPr());
		wsp.Append(spPr);
		return wsp;
	}

	private static OpenXmlUnknownElement BuildGrpSpPr(long offsetX, long offsetY, long widthEmu, long heightEmu)
	{
		var grpSpPr = new OpenXmlUnknownElement("grpSpPr");
		grpSpPr.Append(BuildXfrm(offsetX, offsetY, widthEmu, heightEmu));
		return grpSpPr;
	}

	private static A.Transform2D BuildXfrm(long offsetX, long offsetY, long widthEmu, long heightEmu)
	{
		var off = new A.Offset { X = offsetX, Y = offsetY };
		var ext = new A.Extents { Cx = widthEmu, Cy = heightEmu };
		return new A.Transform2D(off, ext);
	}

	private static OpenXmlUnknownElement BuildCnvSpPr()
	{
		return new OpenXmlUnknownElement("cNvSpPr");
	}

	private static A.ShapeTypeValues MapPreset(string name)
	{
		return name switch
		{
			"rect" => A.ShapeTypeValues.Rectangle,
			"ellipse" => A.ShapeTypeValues.Ellipse,
			"diamond" => A.ShapeTypeValues.Diamond,
			_ => A.ShapeTypeValues.Rectangle
		};
	}
}
