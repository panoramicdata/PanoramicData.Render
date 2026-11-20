namespace PanoramicData.Render.ReferenceGenerator;

using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

internal static partial class TestCorpusGenerator
{
	private static Drawing CreateInlineImageDrawing(string relationshipId, long widthEmu, long heightEmu, string name)
	{
		var inline = new DW.Inline(
			new DW.Extent { Cx = widthEmu, Cy = heightEmu },
			new DW.DocProperties { Id = 1U, Name = name },
			new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks { NoChangeAspect = true }),
			new A.Graphic(CreatePictureGraphicData(relationshipId, name, widthEmu, heightEmu)))
		{
			DistanceFromTop = 0U,
			DistanceFromBottom = 0U,
			DistanceFromLeft = 0U,
			DistanceFromRight = 0U
		};

		return new Drawing(inline);
	}

	private static Drawing CreateAnchorImageDrawing(string relationshipId, long widthEmu, long heightEmu, string name)
	{
		var anchor = new DW.Anchor(
			new DW.SimplePosition { X = 0, Y = 0 },
			new DW.HorizontalPosition(new DW.PositionOffset("914400"))
			{
				RelativeFrom = DW.HorizontalRelativePositionValues.Margin
			},
			new DW.VerticalPosition(new DW.PositionOffset("914400"))
			{
				RelativeFrom = DW.VerticalRelativePositionValues.Paragraph
			},
			new DW.Extent { Cx = widthEmu, Cy = heightEmu },
			new DW.WrapSquare { WrapText = DW.WrapTextValues.BothSides },
			new DW.DocProperties { Id = 2U, Name = name },
			new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks { NoChangeAspect = true }),
			new A.Graphic(CreatePictureGraphicData(relationshipId, name, widthEmu, heightEmu)))
		{
			DistanceFromTop = 0U,
			DistanceFromBottom = 0U,
			DistanceFromLeft = 114300U,
			DistanceFromRight = 114300U,
			SimplePos = false,
			RelativeHeight = 0U,
			BehindDoc = false,
			Locked = false,
			LayoutInCell = true,
			AllowOverlap = true
		};

		return new Drawing(anchor);
	}

	private static A.GraphicData CreatePictureGraphicData(string relationshipId, string name, long widthEmu, long heightEmu)
		=> new(CreatePictureElement(relationshipId, name, widthEmu, heightEmu))
		{
			Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture"
		};

	private static PIC.Picture CreatePictureElement(string relationshipId, string name, long widthEmu, long heightEmu)
	{
		return new PIC.Picture(
			new PIC.NonVisualPictureProperties(
				new PIC.NonVisualDrawingProperties { Id = 1U, Name = name },
				new PIC.NonVisualPictureDrawingProperties()),
			new PIC.BlipFill(
				new A.Blip { Embed = relationshipId },
				new A.Stretch(new A.FillRectangle())),
			new PIC.ShapeProperties(
				new A.Transform2D(
					new A.Offset { X = 0L, Y = 0L },
					new A.Extents { Cx = widthEmu, Cy = heightEmu }),
				new A.PresetGeometry(new A.AdjustValueList())
				{
					Preset = A.ShapeTypeValues.Rectangle
				}));
	}

	private static byte[] CreateMinimalPngBytes()
	{
		const string base64Png = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAdSURBVDhPY/jPwPCfEsyALkAqHjVg1IBRAwaLAQAwxP4Q7zYsrwAAAABJRU5ErkJggg==";
		return Convert.FromBase64String(base64Png);
	}
}
