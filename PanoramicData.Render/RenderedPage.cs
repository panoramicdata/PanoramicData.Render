namespace PanoramicData.Render;

/// <summary>
/// Represents a single rendered page of a DOCX document.
/// </summary>
public sealed class RenderedPage
{
	private readonly LayoutPage _layoutPage;
	private readonly RenderOptions _options;

	internal RenderedPage(LayoutPage layoutPage, RenderOptions options)
	{
		_layoutPage = layoutPage;
		_options = options;
	}

	/// <summary>
	/// Gets the width of the page in points (1/72 inch).
	/// </summary>
	public double WidthPoints => _layoutPage.Section.PageWidth / 20.0;

	/// <summary>
	/// Gets the height of the page in points (1/72 inch).
	/// </summary>
	public double HeightPoints => _layoutPage.Section.PageHeight / 20.0;

	/// <summary>
	/// Gets the 1-based page number.
	/// </summary>
	public int PageNumber => _layoutPage.PageNumber;

	/// <summary>
	/// Renders this page as a standalone SVG string.
	/// </summary>
	/// <returns>A complete SVG document for this single page.</returns>
	public string ToSvg()
	{
		var svgPages = SvgPageRenderer.RenderPages([_layoutPage], _options);
		return svgPages[0];
	}

	/// <summary>
	/// Gets the internal layout page for this rendered page.
	/// </summary>
	internal LayoutPage LayoutPage => _layoutPage;
}
