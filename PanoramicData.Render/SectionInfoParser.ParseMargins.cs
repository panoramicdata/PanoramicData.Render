namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using OoxmlSectionProperties = DocumentFormat.OpenXml.Wordprocessing.SectionProperties;

internal static partial class SectionInfoParser
{
	private static PageMargins ParseMargins(PageMargin? pageMargin)
		=> new(
			ToTwips(pageMargin?.Top, 1440),
			ToTwips(pageMargin?.Right, 1440),
			ToTwips(pageMargin?.Bottom, 1440),
			ToTwips(pageMargin?.Left, 1440),
			ToTwips(pageMargin?.Header, 720),
			ToTwips(pageMargin?.Footer, 720),
			ToTwips(pageMargin?.Gutter, 0));

	private static int ToTwips(Int32Value? value, int fallback) => value?.Value ?? fallback;

	private static int ToTwips(UInt32Value? value, int fallback) => (int?)value?.Value ?? fallback;
	/// <summary>
	/// Extracts all sections from a document body.
	/// Sections are defined by <c>w:sectPr</c> elements in paragraph properties (section breaks)
	/// and the final <c>w:sectPr</c> as a direct child of the body.
	/// </summary>
	/// <param name="body">The document body.</param>
	/// <returns>An ordered list of <see cref="SectionInfo"/> instances.</returns>
	public static IReadOnlyList<SectionInfo> ParseAll(Body body)
	{
		ArgumentNullException.ThrowIfNull(body);

		var sections = new List<SectionInfo>();

		// Section breaks within paragraphs
		foreach (var paragraph in body.Elements<Paragraph>())
		{
			var pPr = paragraph.ParagraphProperties;
			var sectPr = pPr?.GetFirstChild<OoxmlSectionProperties>();
			if (sectPr is not null)
			{
				sections.Add(Parse(sectPr));
			}
		}

		// Final section properties (direct child of body)
		var bodySectPr = body.GetFirstChild<OoxmlSectionProperties>();
		sections.Add(bodySectPr is not null ? Parse(bodySectPr) : new SectionInfo());

		return sections;
	}

	private static int ParseColumnSpacing(Columns? columns)
	{
		if (columns?.Space?.Value is { } space)
		{
			return ParseInteger(space);
		}

		return 720;
	}
}
