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

	private static void MergeProperties(OpenXmlCompositeElement target, OpenXmlCompositeElement? source)
	{
		if (source is null)
		{
			return;
		}

		foreach (var child in source.ChildElements)
		{
			var existing = target.ChildElements.FirstOrDefault(element => element.LocalName == child.LocalName && element.NamespaceUri == child.NamespaceUri);
			existing?.Remove();
			target.Append(child.CloneNode(true));
		}
	}

	private static Run CreateRun(OpenXmlElement child, RunProperties? runProperties)
	{
		var run = new Run();
		if (runProperties is not null)
		{
			run.AppendChild((RunProperties)runProperties.CloneNode(true));
		}

		run.AppendChild(child);
		return run;
	}

	private static string GetParagraphText(Paragraph paragraph)
		=> string.Concat(paragraph.Descendants<Text>().Select(text => text.Text));

	private static string NormalizeWhitespace(string value)
		=> string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
