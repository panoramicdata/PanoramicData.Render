namespace PanoramicData.Render;

using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Applies opt-in field updates to the in-memory document model.
/// </summary>
internal static class FieldUpdateEngine
{
	public static FieldUpdatePassResult Apply(
		DocxDocument doc,
		IReadOnlyList<DocumentBlock> blocks,
		IReadOnlyList<LayoutPage> pages,
		RenderOptions renderOptions)
	{
		ArgumentNullException.ThrowIfNull(doc);
		ArgumentNullException.ThrowIfNull(blocks);
		ArgumentNullException.ThrowIfNull(pages);
		ArgumentNullException.ThrowIfNull(renderOptions);

		var options = renderOptions.FieldUpdate;
		if (options is null)
		{
			return FieldUpdatePassResult.NoChanges;
		}

		if ((!options.UpdatePageFields && !options.UpdateDocumentProperties && !options.UpdateTableOfContents && !options.UpdateTableOfFigures && !options.UpdateSequenceFields && !options.UpdateCrossReferences) || pages.Count == 0)
		{
			return FieldUpdatePassResult.NoChanges;
		}

		var blockPageMap = BuildBlockPageMap(pages);
		var propertyMap = BuildDocumentPropertyMap(doc, renderOptions);
		var paragraphStyles = ParagraphStyleHierarchyParser.Parse(doc.StylesPart);
		var updatedFields = new HashSet<string>(StringComparer.Ordinal);
		var hasChanges = UpdateSequenceFields(doc.DocumentBody, options, updatedFields);
		hasChanges |= UpdateTableOfContents(doc.DocumentBody, blocks, blockPageMap, paragraphStyles, options, updatedFields);

		var bookmarkPageMap = BuildBookmarkPageMap(blocks, blockPageMap);
		var bookmarkTextMap = BuildBookmarkTextMap(doc.DocumentBody);
		hasChanges |= UpdateCrossReferenceFields(doc.DocumentBody, options, bookmarkPageMap, bookmarkTextMap, updatedFields);

		foreach (var paragraphBlock in blocks.OfType<ParagraphBlock>())
		{
			if (!blockPageMap.TryGetValue(paragraphBlock, out var pageNumber))
			{
				pageNumber = 1;
			}

			hasChanges |= UpdateComplexFields(paragraphBlock.SourceElement, options, propertyMap, pageNumber, pages.Count, updatedFields);
			hasChanges |= UpdateSimpleFields(paragraphBlock.SourceElement, options, propertyMap, pageNumber, pages.Count, updatedFields);
		}

		return new FieldUpdatePassResult(
			hasChanges,
			[.. updatedFields.OrderBy(value => value, StringComparer.Ordinal)]);
	}

	private static Dictionary<DocumentBlock, int> BuildBlockPageMap(IReadOnlyList<LayoutPage> pages)
	{
		var blockPageMap = new Dictionary<DocumentBlock, int>();

		foreach (var page in pages)
		{
			foreach (var block in page.Blocks)
			{
				blockPageMap.TryAdd(block.Block, page.PageNumber);
			}
		}

		return blockPageMap;
	}

	private static bool UpdateTableOfContents(
		Body documentBody,
		IReadOnlyList<DocumentBlock> blocks,
		IReadOnlyDictionary<DocumentBlock, int> blockPageMap,
		ParagraphStyleHierarchy paragraphStyles,
		FieldUpdateOptions options,
		ISet<string> updatedFields)
	{
		if (!options.UpdateTableOfContents && !options.UpdateTableOfFigures)
		{
			return false;
		}

		var hasChanges = false;
		var bookmarkAllocator = new BookmarkAllocator(GetNextBookmarkId(documentBody));

		foreach (var paragraph in documentBody.Elements<Paragraph>().ToArray())
		{
			if (!TryGetTocInstruction(paragraph, out var instruction))
			{
				continue;
			}

			var switchSet = ParseTocSwitchSet(instruction);
			if (switchSet.IsTableOfFigures ? !options.UpdateTableOfFigures : !options.UpdateTableOfContents)
			{
				continue;
			}

			var tocEntryBuildResult = BuildTocEntries(blocks, blockPageMap, paragraphStyles, switchSet, bookmarkAllocator);
			var filteredEntries = tocEntryBuildResult.Entries
				.Where(entry => entry.Level >= switchSet.MinimumLevel && entry.Level <= switchSet.MaximumLevel)
				.ToArray();

			var tocParagraphsChanged = ReplaceTocResultParagraphs(paragraph, filteredEntries, switchSet, paragraphStyles);
			if (!tocEntryBuildResult.HasChanges && !tocParagraphsChanged)
			{
				continue;
			}

			hasChanges = true;
			updatedFields.Add(switchSet.IsTableOfFigures ? "TOF" : "TOC");
		}

		return hasChanges;
	}

	private static TocEntryBuildResult BuildTocEntries(
		IReadOnlyList<DocumentBlock> blocks,
		IReadOnlyDictionary<DocumentBlock, int> blockPageMap,
		ParagraphStyleHierarchy paragraphStyles,
		TocSwitchSet switchSet,
		BookmarkAllocator bookmarkAllocator)
	{
		if (switchSet.IsTableOfFigures)
		{
			return BuildTofEntries(blocks, blockPageMap, paragraphStyles, switchSet, bookmarkAllocator);
		}

		var entries = new List<TocEntry>();
		var hasChanges = false;

		foreach (var paragraphBlock in blocks.OfType<ParagraphBlock>())
		{
			if (!TryGetTocEntryLevel(paragraphBlock, paragraphStyles, switchSet, out var headingLevel))
			{
				continue;
			}

			var displayText = NormalizeWhitespace(string.Concat(paragraphBlock.SourceElement.Descendants<Text>().Select(text => text.Text)));
			if (string.IsNullOrWhiteSpace(displayText))
			{
				continue;
			}

			if (!blockPageMap.TryGetValue(paragraphBlock, out var pageNumber))
			{
				pageNumber = 1;
			}

			var hyperlinkAnchor = (string?)null;
			if (switchSet.UseHyperlinks)
			{
				hyperlinkAnchor = GetOrCreateTocHyperlinkAnchor(paragraphBlock, bookmarkAllocator, out var anchorCreated);
				hasChanges |= anchorCreated;
			}

			entries.Add(new TocEntry(
				headingLevel,
				displayText,
				pageNumber,
				hyperlinkAnchor));
		}

		return new TocEntryBuildResult([.. entries], hasChanges);
	}

