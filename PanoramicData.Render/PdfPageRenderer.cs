namespace PanoramicData.Render;

/// <summary>
/// Renders paginated layout pages to a PDF document.
/// </summary>
internal static class PdfPageRenderer
{
	/// <summary>
	/// Renders the input pages to a PDF byte array.
	/// </summary>
	/// <param name="pages">The paginated layout pages.</param>
	/// <param name="options">Optional render options.</param>
	/// <returns>The rendered PDF document bytes.</returns>
	public static byte[] RenderPages(IReadOnlyList<LayoutPage> pages, RenderOptions? options = null)
	{
		ArgumentNullException.ThrowIfNull(pages);
		if (pages.Count == 0)
		{
			return [];
		}

		var renderOptions = options ?? new RenderOptions();
		using var target = new PdfRenderTarget(pages[0].Section.PageWidth, pages[0].Section.PageHeight);
		RenderCommandEmitter.EmitPage(pages[0], target, renderOptions);
		return target.BuildPdf();
	}
}
