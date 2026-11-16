namespace PanoramicData.Render;

using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Applies opt-in field updates to the in-memory document model.
/// </summary>
internal static partial class FieldUpdateEngine
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
