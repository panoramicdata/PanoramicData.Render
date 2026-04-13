namespace PanoramicData.Render;

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

		// 2. Parse document blocks from the body
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		_logger.LogDebug("Parsed {BlockCount} document blocks", blocks.Count);

		// 3. Measure blocks into layout blocks
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		_logger.LogDebug("Measured {LayoutBlockCount} layout blocks", layoutBlocks.Count);

		// 4. Determine body section info (final section properties)
		var bodySectionInfo = GetBodySectionInfo(doc);

		if (_options.FieldUpdate is null)
		{
			// 5. Paginate
			var pages = PageBuilder.PaginateDocument(layoutBlocks, bodySectionInfo);
			_logger.LogDebug("Paginated into {PageCount} pages", pages.Count);

			return new RenderResult(pages, _options);
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
				layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
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
			layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
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

		return new RenderResult(updatedPages, _options, fieldUpdateResult);
	}

	private static SectionInfo GetBodySectionInfo(DocxDocument doc)
	{
		var bodySectPr = doc.DocumentBody
			.GetFirstChild<DocumentFormat.OpenXml.Wordprocessing.SectionProperties>();

		return bodySectPr is not null
			? SectionInfoParser.Parse(bodySectPr)
			: new SectionInfo();
	}
}
