namespace PanoramicData.Render;

/// <summary>
/// The result of rendering a DOCX document, providing access to individual pages
/// and whole-document export operations.
/// </summary>
public sealed class RenderResult
{
	private readonly IReadOnlyList<LayoutPage> _layoutPages;
	private readonly RenderOptions _options;

	internal RenderResult(IReadOnlyList<LayoutPage> layoutPages, RenderOptions options, FieldUpdateResult? fieldUpdateResult = null)
	{
		_layoutPages = layoutPages;
		_options = options;
		FieldUpdateResult = fieldUpdateResult;

		Pages = layoutPages
			.Select(lp => new RenderedPage(lp, options))
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

		var pdfBytes = PdfPageRenderer.RenderPages(_layoutPages, _options, metadata);
		return output.WriteAsync(pdfBytes, cancellationToken).AsTask();
	}

	/// <summary>
	/// Exports the document as a PDF byte array.
	/// </summary>
	/// <param name="metadata">Optional PDF metadata to embed in the document.</param>
	/// <returns>The rendered PDF document bytes.</returns>
	public byte[] ToPdf(PdfMetadata? metadata = null)
		=> PdfPageRenderer.RenderPages(_layoutPages, _options, metadata);
}
