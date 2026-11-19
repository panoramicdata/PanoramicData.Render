namespace PanoramicData.Render;

using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static partial class FieldUpdateEngine
{
	private static TocEntryBuildResult BuildEntries(
		IReadOnlyList<DocumentBlock> blocks,
		IReadOnlyDictionary<DocumentBlock, int> blockPageMap,
		TocSwitchSet switchSet,
		BookmarkAllocator bookmarkAllocator,
		Func<ParagraphBlock, int?> getLevel)
	{
		var entries = new List<TocEntry>();
		var hasChanges = false;

		foreach (var paragraphBlock in blocks.OfType<ParagraphBlock>())
		{
			if (getLevel(paragraphBlock) is not { } level)
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

			entries.Add(new TocEntry(level, displayText, pageNumber, hyperlinkAnchor));
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
}
