namespace PanoramicData.Render;

using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class FieldUpdateEngine
{
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

			if (UpdateTocParagraph(paragraph, switchSet, blocks, blockPageMap, paragraphStyles, bookmarkAllocator))
			{
				hasChanges = true;
				updatedFields.Add(switchSet.IsTableOfFigures ? "TOF" : "TOC");
			}
		}

		return hasChanges;
	}

	private static bool UpdateTocParagraph(
		Paragraph paragraph,
		TocSwitchSet switchSet,
		IReadOnlyList<DocumentBlock> blocks,
		IReadOnlyDictionary<DocumentBlock, int> blockPageMap,
		ParagraphStyleHierarchy paragraphStyles,
		BookmarkAllocator bookmarkAllocator)
	{
		var tocEntryBuildResult = BuildTocEntries(blocks, blockPageMap, paragraphStyles, switchSet, bookmarkAllocator);
		var filteredEntries = tocEntryBuildResult.Entries
			.Where(entry => entry.Level >= switchSet.MinimumLevel && entry.Level <= switchSet.MaximumLevel)
			.ToArray();

		var tocParagraphsChanged = ReplaceTocResultParagraphs(paragraph, filteredEntries, switchSet, paragraphStyles);
		return tocEntryBuildResult.HasChanges || tocParagraphsChanged;
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
				if (child is FieldChar)
				{
					continue;
				}

				if (child is FieldCode fieldCode)
				{
					return NormalizeWhitespace(fieldCode.Text).StartsWith($"SEQ {sequenceIdentifier}", StringComparison.OrdinalIgnoreCase);
				}

				if (child is Text text && !string.IsNullOrWhiteSpace(text.Text))
				{
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
}