	private static TocEntryBuildResult BuildTofEntries(
		IReadOnlyList<DocumentBlock> blocks,
		IReadOnlyDictionary<DocumentBlock, int> blockPageMap,
		ParagraphStyleHierarchy paragraphStyles,
		TocSwitchSet switchSet,
		BookmarkAllocator bookmarkAllocator)
	{
		var entries = new List<TocEntry>();
		var hasChanges = false;

		foreach (var paragraphBlock in blocks.OfType<ParagraphBlock>())
		{
			if (!IsFigureCaptionParagraph(paragraphBlock, paragraphStyles))
			{
				continue;
			}

			var displayText = NormalizeWhitespace(string.Concat(paragraphBlock.SourceElement.Descendants<Text>().Select(text => text.Text)));
			if (string.IsNullOrWhiteSpace(displayText))
			{
				continue;
			}

			if (!blockPageMap.TryGetValue(paragraphBlock, out var pageNumber))
			{
				pageNumber = 1;
			}

			var hyperlinkAnchor = (string?)null;
			if (switchSet.UseHyperlinks)
			{
				hyperlinkAnchor = GetOrCreateTocHyperlinkAnchor(paragraphBlock, bookmarkAllocator, out var anchorCreated);
				hasChanges |= anchorCreated;
			}

			entries.Add(new TocEntry(
				1,
				displayText,
				pageNumber,
				hyperlinkAnchor));
		}

		return new TocEntryBuildResult([.. entries], hasChanges);
	}

