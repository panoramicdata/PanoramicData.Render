namespace PanoramicData.Render;

/// <summary>
/// Represents a 2D point in twips.
/// </summary>
/// <param name="XTwips">The X coordinate in twips.</param>
/// <param name="YTwips">The Y coordinate in twips.</param>
internal readonly record struct RenderPoint(float XTwips, float YTwips);

/// <summary>
/// Represents an axis-aligned rectangle in twips.
/// </summary>
/// <param name="XTwips">The left coordinate in twips.</param>
/// <param name="YTwips">The top coordinate in twips.</param>
/// <param name="WidthTwips">The width in twips.</param>
/// <param name="HeightTwips">The height in twips.</param>
internal readonly record struct RenderRect(float XTwips, float YTwips, float WidthTwips, float HeightTwips);

/// <summary>
/// Represents an RGBA color value.
/// </summary>
/// <param name="R">The red channel.</param>
/// <param name="G">The green channel.</param>
/// <param name="B">The blue channel.</param>
/// <param name="A">The alpha channel (0-255).</param>
internal readonly record struct RenderColor(byte R, byte G, byte B, byte A = 255);

/// <summary>
/// Represents font settings used by render targets.
/// </summary>
/// <param name="Family">The font family name.</param>
/// <param name="SizePoints">The font size in points.</param>
/// <param name="IsBold">Whether bold style is applied.</param>
/// <param name="IsItalic">Whether italic style is applied.</param>
internal readonly record struct RenderFont(string Family, float SizePoints, bool IsBold = false, bool IsItalic = false);

/// <summary>
/// Represents stroke styling for lines and outlines.
/// </summary>
/// <param name="Color">The stroke color.</param>
/// <param name="WidthTwips">The stroke width in twips.</param>
internal readonly record struct RenderStroke(RenderColor Color, float WidthTwips);

/// <summary>
/// Base type for brush definitions.
/// </summary>
internal abstract record RenderBrush;

/// <summary>
/// Represents a solid color brush.
/// </summary>
/// <param name="Color">The solid fill color.</param>
internal sealed record SolidRenderBrush(RenderColor Color) : RenderBrush;

/// <summary>
/// Represents a linear gradient brush between two points.
/// </summary>
/// <param name="Start">The gradient start point in twips.</param>
/// <param name="End">The gradient end point in twips.</param>
/// <param name="StartColor">The start color.</param>
/// <param name="EndColor">The end color.</param>
internal sealed record LinearGradientRenderBrush(RenderPoint Start, RenderPoint End, RenderColor StartColor, RenderColor EndColor) : RenderBrush;
