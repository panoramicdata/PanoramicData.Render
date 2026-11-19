namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using A = DocumentFormat.OpenXml.Drawing;

public sealed class DrawingGroupRunElementTests
{
	private static DrawingGroupRunElement ParseGroup(Drawing drawing)
		=> RunElementParser.Parse(new Run(drawing)).Should().ContainSingle()
			.Which.Should().BeOfType<DrawingGroupRunElement>()
			.Subject;

	private static DrawingGroupRunElement ParseFlatGroup()
		=> ParseGroup(ShapeDrawingBuilder.InlineOf(ShapeDrawingBuilder.GroupGraphic(CreateFlatGroup(1800000L, 900000L)), 1800000L, 900000L));

	private static void AssertChildBounds(DrawingGroupRunElement group, int index, long offsetX)
	{
		group.Children[index].OffsetXEmu.Should().Be(offsetX);
		group.Children[index].OffsetYEmu.Should().Be(0);
		group.Children[index].WidthEmu.Should().Be(900000L);
		group.Children[index].HeightEmu.Should().Be(900000L);
	}

	private static void AssertPreset(DrawingGroupRunElement group, int index, string expectedKind)
		=> group.Children[index].Shape.Should().BeOfType<DrawingShapeRunElement>()
			.Which.PresetKind.ToString().Should().Be(expectedKind);

	[Fact]
	public void Parse_InlineGroupWithTwoPresetChildren_ReturnsGroupWithBothChildren()
	{
		var group = ParseFlatGroup();

		group.WidthEmu.Should().Be(1800000L);
		group.HeightEmu.Should().Be(900000L);
		group.Children.Should().HaveCount(2);
	}

	[Fact]
	public void Parse_InlineGroupChildren_HaveCorrectOffsetAndSize()
	{
		var group = ParseFlatGroup();

		AssertChildBounds(group, 0, 0);
		AssertChildBounds(group, 1, 900000L);
	}

	[Fact]
	public void Parse_InlineGroupChildren_HaveCorrectPresetKinds()
	{
		var group = ParseFlatGroup();

		AssertPreset(group, 0, "Rectangle");
		AssertPreset(group, 1, "Ellipse");
	}

	[Fact]
	public void Parse_AnchorGroupWithOneChild_ReturnsGroupRunElement()
	{
		var group = ParseGroup(ShapeDrawingBuilder.AnchorOf(
			ShapeDrawingBuilder.GroupGraphic(CreateWgpElement(500000L, 500000L, CreateWsp("diamond", 0, 0, 500000L, 500000L))),
			"Group",
			500000L,
			500000L));

		group.Children.Should().HaveCount(1);
		AssertPreset(group, 0, "Diamond");
	}

	[Fact]
	public void Parse_InlineNestedGroup_ReturnsGroupWithNestedGroupChild()
	{
		var innerWgp = CreateWgpElement(1800000L, 900000L, CreateWsp("rect", 0, 0, 1800000L, 900000L));
		var outerWgp = CreateWgpElement(1800000L, 900000L, innerWgp);

		var outer = ParseGroup(ShapeDrawingBuilder.InlineOf(ShapeDrawingBuilder.GroupGraphic(outerWgp), 1800000L, 900000L));

		outer.Children.Should().HaveCount(1);
		var inner = outer.Children[0].Shape.Should().BeOfType<DrawingGroupRunElement>().Subject;
		inner.Children.Should().HaveCount(1);
		inner.Children[0].Shape.Should().BeOfType<DrawingShapeRunElement>();
	}

	private static OpenXmlUnknownElement CreateFlatGroup(long widthEmu, long heightEmu)
	{
		var half = widthEmu / 2;
		return CreateWgpElement(
			widthEmu,
			heightEmu,
			CreateWsp("rect", 0, 0, half, heightEmu),
			CreateWsp("ellipse", half, 0, half, heightEmu));
	}

	private static OpenXmlUnknownElement CreateWgpElement(long widthEmu, long heightEmu, params OpenXmlElement[] children)
	{
		var grpSpPr = new OpenXmlUnknownElement("grpSpPr");
		grpSpPr.Append(BuildXfrm(0, 0, widthEmu, heightEmu));
		var wgp = new OpenXmlUnknownElement("wgp");
		wgp.Append(grpSpPr);
		foreach (var child in children)
		{
			wgp.Append(child);
		}

		return wgp;
	}

	private static OpenXmlUnknownElement CreateWsp(string presetName, long offsetX, long offsetY, long widthEmu, long heightEmu)
	{
		var preset = presetName switch
		{
			"ellipse" => A.ShapeTypeValues.Ellipse,
			"diamond" => A.ShapeTypeValues.Diamond,
			_ => A.ShapeTypeValues.Rectangle
		};
		var spPr = new A.ShapeProperties(
			BuildXfrm(offsetX, offsetY, widthEmu, heightEmu),
			new A.PresetGeometry { Preset = preset });

		var wsp = new OpenXmlUnknownElement("wsp");
		wsp.Append(new OpenXmlUnknownElement("cNvSpPr"));
		wsp.Append(spPr);
		return wsp;
	}

	private static A.Transform2D BuildXfrm(long offsetX, long offsetY, long widthEmu, long heightEmu)
		=> new(new A.Offset { X = offsetX, Y = offsetY }, new A.Extents { Cx = widthEmu, Cy = heightEmu });
}
