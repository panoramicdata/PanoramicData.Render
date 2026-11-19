namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;

public sealed partial class RunElementParserTests
{
	[Theory]
	[InlineData("left", "top", 1, 6)]
	[InlineData("right", "center", 3, 2)]
	[InlineData("inside", "outside", 4, 5)]
	public void Parse_AnchorDrawingWithDifferentAlignmentKeywords_ParsesExpectedValues(
		string horizontalAlign,
		string verticalAlign,
		int expectedHorizontal,
		int expectedVertical)
	{
		var img = ParseAnchor(new AnchorOptions { HorizontalAlign = horizontalAlign, VerticalAlign = verticalAlign });

		img.Placement.HorizontalAlignment.Should().Be((AnchorAlignment)expectedHorizontal);
		img.Placement.VerticalAlignment.Should().Be((AnchorAlignment)expectedVertical);
	}

	[Fact]
	public void Parse_AnchorDrawingWithNegativeOffsets_PreservesAbsoluteOffsetValues()
	{
		var img = ParseAnchor(new AnchorOptions { HorizontalRelativeFrom = DW.HorizontalRelativePositionValues.Margin, VerticalRelativeFrom = DW.VerticalRelativePositionValues.Margin, HorizontalOffset = "-91440", VerticalOffset = "182880" });

		AssertPlacement(img, AnchorRelativeFrom.Margin, AnchorRelativeFrom.Margin, -91440, 182880);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void Parse_AnchorDrawing_ParsesZOrderFlag(bool behindDocument)
	{
		var img = ParseAnchor(new AnchorOptions { BehindDocument = behindDocument });

		img.Placement.BehindDocument.Should().Be(behindDocument);
	}

	[Fact]
	public void Parse_AnchorPresetShape_PreservesAnchorPlacementAndWrapMetadata()
	{
		var anchor = CreateAnchor(
			widthEmu: 300,
			heightEmu: 400,
			graphic: CreateShapeGraphic(ShapeDrawingBuilder.Preset(A.ShapeTypeValues.Rectangle)),
			options: new AnchorOptions
			{
				HorizontalRelativeFrom = DW.HorizontalRelativePositionValues.Margin,
				VerticalRelativeFrom = DW.VerticalRelativePositionValues.Paragraph,
				HorizontalAlign = "center",
				VerticalOffset = "12700",
				BehindDocument = true,
				WrapElement = new DW.WrapSquare(),
				DistanceFromTop = 6350U,
				DistanceFromBottom = 12700U,
				DistanceFromLeft = 19050U,
				DistanceFromRight = 25400U
			});

		var placement = Single<DrawingShapeRunElement>(new Drawing(anchor)).AnchorPlacement;

		placement.HorizontalRelativeFrom.Should().Be(AnchorRelativeFrom.Margin);
		placement.VerticalRelativeFrom.Should().Be(AnchorRelativeFrom.Paragraph);
		placement.HorizontalAlignment.Should().Be(AnchorAlignment.Center);
		placement.VerticalOffsetEmu.Should().Be(12700);
		placement.BehindDocument.Should().BeTrue();
		placement.WrapStyle.Should().Be(AnchorWrapStyle.Square);
		placement.DistanceTopEmu.Should().Be(6350);
		placement.DistanceBottomEmu.Should().Be(12700);
		placement.DistanceLeftEmu.Should().Be(19050);
		placement.DistanceRightEmu.Should().Be(25400);
	}

	[Fact]
	public void Parse_AnchorCustomGeometry_PreservesAnchorPlacement()
	{
		var anchor = CreateAnchor(
			widthEmu: 500,
			heightEmu: 600,
			graphic: CreateShapeGraphic(ShapeDrawingBuilder.CustomGeometry("<a:pathLst xmlns:a='http://schemas.openxmlformats.org/drawingml/2006/main'><a:path><a:moveTo><a:pt x='0' y='0'/></a:moveTo><a:lnTo><a:pt x='1' y='1'/></a:lnTo><a:close/></a:path></a:pathLst>")),
			options: new AnchorOptions
			{
				HorizontalRelativeFrom = DW.HorizontalRelativePositionValues.Page,
				VerticalRelativeFrom = DW.VerticalRelativePositionValues.Margin,
				HorizontalOffset = "6350",
				VerticalAlign = "bottom",
				WrapElement = new DW.WrapTopBottom()
			});

		var placement = Single<DrawingCustomGeometryRunElement>(new Drawing(anchor)).AnchorPlacement;

		placement.HorizontalRelativeFrom.Should().Be(AnchorRelativeFrom.Page);
		placement.VerticalRelativeFrom.Should().Be(AnchorRelativeFrom.Margin);
		placement.HorizontalOffsetEmu.Should().Be(6350);
		placement.VerticalAlignment.Should().Be(AnchorAlignment.Bottom);
		placement.WrapStyle.Should().Be(AnchorWrapStyle.TopAndBottom);
	}

	private static ImageRunElement ParseImage(
		bool anchored,
		string relationshipId,
		long widthEmu,
		long heightEmu,
		(int Left, int Top, int Right, int Bottom)? crop = null)
	{
		var blipFill = new A.Pictures.BlipFill(new A.Blip { Embed = relationshipId });
		if (crop is { } c)
		{
			blipFill.SourceRectangle = new A.SourceRectangle { Left = c.Left, Top = c.Top, Right = c.Right, Bottom = c.Bottom };
		}

		var pic = new A.Pictures.Picture(
			new A.Pictures.NonVisualPictureProperties(
				new A.Pictures.NonVisualDrawingProperties { Id = 1, Name = "test" },
				new A.Pictures.NonVisualPictureDrawingProperties()),
			blipFill,
			new A.Pictures.ShapeProperties());
		var graphic = new A.Graphic(new A.GraphicData(pic)
		{
			Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture"
		});

		return anchored
			? Single<AnchorImageRunElement>(new Drawing(CreateAnchor(widthEmu, heightEmu, graphic)))
			: Single<InlineImageRunElement>(ShapeDrawingBuilder.InlineOf(graphic, widthEmu, heightEmu));
	}

	private static AnchorImageRunElement ParseAnchor(AnchorOptions options)
		=> Single<AnchorImageRunElement>(new Drawing(CreateAnchor(100, 200, options: options)));

	private static void AssertPlacement(AnchorImageRunElement img, AnchorRelativeFrom horizontal, AnchorRelativeFrom vertical, long offsetX, long offsetY)
	{
		img.Placement.HorizontalRelativeFrom.Should().Be(horizontal);
		img.Placement.VerticalRelativeFrom.Should().Be(vertical);
		img.Placement.HorizontalOffsetEmu.Should().Be(offsetX);
		img.Placement.VerticalOffsetEmu.Should().Be(offsetY);
	}

	private sealed record AnchorOptions
	{
		public DW.HorizontalRelativePositionValues? HorizontalRelativeFrom { get; init; }

		public DW.VerticalRelativePositionValues? VerticalRelativeFrom { get; init; }

		public string HorizontalOffset { get; init; } = "0";

		public string VerticalOffset { get; init; } = "0";

		public string? HorizontalAlign { get; init; }

		public string? VerticalAlign { get; init; }

		public bool BehindDocument { get; init; }

		public OpenXmlElement? WrapElement { get; init; }

		public uint DistanceFromTop { get; init; }

		public uint DistanceFromBottom { get; init; }

		public uint DistanceFromLeft { get; init; }

		public uint DistanceFromRight { get; init; }
	}

	private static DW.Anchor CreateAnchor(long widthEmu, long heightEmu, A.Graphic? graphic = null, AnchorOptions? options = null)
	{
		options ??= new AnchorOptions();
		var horizontalPosition = options.HorizontalAlign is null
			? new DW.HorizontalPosition(new DW.PositionOffset(options.HorizontalOffset))
			: new DW.HorizontalPosition(new DW.HorizontalAlignment(options.HorizontalAlign));
		horizontalPosition.RelativeFrom = options.HorizontalRelativeFrom ?? DW.HorizontalRelativePositionValues.Page;

		var verticalPosition = options.VerticalAlign is null
			? new DW.VerticalPosition(new DW.PositionOffset(options.VerticalOffset))
			: new DW.VerticalPosition(new DW.VerticalAlignment(options.VerticalAlign));
		verticalPosition.RelativeFrom = options.VerticalRelativeFrom ?? DW.VerticalRelativePositionValues.Page;

		return new DW.Anchor(
			new DW.SimplePosition { X = 0, Y = 0 },
			horizontalPosition,
			verticalPosition,
			new DW.Extent { Cx = widthEmu, Cy = heightEmu },
			new DW.EffectExtent(),
			options.WrapElement ?? new DW.WrapNone(),
			new DW.DocProperties { Id = 1U, Name = "AnchorImage" },
			new DW.NonVisualGraphicFrameDrawingProperties(),
			graphic ?? new A.Graphic())
		{
			DistanceFromTop = options.DistanceFromTop,
			DistanceFromBottom = options.DistanceFromBottom,
			DistanceFromLeft = options.DistanceFromLeft,
			DistanceFromRight = options.DistanceFromRight,
			SimplePos = false,
			RelativeHeight = 0U,
			BehindDoc = options.BehindDocument,
			Locked = false,
			LayoutInCell = true,
			AllowOverlap = true
		};
	}

	private static A.Graphic CreateShapeGraphic(OpenXmlElement geometry)
		=> new(new A.GraphicData(new A.ShapeProperties(geometry))
		{
			Uri = "http://schemas.openxmlformats.org/drawingml/2006/main"
		});
}
