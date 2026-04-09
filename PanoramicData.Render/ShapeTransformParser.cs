namespace PanoramicData.Render;

using DocumentFormat.OpenXml;

/// <summary>
/// Parses DrawingML shape transform metadata.
/// </summary>
internal static class ShapeTransformParser
{
	/// <summary>
	/// Parses transform data from shape properties.
	/// </summary>
	/// <param name="shapeProperties">Shape properties element.</param>
	/// <returns>Parsed transform metadata.</returns>
	public static ShapeTransformInfo Parse(OpenXmlElement? shapeProperties)
	{
		if (shapeProperties is null)
		{
			return ShapeTransformInfo.None;
		}

		var transform = shapeProperties.ChildElements.FirstOrDefault(e => e.LocalName == "xfrm");
		if (transform is null)
		{
			return ShapeTransformInfo.None;
		}

		return new ShapeTransformInfo
		{
			HasTransform = true,
			RotationAngle60000 = ParseIntAttribute(transform, "rot"),
			FlipHorizontal = ParseBooleanAttribute(transform, "flipH"),
			FlipVertical = ParseBooleanAttribute(transform, "flipV")
		};
	}

	private static int ParseIntAttribute(OpenXmlElement element, string localName)
	{
		var attributes = element.GetAttributes();
		for (var i = 0; i < attributes.Count; i++)
		{
			if (attributes[i].LocalName == localName && int.TryParse(attributes[i].Value, out var parsed))
			{
				return parsed;
			}
		}

		return 0;
	}

	private static bool ParseBooleanAttribute(OpenXmlElement element, string localName)
	{
		var attributes = element.GetAttributes();
		for (var i = 0; i < attributes.Count; i++)
		{
			if (attributes[i].LocalName == localName)
			{
				if (attributes[i].Value == "1" || attributes[i].Value == "true")
				{
					return true;
				}

				if (attributes[i].Value == "0" || attributes[i].Value == "false")
				{
					return false;
				}
			}
		}

		return false;
	}
}
