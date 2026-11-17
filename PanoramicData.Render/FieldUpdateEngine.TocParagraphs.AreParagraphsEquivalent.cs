namespace PanoramicData.Render;

using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class FieldUpdateEngine
{
	private static bool AreParagraphsEquivalent(Paragraph existing, Paragraph desired)
		=> string.Equals(GetParagraphStyleId(existing), GetParagraphStyleId(desired), StringComparison.Ordinal)
			&& string.Equals(GetParagraphText(existing), GetParagraphText(desired), StringComparison.Ordinal)
			&& GetParagraphHyperlinkAnchors(existing).SequenceEqual(GetParagraphHyperlinkAnchors(desired), StringComparer.Ordinal)
			&& GetParagraphTabCharCount(existing) == GetParagraphTabCharCount(desired)
			&& string.Equals(GetParagraphTabsSignature(existing), GetParagraphTabsSignature(desired), StringComparison.Ordinal)
			&& GetParagraphRunPropertiesSignature(existing).SequenceEqual(GetParagraphRunPropertiesSignature(desired), StringComparer.Ordinal);

	private static string? GetParagraphStyleId(Paragraph paragraph)
		=> paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
	private static string[] GetParagraphHyperlinkAnchors(Paragraph paragraph)
		=> [.. paragraph.Descendants<Hyperlink>()
			.Select(hyperlink => hyperlink.Anchor?.Value ?? string.Empty)];

	private static int GetParagraphTabCharCount(Paragraph paragraph)
		=> paragraph.Descendants<TabChar>().Count();

	private static string GetParagraphTabsSignature(Paragraph paragraph)
		=> paragraph.ParagraphProperties?.Tabs?.OuterXml ?? string.Empty;

	private static string[] GetParagraphRunPropertiesSignature(Paragraph paragraph)
		=> [.. paragraph.Descendants<Run>()
			.Select(run => run.RunProperties?.OuterXml ?? string.Empty)];

	private static Paragraph CreateTocParagraph(TocEntry entry, TocSwitchSet switchSet, Paragraph? templateParagraph, TocStyleFormatting styleFormatting)
	{
		var paragraphProperties = CloneTocParagraphProperties(templateParagraph, styleFormatting.ParagraphProperties, entry.Level);
		var contentRuns = CreateTocContentRuns(entry, switchSet, templateParagraph, styleFormatting.RunProperties);

		if (switchSet.UseHyperlinks && !string.IsNullOrWhiteSpace(entry.HyperlinkAnchor))
		{
			return new Paragraph(
				paragraphProperties,
				new Hyperlink(contentRuns)
				{
					Anchor = entry.HyperlinkAnchor
				});
		}

		var paragraph = new Paragraph(paragraphProperties);
		paragraph.Append(contentRuns);
		return paragraph;
	}
}
