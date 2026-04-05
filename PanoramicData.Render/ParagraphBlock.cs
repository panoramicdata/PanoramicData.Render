namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Represents a paragraph block in the document.
/// </summary>
internal sealed class ParagraphBlock : DocumentBlock
{
	/// <summary>
	/// Gets the original OpenXML paragraph element.
	/// </summary>
	public required Paragraph SourceElement { get; init; }

	/// <summary>
	/// Gets the paragraph style ID (e.g. "Heading1"), or <see langword="null"/> if none is set.
	/// </summary>
	public string? StyleId { get; init; }

	/// <summary>
	/// Gets the numbering definition ID, or <see langword="null"/> if the paragraph is not numbered.
	/// </summary>
	public int? NumberingId { get; init; }

	/// <summary>
	/// Gets the numbering level (0-based), or <see langword="null"/> if the paragraph is not numbered.
	/// </summary>
	public int? NumberingLevel { get; init; }
}
