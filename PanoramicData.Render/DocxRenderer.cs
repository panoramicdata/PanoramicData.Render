namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Main entry point for rendering DOCX documents to SVG and PDF output.
/// Thread-safe: a single instance can render multiple documents concurrently.
/// </summary>
public sealed class DocxRenderer
{
private readonly RenderOptions _options;
private readonly ILogger _logger;

/// <summary>
/// Initializes a new instance of the <see cref="DocxRenderer"/> class with the specified options.
/// </summary>
/// <param name="options">The rendering configuration.</param>
public DocxRenderer(RenderOptions options)
: this(options, NullLogger.Instance)
{
}

/// <summary>
/// Initializes a new instance of the <see cref="DocxRenderer"/> class with the specified options and logger.
/// </summary>
/// <param name="options">The rendering configuration.</param>
/// <param name="logger">The logger for diagnostic output.</param>
public DocxRenderer(RenderOptions options, ILogger logger)
{
ArgumentNullException.ThrowIfNull(options);
ArgumentNullException.ThrowIfNull(logger);

_options = options;
_logger = logger;
}

/// <summary>
/// Renders a DOCX document from the specified stream.
/// </summary>
/// <param name="docxStream">A readable stream containing the DOCX file.</param>
/// <param name="cancellationToken">A cancellation token.</param>
/// <returns>A <see cref="RenderResult"/> containing the rendered pages.</returns>
/// <exception cref="ArgumentNullException"><paramref name="docxStream"/> is <see langword="null"/>.</exception>
/// <exception cref="InvalidOperationException">The stream does not contain a valid DOCX document.</exception>
public Task<RenderResult> RenderAsync(Stream docxStream, CancellationToken cancellationToken = default)
{
ArgumentNullException.ThrowIfNull(docxStream);
cancellationToken.ThrowIfCancellationRequested();

var result = RenderCore(docxStream);
return Task.FromResult(result);
}

/// <summary>
/// Renders a DOCX document from the specified stream synchronously.
/// </summary>
/// <param name="docxStream">A readable stream containing the DOCX file.</param>
/// <returns>A <see cref="RenderResult"/> containing the rendered pages.</returns>
/// <exception cref="ArgumentNullException"><paramref name="docxStream"/> is <see langword="null"/>.</exception>
/// <exception cref="InvalidOperationException">The stream does not contain a valid DOCX document.</exception>
public RenderResult Render(Stream docxStream)
{
ArgumentNullException.ThrowIfNull(docxStream);
return RenderCore(docxStream);
}

private RenderResult RenderCore(Stream docxStream)
{
// 1. Load the DOCX document
using var doc = DocxDocument.Load(docxStream);
_logger.LogDebug("Loaded DOCX document");
StyleCascadeMaterializer.Apply(doc);
_logger.LogDebug("Materialized effective style formatting");

// 2. Pre-load all images into memory before the document is disposed
var images = PreLoadImages(doc);
_logger.LogDebug("Pre-loaded {ImageCount} images", images.Count);

// 2a. Extract embedded fonts from the DOCX before document disposal
var extractedFonts = DocxFontExtractor.Extract(doc.MainDocumentPart);
if (extractedFonts.Count > 0)
{
foreach (var kvp in extractedFonts)
{
_options.ExtractedFontData[kvp.Key] = kvp.Value;
}

_logger.LogDebug("Extracted {FontCount} embedded font variants", extractedFonts.Count);
}

// 2b. Clone styles for table-style resolution after document disposal
var styles = doc.StylesPart?.Styles is { } s ? (Styles)s.CloneNode(true) : null;

// 3. Parse document blocks from the body
var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
_logger.LogDebug("Parsed {BlockCount} document blocks", blocks.Count);

// 3a. Resolve style-inherited numbering: headings that inherit numPr via
// the paragraph style chain (e.g. Heading3 → Heading2 → Heading1) need
// their NumberingId/NumberingLevel set before numbering styles are loaded.
ResolveStyleNumbering(blocks, doc);

// 4. Determine body section info (final section properties)
var bodySectionInfo = GetBodySectionInfo(doc);

// 4a. Load numbering styles from the document''s numbering definitions
LoadNumberingStyles(doc, blocks);

// 4b. Parse header and footer content while the document parts are still accessible
var (headerContentsByRelId, footerContentsByRelId) = ParseHeaderFooterContent(doc, blocks, bodySectionInfo);
_logger.LogDebug(
"Parsed {HeaderCount} header variants and {FooterCount} footer variants",
headerContentsByRelId.Count,
footerContentsByRelId.Count);

// 5. Measure blocks into layout blocks
var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks, bodySectionInfo);
_logger.LogDebug("Measured {LayoutBlockCount} layout blocks", layoutBlocks.Count);

if (_options.FieldUpdate is null)
{
// 6. Paginate
var pages = PageBuilder.PaginateDocument(layoutBlocks, bodySectionInfo);
_logger.LogDebug("Paginated into {PageCount} pages", pages.Count);

pages = AttachHeaderFooterBlocks(pages, headerContentsByRelId, footerContentsByRelId);
return new RenderResult(pages, _options, images: images, styles: styles);
}

var updatedFields = new HashSet<string>(StringComparer.Ordinal);
var iterationsRequired = 0;
var hasChanges = false;
IReadOnlyList<LayoutPage> updatedPages;

do
{
updatedPages = PageBuilder.PaginateDocument(layoutBlocks, bodySectionInfo);
_logger.LogDebug("Paginated into {PageCount} pages on field-update iteration {Iteration}", updatedPages.Count, iterationsRequired + 1);

iterationsRequired++;
var passResult = FieldUpdateEngine.Apply(doc, blocks, updatedPages, _options);
foreach (var fieldName in passResult.UpdatedFields)
{
updatedFields.Add(fieldName);
}

hasChanges = passResult.HasChanges;
if (hasChanges && iterationsRequired < _options.FieldUpdate.MaxIterations)
{
StyleCascadeMaterializer.Apply(doc);
_logger.LogDebug("Materialized effective style formatting after field updates");
blocks = DocumentBlockParser.Parse(doc.DocumentBody);
_logger.LogDebug("Re-parsed {BlockCount} document blocks after field updates", blocks.Count);
layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks, bodySectionInfo);
_logger.LogDebug("Re-measured {LayoutBlockCount} layout blocks after field updates", layoutBlocks.Count);
}
}
while (hasChanges && iterationsRequired < _options.FieldUpdate.MaxIterations);

