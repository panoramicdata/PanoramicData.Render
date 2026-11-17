namespace PanoramicData.Render;

using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class FieldUpdateEngine
{
	private static bool TryGetHeadingLevel(ParagraphBlock paragraphBlock, ParagraphStyleHierarchy paragraphStyles, out int headingLevel)
	{
		if (TryGetOutlineLevel(paragraphBlock.SourceElement.ParagraphProperties, out headingLevel))
		{
			return true;
		}

		if (TryGetHeadingLevel(paragraphBlock.StyleId, paragraphStyles, out headingLevel))
		{
			return true;
		}

		headingLevel = 0;
		return false;
	}

	private static bool TryGetHeadingLevel(string? styleId, ParagraphStyleHierarchy paragraphStyles, out int headingLevel)
	{
		if (TryParseHeadingStyleId(styleId, out headingLevel))
		{
			return true;
		}

		if (string.IsNullOrWhiteSpace(styleId))
		{
			headingLevel = 0;
			return false;
		}

		foreach (var inheritedStyleId in paragraphStyles.GetInheritanceChain(styleId))
		{
			if (TryParseHeadingStyleId(inheritedStyleId, out headingLevel))
			{
				return true;
			}

			if (paragraphStyles.Styles.TryGetValue(inheritedStyleId, out var styleInfo)
				&& TryGetOutlineLevel(styleInfo.Properties, out headingLevel))
			{
				return true;
			}
		}

		headingLevel = 0;
		return false;
	}

	private static bool TryGetOutlineLevel(OpenXmlCompositeElement? properties, out int headingLevel)
	{
		var outlineLevel = properties?.Elements<OutlineLevel>().FirstOrDefault()?.Val?.Value;
		if (outlineLevel is int zeroBasedLevel && zeroBasedLevel is >= 0 and <= 8)
		{
			headingLevel = zeroBasedLevel + 1;
			return true;
		}

		headingLevel = 0;
		return false;
	}

	private static bool TryParseHeadingStyleId(string? styleId, out int headingLevel)
	{
		if (!string.IsNullOrWhiteSpace(styleId)
			&& styleId.StartsWith("Heading", StringComparison.OrdinalIgnoreCase)
			&& int.TryParse(styleId["Heading".Length..], NumberStyles.None, CultureInfo.InvariantCulture, out var parsedLevel)
			&& parsedLevel is >= 1 and <= 9)
		{
			headingLevel = parsedLevel;
			return true;
		}

		headingLevel = 0;
		return false;
	}
}
