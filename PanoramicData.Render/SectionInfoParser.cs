namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;
using OoxmlSectionProperties = DocumentFormat.OpenXml.Wordprocessing.SectionProperties;

/// <summary>
/// Parses OpenXML section properties into <see cref="SectionInfo"/> instances.
/// </summary>
internal static class SectionInfoParser
{
	/// <summary>
	/// Parses a single OpenXML <see cref="OoxmlSectionProperties"/> element into a <see cref="SectionInfo"/>.
	/// </summary>
	/// <param name="sectPr">The OpenXML section properties element.</param>
	/// <returns>A parsed <see cref="SectionInfo"/>.</returns>
	public static SectionInfo Parse(OoxmlSectionProperties sectPr)
	{
		ArgumentNullException.ThrowIfNull(sectPr);

		var pageSize = sectPr.GetFirstChild<PageSize>();
		var pageMargin = sectPr.GetFirstChild<PageMargin>();
		var sectionType = sectPr.GetFirstChild<SectionType>();
		var columns = sectPr.GetFirstChild<Columns>();

		return new SectionInfo
		{
			PageWidth = (int?)pageSize?.Width?.Value ?? 12240,
			PageHeight = (int?)pageSize?.Height?.Value ?? 15840,
			Orientation = ParseOrientation(pageSize),
			MarginTop = pageMargin?.Top?.Value ?? 1440,
			MarginRight = (int?)pageMargin?.Right?.Value ?? 1440,
			MarginBottom = pageMargin?.Bottom?.Value ?? 1440,
			MarginLeft = (int?)pageMargin?.Left?.Value ?? 1440,
			MarginHeader = (int?)pageMargin?.Header?.Value ?? 720,
			MarginFooter = (int?)pageMargin?.Footer?.Value ?? 720,
			MarginGutter = (int?)pageMargin?.Gutter?.Value ?? 0,
			BreakType = ParseBreakType(sectionType),
			ColumnCount = (int?)columns?.ColumnCount?.Value ?? 1,
			HeaderReferences = ParseHeaderReferences(sectPr),
			FooterReferences = ParseFooterReferences(sectPr)
		};
	}

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

	private static PageOrientation ParseOrientation(PageSize? pageSize)
	{
		if (pageSize?.Orient is not null && pageSize.Orient.Value == PageOrientationValues.Landscape)
		{
			return PageOrientation.Landscape;
		}

		return PageOrientation.Portrait;
	}

	private static SectionBreakType ParseBreakType(SectionType? sectionType)
	{
		if (sectionType?.Val?.Value is null)
		{
			return SectionBreakType.NextPage;
		}

		var val = sectionType.Val.Value;
		if (val == SectionMarkValues.Continuous)
		{
			return SectionBreakType.Continuous;
		}

		if (val == SectionMarkValues.EvenPage)
		{
			return SectionBreakType.EvenPage;
		}

		if (val == SectionMarkValues.OddPage)
		{
			return SectionBreakType.OddPage;
		}

		if (val == SectionMarkValues.NextColumn)
		{
			return SectionBreakType.NextColumn;
		}

		return SectionBreakType.NextPage;
	}

	private static List<HeaderFooterReference> ParseHeaderReferences(OoxmlSectionProperties sectPr)
	{
		var refs = new List<HeaderFooterReference>();
		foreach (var headerRef in sectPr.Elements<HeaderReference>())
		{
			refs.Add(new HeaderFooterReference(
				ParseHeaderFooterType(headerRef.Type?.Value),
				headerRef.Id?.Value ?? string.Empty));
		}

		return refs;
	}

	private static List<HeaderFooterReference> ParseFooterReferences(OoxmlSectionProperties sectPr)
	{
		var refs = new List<HeaderFooterReference>();
		foreach (var footerRef in sectPr.Elements<FooterReference>())
		{
			refs.Add(new HeaderFooterReference(
				ParseHeaderFooterType(footerRef.Type?.Value),
				footerRef.Id?.Value ?? string.Empty));
		}

		return refs;
	}

	private static HeaderFooterKind ParseHeaderFooterType(HeaderFooterValues? type)
	{
		if (type is not null && type.Value == HeaderFooterValues.First)
		{
			return HeaderFooterKind.First;
		}

		if (type is not null && type.Value == HeaderFooterValues.Even)
		{
			return HeaderFooterKind.Even;
		}

		return HeaderFooterKind.Default;
	}
}
