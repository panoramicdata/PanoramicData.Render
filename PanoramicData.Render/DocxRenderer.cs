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
	/// <returns>A <see cref="RenderResult"/> containing the rendered pages.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="docxStream"/> is <see langword="null"/>.</exception>
	/// <exception cref="InvalidOperationException">The stream does not contain a valid DOCX document.</exception>
	public Task<RenderResult> RenderAsync(Stream docxStream)
	=> RenderAsync(docxStream, CancellationToken.None);

	/// <summary>
	/// Renders a DOCX document from the specified stream.
	/// </summary>
	/// <param name="docxStream">A readable stream containing the DOCX file.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A <see cref="RenderResult"/> containing the rendered pages.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="docxStream"/> is <see langword="null"/>.</exception>
	/// <exception cref="InvalidOperationException">The stream does not contain a valid DOCX document.</exception>
	public Task<RenderResult> RenderAsync(Stream docxStream, CancellationToken cancellationToken)
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
		ExtractEmbeddedFonts(doc);

		cancellationToken.ThrowIfCancellationRequested();

		// 2b. Clone styles for table-style resolution after document disposal
		var styles = doc.StylesPart?.Styles is { } s ? (Styles)s.CloneNode(true) : null;

		var prepared = ParseAndMeasure(doc, styles, cancellationToken);
		return _options.FieldUpdate is null
			? RenderWithoutFieldUpdate(prepared, images, styles)
			: RenderWithFieldUpdate(doc, prepared, images, styles, cancellationToken);
	}

	private void ExtractEmbeddedFonts(DocxDocument doc)
	{
		var extractedFonts = DocxFontExtractor.Extract(doc.MainDocumentPart);
		if (extractedFonts.Count == 0)
		{
			return;
		}

		foreach (var kvp in extractedFonts)
		{
			_options.ExtractedFontData[kvp.Key] = kvp.Value;
		}

		_logger.LogDebug("Extracted {FontCount} embedded font variants", extractedFonts.Count);
	}

	private PreparedDocument ParseAndMeasure(DocxDocument doc, Styles? styles, CancellationToken cancellationToken)
	{
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
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks, bodySectionInfo, styles);
		_logger.LogDebug("Measured {LayoutBlockCount} layout blocks", layoutBlocks.Count);

		cancellationToken.ThrowIfCancellationRequested();

		return new PreparedDocument(new LayoutState(blocks, layoutBlocks), bodySectionInfo, evenAndOddHeaders, headerContentsByRelId, footerContentsByRelId);
	}

	private RenderResult RenderWithoutFieldUpdate(PreparedDocument prepared, Dictionary<string, ImageData> images, Styles? styles)
	{
		// 6. Paginate
		var pages = PageBuilder.PaginateDocument(prepared.Layout.LayoutBlocks, prepared.BodySectionInfo);
		_logger.LogDebug("Paginated into {PageCount} pages", pages.Count);

		pages = AttachHeaderFooterBlocks(pages, prepared.Headers, prepared.Footers, prepared.EvenAndOddHeaders);
		return new RenderResult(pages, _options, images: images, styles: styles);
	}

	private RenderResult RenderWithFieldUpdate(
		DocxDocument doc,
		PreparedDocument prepared,
		Dictionary<string, ImageData> images,
		Styles? styles,
		CancellationToken cancellationToken)
	{
		var maxIterations = _options.FieldUpdate!.MaxIterations;
		var layout = prepared.Layout;
		var updatedFields = new HashSet<string>(StringComparer.Ordinal);
		var iterationsRequired = 0;
		var hasChanges = false;
		IReadOnlyList<LayoutPage> updatedPages;

		do
		{
			cancellationToken.ThrowIfCancellationRequested();

			updatedPages = PageBuilder.PaginateDocument(layout.LayoutBlocks, prepared.BodySectionInfo);
			_logger.LogDebug("Paginated into {PageCount} pages on field-update iteration {Iteration}", updatedPages.Count, iterationsRequired + 1);

			iterationsRequired++;
			var passResult = FieldUpdateEngine.Apply(doc, layout.Blocks, updatedPages, _options);
			updatedFields.UnionWith(passResult.UpdatedFields);

			hasChanges = passResult.HasChanges;
			if (hasChanges && iterationsRequired < maxIterations)
			{
				cancellationToken.ThrowIfCancellationRequested();
				layout = ReloadLayout(doc, prepared.BodySectionInfo, styles, "after field updates");
			}
		}
		while (hasChanges && iterationsRequired < maxIterations);

		if (hasChanges)
		{
			cancellationToken.ThrowIfCancellationRequested();

			layout = ReloadLayout(doc, prepared.BodySectionInfo, styles, "after hitting the field-update iteration cap");
			updatedPages = PageBuilder.PaginateDocument(layout.LayoutBlocks, prepared.BodySectionInfo);
			_logger.LogDebug("Paginated into {PageCount} pages after the final field-update pass", updatedPages.Count);

			_logger.LogWarning(
				"Field updates did not converge within {MaxIterations} iterations; using the latest computed values",
				maxIterations);
		}

		var fieldUpdateResult = new FieldUpdateResult
		{
			IterationsRequired = iterationsRequired,
			UpdatedFields = [.. updatedFields.OrderBy(value => value, StringComparer.Ordinal)]
		};

		updatedPages = AttachHeaderFooterBlocks(updatedPages, prepared.Headers, prepared.Footers, prepared.EvenAndOddHeaders);
		return new RenderResult(updatedPages, _options, fieldUpdateResult, images, styles);
	}

	private LayoutState ReloadLayout(DocxDocument doc, SectionInfo bodySectionInfo, Styles? styles, string context)
	{
		StyleCascadeMaterializer.Apply(doc);
		_logger.LogDebug("Materialized effective style formatting {Context}", context);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		_logger.LogDebug("Re-parsed {BlockCount} document blocks {Context}", blocks.Count, context);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks, bodySectionInfo, styles);
		_logger.LogDebug("Re-measured {LayoutBlockCount} layout blocks {Context}", layoutBlocks.Count, context);
		return new LayoutState(blocks, layoutBlocks);
	}

	private sealed record LayoutState(IReadOnlyList<DocumentBlock> Blocks, IReadOnlyList<LayoutBlock> LayoutBlocks);

	private sealed record PreparedDocument(
		LayoutState Layout,
		SectionInfo BodySectionInfo,
		bool EvenAndOddHeaders,
		Dictionary<string, HeaderFooterContent> Headers,
		Dictionary<string, HeaderFooterContent> Footers);
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

		var instances = numbering.Elements<DocumentFormat.OpenXml.Wordprocessing.NumberingInstance>()
			.Select(TryGetInstanceIds)
			.OfType<(int NumId, int AbstractId)>()
			.ToList();

		// Group numIds by abstractNumId and pick the lowest numId as canonical.
		var abstractToCanonical = new Dictionary<int, int>();
		foreach (var (numId, abstractId) in instances)
		{
			if (!abstractToCanonical.TryGetValue(abstractId, out var canonical) || numId < canonical)
			{
				abstractToCanonical[abstractId] = numId;
			}
		}

		foreach (var (numId, abstractId) in instances)
		{
			var canonical = abstractToCanonical[abstractId];
			if (canonical != numId)
			{
				_options.NumberingIdNormalization[numId] = canonical;
			}
		}
	}

	private static (int NumId, int AbstractId)? TryGetInstanceIds(DocumentFormat.OpenXml.Wordprocessing.NumberingInstance instance)
	{
		var numId = instance.NumberID?.Value;
		var abstractId = instance.AbstractNumId?.Val?.Value;
		return numId is null || abstractId is null ? null : (numId.Value, abstractId.Value);
	}
}