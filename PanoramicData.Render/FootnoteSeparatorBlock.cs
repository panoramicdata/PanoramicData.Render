namespace PanoramicData.Render;

/// <summary>
/// Represents the horizontal separator line drawn between body content and footnote content.
/// In Word, this is typically a short horizontal rule spanning approximately one-third of the page width.
/// </summary>
internal sealed class FootnoteSeparatorBlock : DocumentBlock
{
	/// <summary>
	/// The fraction of the page content width that the separator line spans.
	/// Word's default is approximately one-third.
	/// </summary>
	public const float DefaultWidthFraction = 1f / 3f;

	/// <summary>
	/// Gets the fraction of the page content width that the separator line spans.
	/// </summary>
	public float WidthFraction { get; init; } = DefaultWidthFraction;
}
