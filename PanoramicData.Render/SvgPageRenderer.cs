namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Renders paginated layout pages to standalone SVG documents.
/// </summary>
internal static class SvgPageRenderer
{
	/// <summary>
	/// Renders each input page as a standalone SVG string.
	/// </summary>
	/// <param name="pages">The paginated layout pages.</param>
	/// <param name="options">Optional render options.</param>
	/// <param name="images">Optional pre-loaded image data keyed by relationship ID.</param>
	/// <param name="styles">Optional cloned document styles for table-style resolution.</param>
	/// <param name="totalPageCountOverride">Optional total page count used for NUMPAGES fields. When <see langword="null"/>, the count of <paramref name="pages"/> is used.</param>
	/// <returns>One SVG string per page in input order.</returns>
	public static IReadOnlyList<string> RenderPages(IReadOnlyList<LayoutPage> pages, RenderOptions? options = null, IReadOnlyDictionary<string, ImageData>? images = null, Styles? styles = null, int? totalPageCountOverride = null)
	{
		ArgumentNullException.ThrowIfNull(pages);

		// Use default options if none provided
		var renderOptions = options ?? new RenderOptions();
		var pagesToRender = ApplyPageRange(pages, renderOptions.PageRange);
		var renderTimestampUtc = DateTime.UtcNow;
		var totalPageCount = totalPageCountOverride ?? pagesToRender.Count;

		var svgPages = new List<string>(pagesToRender.Count);
		foreach (var page in pagesToRender)
		{
			var target = new SvgRenderTarget(page.Section.PageWidth, page.Section.PageHeight, renderOptions);
			RenderCommandEmitter.EmitPage(page, target, renderOptions, totalPageCount, renderTimestampUtc, images: images, styles: styles);
			svgPages.Add(target.BuildSvg());
		}

		return svgPages;
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
