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
			var extent = inline.Extent;
			var inlineShapeProperties = inline.Descendants<A.ShapeProperties>().FirstOrDefault();
			var inlineTextFrame = ShapeTextFrameParser.Parse(inline);
			var inlineTransform = ShapeTransformParser.Parse(inlineShapeProperties);

			// Check for grouped shapes (wpg:wgp) first.
			var inlineWgp = inline.Descendants().FirstOrDefault(e => e.LocalName == "wgp");
			if (inlineWgp is not null)
			{
				elements.Add(GroupShapeParser.Parse(inlineWgp, extent?.Cx ?? 0, extent?.Cy ?? 0));
				return;
			}

			// Check for DrawingML shape (a:prstGeom) before image blip.
			var presetGeom = inline.Descendants<A.PresetGeometry>().FirstOrDefault();
			if (presetGeom is not null)
			{
				elements.Add(ParseDrawingShape(extent?.Cx ?? 0, extent?.Cy ?? 0, presetGeom, inlineShapeProperties, inlineTextFrame, inlineTransform));
				return;
			}

			var customGeom = inline.Descendants().FirstOrDefault(e => e.LocalName == "custGeom");
			if (customGeom is not null)
			{
				elements.Add(new DrawingCustomGeometryRunElement
				{
					WidthEmu = extent?.Cx ?? 0,
					HeightEmu = extent?.Cy ?? 0,
					Commands = CustomGeometryParser.Parse(customGeom),
					Fill = ShapeFillParser.Parse(inlineShapeProperties),
					Outline = ShapeOutlineParser.Parse(inlineShapeProperties),
					TextFrame = inlineTextFrame,
					Transform = inlineTransform,
					AnchorPlacement = AnchorPlacementInfo.None
				});
				return;
			}

			// Check for chart reference (c:chart element).
			var inlineChart = inline.Descendants().FirstOrDefault(e => e.LocalName == "chart");
			if (inlineChart is not null)
			{
				var chartRelId = inlineChart.GetAttributes().FirstOrDefault(a => a.LocalName == "id").Value ?? string.Empty;
				elements.Add(new ChartRunElement
				{
					RelationshipId = chartRelId,
					WidthEmu = extent?.Cx ?? 0,
					HeightEmu = extent?.Cy ?? 0
				});
				return;
			}

			// Check for SmartArt (dgm:relIds element).
			var inlineRelIds = inline.Descendants().FirstOrDefault(e => e.LocalName == "relIds");
			if (inlineRelIds is not null)
			{
				var smartArtRelId = inlineRelIds.GetAttributes().FirstOrDefault(a => a.LocalName == "dm").Value ?? string.Empty;
				var hasFallback = inline.Descendants<A.ShapeProperties>().Any();
				elements.Add(new SmartArtRunElement
				{
					RelationshipId = smartArtRelId,
					WidthEmu = extent?.Cx ?? 0,
					HeightEmu = extent?.Cy ?? 0,
					HasFallback = hasFallback
				});
				return;
			}

			var blip = inline.Descendants<A.Blip>().FirstOrDefault();
			var sourceRectangle = inline.Descendants<A.SourceRectangle>().FirstOrDefault();

			elements.Add(new InlineImageRunElement
			{
				RelationshipId = blip?.Embed?.Value ?? string.Empty,
				WidthEmu = extent?.Cx ?? 0,
				HeightEmu = extent?.Cy ?? 0,
				CropLeft = ParsePercentage(sourceRectangle?.Left),
				CropTop = ParsePercentage(sourceRectangle?.Top),
				CropRight = ParsePercentage(sourceRectangle?.Right),
				CropBottom = ParsePercentage(sourceRectangle?.Bottom)
			});
			return;
		}

		var anchor = drawing.GetFirstChild<DW.Anchor>();
		if (anchor is null)
		{
			return;
		}

		var anchorExtent = anchor.Extent;
		var anchorShapeProperties = anchor.Descendants<A.ShapeProperties>().FirstOrDefault();
		var anchorTextFrame = ShapeTextFrameParser.Parse(anchor);
		var anchorTransform = ShapeTransformParser.Parse(anchorShapeProperties);
		var anchorPlacement = ParseAnchorPlacement(anchor);

		// Check for grouped shapes (wpg:wgp) first.
		var anchorWgp = anchor.Descendants().FirstOrDefault(e => e.LocalName == "wgp");
		if (anchorWgp is not null)
		{
			elements.Add(GroupShapeParser.Parse(anchorWgp, anchorExtent?.Cx ?? 0, anchorExtent?.Cy ?? 0));
			return;
		}

		// Check for DrawingML shape in anchor before image blip.
		var anchorPresetGeom = anchor.Descendants<A.PresetGeometry>().FirstOrDefault();
		if (anchorPresetGeom is not null)
		{
			elements.Add(ParseDrawingShape(anchorExtent?.Cx ?? 0, anchorExtent?.Cy ?? 0, anchorPresetGeom, anchorShapeProperties, anchorTextFrame, anchorTransform, anchorPlacement));
			return;
		}

		var anchorCustomGeom = anchor.Descendants().FirstOrDefault(e => e.LocalName == "custGeom");
		if (anchorCustomGeom is not null)
		{
			elements.Add(new DrawingCustomGeometryRunElement
			{
				WidthEmu = anchorExtent?.Cx ?? 0,
				HeightEmu = anchorExtent?.Cy ?? 0,
				Commands = CustomGeometryParser.Parse(anchorCustomGeom),
				Fill = ShapeFillParser.Parse(anchorShapeProperties),
				Outline = ShapeOutlineParser.Parse(anchorShapeProperties),
				TextFrame = anchorTextFrame,
				Transform = anchorTransform,
				AnchorPlacement = anchorPlacement
			});
			return;
		}

		// Check for chart reference (c:chart element).
		var anchorChart = anchor.Descendants().FirstOrDefault(e => e.LocalName == "chart");
		if (anchorChart is not null)
		{
			var anchorChartRelId = anchorChart.GetAttributes().FirstOrDefault(a => a.LocalName == "id").Value ?? string.Empty;
			elements.Add(new ChartRunElement
			{
				RelationshipId = anchorChartRelId,
				WidthEmu = anchorExtent?.Cx ?? 0,
				HeightEmu = anchorExtent?.Cy ?? 0
			});
			return;
		}

		// Check for SmartArt (dgm:relIds element).
		var anchorRelIds = anchor.Descendants().FirstOrDefault(e => e.LocalName == "relIds");
		if (anchorRelIds is not null)
		{
			var anchorSmartArtRelId = anchorRelIds.GetAttributes().FirstOrDefault(a => a.LocalName == "dm").Value ?? string.Empty;
			var anchorHasFallback = anchor.Descendants<A.ShapeProperties>().Any();
			elements.Add(new SmartArtRunElement
			{
				RelationshipId = anchorSmartArtRelId,
				WidthEmu = anchorExtent?.Cx ?? 0,
				HeightEmu = anchorExtent?.Cy ?? 0,
				HasFallback = anchorHasFallback
			});
			return;
		}

		var anchorBlip = anchor.Descendants<A.Blip>().FirstOrDefault();
		var anchorSourceRectangle = anchor.Descendants<A.SourceRectangle>().FirstOrDefault();
		var horizontalPosition = anchor.GetFirstChild<DW.HorizontalPosition>();
		var verticalPosition = anchor.GetFirstChild<DW.VerticalPosition>();

		elements.Add(new AnchorImageRunElement
		{
			RelationshipId = anchorBlip?.Embed?.Value ?? string.Empty,
			WidthEmu = anchorExtent?.Cx ?? 0,
			HeightEmu = anchorExtent?.Cy ?? 0,
			CropLeft = ParsePercentage(anchorSourceRectangle?.Left),
			CropTop = ParsePercentage(anchorSourceRectangle?.Top),
			CropRight = ParsePercentage(anchorSourceRectangle?.Right),
			CropBottom = ParsePercentage(anchorSourceRectangle?.Bottom),
			HorizontalRelativeFrom = ParseHorizontalRelativeFrom(horizontalPosition?.RelativeFrom?.Value),
			VerticalRelativeFrom = ParseVerticalRelativeFrom(verticalPosition?.RelativeFrom?.Value),
			HorizontalOffsetEmu = ParseOffset(horizontalPosition?.GetFirstChild<DW.PositionOffset>()?.InnerText),
			VerticalOffsetEmu = ParseOffset(verticalPosition?.GetFirstChild<DW.PositionOffset>()?.InnerText),
			HorizontalAlignment = ParseHorizontalAlignment(horizontalPosition?.GetFirstChild<DW.HorizontalAlignment>()?.InnerText),
			VerticalAlignment = ParseVerticalAlignment(verticalPosition?.GetFirstChild<DW.VerticalAlignment>()?.InnerText),
			BehindDocument = ParseOnOffValue(anchor.BehindDoc)
		});
	}

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
