namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Placeholder block for a table element. Full table parsing is deferred to Phase 4.
/// </summary>
internal sealed class TablePlaceholderBlock : DocumentBlock
{
	/// <summary>
	/// Gets the original OpenXML table element.
	/// </summary>
	public required Table TableElement { get; init; }
}
