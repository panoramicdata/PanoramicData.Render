namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Represents a parsed footnote or endnote definition.
/// </summary>
/// <param name="Id">The OpenXML note identifier.</param>
/// <param name="Type">The optional note type (for example separator).</param>
/// <param name="Blocks">The parsed block content of the note definition.</param>
internal sealed record NoteDefinition(
	int Id,
	FootnoteEndnoteValues? Type,
	IReadOnlyList<DocumentBlock> Blocks);