if (hasChanges)
{
StyleCascadeMaterializer.Apply(doc);
_logger.LogDebug("Materialized effective style formatting after hitting the field-update iteration cap");
blocks = DocumentBlockParser.Parse(doc.DocumentBody);
_logger.LogDebug("Re-parsed {BlockCount} document blocks after hitting the field-update iteration cap", blocks.Count);
layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks, bodySectionInfo);
_logger.LogDebug("Re-measured {LayoutBlockCount} layout blocks after hitting the field-update iteration cap", layoutBlocks.Count);
updatedPages = PageBuilder.PaginateDocument(layoutBlocks, bodySectionInfo);
_logger.LogDebug("Paginated into {PageCount} pages after the final field-update pass", updatedPages.Count);

_logger.LogWarning(
"Field updates did not converge within {MaxIterations} iterations; using the latest computed values",
_options.FieldUpdate.MaxIterations);
}

var fieldUpdateResult = new FieldUpdateResult
{
IterationsRequired = iterationsRequired,
UpdatedFields = [.. updatedFields.OrderBy(value => value, StringComparer.Ordinal)]
};

updatedPages = AttachHeaderFooterBlocks(updatedPages, headerContentsByRelId, footerContentsByRelId);
return new RenderResult(updatedPages, _options, fieldUpdateResult, images, styles);
}

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
IReadOnlyDictionary<string, HeaderFooterContent> footerContents)
{
if (headerContents.Count == 0 && footerContents.Count == 0)
{
return pages;
}

var result = new List<LayoutPage>(pages.Count);
SectionInfo? previousSection = null;

for (var i = 0; i < pages.Count; i++)
{
var page = pages[i];
var isFirstOfSection = i == 0 || !ReferenceEquals(pages[i - 1].Section, page.Section);

// When a section has no header/footer references, OOXML semantics say it
// inherits from the previous section (equivalent to Word's "Link to Previous").
var sectionForHeaders = page.Section.HeaderReferences.Count > 0
? page.Section
: previousSection ?? page.Section;
var sectionForFooters = page.Section.FooterReferences.Count > 0
? page.Section
: previousSection ?? page.Section;

var headerRef = HeaderFooterResolver.ResolveHeader(sectionForHeaders, isFirstOfSection, page.PageNumber, evenAndOddHeaders: false);
var footerRef = HeaderFooterResolver.ResolveFooter(sectionForFooters, isFirstOfSection, page.PageNumber, evenAndOddHeaders: false);

IReadOnlyList<LayoutBlock>? headerBlocks = null;
if (headerRef is not null && headerContents.TryGetValue(headerRef.RelationshipId, out var hContent))
{
headerBlocks = HeaderFooterLayoutEngine.Layout(hContent).Blocks;
}

IReadOnlyList<LayoutBlock>? footerBlocks = null;
if (footerRef is not null && footerContents.TryGetValue(footerRef.RelationshipId, out var fContent))
{
footerBlocks = HeaderFooterLayoutEngine.Layout(fContent).Blocks;
}

if (isFirstOfSection)
{
previousSection = page.Section;
}

result.Add(page.WithHeaderAndFooterBlocks(headerBlocks, footerBlocks));
}

return result;
}

private static Dictionary<string, ImageData> PreLoadImages(DocxDocument doc)
{
var store = new MediaStore(doc);
var ids = store.GetImagePartRelationshipIds();
var images = new Dictionary<string, ImageData>(ids.Count, StringComparer.Ordinal);
foreach (var id in ids)
{
if (store.TryGetImage(id, out var imageData) && imageData is not null)
{
images[id] = imageData;
}
}

return images;
}

