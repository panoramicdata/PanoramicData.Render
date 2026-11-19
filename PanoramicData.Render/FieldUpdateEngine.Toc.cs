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
		Func<ParagraphBlock, int?> getLevel = switchSet.IsTableOfFigures
			? paragraphBlock => IsFigureCaptionParagraph(paragraphBlock, paragraphStyles) ? 1 : null
			: paragraphBlock => TryGetTocEntryLevel(paragraphBlock, paragraphStyles, switchSet, out var headingLevel) ? headingLevel : null;

		return BuildEntries(blocks, blockPageMap, switchSet, bookmarkAllocator, getLevel);	}
}
