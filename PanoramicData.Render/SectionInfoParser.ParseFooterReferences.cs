namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using OoxmlSectionProperties = DocumentFormat.OpenXml.Wordprocessing.SectionProperties;

internal static partial class SectionInfoParser
{
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

	private static LineNumberingInfo? ParseLineNumbering(LineNumberType? lineNumberType)
	{
		if (lineNumberType is null)
		{
			return null;
		}

		return new LineNumberingInfo(
			CountBy: ParseCounter(lineNumberType.CountBy),
			Start: ParseCounter(lineNumberType.Start),
			Restart: ParseLineNumberRestart(lineNumberType.Restart?.Value),
			DistanceTwips: ParseInteger(lineNumberType.Distance?.Value));
	}

	private static int ParseCounter(Int16Value? value) => (int?)value?.Value ?? 1;

	private static LineNumberRestart ParseLineNumberRestart(LineNumberRestartValues? restart)
	{
		if (restart is null)
		{
			return LineNumberRestart.NewPage;
		}

		if (restart.Value == LineNumberRestartValues.NewSection)
		{
			return LineNumberRestart.NewSection;
		}

		if (restart.Value == LineNumberRestartValues.Continuous)
		{
			return LineNumberRestart.Continuous;
		}

		return LineNumberRestart.NewPage;
	}

	private static int ParseInteger(string? value)
	{
		return int.TryParse(value, out var parsed) ? parsed : 0;
	}
}
