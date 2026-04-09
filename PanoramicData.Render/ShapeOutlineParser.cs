namespace PanoramicData.Render;

using A = DocumentFormat.OpenXml.Drawing;

/// <summary>
/// Parses DrawingML shape outline definitions.
/// </summary>
internal static class ShapeOutlineParser
{
	/// <summary>
	/// Parses outline information from shape properties.
	/// </summary>
	/// <param name="shapeProperties">The shape properties element.</param>
	/// <returns>Parsed outline information.</returns>
	public static ShapeOutlineInfo Parse(A.ShapeProperties? shapeProperties)
	{
		if (shapeProperties is null)
		{
			return ShapeOutlineInfo.None;
		}

		var line = shapeProperties.ChildElements.FirstOrDefault(e => e.LocalName == "ln");
		if (line is null)
		{
			return ShapeOutlineInfo.None;
		}

		return new ShapeOutlineInfo
		{
			HasOutline = true,
			WidthEmu = ParseLongAttribute(line, "w"),
			ColorHex = ParseColorHex(line),
			DashStyle = ParseDashStyle(line),
			JoinStyle = ParseJoinStyle(line)
		};
	}

	private static long ParseLongAttribute(DocumentFormat.OpenXml.OpenXmlElement element, string localName)
	{
		var attrs = element.GetAttributes();
		for (var i = 0; i < attrs.Count; i++)
		{
			if (attrs[i].LocalName == localName && long.TryParse(attrs[i].Value, out var parsed))
			{
				return parsed;
			}
		}

		return 0;
	}

	private static string? ParseColorHex(DocumentFormat.OpenXml.OpenXmlElement element)
	{
		var color = element.Descendants().FirstOrDefault(e => e.LocalName == "srgbClr");
		if (color is null)
		{
			return null;
		}

		var attrs = color.GetAttributes();
		for (var i = 0; i < attrs.Count; i++)
		{
			if (attrs[i].LocalName == "val")
			{
				return attrs[i].Value;
			}
		}

		return null;
	}

	private static string? ParseDashStyle(DocumentFormat.OpenXml.OpenXmlElement element)
	{
		var dash = element.ChildElements.FirstOrDefault(e => e.LocalName == "prstDash");
		if (dash is null)
		{
			return null;
		}

		var attrs = dash.GetAttributes();
		for (var i = 0; i < attrs.Count; i++)
		{
			if (attrs[i].LocalName == "val")
			{
				return attrs[i].Value;
			}
		}

		return null;
	}

	private static ShapeLineJoinKind ParseJoinStyle(DocumentFormat.OpenXml.OpenXmlElement element)
	{
		if (element.ChildElements.Any(e => e.LocalName == "miter"))
		{
			return ShapeLineJoinKind.Miter;
		}

		if (element.ChildElements.Any(e => e.LocalName == "round"))
		{
			return ShapeLineJoinKind.Round;
		}

		if (element.ChildElements.Any(e => e.LocalName == "bevel"))
		{
			return ShapeLineJoinKind.Bevel;
		}

		return ShapeLineJoinKind.None;
	}
}
