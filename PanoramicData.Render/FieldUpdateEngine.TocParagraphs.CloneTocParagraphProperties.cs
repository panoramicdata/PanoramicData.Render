namespace PanoramicData.Render;

using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class FieldUpdateEngine
{
	private static ParagraphProperties CloneTocParagraphProperties(Paragraph? templateParagraph, ParagraphProperties? styleParagraphProperties, int level)
	{
		var properties = styleParagraphProperties is null
			? new ParagraphProperties()
			: (ParagraphProperties)styleParagraphProperties.CloneNode(true);
		MergeProperties(properties, templateParagraph?.ParagraphProperties);

		var styleId = properties.GetFirstChild<ParagraphStyleId>();
		if (styleId is null)
		{
			properties.PrependChild(new ParagraphStyleId { Val = $"TOC{level}" });
		}
		else
		{
			styleId.Val = $"TOC{level}";
		}

		return properties;
	}

	private static OpenXmlElement[] CreateTocContentRuns(TocEntry entry, TocSwitchSet switchSet, Paragraph? templateParagraph, RunProperties? styleRunProperties)
	{
		var templateRunProperties = GetTemplateRunProperties(templateParagraph, styleRunProperties);
		var contentRuns = new List<OpenXmlElement>
		{
			CreateRun(
				new Text(entry.Text)
				{
					Space = SpaceProcessingModeValues.Preserve
				},
				templateRunProperties.EntryRunProperties)
		};

		if (switchSet.IncludePageNumbers)
		{
			if (string.Equals(switchSet.Separator, "\t", StringComparison.Ordinal))
			{
				contentRuns.Add(CreateRun(new TabChar(), templateRunProperties.SeparatorRunProperties));
			}
			else
			{
				contentRuns.Add(CreateRun(
					new Text(switchSet.Separator)
					{
						Space = SpaceProcessingModeValues.Preserve
					},
					templateRunProperties.SeparatorRunProperties));
			}

			contentRuns.Add(CreateRun(
				new Text(entry.PageNumber.ToString(CultureInfo.InvariantCulture))
				{
					Space = SpaceProcessingModeValues.Preserve
				},
				templateRunProperties.PageNumberRunProperties));
		}

		return [.. contentRuns];
	}

	private static TocTemplateRunProperties GetTemplateRunProperties(Paragraph? templateParagraph, RunProperties? styleRunProperties)
	{
		var scan = new TemplateRunScan(styleRunProperties);
		foreach (var run in templateParagraph?.Descendants<Run>() ?? Enumerable.Empty<Run>())
		{
			ScanTemplateRun(run, scan, styleRunProperties);
		}

		ApplyTemplateFallbacks(scan, templateParagraph, styleRunProperties);
		return new TocTemplateRunProperties(scan.Entry, scan.Separator, scan.PageNumber ?? scan.Entry);
	}

	private static void ApplyTemplateFallbacks(TemplateRunScan scan, Paragraph? templateParagraph, RunProperties? styleRunProperties)
	{
		if (!scan.HasEntry)
		{
			scan.Entry = MergeRunProperties(styleRunProperties, FirstRunProperties(templateParagraph));
		}

		if (!scan.HasPageNumber)
		{
			scan.PageNumber = MergeRunProperties(styleRunProperties, LastTextRunProperties(templateParagraph));
		}

		if (!scan.HasSeparator)
		{
			scan.Separator = MergeRunProperties(styleRunProperties, FirstTabRunProperties(templateParagraph))
				?? scan.PageNumber
				?? scan.Entry;
		}
	}
	private static RunProperties? FirstRunProperties(Paragraph? paragraph)
		=> paragraph?.Descendants<Run>().FirstOrDefault()?.RunProperties;

	private static RunProperties? LastTextRunProperties(Paragraph? paragraph)
		=> paragraph?.Descendants<Run>().LastOrDefault(static run => run.Elements<Text>().Any())?.RunProperties;
}
