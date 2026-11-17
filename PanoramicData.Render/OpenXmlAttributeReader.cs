namespace PanoramicData.Render;

using DocumentFormat.OpenXml;

/// <summary>
/// Reads numeric attributes from OpenXML elements by local name.
/// </summary>
internal static class OpenXmlAttributeReader
{
	public static long ParseLong(OpenXmlElement? element, string localName)
		=> element is null ? 0 : ParseLong(element.GetAttributes(), localName);

	public static long ParseLong(IList<OpenXmlAttribute> attributes, string localName)
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
}