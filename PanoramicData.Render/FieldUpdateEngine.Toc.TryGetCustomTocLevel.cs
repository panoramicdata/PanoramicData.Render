namespace PanoramicData.Render;

using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class FieldUpdateEngine
{
	private static bool TryGetCustomTocLevel(
		string? styleId,
		ParagraphStyleHierarchy paragraphStyles,
		TocSwitchSet switchSet,
		out int headingLevel)
	{
		if (string.IsNullOrWhiteSpace(styleId))
		{
			headingLevel = 0;
			return false;
		}

		if (switchSet.CustomStyleLevels.TryGetValue(styleId, out headingLevel))
		{
			return true;
		}

		if (paragraphStyles.Styles.TryGetValue(styleId, out var styleInfo)
			&& !string.IsNullOrWhiteSpace(styleInfo.Name)
			&& switchSet.CustomStyleLevels.TryGetValue(styleInfo.Name, out headingLevel))
		{
			return true;
		}

		headingLevel = 0;
		return false;
	}

	private static bool IsFigureCaptionParagraph(ParagraphBlock paragraphBlock, ParagraphStyleHierarchy paragraphStyles)
	{
		if (IsCaptionStyle(paragraphBlock.StyleId, paragraphStyles))
		{
			return true;
		}

		return StartsWithSequenceField(paragraphBlock.SourceElement, "Figure");
	}

	private static bool IsCaptionStyle(string? styleId, ParagraphStyleHierarchy paragraphStyles)
	{
		if (string.IsNullOrWhiteSpace(styleId))
		{
			return false;
		}

		if (string.Equals(styleId, "Caption", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		foreach (var inheritedStyleId in paragraphStyles.GetInheritanceChain(styleId))
		{
			if (string.Equals(inheritedStyleId, "Caption", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			if (paragraphStyles.Styles.TryGetValue(inheritedStyleId, out var styleInfo)
				&& string.Equals(styleInfo.Name, "Caption", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	private static bool StartsWithSequenceField(Paragraph paragraph, string sequenceIdentifier)
	{
		foreach (var run in paragraph.Elements<Run>())
		{
			foreach (var child in run.ChildElements)
			{
				if (child is FieldChar)
				{
					continue;
				}

				if (child is FieldCode fieldCode)
				{
					return NormalizeWhitespace(fieldCode.Text).StartsWith($"SEQ {sequenceIdentifier}", StringComparison.OrdinalIgnoreCase);
				}

				if (child is Text text && !string.IsNullOrWhiteSpace(text.Text))
				{
					return false;
				}
			}
		}

		return false;
	}
}
