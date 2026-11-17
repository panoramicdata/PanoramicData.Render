namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;
using OoxmlSectionProperties = DocumentFormat.OpenXml.Wordprocessing.SectionProperties;
using OoxmlPBdr = DocumentFormat.OpenXml.Wordprocessing.ParagraphBorders;
using OoxmlBorderType = DocumentFormat.OpenXml.Wordprocessing.BorderType;

internal static partial class DocumentBlockParser
{
	private static int? GetNumberingId(NumberingProperties? numPr)
		=> numPr?.NumberingId?.Val?.Value is int numberingId && numberingId > 0 ? numberingId : null;

	private static int GetNumberingLevel(NumberingProperties? numPr)
		=> numPr?.NumberingLevelReference?.Val?.Value ?? 0;
	private static bool IsEnabled(OnOffType? element)
		=> element is { } on && (on.Val is null || on.Val.Value);

	private static BookmarkStartInfo[] ParseBookmarkStarts(Paragraph paragraph)
		=> paragraph.Elements<BookmarkStart>()
			.Where(bs => bs.Id?.Value is not null && bs.Name?.Value is not null)
			.Select(bs => new BookmarkStartInfo(int.Parse(bs.Id!.Value!, System.Globalization.CultureInfo.InvariantCulture), bs.Name!.Value!))
			.ToArray();

	private static BookmarkEndInfo[] ParseBookmarkEnds(Paragraph paragraph)
		=> paragraph.Elements<BookmarkEnd>()
			.Where(be => be.Id?.Value is not null)
			.Select(be => new BookmarkEndInfo(int.Parse(be.Id!.Value!, System.Globalization.CultureInfo.InvariantCulture)))
			.ToArray();
	private static ParagraphBorders ParseParagraphBorders(OoxmlPBdr? pBdr)
	{
		if (pBdr is null)
		{
			return ParagraphBorders.None;
		}

		return new ParagraphBorders(
			Top: ParseBorderEdge(pBdr.TopBorder),
			Bottom: ParseBorderEdge(pBdr.BottomBorder),
			Left: ParseBorderEdge(pBdr.LeftBorder),
			Right: ParseBorderEdge(pBdr.RightBorder),
			Between: ParseBorderEdge(pBdr.BetweenBorder),
			Bar: ParseBorderEdge(pBdr.BarBorder));
	}
}
