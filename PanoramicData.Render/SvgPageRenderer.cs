namespace PanoramicData.Render;

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
	/// <returns>One SVG string per page in input order.</returns>
	public static IReadOnlyList<string> RenderPages(IReadOnlyList<LayoutPage> pages, RenderOptions? options = null)
	{
		ArgumentNullException.ThrowIfNull(pages);

		// Use default options if none provided
		var renderOptions = options ?? new RenderOptions();

		var svgPages = new List<string>(pages.Count);
		foreach (var page in pages)
		{
			var target = new SvgRenderTarget(page.Section.PageWidth, page.Section.PageHeight, renderOptions);
			RenderCommandEmitter.EmitPage(page, target, renderOptions);
			svgPages.Add(target.BuildSvg());
		}

		return svgPages;
	}
}
