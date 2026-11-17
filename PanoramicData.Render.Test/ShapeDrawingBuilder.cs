namespace PanoramicData.Render.Test;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;

internal static class ShapeDrawingBuilder
{
	private const string DrawingMlNamespace = "http://schemas.openxmlformats.org/drawingml/2006/main";

	public static Drawing CreateInline(A.ShapeProperties shapeProperties, OpenXmlElement? extraGraphicContent = null, long widthEmu = 914400, long heightEmu = 457200)
	{
		var inline = new DW.Inline(
			new DW.Extent { Cx = widthEmu, Cy = heightEmu },
			CreateGraphic(shapeProperties, extraGraphicContent))
		{
			DistanceFromTop = 0,
			DistanceFromBottom = 0,
			DistanceFromLeft = 0,
			DistanceFromRight = 0
		};
		return new Drawing(inline);
	}

	public static Drawing CreateAnchor(A.ShapeProperties shapeProperties, string name, OpenXmlElement? extraGraphicContent = null, long widthEmu = 914400, long heightEmu = 457200)
	{
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
			new DW.DocProperties { Id = 1U, Name = name },
			new DW.NonVisualGraphicFrameDrawingProperties(),
			CreateGraphic(shapeProperties, extraGraphicContent))
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

	private static A.Graphic CreateGraphic(A.ShapeProperties shapeProperties, OpenXmlElement? extraGraphicContent)
	{
		var graphicData = new A.GraphicData(shapeProperties)
		{
			Uri = DrawingMlNamespace
		};
		if (extraGraphicContent is not null)
		{
			graphicData.Append(extraGraphicContent);
		}

		return new A.Graphic(graphicData);
	}
}