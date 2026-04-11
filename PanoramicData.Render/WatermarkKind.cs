namespace PanoramicData.Render;

/// <summary>
/// The kind of watermark in a DOCX document.
/// </summary>
internal enum WatermarkKind
{
	/// <summary>
	/// A text watermark rendered using a VML text path.
	/// </summary>
	Text,

	/// <summary>
	/// An image watermark rendered using a VML image shape.
	/// </summary>
	Image
}
