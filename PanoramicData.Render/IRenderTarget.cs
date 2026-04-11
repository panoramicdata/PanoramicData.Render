namespace PanoramicData.Render;

/// <summary>
/// Abstraction for backend-specific drawing commands.
/// </summary>
internal interface IRenderTarget
{
	/// <summary>
	/// Draws text at the specified baseline position.
	/// </summary>
	/// <param name="text">The text to draw.</param>
	/// <param name="baselineXTwips">The text baseline X coordinate in twips.</param>
	/// <param name="baselineYTwips">The text baseline Y coordinate in twips.</param>
	/// <param name="font">The font style information.</param>
	/// <param name="brush">The text brush.</param>
	void DrawText(string text, float baselineXTwips, float baselineYTwips, RenderFont font, RenderBrush brush);

	/// <summary>
	/// Draws a line segment.
	/// </summary>
	/// <param name="from">The start point in twips.</param>
	/// <param name="to">The end point in twips.</param>
	/// <param name="stroke">The stroke settings.</param>
	void DrawLine(RenderPoint from, RenderPoint to, RenderStroke stroke);

	/// <summary>
	/// Draws a rectangle.
	/// </summary>
	/// <param name="rect">The rectangle bounds in twips.</param>
	/// <param name="fill">The fill brush, or <see langword="null"/> for no fill.</param>
	/// <param name="stroke">The stroke, or <see langword="null"/> for no outline.</param>
	void DrawRect(RenderRect rect, RenderBrush? fill, RenderStroke? stroke);

	/// <summary>
	/// Draws an image.
	/// </summary>
	/// <param name="image">The image data.</param>
	/// <param name="rect">The destination rectangle in twips.</param>
	void DrawImage(ImageData image, RenderRect rect);

	/// <summary>
	/// Draws an arbitrary path.
	/// </summary>
	/// <param name="pathData">Path data in target-independent string form.</param>
	/// <param name="fill">The fill brush, or <see langword="null"/> for no fill.</param>
	/// <param name="stroke">The stroke, or <see langword="null"/> for no outline.</param>
	void DrawPath(string pathData, RenderBrush? fill, RenderStroke? stroke);

	/// <summary>
	/// Pushes a clipping rectangle onto the clip stack.
	/// </summary>
	/// <param name="clipRect">The clip rectangle in twips.</param>
	void PushClip(RenderRect clipRect);

	/// <summary>
	/// Pops the current clipping rectangle from the clip stack.
	/// </summary>
	void PopClip();

	/// <summary>
	/// Sets a hyperlink region.
	/// </summary>
	/// <param name="rect">The clickable region in twips.</param>
	/// <param name="uri">The link target URI.</param>
	void SetHyperlink(RenderRect rect, string uri);

	/// <summary>
	/// Emits a named destination (bookmark anchor) at the specified position.
	/// </summary>
	/// <param name="name">The destination name (must match bookmark names used in internal hyperlinks).</param>
	/// <param name="xTwips">The X coordinate of the destination in twips.</param>
	/// <param name="yTwips">The Y coordinate of the destination in twips.</param>
	void SetNamedDestination(string name, float xTwips, float yTwips);

	/// <summary>
	/// Draws text rotated around a centre point.
	/// </summary>
	/// <param name="text">The text to draw.</param>
	/// <param name="centerXTwips">The rotation centre X coordinate in twips.</param>
	/// <param name="centerYTwips">The rotation centre Y coordinate in twips.</param>
	/// <param name="rotationDegrees">The clockwise rotation in degrees.</param>
	/// <param name="font">The font style information.</param>
	/// <param name="brush">The text brush.</param>
	void DrawRotatedText(string text, float centerXTwips, float centerYTwips, float rotationDegrees, RenderFont font, RenderBrush brush);
}
