namespace PanoramicData.Render;

using DocumentFormat.OpenXml;

/// <summary>
/// Parses DrawingML custom geometry elements (<c>a:custGeom</c>) into command sequences.
/// </summary>
internal static class CustomGeometryParser
{
	/// <summary>
	/// Parses custom geometry path commands from a <c>custGeom</c> element.
	/// </summary>
	/// <param name="customGeometry">The custom geometry element.</param>
	/// <returns>Parsed command list. Returns empty when no path commands are present.</returns>
	public static IReadOnlyList<CustomGeometryCommand> Parse(OpenXmlElement customGeometry)
	{
		ArgumentNullException.ThrowIfNull(customGeometry);

		var commands = new List<CustomGeometryCommand>();
		var pathElements = customGeometry
			.Descendants()
			.Where(e => e.LocalName == "path");

		foreach (var path in pathElements)
		{
			foreach (var commandElement in path.ChildElements)
			{
				if (commandElement.LocalName == "moveTo")
				{
					commands.Add(new CustomGeometryCommand(
						CustomGeometryCommandKind.MoveTo,
						ParsePoints(commandElement)));
					continue;
				}

				if (commandElement.LocalName == "lnTo")
				{
					commands.Add(new CustomGeometryCommand(
						CustomGeometryCommandKind.LineTo,
						ParsePoints(commandElement)));
					continue;
				}

				if (commandElement.LocalName == "cubicBezTo")
				{
					commands.Add(new CustomGeometryCommand(
						CustomGeometryCommandKind.CubicBezierTo,
						ParsePoints(commandElement)));
					continue;
				}

				if (commandElement.LocalName == "arcTo")
				{
					commands.Add(ParseArc(commandElement));
					continue;
				}

				if (commandElement.LocalName == "close")
				{
					commands.Add(new CustomGeometryCommand(CustomGeometryCommandKind.Close, []));
				}
			}
		}

		return commands;
	}

	private static CustomGeometryCommand ParseArc(OpenXmlElement arcElement)
	{
		var attrs = arcElement.GetAttributes();
		return new CustomGeometryCommand(
			CustomGeometryCommandKind.ArcTo,
			[],
			ArcWidthRadius: ParseLongAttribute(attrs, "wR"),
			ArcHeightRadius: ParseLongAttribute(attrs, "hR"),
			ArcStartAngle: ParseIntAttribute(attrs, "stAng"),
			ArcSweepAngle: ParseIntAttribute(attrs, "swAng"));
	}

	private static IReadOnlyList<CustomGeometryPoint> ParsePoints(OpenXmlElement commandElement)
	{
		var points = new List<CustomGeometryPoint>();
		foreach (var point in commandElement.Descendants().Where(e => e.LocalName == "pt"))
		{
			var attrs = point.GetAttributes();
			points.Add(new CustomGeometryPoint(
				ParseLongAttribute(attrs, "x"),
				ParseLongAttribute(attrs, "y")));
		}

		return points;
	}

	private static long ParseLongAttribute(IList<OpenXmlAttribute> attributes, string localName)
	{
		for (var i = 0; i < attributes.Count; i++)
		{
			if (attributes[i].LocalName == localName && long.TryParse(attributes[i].Value, out var parsed))
			{
				return parsed;
			}
		}

		return 0;
	}

	private static int ParseIntAttribute(IList<OpenXmlAttribute> attributes, string localName)
	{
		for (var i = 0; i < attributes.Count; i++)
		{
			if (attributes[i].LocalName == localName && int.TryParse(attributes[i].Value, out var parsed))
			{
				return parsed;
			}
		}

		return 0;
	}
}
