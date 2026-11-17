namespace PanoramicData.Render;

using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class FieldUpdateEngine
{
	private static RunProperties? FirstTabRunProperties(Paragraph? paragraph)
		=> paragraph?.Descendants<Run>().FirstOrDefault(static run => run.Elements<TabChar>().Any())?.RunProperties;

	private static void ScanTemplateRun(Run run, TemplateRunScan scan, RunProperties? styleRunProperties)
	{
		if (run.Elements<Text>().Any())
		{
			if (!scan.EncounteredSeparator && !scan.HasEntry)
			{
				scan.Entry = MergeRunProperties(styleRunProperties, run.RunProperties);
				scan.HasEntry = true;
			}
			else if (scan.EncounteredSeparator && !scan.HasPageNumber)
			{
				scan.PageNumber = MergeRunProperties(styleRunProperties, run.RunProperties);
				scan.HasPageNumber = true;
			}
		}

		if (!run.Elements<TabChar>().Any())
		{
			return;
		}

		if (!scan.HasSeparator)
		{
			scan.Separator = MergeRunProperties(styleRunProperties, run.RunProperties);
			scan.HasSeparator = true;
		}

		scan.EncounteredSeparator = true;
	}

	private sealed class TemplateRunScan(RunProperties? styleRunProperties)
	{
		public RunProperties? Entry { get; set; } = CloneRunProperties(styleRunProperties);

		public RunProperties? Separator { get; set; } = CloneRunProperties(styleRunProperties);

		public RunProperties? PageNumber { get; set; } = CloneRunProperties(styleRunProperties);

		public bool HasEntry { get; set; }

		public bool HasSeparator { get; set; }

		public bool HasPageNumber { get; set; }

		public bool EncounteredSeparator { get; set; }
	}
	private static TocStyleFormatting GetTocStyleFormatting(ParagraphStyleHierarchy paragraphStyles, int level)
	{
		var styleId = $"TOC{level}";
		var styleChain = paragraphStyles.GetInheritanceChain(styleId);
		if (styleChain.Count == 0)
		{
			return new TocStyleFormatting(null, null);
		}

		var paragraphProperties = new ParagraphProperties();
		var runProperties = new RunProperties();
		var hasParagraphProperties = false;
		var hasRunProperties = false;

		foreach (var inheritedStyleId in styleChain.Reverse())
		{
			if (!paragraphStyles.Styles.TryGetValue(inheritedStyleId, out var style))
			{
				continue;
			}

			MergeProperties(paragraphProperties, style.Properties);
			hasParagraphProperties |= style.Properties.ChildElements.Count > 0;

			MergeProperties(runProperties, style.RunProperties);
			hasRunProperties |= style.RunProperties?.ChildElements.Count > 0;
		}

		return new TocStyleFormatting(
			hasParagraphProperties ? paragraphProperties : null,
			hasRunProperties ? runProperties : null);
	}

	private static RunProperties? CloneRunProperties(RunProperties? runProperties)
		=> runProperties is null ? null : (RunProperties)runProperties.CloneNode(true);

	private static RunProperties? MergeRunProperties(RunProperties? baseRunProperties, OpenXmlCompositeElement? overrideRunProperties)
	{
		if (baseRunProperties is null && overrideRunProperties is null)
		{
			return null;
		}

		var merged = baseRunProperties is null
			? new RunProperties()
			: (RunProperties)baseRunProperties.CloneNode(true);
		MergeProperties(merged, overrideRunProperties);
		return merged;
	}
}
