using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PanoramicData.Render;

/// <summary>
/// Main entry point for rendering DOCX documents to SVG and PDF output.
/// Thread-safe: a single instance can render multiple documents concurrently.
/// </summary>
public sealed partial class DocxRenderer
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
}
