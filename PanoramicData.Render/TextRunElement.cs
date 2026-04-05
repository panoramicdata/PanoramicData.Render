namespace PanoramicData.Render;

/// <summary>
/// Represents a text content element within a run.
/// </summary>
internal sealed class TextRunElement : RunElement
{
	/// <summary>
	/// Gets the text content.
	/// </summary>
	public required string Text { get; init; }
}
