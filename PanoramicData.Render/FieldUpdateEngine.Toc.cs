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
}
