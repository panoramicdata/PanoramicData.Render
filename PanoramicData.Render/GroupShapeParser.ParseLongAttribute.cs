namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using A = DocumentFormat.OpenXml.Drawing;

internal static partial class GroupShapeParser
{
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
