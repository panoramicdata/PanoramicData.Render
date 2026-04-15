using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PanoramicData.Render;

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
var result = RenderCore(docxStream, cancellationToken);
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
return RenderCore(docxStream, CancellationToken.None);
}

private RenderResult RenderCore(Stream docxStream, CancellationToken cancellationToken)
{
cancellationToken.ThrowIfCancellationRequested();

// 1. Load the DOCX document
using var doc = DocxDocument.Load(docxStream);
_logger.LogDebug("Loaded DOCX document");
StyleCascadeMaterializer.Apply(doc);
_logger.LogDebug("Materialized effective style formatting");

cancellationToken.ThrowIfCancellationRequested();

// 2. Pre-load all images into memory before the document is disposed
var images = PreLoadImages(doc);
_logger.LogDebug("Pre-loaded {ImageCount} images", images.Count);

cancellationToken.ThrowIfCancellationRequested();

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

cancellationToken.ThrowIfCancellationRequested();

// 2b. Clone styles for table-style resolution after document disposal
var styles = doc.StylesPart?.Styles is { } s ? (Styles)s.CloneNode(true) : null;

// 3. Parse document blocks from the body
var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
_logger.LogDebug("Parsed {BlockCount} document blocks", blocks.Count);

cancellationToken.ThrowIfCancellationRequested();

// 4. Determine body section info (final section properties)
var bodySectionInfo = GetBodySectionInfo(doc);

// 4a. Determine whether even/odd headers are enabled in document settings.
// The presence of the <w:evenAndOddHeaders/> element (regardless of value) enables even/odd mode.
var evenAndOddHeaders = doc.SettingsPart?.Settings?.GetFirstChild<DocumentFormat.OpenXml.Wordprocessing.EvenAndOddHeaders>() is not null;

// 4b. Load numbering styles from the document's numbering definitions
LoadNumberingStyles(doc, blocks);

cancellationToken.ThrowIfCancellationRequested();

// 4c. Parse header and footer content while the document parts are still accessible
var (headerContentsByRelId, footerContentsByRelId) = ParseHeaderFooterContent(doc, blocks, bodySectionInfo);
_logger.LogDebug(
"Parsed {HeaderCount} header variants and {FooterCount} footer variants",
headerContentsByRelId.Count,
footerContentsByRelId.Count);

cancellationToken.ThrowIfCancellationRequested();

// 5. Measure blocks into layout blocks
var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks, bodySectionInfo);
_logger.LogDebug("Measured {LayoutBlockCount} layout blocks", layoutBlocks.Count);

cancellationToken.ThrowIfCancellationRequested();

if (_options.FieldUpdate is null)
{
// 6. Paginate
var pages = PageBuilder.PaginateDocument(layoutBlocks, bodySectionInfo);
_logger.LogDebug("Paginated into {PageCount} pages", pages.Count);

pages = AttachHeaderFooterBlocks(pages, headerContentsByRelId, footerContentsByRelId, evenAndOddHeaders);
return new RenderResult(pages, _options, images: images, styles: styles);
}

var updatedFields = new HashSet<string>(StringComparer.Ordinal);
var iterationsRequired = 0;
var hasChanges = false;
IReadOnlyList<LayoutPage> updatedPages;

do
{
cancellationToken.ThrowIfCancellationRequested();

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
cancellationToken.ThrowIfCancellationRequested();

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
cancellationToken.ThrowIfCancellationRequested();

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

updatedPages = AttachHeaderFooterBlocks(updatedPages, headerContentsByRelId, footerContentsByRelId, evenAndOddHeaders);
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
	IReadOnlyDictionary<string, HeaderFooterContent> footerContents,
	bool evenAndOddHeaders)
{
if (headerContents.Count == 0 && footerContents.Count == 0)
{
	return pages;
}

var result = new List<LayoutPage>(pages.Count);

// Track the most recent sections that define their own header/footer references,
// independently for headers and footers. Only updated when a section has its own refs,
// so that "link to previous" chains always resolve to the nearest owning section.
SectionInfo? previousSectionWithHeaders = null;
SectionInfo? previousSectionWithFooters = null;

for (var i = 0; i < pages.Count; i++)
{
var page = pages[i];
var isFirstOfSection = i == 0 || !ReferenceEquals(pages[i - 1].Section, page.Section);
var pageHasOwnHeaders = page.Section.HeaderReferences.Count > 0;
var pageHasOwnFooters = page.Section.FooterReferences.Count > 0;

// When a section has no header/footer references, OOXML semantics say it
// inherits from the previous section (equivalent to Word's "Link to Previous").
var sectionForHeaders = pageHasOwnHeaders
? page.Section
: previousSectionWithHeaders ?? page.Section;
var sectionForFooters = pageHasOwnFooters
? page.Section
: previousSectionWithFooters ?? page.Section;

// titlePage suppression (first-page header) only applies to pages in the section that
// defines its own header. When inheriting from a previous section, the titlePage flag
// of that previous section must not suppress headers on the first page of this section.
var isFirstForHeaders = isFirstOfSection && pageHasOwnHeaders;
var isFirstForFooters = isFirstOfSection && pageHasOwnFooters;

var headerRef = HeaderFooterResolver.ResolveHeader(sectionForHeaders, isFirstForHeaders, page.PageNumber, evenAndOddHeaders);
var footerRef = HeaderFooterResolver.ResolveFooter(sectionForFooters, isFirstForFooters, page.PageNumber, evenAndOddHeaders);

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

// Only record this section as providing headers/footers for future inherited sections
// if it actually owns its own references.
if (isFirstOfSection && pageHasOwnHeaders)
{
previousSectionWithHeaders = page.Section;
}

if (isFirstOfSection && pageHasOwnFooters)
{
previousSectionWithFooters = page.Section;
}

result.Add(page.WithHeaderAndFooterBlocks(headerBlocks, footerBlocks));
}

return result;
}

private static Dictionary<string, ImageData> PreLoadImages(DocxDocument doc)
{
var store = new MediaStore(doc);
var ids = store.GetImagePartRelationshipIds();
var images = new Dictionary<string, ImageData>(ids.Count + 16, StringComparer.Ordinal);
foreach (var id in ids)
{
if (store.TryGetImage(id, out var imageData) && imageData is not null)
{
images[id] = imageData;
}
}

// Also load images from header and footer parts.
// Each header/footer part has its own relationship namespace, so image rIds in those parts
// are independent of the main document's rIds. We add them only when the key is not already
// present (body images take precedence in the unlikely case of a key collision).
var rasterizer = new VectorImageRasterizer();
foreach (var headerPart in doc.MainDocumentPart.HeaderParts)
{
LoadPartImages(headerPart, images, rasterizer);
}

foreach (var footerPart in doc.MainDocumentPart.FooterParts)
{
LoadPartImages(footerPart, images, rasterizer);
}

return images;
}

private static void LoadPartImages(OpenXmlPart part, Dictionary<string, ImageData> images, VectorImageRasterizer rasterizer)
{
foreach (var partRef in part.Parts)
{
if (partRef.OpenXmlPart is not ImagePart imagePart)
{
continue;
}

var relId = partRef.RelationshipId;
if (images.ContainsKey(relId))
{
// Body image with the same relationship ID takes precedence.
continue;
}

using var stream = imagePart.GetStream(System.IO.FileMode.Open, System.IO.FileAccess.Read);
using var ms = new System.IO.MemoryStream();
stream.CopyTo(ms);
ImageData imageData = new(ms.ToArray(), imagePart.ContentType);
imageData = rasterizer.RasterizeToPngIfSupported(imageData);
images[relId] = imageData;
}
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
}
