namespace PanoramicData.Render;

/// <summary>
/// Represents a parsed watermark from a DOCX document header.
/// </summary>
internal sealed class WatermarkInfo
{
	/// <summary>
	/// Gets the kind of watermark (text or image).
	/// </summary>
	public required WatermarkKind Kind { get; init; }

	/// <summary>
	/// Gets the watermark text (for text watermarks).
	/// </summary>
	public string? Text { get; init; }

	/// <summary>
	/// Gets the font family for text watermarks.
	/// </summary>
	public string? FontFamily { get; init; }

	/// <summary>
	/// Gets the fill color as a CSS/VML color string (e.g. "silver", "#C0C0C0").
	/// </summary>
	public string? FillColor { get; init; }

	/// <summary>
	/// Gets the fill opacity (0.0 to 1.0).
	/// </summary>
	public float Opacity { get; init; } = 0.5f;

	/// <summary>
	/// Gets the rotation angle in degrees.
	/// </summary>
	public float RotationDegrees { get; init; }

	/// <summary>
	/// Gets the width in twips.
	/// </summary>
	public float WidthTwips { get; init; }

	/// <summary>
	/// Gets the height in twips.
	/// </summary>
	public float HeightTwips { get; init; }

	/// <summary>
	/// Gets the image relationship ID (for image watermarks).
	/// </summary>
	public string? ImageRelationshipId { get; init; }

	/// <summary>
	/// Gets whether the watermark is horizontally centered.
	/// </summary>
	public bool IsHorizontallyCentered { get; init; } = true;

	/// <summary>
	/// Gets whether the watermark is vertically centered.
	/// </summary>
	public bool IsVerticallyCentered { get; init; } = true;
}
