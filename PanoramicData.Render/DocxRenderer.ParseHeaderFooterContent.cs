using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PanoramicData.Render;

public sealed partial class DocxRenderer
{
	/// <summary>
	/// Parses all header and footer parts referenced by any section in the document,
	/// building lookup dictionaries keyed by relationship ID.
	/// Must be called while the document package is still open.
	/// </summary>
	private static (Dictionary<string, HeaderFooterContent> Headers, Dictionary<string, HeaderFooterContent> Footers) ParseHeaderFooterContent(
	DocxDocument doc,
	IReadOnlyList<DocumentBlock> blocks,
	SectionInfo bodySectionInfo)
	{
		var headers = new Dictionary<string, HeaderFooterContent>(StringComparer.Ordinal);
		var footers = new Dictionary<string, HeaderFooterContent>(StringComparer.Ordinal);

		// Collect all unique section info objects referenced in the document
		var allSections = new List<SectionInfo> { bodySectionInfo };
		foreach (var block in blocks)
		{
			if (block is SectionBreakBlock sectionBreak)
			{
				allSections.Add(sectionBreak.SectionInfo);
			}
		}

		foreach (var section in allSections)
		{
			foreach (var content in HeaderFooterPartParser.ParseHeaders(doc.MainDocumentPart, section.HeaderReferences))
			{
				headers[content.RelationshipId] = content;
			}

			foreach (var content in HeaderFooterPartParser.ParseFooters(doc.MainDocumentPart, section.FooterReferences))
			{
				footers[content.RelationshipId] = content;
			}
		}

		return (headers, footers);
	}

	/// <summary>
	/// Reconstructs each page with resolved header and footer layout blocks attached.
	/// </summary>
	private static IReadOnlyList<LayoutPage> AttachHeaderFooterBlocks(
		IReadOnlyList<LayoutPage> pages,
		IReadOnlyDictionary<string, HeaderFooterContent> headerContents,
		IReadOnlyDictionary<string, HeaderFooterContent> footerContents,
		bool evenAndOddHeaders)
	{
		if (headerContents.Count == 0 && footerContents.Count == 0)
		{
			return pages;
		}

		var attacher = new HeaderFooterAttacher(headerContents, footerContents, evenAndOddHeaders);
		var result = new List<LayoutPage>(pages.Count);
		for (var i = 0; i < pages.Count; i++)
		{
			var isFirstOfSection = i == 0 || !ReferenceEquals(pages[i - 1].Section, pages[i].Section);
			result.Add(attacher.Attach(pages[i], isFirstOfSection));
		}

		return result;
	}

	private sealed class HeaderFooterAttacher(
		IReadOnlyDictionary<string, HeaderFooterContent> headerContents,
		IReadOnlyDictionary<string, HeaderFooterContent> footerContents,
		bool evenAndOddHeaders)
	{
		// The most recent sections that define their own header/footer references, tracked independently.
		// Only updated by a section that owns its references, so inheritance chains resolve to the nearest owner.
		private SectionInfo? _previousSectionWithHeaders;
		private SectionInfo? _previousSectionWithFooters;

		public LayoutPage Attach(LayoutPage page, bool isFirstOfSection)
		{
			var pageHasOwnHeaders = page.Section.HeaderReferences.Count > 0;
			var pageHasOwnFooters = page.Section.FooterReferences.Count > 0;

			var headerBlocks = ResolveBlocks(page, isFirstOfSection, pageHasOwnHeaders, _previousSectionWithHeaders, HeaderFooterResolver.ResolveHeader, headerContents);
			var footerBlocks = ResolveBlocks(page, isFirstOfSection, pageHasOwnFooters, _previousSectionWithFooters, HeaderFooterResolver.ResolveFooter, footerContents);

			// Only a section that owns its references provides them to later sections.
			if (isFirstOfSection && pageHasOwnHeaders)
			{
				_previousSectionWithHeaders = page.Section;
			}

			if (isFirstOfSection && pageHasOwnFooters)
			{
				_previousSectionWithFooters = page.Section;
			}

			return page.WithHeaderAndFooterBlocks(headerBlocks, footerBlocks);
		}

		// A section without references inherits from the previous owning section, like Word's Link to Previous.
		// The title page rule only applies to a section that owns its references.
		private IReadOnlyList<LayoutBlock>? ResolveBlocks(
			LayoutPage page,
			bool isFirstOfSection,
			bool ownsReferences,
			SectionInfo? previousOwner,
			Func<SectionInfo, bool, int, bool, HeaderFooterReference?> resolve,
			IReadOnlyDictionary<string, HeaderFooterContent> contents)
		{
			var section = ownsReferences ? page.Section : previousOwner ?? page.Section;
			var reference = resolve(section, isFirstOfSection && ownsReferences, page.PageNumber, evenAndOddHeaders);
			return LayoutBlocksFor(reference, contents);
		}
		private static IReadOnlyList<LayoutBlock>? LayoutBlocksFor(HeaderFooterReference? reference, IReadOnlyDictionary<string, HeaderFooterContent> contents)
			=> reference is not null && contents.TryGetValue(reference.RelationshipId, out var content)
				? HeaderFooterLayoutEngine.Layout(content).Blocks
				: null;
	}
}
