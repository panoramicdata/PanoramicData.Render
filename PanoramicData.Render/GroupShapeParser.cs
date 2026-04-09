namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using A = DocumentFormat.OpenXml.Drawing;

/// <summary>
/// Parses DrawingML group shape elements (<c>wpg:wgp</c>) recursively.
/// </summary>
internal static class GroupShapeParser
{
	/// <summary>
	/// Parses a <c>wpg:wgp</c> element into a <see cref="DrawingGroupRunElement"/>.
	/// </summary>
	/// <param name="wgpElement">The group element.</param>
	/// <param name="widthEmu">Width of the group in EMUs (from anchor/inline extent).</param>
	/// <param name="heightEmu">Height of the group in EMUs (from anchor/inline extent).</param>
	/// <returns>Parsed group run element.</returns>
	public static DrawingGroupRunElement Parse(OpenXmlElement wgpElement, long widthEmu, long heightEmu)
	{
		ArgumentNullException.ThrowIfNull(wgpElement);

		// Group-level transform lives inside wpg:grpSpPr/a:xfrm.
		var grpSpPr = wgpElement.ChildElements.FirstOrDefault(e => e.LocalName == "grpSpPr");
		var groupXfrm = grpSpPr?.ChildElements.FirstOrDefault(e => e.LocalName == "xfrm");
		var groupTransform = ParseGroupXfrm(groupXfrm);

		var children = new List<GroupedShapeItem>();
		foreach (var child in wgpElement.ChildElements)
		{
			switch (child.LocalName)
			{
				case "wsp":
					// Word Processing Shape: individual shape child.
					var item = ParseWsp(child);
					if (item is not null)
					{
						children.Add(item);
					}

					break;

				case "wgp":
					// Nested group — recurse.
					var nestedItem = ParseNestedGroup(child);
					if (nestedItem is not null)
					{
						children.Add(nestedItem);
					}

					break;
			}
		}

		return new DrawingGroupRunElement
		{
			WidthEmu = widthEmu,
			HeightEmu = heightEmu,
			GroupTransform = groupTransform,
			Children = children
		};
	}

	private static GroupedShapeItem? ParseWsp(OpenXmlElement wsp)
	{
		var spPr = wsp.ChildElements.FirstOrDefault(e => e.LocalName == "spPr");
		if (spPr is null)
		{
			return null;
		}

		var (offsetX, offsetY, width, height) = ParseChildXfrm(spPr);

		var textFrame = ShapeTextFrameParser.Parse(wsp);
		var transform = ShapeTransformParser.Parse(spPr);
		var typedSpPr = spPr as A.ShapeProperties;

		// Use local-name matching so both typed and unknown XML elements are found.
		var presetGeom = spPr.Descendants().FirstOrDefault(e => e.LocalName == "prstGeom")
			?? spPr.Descendants<A.PresetGeometry>().FirstOrDefault() as OpenXmlElement;
		if (presetGeom is not null)
		{
			// Support both typed A.PresetGeometry and raw unknown elements by reading the "prst" attribute.
			var rawName = (presetGeom as A.PresetGeometry)?.Preset?.InnerText
				?? presetGeom.GetAttributes().FirstOrDefault(a => a.LocalName == "prst").Value
				?? string.Empty;
			return new GroupedShapeItem
			{
				OffsetXEmu = offsetX,
				OffsetYEmu = offsetY,
				WidthEmu = width,
				HeightEmu = height,
				Shape = new DrawingShapeRunElement
				{
					WidthEmu = width,
					HeightEmu = height,
					PresetKind = PresetGeometryParser.Parse(rawName),
					RawPresetName = rawName,
					Fill = ShapeFillParser.Parse(typedSpPr),
					Outline = ShapeOutlineParser.Parse(typedSpPr),
					TextFrame = textFrame,
					Transform = transform
				}
			};
		}

		var customGeom = spPr.Descendants().FirstOrDefault(e => e.LocalName == "custGeom");
		if (customGeom is not null)
		{
			return new GroupedShapeItem
			{
				OffsetXEmu = offsetX,
				OffsetYEmu = offsetY,
				WidthEmu = width,
				HeightEmu = height,
				Shape = new DrawingCustomGeometryRunElement
				{
					WidthEmu = width,
					HeightEmu = height,
					Commands = CustomGeometryParser.Parse(customGeom),
					Fill = ShapeFillParser.Parse(typedSpPr),
					Outline = ShapeOutlineParser.Parse(typedSpPr),
					TextFrame = textFrame,
					Transform = transform
				}
			};
		}

		return null;
	}

	private static GroupedShapeItem? ParseNestedGroup(OpenXmlElement wgp)
	{
		var grpSpPr = wgp.ChildElements.FirstOrDefault(e => e.LocalName == "grpSpPr");
		var (offsetX, offsetY, width, height) = ParseChildXfrm(grpSpPr);
		var nested = Parse(wgp, width, height);
		return new GroupedShapeItem
		{
			OffsetXEmu = offsetX,
			OffsetYEmu = offsetY,
			WidthEmu = width,
			HeightEmu = height,
			Shape = nested
		};
	}

	private static ShapeTransformInfo ParseGroupXfrm(OpenXmlElement? xfrm)
	{
		if (xfrm is null)
		{
			return ShapeTransformInfo.None;
		}

		return ShapeTransformParser.Parse(xfrm.Parent);
	}

	private static (long offsetX, long offsetY, long width, long height) ParseChildXfrm(OpenXmlElement? container)
	{
		if (container is null)
		{
			return (0, 0, 0, 0);
		}

		var xfrm = container.ChildElements.FirstOrDefault(e => e.LocalName == "xfrm");
		if (xfrm is null)
		{
			return (0, 0, 0, 0);
		}

		var off = xfrm.ChildElements.FirstOrDefault(e => e.LocalName == "off");
		var ext = xfrm.ChildElements.FirstOrDefault(e => e.LocalName == "ext");

		return (
			ParseLongAttribute(off, "x"),
			ParseLongAttribute(off, "y"),
			ParseLongAttribute(ext, "cx"),
			ParseLongAttribute(ext, "cy")
		);
	}

	private static long ParseLongAttribute(OpenXmlElement? element, string localName)
	{
		if (element is null)
		{
			return 0;
		}

		var attributes = element.GetAttributes();
		for (var i = 0; i < attributes.Count; i++)
		{
			if (attributes[i].LocalName == localName && long.TryParse(attributes[i].Value, out var parsed))
			{
				return parsed;
			}
		}

		return 0;
	}
}
