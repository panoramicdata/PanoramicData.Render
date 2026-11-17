namespace PanoramicData.Render;

using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class FieldUpdateEngine
{
	private static bool ReplaceTocResultParagraphs(Paragraph fieldParagraph, IReadOnlyList<TocEntry> entries, TocSwitchSet switchSet, ParagraphStyleHierarchy paragraphStyles)
	{
		var existingResultParagraphs = GetExistingTocResultParagraphs(fieldParagraph);
		var desiredResultParagraphs = BuildDesiredTocParagraphs(existingResultParagraphs, entries, switchSet, paragraphStyles);

		if (AreEquivalent(existingResultParagraphs, desiredResultParagraphs))
		{
			return false;
		}

		foreach (var paragraph in existingResultParagraphs)
		{
			paragraph.Remove();
		}

		OpenXmlElement insertionPoint = fieldParagraph;
		foreach (var paragraph in desiredResultParagraphs)
		{
			insertionPoint = insertionPoint.InsertAfterSelf(paragraph);
		}

		return true;
	}

	private static Paragraph[] BuildDesiredTocParagraphs(
		IReadOnlyList<Paragraph> existingResultParagraphs,
		IReadOnlyList<TocEntry> entries,
		TocSwitchSet switchSet,
		ParagraphStyleHierarchy paragraphStyles)
	{
		var templateParagraphs = existingResultParagraphs.Count == 0
			? Array.Empty<Paragraph>()
			: [.. existingResultParagraphs];

		var result = new Paragraph[entries.Count];
		for (var i = 0; i < entries.Count; i++)
		{
			var templateParagraph = templateParagraphs.Length == 0
				? null
				: templateParagraphs[Math.Min(i, templateParagraphs.Length - 1)];
			var styleFormatting = GetTocStyleFormatting(paragraphStyles, entries[i].Level);
			result[i] = CreateTocParagraph(entries[i], switchSet, templateParagraph, styleFormatting);
		}

		return result;
	}

	private static Paragraph[] GetExistingTocResultParagraphs(Paragraph fieldParagraph)
	{
		var resultParagraphs = new List<Paragraph>();
		for (var nextParagraph = fieldParagraph.NextSibling<Paragraph>(); nextParagraph is not null; nextParagraph = nextParagraph.NextSibling<Paragraph>())
		{
			if (!IsTocResultParagraph(nextParagraph))
			{
				break;
			}

			resultParagraphs.Add(nextParagraph);
		}

		return [.. resultParagraphs];
	}

	private static bool IsTocResultParagraph(Paragraph paragraph)
	{
		var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
		return !string.IsNullOrWhiteSpace(styleId)
			&& styleId.StartsWith("TOC", StringComparison.OrdinalIgnoreCase)
			&& int.TryParse(styleId[3..], NumberStyles.None, CultureInfo.InvariantCulture, out var level)
			&& level is >= 1 and <= 9;
	}

	private static bool AreEquivalent(IReadOnlyList<Paragraph> existingParagraphs, IReadOnlyList<Paragraph> desiredParagraphs)
	{
		if (existingParagraphs.Count != desiredParagraphs.Count)
		{
			return false;
		}

		for (var i = 0; i < existingParagraphs.Count; i++)
		{
			if (!AreParagraphsEquivalent(existingParagraphs[i], desiredParagraphs[i]))
			{
				return false;
			}
		}

		return true;
	}
}
