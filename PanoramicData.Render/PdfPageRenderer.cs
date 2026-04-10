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
	/// <param name="metadata">Optional PDF metadata.</param>
	/// <returns>The rendered PDF document bytes.</returns>
	public static byte[] RenderPages(IReadOnlyList<LayoutPage> pages, RenderOptions? options = null, PdfMetadata? metadata = null)
	{
		ArgumentNullException.ThrowIfNull(pages);
		var renderOptions = options ?? new RenderOptions();
		var pagesToRender = ApplyPageRange(pages, renderOptions.PageRange);
		if (pagesToRender.Count == 0)
		{
			return [];
		}

		using var target = new PdfRenderTarget(pagesToRender[0].Section.PageWidth, pagesToRender[0].Section.PageHeight, metadata);
		RenderCommandEmitter.EmitPage(pagesToRender[0], target, renderOptions);
		for (var index = 1; index < pagesToRender.Count; index++)
		{
			var page = pagesToRender[index];
			target.BeginPage(page.Section.PageWidth, page.Section.PageHeight);
			RenderCommandEmitter.EmitPage(page, target, renderOptions);
		}

		return target.BuildPdf();
	}

	private static IReadOnlyList<LayoutPage> ApplyPageRange(IReadOnlyList<LayoutPage> pages, Range? pageRange)
	{
		if (pageRange is null)
		{
			return pages;
		}

		var start = pageRange.Value.Start.GetOffset(pages.Count);
		var end = pageRange.Value.End.GetOffset(pages.Count);
		if (start < 0 || end < start || start > pages.Count)
		{
			return [];
		}

		end = Math.Min(end, pages.Count);
		var result = new List<LayoutPage>(end - start);
		for (var index = start; index < end; index++)
		{
			result.Add(pages[index]);
		}

		return result;
	}
}
