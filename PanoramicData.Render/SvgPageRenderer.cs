namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Renders paginated layout pages to standalone SVG documents.
/// </summary>
internal static class SvgPageRenderer
{
	private const float DefaultPageWidthTwips = 12240f;
	private const float DefaultPageHeightTwips = 15840f;

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
		var listState = new ListNumberingState();
		foreach (var page in pagesToRender)
		{
			var (pageWidth, pageHeight) = ResolveSafePageSize(page);
			try
			{
				var target = new SvgRenderTarget(pageWidth, pageHeight, renderOptions);
				RenderCommandEmitter.EmitPage(page, target, renderOptions, totalPageCount, renderTimestampUtc, listState, images, styles);
				svgPages.Add(target.BuildSvg());
			}
			catch
			{
				// Keep rendering remaining pages when one page has malformed pagination/layout data.
				var fallbackTarget = new SvgRenderTarget(pageWidth, pageHeight, renderOptions);
				svgPages.Add(fallbackTarget.BuildSvg());
			}
		}

		return svgPages;
	}

	private static (float PageWidthTwips, float PageHeightTwips) ResolveSafePageSize(LayoutPage page)
	{
		var width = (float)page.Section.PageWidth;
		var height = (float)page.Section.PageHeight;

		if (!float.IsFinite(width) || width <= 0f)
		{
			width = DefaultPageWidthTwips;
		}

		if (!float.IsFinite(height) || height <= 0f)
		{
			height = DefaultPageHeightTwips;
		}

		return (width, height);
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