	private static int GetNextBookmarkId(Body documentBody)
	{
		var maxBookmarkId = documentBody.Descendants<BookmarkStart>()
			.Select(bookmark => int.TryParse(bookmark.Id?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : 0)
			.Concat(documentBody.Descendants<BookmarkEnd>()
				.Select(bookmark => int.TryParse(bookmark.Id?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : 0))
			.DefaultIfEmpty(0)
			.Max();

		return maxBookmarkId + 1;
	}

	private static string? GetOrCreateTocHyperlinkAnchor(ParagraphBlock paragraphBlock, BookmarkAllocator bookmarkAllocator, out bool created)
	{
		var existingBookmark = GetPreferredBookmarkName(paragraphBlock.SourceElement);
		if (!string.IsNullOrWhiteSpace(existingBookmark))
		{
			created = false;
			return existingBookmark;
		}

		var bookmarkId = bookmarkAllocator.Allocate();
		var bookmarkName = $"_TocGenerated{bookmarkId.ToString(CultureInfo.InvariantCulture)}";
		InsertSyntheticBookmark(paragraphBlock.SourceElement, bookmarkId, bookmarkName);
		created = true;
		return bookmarkName;
	}

	private static string? GetPreferredBookmarkName(Paragraph paragraph)
	{
		var bookmarkStarts = paragraph.Elements<BookmarkStart>()
			.Where(static bookmark => !string.IsNullOrWhiteSpace(bookmark.Name?.Value))
			.ToArray();

		var preferredBookmark = bookmarkStarts.FirstOrDefault(static bookmark => bookmark.Name!.Value!.StartsWith("_Toc", StringComparison.OrdinalIgnoreCase));
		if (!string.IsNullOrWhiteSpace(preferredBookmark?.Name?.Value))
		{
			return preferredBookmark.Name!.Value;
		}

		return bookmarkStarts.FirstOrDefault()?.Name?.Value;
	}

	private static void InsertSyntheticBookmark(Paragraph paragraph, int bookmarkId, string bookmarkName)
	{
		var bookmarkIdValue = bookmarkId.ToString(CultureInfo.InvariantCulture);
		var bookmarkStart = new BookmarkStart
		{
			Id = bookmarkIdValue,
			Name = bookmarkName
		};
		var bookmarkEnd = new BookmarkEnd
		{
			Id = bookmarkIdValue
		};

		var insertIndex = paragraph.ParagraphProperties is null ? 0 : 1;
		paragraph.InsertAt(bookmarkStart, insertIndex);
		paragraph.AppendChild(bookmarkEnd);
	}

	private static bool TryGetTocEntryLevel(
		ParagraphBlock paragraphBlock,
		ParagraphStyleHierarchy paragraphStyles,
		TocSwitchSet switchSet,
		out int headingLevel)
	{
		if (TryGetCustomTocLevel(paragraphBlock.StyleId, paragraphStyles, switchSet, out headingLevel))
		{
			return true;
		}

		return TryGetHeadingLevel(paragraphBlock, paragraphStyles, out headingLevel);
	}

	private static bool TryGetCustomTocLevel(
		string? styleId,
		ParagraphStyleHierarchy paragraphStyles,
		TocSwitchSet switchSet,
		out int headingLevel)
	{
		if (string.IsNullOrWhiteSpace(styleId))
		{
			headingLevel = 0;
			return false;
		}

		if (switchSet.CustomStyleLevels.TryGetValue(styleId, out headingLevel))
		{
			return true;
		}

		if (paragraphStyles.Styles.TryGetValue(styleId, out var styleInfo)
			&& !string.IsNullOrWhiteSpace(styleInfo.Name)
			&& switchSet.CustomStyleLevels.TryGetValue(styleInfo.Name, out headingLevel))
		{
			return true;
		}

		headingLevel = 0;
		return false;
	}

	private static bool IsFigureCaptionParagraph(ParagraphBlock paragraphBlock, ParagraphStyleHierarchy paragraphStyles)
	{
		if (IsCaptionStyle(paragraphBlock.StyleId, paragraphStyles))
		{
			return true;
		}

		return StartsWithSequenceField(paragraphBlock.SourceElement, "Figure");
	}

	private static bool IsCaptionStyle(string? styleId, ParagraphStyleHierarchy paragraphStyles)
	{
		if (string.IsNullOrWhiteSpace(styleId))
		{
			return false;
		}

		if (string.Equals(styleId, "Caption", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		foreach (var inheritedStyleId in paragraphStyles.GetInheritanceChain(styleId))
		{
			if (string.Equals(inheritedStyleId, "Caption", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			if (paragraphStyles.Styles.TryGetValue(inheritedStyleId, out var styleInfo)
				&& string.Equals(styleInfo.Name, "Caption", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	private static bool StartsWithSequenceField(Paragraph paragraph, string sequenceIdentifier)
	{
		foreach (var run in paragraph.Elements<Run>())
		{
			foreach (var child in run.ChildElements)
			{
				switch (child)
				{
					case FieldChar:
						continue;

					case FieldCode fieldCode:
						return NormalizeWhitespace(fieldCode.Text).StartsWith($"SEQ {sequenceIdentifier}", StringComparison.OrdinalIgnoreCase);

					case Text text when !string.IsNullOrWhiteSpace(text.Text):
						return false;
				}
			}
		}

		return false;
	}

	private static bool TryGetHeadingLevel(ParagraphBlock paragraphBlock, ParagraphStyleHierarchy paragraphStyles, out int headingLevel)
	{
		if (TryGetOutlineLevel(paragraphBlock.SourceElement.ParagraphProperties, out headingLevel))
		{
			return true;
		}

		if (TryGetHeadingLevel(paragraphBlock.StyleId, paragraphStyles, out headingLevel))
		{
			return true;
		}

		headingLevel = 0;
		return false;
	}

	private static bool TryGetHeadingLevel(string? styleId, ParagraphStyleHierarchy paragraphStyles, out int headingLevel)
	{
		if (TryParseHeadingStyleId(styleId, out headingLevel))
		{
			return true;
		}

		if (string.IsNullOrWhiteSpace(styleId))
		{
			headingLevel = 0;
			return false;
		}

		foreach (var inheritedStyleId in paragraphStyles.GetInheritanceChain(styleId))
		{
			if (TryParseHeadingStyleId(inheritedStyleId, out headingLevel))
			{
				return true;
			}

			if (paragraphStyles.Styles.TryGetValue(inheritedStyleId, out var styleInfo)
				&& TryGetOutlineLevel(styleInfo.Properties, out headingLevel))
			{
				return true;
			}
		}

		headingLevel = 0;
		return false;
	}

	private static bool TryGetOutlineLevel(OpenXmlCompositeElement? properties, out int headingLevel)
	{
		var outlineLevel = properties?.Elements<OutlineLevel>().FirstOrDefault()?.Val?.Value;
		if (outlineLevel is int zeroBasedLevel && zeroBasedLevel is >= 0 and <= 8)
		{
			headingLevel = zeroBasedLevel + 1;
			return true;
		}

		headingLevel = 0;
		return false;
	}

	private static bool TryParseHeadingStyleId(string? styleId, out int headingLevel)
	{
		if (!string.IsNullOrWhiteSpace(styleId)
			&& styleId.StartsWith("Heading", StringComparison.OrdinalIgnoreCase)
			&& int.TryParse(styleId["Heading".Length..], NumberStyles.None, CultureInfo.InvariantCulture, out var parsedLevel)
			&& parsedLevel is >= 1 and <= 9)
		{
			headingLevel = parsedLevel;
			return true;
		}

		headingLevel = 0;
		return false;
	}

	private static bool TryGetTocInstruction(Paragraph paragraph, out string instruction)
	{
		foreach (var simpleField in paragraph.Descendants<SimpleField>())
		{
			var candidateInstruction = simpleField.Instruction?.Value;
			if (ParseFieldKind(candidateInstruction) is FieldUpdateKind.Toc)
			{
				instruction = candidateInstruction ?? string.Empty;
				return true;
			}
		}

		var activeInstructions = new Stack<StringBuilder>();

		foreach (var run in paragraph.Descendants<Run>())
		{
			foreach (var child in run.ChildElements)
			{
					switch (child)
					{
						case FieldChar fieldChar when fieldChar.FieldCharType?.Value == FieldCharValues.Begin:
						activeInstructions.Push(new StringBuilder());
						break;

					case FieldCode fieldCode when activeInstructions.Count > 0:
						activeInstructions.Peek().Append(fieldCode.Text);
						break;

						case FieldChar fieldChar when activeInstructions.Count > 0 && fieldChar.FieldCharType?.Value == FieldCharValues.Separate:
						var candidateInstruction = activeInstructions.Peek().ToString();
						if (ParseFieldKind(candidateInstruction) is FieldUpdateKind.Toc)
						{
							instruction = candidateInstruction;
							return true;
						}

						break;

						case FieldChar fieldChar when activeInstructions.Count > 0 && fieldChar.FieldCharType?.Value == FieldCharValues.End:
						var completedInstruction = activeInstructions.Pop().ToString();
						if (ParseFieldKind(completedInstruction) is FieldUpdateKind.Toc)
						{
							instruction = completedInstruction;
							return true;
						}

						break;
				}
			}
		}

		instruction = string.Empty;
		return false;
	}

	private static TocSwitchSet ParseTocSwitchSet(string instruction)
	{
		var minimumLevel = 1;
		var maximumLevel = 3;
		if (TryGetQuotedSwitchValue(instruction, 'o', out var outlineLevels))
		{
			var parts = outlineLevels.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length == 2
				&& int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var parsedMinimum)
				&& int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var parsedMaximum)
				&& parsedMinimum <= parsedMaximum)
			{
				minimumLevel = Math.Clamp(parsedMinimum, 1, 9);
				maximumLevel = Math.Clamp(parsedMaximum, minimumLevel, 9);
			}
		}

		var separator = TryGetQuotedSwitchValue(instruction, 'p', out var customSeparator)
			? customSeparator
			: "\t";
		var customStyleLevels = ParseCustomStyleLevels(instruction);
		var isTableOfFigures = TryGetQuotedSwitchValue(instruction, 'f', out _);

		return new TocSwitchSet(
			minimumLevel,
			maximumLevel,
			!HasSwitch(instruction, 'n'),
			HasSwitch(instruction, 'h'),
			separator,
			customStyleLevels,
			isTableOfFigures);
	}

	private static IReadOnlyDictionary<string, int> ParseCustomStyleLevels(string instruction)
	{
		if (!TryGetQuotedSwitchValue(instruction, 't', out var customStyles))
		{
			return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}

		var styleLevels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		var parts = customStyles.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		for (var i = 0; i + 1 < parts.Length; i += 2)
		{
			if (!int.TryParse(parts[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out var level)
				|| level is < 1 or > 9)
			{
				continue;
			}

			styleLevels[parts[i]] = level;
		}

		return styleLevels;
	}

	private static bool HasSwitch(string instruction, char switchName)
		=> instruction.IndexOf($"\\{switchName}", StringComparison.OrdinalIgnoreCase) >= 0;

	private static bool TryGetQuotedSwitchValue(string instruction, char switchName, out string value)
	{
		var markerIndex = instruction.IndexOf($"\\{switchName}", StringComparison.OrdinalIgnoreCase);
		if (markerIndex < 0)
		{
			value = string.Empty;
			return false;
		}

		var valueStart = markerIndex + 2;
		while (valueStart < instruction.Length && char.IsWhiteSpace(instruction[valueStart]))
		{
			valueStart++;
		}

		if (valueStart >= instruction.Length || instruction[valueStart] != '"')
		{
			value = string.Empty;
			return false;
		}

		valueStart++;
		var valueEnd = instruction.IndexOf('"', valueStart);
		if (valueEnd < 0)
		{
			value = string.Empty;
			return false;
		}

		value = instruction[valueStart..valueEnd];
		return true;
	}

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
			var existingStyleId = existingParagraphs[i].ParagraphProperties?.ParagraphStyleId?.Val?.Value;
			var desiredStyleId = desiredParagraphs[i].ParagraphProperties?.ParagraphStyleId?.Val?.Value;
			if (!string.Equals(existingStyleId, desiredStyleId, StringComparison.Ordinal))
			{
				return false;
			}

			if (!string.Equals(GetParagraphText(existingParagraphs[i]), GetParagraphText(desiredParagraphs[i]), StringComparison.Ordinal))
			{
				return false;
			}

			if (!GetParagraphHyperlinkAnchors(existingParagraphs[i]).SequenceEqual(GetParagraphHyperlinkAnchors(desiredParagraphs[i]), StringComparer.Ordinal))
			{
				return false;
			}

			if (GetParagraphTabCharCount(existingParagraphs[i]) != GetParagraphTabCharCount(desiredParagraphs[i]))
			{
				return false;
			}

			if (!string.Equals(GetParagraphTabsSignature(existingParagraphs[i]), GetParagraphTabsSignature(desiredParagraphs[i]), StringComparison.Ordinal))
			{
				return false;
			}

			if (!GetParagraphRunPropertiesSignature(existingParagraphs[i]).SequenceEqual(GetParagraphRunPropertiesSignature(desiredParagraphs[i]), StringComparer.Ordinal))
			{
				return false;
			}
		}

		return true;
	}

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
		RunProperties? entryRunProperties = CloneRunProperties(styleRunProperties);
		RunProperties? separatorRunProperties = CloneRunProperties(styleRunProperties);
		RunProperties? pageNumberRunProperties = CloneRunProperties(styleRunProperties);
		var hasEntryRunProperties = false;
		var hasSeparatorRunProperties = false;
		var hasPageNumberRunProperties = false;
		var encounteredSeparator = false;

		foreach (var run in templateParagraph?.Descendants<Run>() ?? Enumerable.Empty<Run>())
		{
			var hasText = run.Elements<Text>().Any();
			var hasTab = run.Elements<TabChar>().Any();

			if (hasText)
			{
				if (!encounteredSeparator && !hasEntryRunProperties)
				{
					entryRunProperties = MergeRunProperties(styleRunProperties, run.RunProperties);
					hasEntryRunProperties = true;
				}
				else if (encounteredSeparator && !hasPageNumberRunProperties)
				{
					pageNumberRunProperties = MergeRunProperties(styleRunProperties, run.RunProperties);
					hasPageNumberRunProperties = true;
				}
			}

			if (hasTab)
			{
				if (!hasSeparatorRunProperties)
				{
					separatorRunProperties = MergeRunProperties(styleRunProperties, run.RunProperties);
					hasSeparatorRunProperties = true;
				}
				encounteredSeparator = true;
			}
		}

		if (!hasEntryRunProperties)
		{
			entryRunProperties = MergeRunProperties(styleRunProperties, templateParagraph?.Descendants<Run>().FirstOrDefault()?.RunProperties);
		}

		if (!hasPageNumberRunProperties)
		{
			pageNumberRunProperties = MergeRunProperties(styleRunProperties, templateParagraph?.Descendants<Run>().LastOrDefault(static run => run.Elements<Text>().Any())?.RunProperties);
		}

		if (!hasSeparatorRunProperties)
		{
			separatorRunProperties = MergeRunProperties(styleRunProperties, templateParagraph?.Descendants<Run>().FirstOrDefault(static run => run.Elements<TabChar>().Any())?.RunProperties)
			?? pageNumberRunProperties
				?? entryRunProperties;
		}

		return new TocTemplateRunProperties(entryRunProperties, separatorRunProperties, pageNumberRunProperties ?? entryRunProperties);
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

	private static bool UpdateSequenceFields(
		Body documentBody,
		FieldUpdateOptions options,
		ISet<string> updatedFields)
	{
		if (!options.UpdateSequenceFields)
		{
			return false;
		}

		var counters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		var hasChanges = false;

		foreach (var paragraph in documentBody.Descendants<Paragraph>())
		{
			hasChanges |= UpdateSequenceComplexFieldsInParagraph(paragraph, counters, updatedFields);
			hasChanges |= UpdateSequenceSimpleFieldsInParagraph(paragraph, counters, updatedFields);
		}

		return hasChanges;
	}

	private static bool UpdateSequenceComplexFieldsInParagraph(
		Paragraph paragraph,
		Dictionary<string, int> counters,
		ISet<string> updatedFields)
	{
		var activeFields = new Stack<ActiveFieldUpdate>();
		var hasChanges = false;

		foreach (var run in paragraph.Descendants<Run>())
		{
			foreach (var child in run.ChildElements)
			{
				switch (child)
				{
					case FieldCode fieldCode when activeFields.Count > 0 && !activeFields.Peek().IsResultSection:
						activeFields.Peek().InstructionBuilder.Append(fieldCode.Text);
						break;

					case FieldChar fieldChar:
						HandleFieldChar(fieldChar, activeFields);
						break;

					case Text text when activeFields.Count > 0:
						var activeField = activeFields.Peek();
						if (!activeField.IsResultSection)
						{
							break;
						}

						if (!TryParseSeqInstruction(activeField.InstructionBuilder.ToString(), out var identifier, out var resetValue, out var isHidden))
						{
							break;
						}

						var seqValue = ComputeSequenceValue(counters, identifier, resetValue);
						if (!activeField.HasWrittenValue)
						{
							var displayValue = isHidden ? string.Empty : seqValue.ToString(CultureInfo.InvariantCulture);
							if (!string.Equals(text.Text, displayValue, StringComparison.Ordinal))
							{
								text.Text = displayValue;
								hasChanges = true;
								updatedFields.Add("SEQ");
							}

							activeField.HasWrittenValue = true;
						}
						else if (!string.IsNullOrEmpty(text.Text))
						{
							text.Text = string.Empty;
							hasChanges = true;
							updatedFields.Add("SEQ");
						}

						break;
				}
			}
		}

		return hasChanges;
	}

	private static bool UpdateSequenceSimpleFieldsInParagraph(
		Paragraph paragraph,
		Dictionary<string, int> counters,
		ISet<string> updatedFields)
	{
		var hasChanges = false;

		foreach (var simpleField in paragraph.Descendants<SimpleField>())
		{
			var instruction = simpleField.Instruction?.Value;
			if (!TryParseSeqInstruction(instruction, out var identifier, out var resetValue, out var isHidden))
			{
				continue;
			}

			var seqValue = ComputeSequenceValue(counters, identifier, resetValue);
			var displayValue = isHidden ? string.Empty : seqValue.ToString(CultureInfo.InvariantCulture);

			var textNodes = simpleField.Descendants<Text>().ToArray();
			if (textNodes.Length == 0)
			{
				if (!isHidden)
				{
					simpleField.AppendChild(new Run(new Text(displayValue)));
					hasChanges = true;
					updatedFields.Add("SEQ");
				}

				continue;
			}

			if (!string.Equals(textNodes[0].Text, displayValue, StringComparison.Ordinal))
			{
				textNodes[0].Text = displayValue;
				hasChanges = true;
				updatedFields.Add("SEQ");
			}

			for (var i = 1; i < textNodes.Length; i++)
			{
				if (!string.IsNullOrEmpty(textNodes[i].Text))
				{
					textNodes[i].Text = string.Empty;
					hasChanges = true;
					updatedFields.Add("SEQ");
				}
			}
		}

		return hasChanges;
	}

	private static bool TryParseSeqInstruction(
		string? instruction,
		out string identifier,
		out int? resetValue,
		out bool isHidden)
	{
		identifier = string.Empty;
		resetValue = null;
		isHidden = false;

		if (string.IsNullOrWhiteSpace(instruction))
		{
			return false;
		}

		var trimmed = NormalizeWhitespace(instruction);
		var tokens = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);

		if (tokens.Length < 2 || !string.Equals(tokens[0], "SEQ", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		identifier = tokens[1];

		for (var i = 2; i < tokens.Length; i++)
		{
			if (string.Equals(tokens[i], "\\r", StringComparison.Ordinal) && i + 1 < tokens.Length)
			{
				if (int.TryParse(tokens[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var resetVal))
				{
					resetValue = resetVal;
				}

				i++;
			}
			else if (string.Equals(tokens[i], "\\h", StringComparison.Ordinal))
			{
				isHidden = true;
			}
		}

		return true;
	}

	private static int ComputeSequenceValue(
		Dictionary<string, int> counters,
		string identifier,
		int? resetValue)
	{
		if (resetValue.HasValue)
		{
			counters[identifier] = resetValue.Value;
			return resetValue.Value;
		}

		if (!counters.TryGetValue(identifier, out var current))
		{
			current = 0;
		}

		current++;
		counters[identifier] = current;
		return current;
	}

	private static Dictionary<string, int> BuildBookmarkPageMap(
		IReadOnlyList<DocumentBlock> blocks,
		IReadOnlyDictionary<DocumentBlock, int> blockPageMap)
	{
		var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		foreach (var paragraphBlock in blocks.OfType<ParagraphBlock>())
		{
			if (!blockPageMap.TryGetValue(paragraphBlock, out var pageNumber))
			{
				continue;
			}

			foreach (var bookmarkStart in paragraphBlock.SourceElement.Elements<BookmarkStart>())
			{
				var name = bookmarkStart.Name?.Value;
				if (!string.IsNullOrWhiteSpace(name))
				{
					map.TryAdd(name, pageNumber);
				}
			}
		}

		return map;
	}

	private static Dictionary<string, string> BuildBookmarkTextMap(Body documentBody)
	{
		var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		foreach (var paragraph in documentBody.Descendants<Paragraph>())
		{
			foreach (var bookmarkStart in paragraph.Elements<BookmarkStart>())
			{
				var name = bookmarkStart.Name?.Value;
				var id = bookmarkStart.Id?.Value;
				if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(id))
				{
					continue;
				}

				var textBuilder = new StringBuilder();
				var sibling = bookmarkStart.NextSibling();
				while (sibling is not null)
				{
					if (sibling is BookmarkEnd end && string.Equals(end.Id?.Value, id, StringComparison.Ordinal))
					{
						break;
					}

					if (sibling is Run run)
					{
						foreach (var text in run.Descendants<Text>())
						{
							textBuilder.Append(text.Text);
						}
					}

					sibling = sibling.NextSibling();
				}

				map.TryAdd(name, textBuilder.ToString());
			}
		}

		return map;
	}

	private static bool UpdateCrossReferenceFields(
		Body documentBody,
		FieldUpdateOptions options,
		IReadOnlyDictionary<string, int> bookmarkPageMap,
		IReadOnlyDictionary<string, string> bookmarkTextMap,
		ISet<string> updatedFields)
	{
		if (!options.UpdateCrossReferences)
		{
			return false;
		}

		var hasChanges = false;

		foreach (var paragraph in documentBody.Descendants<Paragraph>())
		{
			hasChanges |= UpdateCrossReferenceComplexFields(paragraph, bookmarkPageMap, bookmarkTextMap, updatedFields);
			hasChanges |= UpdateCrossReferenceSimpleFields(paragraph, bookmarkPageMap, bookmarkTextMap, updatedFields);
		}

		return hasChanges;
	}

	private static bool UpdateCrossReferenceComplexFields(
		Paragraph paragraph,
		IReadOnlyDictionary<string, int> bookmarkPageMap,
		IReadOnlyDictionary<string, string> bookmarkTextMap,
		ISet<string> updatedFields)
	{
		var activeFields = new Stack<ActiveFieldUpdate>();
		var hasChanges = false;

		foreach (var run in paragraph.Descendants<Run>())
		{
			foreach (var child in run.ChildElements)
			{
				switch (child)
				{
					case FieldCode fieldCode when activeFields.Count > 0 && !activeFields.Peek().IsResultSection:
						activeFields.Peek().InstructionBuilder.Append(fieldCode.Text);
						break;

					case FieldChar fieldChar:
						HandleFieldChar(fieldChar, activeFields);
						break;

					case Text text when activeFields.Count > 0:
						var activeField = activeFields.Peek();
						if (!activeField.IsResultSection)
						{
							break;
						}

						if (!TryComputeCrossReferenceValue(activeField.InstructionBuilder.ToString(), bookmarkPageMap, bookmarkTextMap, out var computedValue, out var fieldName))
						{
							break;
						}

						if (!activeField.HasWrittenValue)
						{
							if (!string.Equals(text.Text, computedValue, StringComparison.Ordinal))
							{
								text.Text = computedValue;
								hasChanges = true;
								updatedFields.Add(fieldName);
							}

							activeField.HasWrittenValue = true;
						}
						else if (!string.IsNullOrEmpty(text.Text))
						{
							text.Text = string.Empty;
							hasChanges = true;
							updatedFields.Add(fieldName);
						}

						break;
				}
			}
		}

		return hasChanges;
	}

	private static bool UpdateCrossReferenceSimpleFields(
		Paragraph paragraph,
		IReadOnlyDictionary<string, int> bookmarkPageMap,
		IReadOnlyDictionary<string, string> bookmarkTextMap,
		ISet<string> updatedFields)
	{
		var hasChanges = false;

		foreach (var simpleField in paragraph.Descendants<SimpleField>())
		{
			if (!TryComputeCrossReferenceValue(simpleField.Instruction?.Value, bookmarkPageMap, bookmarkTextMap, out var computedValue, out var fieldName))
			{
				continue;
			}

			var textNodes = simpleField.Descendants<Text>().ToArray();
			if (textNodes.Length == 0)
			{
				simpleField.AppendChild(new Run(new Text(computedValue)));
				hasChanges = true;
				updatedFields.Add(fieldName);
				continue;
			}

			if (!string.Equals(textNodes[0].Text, computedValue, StringComparison.Ordinal))
			{
				textNodes[0].Text = computedValue;
				hasChanges = true;
				updatedFields.Add(fieldName);
			}

			for (var i = 1; i < textNodes.Length; i++)
			{
				if (!string.IsNullOrEmpty(textNodes[i].Text))
				{
					textNodes[i].Text = string.Empty;
					hasChanges = true;
					updatedFields.Add(fieldName);
				}
			}
		}

		return hasChanges;
	}

	private static bool TryComputeCrossReferenceValue(
		string? instruction,
		IReadOnlyDictionary<string, int> bookmarkPageMap,
		IReadOnlyDictionary<string, string> bookmarkTextMap,
		out string computedValue,
		out string fieldName)
	{
		computedValue = string.Empty;
		fieldName = string.Empty;

		if (string.IsNullOrWhiteSpace(instruction))
		{
			return false;
		}

		var trimmed = NormalizeWhitespace(instruction);
		var tokens = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);

		if (tokens.Length < 2)
		{
			return false;
		}

		var fieldType = tokens[0].ToUpperInvariant();
		var bookmarkName = tokens[1];

		if (string.Equals(fieldType, "PAGEREF", StringComparison.Ordinal))
		{
			fieldName = "PAGEREF";
			if (bookmarkPageMap.TryGetValue(bookmarkName, out var pageNumber))
			{
				computedValue = pageNumber.ToString(CultureInfo.InvariantCulture);
				return true;
			}

			return false;
		}

		if (string.Equals(fieldType, "REF", StringComparison.Ordinal))
		{
			fieldName = "REF";
			if (bookmarkTextMap.TryGetValue(bookmarkName, out var text))
			{
				computedValue = text;
				return true;
			}

			return false;
		}

		return false;
	}

	private static bool UpdateComplexFields(
		Paragraph paragraph,
		FieldUpdateOptions options,
		DocumentPropertyMap propertyMap,
		int currentPageNumber,
		int totalPageCount,
		ISet<string> updatedFields)
	{
		var activeFields = new Stack<ActiveFieldUpdate>();
		var hasChanges = false;

		foreach (var run in paragraph.Descendants<Run>())
		{
			foreach (var child in run.ChildElements)
			{
				switch (child)
				{
					case FieldCode fieldCode when activeFields.Count > 0 && !activeFields.Peek().IsResultSection:
						activeFields.Peek().InstructionBuilder.Append(fieldCode.Text);
						break;

					case FieldChar fieldChar:
						HandleFieldChar(fieldChar, activeFields);
						break;

					case Text text when activeFields.Count > 0:
						var activeField = activeFields.Peek();
						if (!activeField.IsResultSection || activeField.Kind is FieldUpdateKind.Other)
						{
							break;
						}

						if (!TryComputeFieldValue(activeField.Kind, options, propertyMap, currentPageNumber, totalPageCount, out var computedValue))
						{
							break;
						}

						if (!activeField.HasWrittenValue)
						{
							if (!string.Equals(text.Text, computedValue, StringComparison.Ordinal))
							{
								text.Text = computedValue;
								hasChanges = true;
								updatedFields.Add(GetFieldName(activeField.Kind));
							}

							activeField.HasWrittenValue = true;
						}
						else if (!string.IsNullOrEmpty(text.Text))
						{
							text.Text = string.Empty;
							hasChanges = true;
							updatedFields.Add(GetFieldName(activeField.Kind));
						}

						break;
				}
			}
		}

		return hasChanges;
	}

	private static bool UpdateSimpleFields(
		Paragraph paragraph,
		FieldUpdateOptions options,
		DocumentPropertyMap propertyMap,
		int currentPageNumber,
		int totalPageCount,
		ISet<string> updatedFields)
	{
		var hasChanges = false;

		foreach (var simpleField in paragraph.Descendants<SimpleField>())
		{
			var kind = ParseFieldKind(simpleField.Instruction?.Value);
			if (kind is FieldUpdateKind.Other)
			{
				continue;
			}

			if (!TryComputeFieldValue(kind, options, propertyMap, currentPageNumber, totalPageCount, out var computedValue))
			{
				continue;
			}

			var textNodes = simpleField.Descendants<Text>().ToArray();
			if (textNodes.Length == 0)
			{
				simpleField.AppendChild(new Run(new Text(computedValue)));
				hasChanges = true;
				updatedFields.Add(GetFieldName(kind));
				continue;
			}

			if (!string.Equals(textNodes[0].Text, computedValue, StringComparison.Ordinal))
			{
				textNodes[0].Text = computedValue;
				hasChanges = true;
				updatedFields.Add(GetFieldName(kind));
			}

			for (var i = 1; i < textNodes.Length; i++)
			{
				if (string.IsNullOrEmpty(textNodes[i].Text))
				{
					continue;
				}

				textNodes[i].Text = string.Empty;
				hasChanges = true;
				updatedFields.Add(GetFieldName(kind));
			}
		}

		return hasChanges;
	}

	private static void HandleFieldChar(FieldChar fieldChar, Stack<ActiveFieldUpdate> activeFields)
	{
		if (fieldChar.FieldCharType is null)
		{
			return;
		}

		if (fieldChar.FieldCharType == FieldCharValues.Begin)
		{
			activeFields.Push(new ActiveFieldUpdate());
			return;
		}

		if (fieldChar.FieldCharType == FieldCharValues.Separate)
		{
			if (activeFields.Count == 0)
			{
				return;
			}

			var activeField = activeFields.Peek();
			activeField.IsResultSection = true;
			activeField.Kind = ParseFieldKind(activeField.InstructionBuilder.ToString());
			return;
		}

		if (fieldChar.FieldCharType == FieldCharValues.End && activeFields.Count > 0)
		{
			activeFields.Pop();
		}
	}

	private static FieldUpdateKind ParseFieldKind(string? instruction)
	{
		if (string.IsNullOrWhiteSpace(instruction))
		{
			return FieldUpdateKind.Other;
		}

		var trimmed = instruction.Trim();
		var firstToken = trimmed.Split([' ', '\\', '"', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)[0].ToUpperInvariant();
		return firstToken switch
		{
			"PAGE" => FieldUpdateKind.Page,
			"NUMPAGES" => FieldUpdateKind.NumPages,
			"TOC" => FieldUpdateKind.Toc,
			"TITLE" => FieldUpdateKind.Title,
			"AUTHOR" => FieldUpdateKind.Author,
			"SUBJECT" => FieldUpdateKind.Subject,
			"KEYWORDS" => FieldUpdateKind.Keywords,
			"DESCRIPTION" => FieldUpdateKind.Description,
			"FILENAME" => FieldUpdateKind.Filename,
			_ => FieldUpdateKind.Other
		};
	}

	private static bool TryComputeFieldValue(
		FieldUpdateKind kind,
		FieldUpdateOptions options,
		DocumentPropertyMap propertyMap,
		int currentPageNumber,
		int totalPageCount,
		out string computedValue)
	{
		computedValue = kind switch
		{
			FieldUpdateKind.Page when options.UpdatePageFields => currentPageNumber.ToString(CultureInfo.InvariantCulture),
			FieldUpdateKind.NumPages when options.UpdatePageFields => totalPageCount.ToString(CultureInfo.InvariantCulture),
			FieldUpdateKind.Title when options.UpdateDocumentProperties => propertyMap.Title,
			FieldUpdateKind.Author when options.UpdateDocumentProperties => propertyMap.Author,
			FieldUpdateKind.Subject when options.UpdateDocumentProperties => propertyMap.Subject,
			FieldUpdateKind.Keywords when options.UpdateDocumentProperties => propertyMap.Keywords,
			FieldUpdateKind.Description when options.UpdateDocumentProperties => propertyMap.Description,
			FieldUpdateKind.Filename when options.UpdateDocumentProperties => propertyMap.Filename,
			_ => string.Empty
		};

		return kind switch
		{
			FieldUpdateKind.Page or FieldUpdateKind.NumPages => options.UpdatePageFields,
			FieldUpdateKind.Title or FieldUpdateKind.Author or FieldUpdateKind.Subject or FieldUpdateKind.Keywords or FieldUpdateKind.Description or FieldUpdateKind.Filename => options.UpdateDocumentProperties,
			_ => false
		};
	}

	private static DocumentPropertyMap BuildDocumentPropertyMap(DocxDocument doc, RenderOptions renderOptions)
		=> new(
			doc.Title ?? string.Empty,
			doc.Author ?? string.Empty,
			doc.Subject ?? string.Empty,
			doc.Keywords ?? string.Empty,
			doc.Description ?? string.Empty,
			renderOptions.SourceFilename ?? "(document)");

	private static string GetFieldName(FieldUpdateKind kind)
		=> kind switch
		{
			FieldUpdateKind.Page => "PAGE",
			FieldUpdateKind.NumPages => "NUMPAGES",
			FieldUpdateKind.Toc => "TOC",
			FieldUpdateKind.Title => "TITLE",
			FieldUpdateKind.Author => "AUTHOR",
			FieldUpdateKind.Subject => "SUBJECT",
			FieldUpdateKind.Keywords => "KEYWORDS",
			FieldUpdateKind.Description => "DESCRIPTION",
			FieldUpdateKind.Filename => "FILENAME",
			_ => string.Empty
		};

	private sealed class ActiveFieldUpdate
	{
		public StringBuilder InstructionBuilder { get; } = new();

		public bool IsResultSection { get; set; }

		public FieldUpdateKind Kind { get; set; } = FieldUpdateKind.Other;

		public bool HasWrittenValue { get; set; }
	}

	private enum FieldUpdateKind
	{
		Other,
		Page,
		NumPages,
		Toc,
		Title,
		Author,
		Subject,
		Keywords,
		Description,
		Filename
	}
}

/// <summary>
/// A single entry in a table of contents or table of figures.
/// </summary>
internal readonly record struct TocEntry(int Level, string Text, int PageNumber, string? HyperlinkAnchor);

/// <summary>
/// Result of building TOC/TOF entries from the document, including whether entries changed.
/// </summary>
internal readonly record struct TocEntryBuildResult(IReadOnlyList<TocEntry> Entries, bool HasChanges);

/// <summary>
/// Run-level formatting templates extracted from stale TOC entry paragraphs.
/// </summary>
internal readonly record struct TocTemplateRunProperties(
	RunProperties? EntryRunProperties,
	RunProperties? SeparatorRunProperties,
	RunProperties? PageNumberRunProperties);

/// <summary>
/// Paragraph and run formatting from a TOC paragraph style definition.
/// </summary>
internal readonly record struct TocStyleFormatting(
	ParagraphProperties? ParagraphProperties,
	RunProperties? RunProperties);

/// <summary>
/// Parsed TOC field switch set controlling which entries are included and how they are formatted.
/// </summary>
internal readonly record struct TocSwitchSet(
	int MinimumLevel,
	int MaximumLevel,
	bool IncludePageNumbers,
	bool UseHyperlinks,
	string Separator,
	IReadOnlyDictionary<string, int> CustomStyleLevels,
	bool IsTableOfFigures);

/// <summary>
/// Allocates monotonically increasing bookmark IDs for synthetic bookmarks.
/// </summary>
internal sealed class BookmarkAllocator(int nextId)
{
	private int _nextId = nextId;

	/// <summary>
	/// Allocates and returns the next available bookmark ID.
	/// </summary>
	public int Allocate() => _nextId++;
}

/// <summary>
/// Document properties resolved from the DOCX package core properties.
/// </summary>
internal readonly record struct DocumentPropertyMap(
	string Title,
	string Author,
	string Subject,
	string Keywords,
	string Description,
	string Filename);

/// <summary>
/// Result of a single field-update pass, indicating whether any field values changed.
/// </summary>
internal readonly record struct FieldUpdatePassResult(bool HasChanges, IReadOnlyList<string> UpdatedFields)
{
	/// <summary>
	/// A singleton representing no changes from the update pass.
	/// </summary>
	public static FieldUpdatePassResult NoChanges { get; } = new(false, Array.Empty<string>());
}