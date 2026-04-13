namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Represents a single rendered page of a DOCX document.
/// </summary>
public sealed class RenderedPage
{
	private readonly LayoutPage _layoutPage;
	private readonly RenderOptions _options;
	private readonly IReadOnlyDictionary<string, ImageData> _images;
	private readonly Styles? _styles;

	internal RenderedPage(LayoutPage layoutPage, RenderOptions options, IReadOnlyDictionary<string, ImageData> images, Styles? styles = null)
	{
		_layoutPage = layoutPage;
		_options = options;
		_images = images;
		_styles = styles;
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
		var svgPages = SvgPageRenderer.RenderPages([_layoutPage], _options, _images, _styles);
		return svgPages[0];
	}

	/// <summary>
	/// Gets the internal layout page for this rendered page.
	/// </summary>
	internal LayoutPage LayoutPage => _layoutPage;
}
