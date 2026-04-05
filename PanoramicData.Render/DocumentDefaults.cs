namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Represents document-level default paragraph and run properties from <c>w:docDefaults</c>.
/// </summary>
internal sealed class DocumentDefaults
{
	/// <summary>
	/// Gets the default paragraph properties.
	/// </summary>
	public required ParagraphPropertiesBaseStyle ParagraphProperties { get; init; }

	/// <summary>
	/// Gets the default run properties.
	/// </summary>
	public required RunPropertiesBaseStyle RunProperties { get; init; }
}
