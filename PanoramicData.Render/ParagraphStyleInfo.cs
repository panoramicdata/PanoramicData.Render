namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Represents a parsed paragraph style definition and its direct metadata.
/// </summary>
internal sealed class ParagraphStyleInfo
{
	/// <summary>
	/// Gets the style identifier.
	/// </summary>
	public required string StyleId { get; init; }

	/// <summary>
	/// Gets the optional human-readable style name.
	/// </summary>
	public string? Name { get; init; }

	/// <summary>
	/// Gets the optional parent style identifier from <c>w:basedOn</c>.
	/// </summary>
	public string? BasedOnStyleId { get; init; }

	/// <summary>
	/// Gets whether this style is marked as the default paragraph style.
	/// </summary>
	public bool IsDefault { get; init; }

	/// <summary>
	/// Gets the paragraph properties declared on this style.
	/// </summary>
	public required StyleParagraphProperties Properties { get; init; }

	/// <summary>
	/// Gets the run properties declared on this style.
	/// </summary>
	public StyleRunProperties? RunProperties { get; init; }
}