private static SectionInfo GetBodySectionInfo(DocxDocument doc)
{
var bodySectPr = doc.DocumentBody
.GetFirstChild<DocumentFormat.OpenXml.Wordprocessing.SectionProperties>();

return bodySectPr is not null
? SectionInfoParser.Parse(bodySectPr)
: new SectionInfo();
}

/// <summary>
/// Loads all numbering definitions from the document into <see cref="RenderOptions.NumberingStyles"/>
/// so the render emitter can format list labels correctly.
/// Already-configured styles are not overwritten.
/// </summary>
private void LoadNumberingStyles(DocxDocument doc, IReadOnlyList<DocumentBlock> blocks)
{
var numberingPart = doc.NumberingPart;
if (numberingPart is null)
{
return;
}

// Build normalization map: numId → canonical numId for shared abstractNumId.
// This ensures heading levels that use different numIds but share the same
// abstract numbering definition share counter state.
BuildNumberingIdNormalization(numberingPart);

// Collect all referenced (numId, ilvl) pairs from the parsed blocks
var referenced = new HashSet<(int NumId, int Level)>();
foreach (var block in blocks)
		{
			if (block is ParagraphBlock pb
				&& pb.NumberingId is int numId)
			{
				var ilvl = pb.NumberingLevel ?? 0;
				var canonicalId = _options.NumberingIdNormalization.TryGetValue(numId, out var cid) ? cid : numId;
				referenced.Add((canonicalId, ilvl));
			}
		}

foreach (var (numId, level) in referenced)
{
var key = $"{numId}:{level}";
if (_options.NumberingStyles.ContainsKey(key))
{
continue;
}

var style = NumberingStyleResolver.ResolveLevel(numberingPart, numId, level);
if (style is not null)
{
_options.NumberingStyles[key] = style;
}
}
}

	/// <summary>
	/// Builds a mapping from each concrete numId to the lowest numId that shares
	/// the same abstractNumId, so numbering instances that logically belong to the
	/// same multilevel definition share counter state.
	/// </summary>
	private void BuildNumberingIdNormalization(DocumentFormat.OpenXml.Packaging.NumberingDefinitionsPart numberingPart)
	{
		var numbering = numberingPart.Numbering;
		if (numbering is null)
		{
			return;
		}

		// Group numIds by abstractNumId and pick the lowest numId as canonical.
		var abstractToCanonical = new Dictionary<int, int>();
		foreach (var instance in numbering.Elements<DocumentFormat.OpenXml.Wordprocessing.NumberingInstance>())
		{
			var numId = instance.NumberID?.Value;
			var abstractId = instance.AbstractNumId?.Val?.Value;
			if (numId is null || abstractId is null)
			{
				continue;
			}

			if (!abstractToCanonical.TryGetValue(abstractId.Value, out var canonical) || numId.Value < canonical)
			{
				abstractToCanonical[abstractId.Value] = numId.Value;
			}
		}

		foreach (var instance in numbering.Elements<DocumentFormat.OpenXml.Wordprocessing.NumberingInstance>())
		{
			var numId = instance.NumberID?.Value;
			var abstractId = instance.AbstractNumId?.Val?.Value;
			if (numId is null || abstractId is null)
			{
				continue;
			}

			var canonical = abstractToCanonical[abstractId.Value];
			if (canonical != numId.Value)
			{
				_options.NumberingIdNormalization[numId.Value] = canonical;
			}
		}
	}

	/// <summary>
	/// Resolves numbering properties from the paragraph style cascade for paragraphs
	/// that do not have explicit <c>w:numPr</c> in their direct formatting but inherit
	/// numbering from styles (e.g. Heading 1, Heading 2).
	/// </summary>
	private static void ResolveStyleNumbering(IReadOnlyList<DocumentBlock> blocks, DocxDocument doc)
	{
		var paragraphStyles = ParagraphStyleHierarchyParser.Parse(doc.StylesPart);

		foreach (var block in blocks)
		{
			if (block is not ParagraphBlock pb || pb.NumberingId is not null)
			{
				continue;
			}

			var styleId = pb.StyleId;
			if (string.IsNullOrEmpty(styleId))
			{
				continue;
			}

			// Walk the style chain from base to derived (reversed inheritance chain).
			// Each derived style's numPr values override the parent's, matching the
			// OOXML cascade behaviour in EffectiveFormattingResolver.Merge().
			int? resolvedNumId = null;
			int? resolvedIlvl = null;

			var chain = paragraphStyles.GetInheritanceChain(styleId);
			for (var i = chain.Count - 1; i >= 0; i--)
			{
				if (!paragraphStyles.Styles.TryGetValue(chain[i], out var psi))
				{
					continue;
				}

				var numPr = psi.Properties.GetFirstChild<NumberingProperties>();
				if (numPr is null)
				{
					continue;
				}

				if (numPr.NumberingId?.Val?.Value is int nid)
				{
					resolvedNumId = nid;
				}

				if (numPr.NumberingLevelReference?.Val?.Value is int lvl)
				{
					resolvedIlvl = lvl;
				}
			}

			if (resolvedNumId is not null)
			{
				pb.NumberingId = resolvedNumId;
				pb.NumberingLevel = resolvedIlvl ?? 0;
			}
		}
	}
}
