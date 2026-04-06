namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Represents a parsed character style definition and its direct metadata.
/// </summary>
internal sealed class CharacterStyleInfo
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
	/// Gets whether this style is marked as the default character style.
	/// </summary>
	public bool IsDefault { get; init; }

	/// <summary>
	/// Gets the run properties declared on this style.
	/// </summary>
	public required StyleRunProperties Properties { get; init; }
}
