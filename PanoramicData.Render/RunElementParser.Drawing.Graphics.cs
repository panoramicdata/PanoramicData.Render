namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using A = DocumentFormat.OpenXml.Drawing;

internal static partial class RunElementParser
{
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
