namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using OoxmlSectionProperties = DocumentFormat.OpenXml.Wordprocessing.SectionProperties;

internal static partial class SectionInfoParser
{
	private static IReadOnlyList<SectionColumnDefinition> ParseColumnDefinitions(Columns? columns)
	{
		if (columns is null)
		{
			return [];
		}

		var result = new List<SectionColumnDefinition>();
		foreach (var column in columns.Elements<Column>())
		{
			result.Add(new SectionColumnDefinition(
				WidthTwips: ParseInteger(column.Width?.Value),
				SpaceAfterTwips: ParseInteger(column.Space?.Value)));
		}

		return result;
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
}
