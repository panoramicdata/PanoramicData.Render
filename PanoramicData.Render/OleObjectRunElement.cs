namespace PanoramicData.Render;

/// <summary>
/// Represents an OLE embedded object detected in a drawing element.
/// </summary>
internal sealed class OleObjectRunElement : ExtentRunElement
{
	/// <summary>
	/// Gets the relationship ID referencing the OLE object part.
	/// </summary>
	public required string RelationshipId { get; init; }

	/// <summary>
	/// Gets the relationship ID of a preview image (EMF/WMF), or empty when none.
	/// </summary>
	public string PreviewImageRelationshipId { get; init; } = string.Empty;

	/// <summary>
	/// Gets a value indicating whether a preview image is available.
	/// </summary>
	public bool HasPreviewImage => !string.IsNullOrEmpty(PreviewImageRelationshipId);
}
