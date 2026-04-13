namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// The result of rendering a DOCX document, providing access to individual pages
/// and whole-document export operations.
/// </summary>
public sealed class RenderResult
{
	private readonly IReadOnlyList<LayoutPage> _layoutPages;
	private readonly RenderOptions _options;

	internal RenderResult(IReadOnlyList<LayoutPage> layoutPages, RenderOptions options, FieldUpdateResult? fieldUpdateResult = null, IReadOnlyDictionary<string, ImageData>? images = null, Styles? styles = null)
	{
		_layoutPages = layoutPages;
		_options = options;
		Images = images ?? new Dictionary<string, ImageData>();
		Styles = styles;
		FieldUpdateResult = fieldUpdateResult;

		Pages = layoutPages
			.Select(lp => new RenderedPage(lp, options, Images, Styles))
			.ToList()
			.AsReadOnly();
	}

	/// <summary>
	/// Gets the rendered pages.
	/// </summary>
	public IReadOnlyList<RenderedPage> Pages { get; }

	/// <summary>
	/// Gets diagnostic information about any field updates applied during rendering.
	/// </summary>
	public FieldUpdateResult? FieldUpdateResult { get; }

	/// <summary>
	/// Gets the pre-loaded image data keyed by relationship ID.
	/// </summary>
	internal IReadOnlyDictionary<string, ImageData> Images { get; }

	/// <summary>
	/// Gets the cloned document styles for table-style conditional formatting resolution.
	/// </summary>
	internal Styles? Styles { get; }

	/// <summary>
	/// Exports the document as a PDF to the specified stream.
	/// </summary>
	/// <param name="output">The stream to write the PDF to.</param>
	/// <param name="metadata">Optional PDF metadata to embed in the document.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task that completes when the PDF has been written.</returns>
	public Task ToPdfAsync(Stream output, PdfMetadata? metadata = null, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(output);

		cancellationToken.ThrowIfCancellationRequested();

		var pdfBytes = PdfPageRenderer.RenderPages(_layoutPages, _options, metadata, Images, Styles);
		return output.WriteAsync(pdfBytes, cancellationToken).AsTask();
	}

	/// <summary>
	/// Exports the document as a PDF byte array.
	/// </summary>
	/// <param name="metadata">Optional PDF metadata to embed in the document.</param>
	/// <returns>The rendered PDF document bytes.</returns>
	public byte[] ToPdf(PdfMetadata? metadata = null)
		=> PdfPageRenderer.RenderPages(_layoutPages, _options, metadata, Images, Styles);
}
