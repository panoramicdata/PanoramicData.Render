namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;

internal static partial class RunElementParser
{
	private static void ParseDrawing(Drawing drawing, List<RunElement> elements)
	{
		var inline = drawing.GetFirstChild<DW.Inline>();
		if (inline is not null)
		{
			elements.Add(ParseInlineDrawing(inline));
			return;
		}

		var anchor = drawing.GetFirstChild<DW.Anchor>();
		if (anchor is not null)
		{
			elements.Add(ParseAnchorDrawing(anchor));
		}
	}

	private static RunElement ParseInlineDrawing(DW.Inline inline)
	{
		var extent = inline.Extent;
		var widthEmu = extent?.Cx ?? 0;
		var heightEmu = extent?.Cy ?? 0;
		var graphic = ParseGraphicContent(inline, widthEmu, heightEmu, null);
		if (graphic is not null)
		{
			return graphic;
		}

		var blip = inline.Descendants<A.Blip>().FirstOrDefault();
		var sourceRectangle = inline.Descendants<A.SourceRectangle>().FirstOrDefault();

		return new InlineImageRunElement
		{
			RelationshipId = blip?.Embed?.Value ?? string.Empty,
			WidthEmu = widthEmu,
			HeightEmu = heightEmu,
			CropLeft = ParsePercentage(sourceRectangle?.Left),
			CropTop = ParsePercentage(sourceRectangle?.Top),
			CropRight = ParsePercentage(sourceRectangle?.Right),
			CropBottom = ParsePercentage(sourceRectangle?.Bottom)
		};
	}

	private static RunElement ParseAnchorDrawing(DW.Anchor anchor)
	{
		var extent = anchor.Extent;
		var widthEmu = extent?.Cx ?? 0;
		var heightEmu = extent?.Cy ?? 0;
		var graphic = ParseGraphicContent(anchor, widthEmu, heightEmu, ParseAnchorPlacement(anchor));
		if (graphic is not null)
		{
			return graphic;
		}

		var blip = anchor.Descendants<A.Blip>().FirstOrDefault();
		var sourceRectangle = anchor.Descendants<A.SourceRectangle>().FirstOrDefault();
		var horizontalPosition = anchor.GetFirstChild<DW.HorizontalPosition>();
		var verticalPosition = anchor.GetFirstChild<DW.VerticalPosition>();

		return new AnchorImageRunElement
		{
			RelationshipId = blip?.Embed?.Value ?? string.Empty,
			WidthEmu = widthEmu,
			HeightEmu = heightEmu,
			CropLeft = ParsePercentage(sourceRectangle?.Left),
			CropTop = ParsePercentage(sourceRectangle?.Top),
			CropRight = ParsePercentage(sourceRectangle?.Right),
			CropBottom = ParsePercentage(sourceRectangle?.Bottom),
			HorizontalRelativeFrom = ParseHorizontalRelativeFrom(horizontalPosition?.RelativeFrom?.Value),
			VerticalRelativeFrom = ParseVerticalRelativeFrom(verticalPosition?.RelativeFrom?.Value),
			HorizontalOffsetEmu = ParseOffset(horizontalPosition?.GetFirstChild<DW.PositionOffset>()?.InnerText),
			VerticalOffsetEmu = ParseOffset(verticalPosition?.GetFirstChild<DW.PositionOffset>()?.InnerText),
			HorizontalAlignment = ParseHorizontalAlignment(horizontalPosition?.GetFirstChild<DW.HorizontalAlignment>()?.InnerText),
			VerticalAlignment = ParseVerticalAlignment(verticalPosition?.GetFirstChild<DW.VerticalAlignment>()?.InnerText),
			BehindDocument = ParseOnOffValue(anchor.BehindDoc)
		};
	}

	private static RunElement? ParseGraphicContent(OpenXmlElement container, long widthEmu, long heightEmu, AnchorPlacementInfo? anchorPlacement)
	{
		var shapeProperties = container.Descendants<A.ShapeProperties>().FirstOrDefault();
		var textFrame = ShapeTextFrameParser.Parse(container);
		var transform = ShapeTransformParser.Parse(shapeProperties);

		var groupShape = FindByLocalName(container, "wgp");
		if (groupShape is not null)
		{
			return GroupShapeParser.Parse(groupShape, widthEmu, heightEmu);
		}

		var presetGeom = container.Descendants<A.PresetGeometry>().FirstOrDefault();
		if (presetGeom is not null)
		{
			return ParseDrawingShape(widthEmu, heightEmu, presetGeom, shapeProperties, textFrame, transform, anchorPlacement);
		}

		var customGeom = FindByLocalName(container, "custGeom");
		if (customGeom is not null)
		{
			return new DrawingCustomGeometryRunElement
			{
				WidthEmu = widthEmu,
				HeightEmu = heightEmu,
				Commands = CustomGeometryParser.Parse(customGeom),
				Fill = ShapeFillParser.Parse(shapeProperties),
				Outline = ShapeOutlineParser.Parse(shapeProperties),
				TextFrame = textFrame,
				Transform = transform,
				AnchorPlacement = anchorPlacement ?? AnchorPlacementInfo.None
			};
		}

		return ParseEmbeddedObject(container, widthEmu, heightEmu);
	}

	private static RunElement? ParseEmbeddedObject(OpenXmlElement container, long widthEmu, long heightEmu)
	{
		var chart = FindByLocalName(container, "chart");
		if (chart is not null)
		{
			return new ChartRunElement
			{
				RelationshipId = GetAttributeValue(chart, "id"),
				WidthEmu = widthEmu,
				HeightEmu = heightEmu
			};
		}

		var smartArtIds = FindByLocalName(container, "relIds");
		if (smartArtIds is null)
		{
			return null;
		}

		return new SmartArtRunElement
		{
			RelationshipId = GetAttributeValue(smartArtIds, "dm"),
			WidthEmu = widthEmu,
			HeightEmu = heightEmu,
			HasFallback = container.Descendants<A.ShapeProperties>().Any()
		};
	}

	private static OpenXmlElement? FindByLocalName(OpenXmlElement container, string localName)
		=> container.Descendants().FirstOrDefault(e => e.LocalName == localName);

	private static string GetAttributeValue(OpenXmlElement element, string localName)
		=> element.GetAttributes().FirstOrDefault(a => a.LocalName == localName).Value ?? string.Empty;
	private static DrawingShapeRunElement ParseDrawingShape(long widthEmu, long heightEmu, A.PresetGeometry presetGeom, A.ShapeProperties? shapeProperties, ShapeTextFrameInfo textFrame, ShapeTransformInfo transform, AnchorPlacementInfo? anchorPlacement = null)
	{
		var rawName = presetGeom.Preset?.InnerText ?? string.Empty;
		return new DrawingShapeRunElement
		{
			WidthEmu = widthEmu,
			HeightEmu = heightEmu,
			PresetKind = PresetGeometryParser.Parse(rawName),
			RawPresetName = rawName,
			Fill = ShapeFillParser.Parse(shapeProperties),
			Outline = ShapeOutlineParser.Parse(shapeProperties),
			TextFrame = textFrame,
			Transform = transform,
			AnchorPlacement = anchorPlacement ?? AnchorPlacementInfo.None
		};
	}
}
