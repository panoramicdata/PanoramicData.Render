namespace PanoramicData.Render;

/// <summary>
/// Represents the parsed content of a header or footer part.
/// </summary>
/// <param name="Kind">The header/footer type (default, first page, even pages).</param>
/// <param name="RelationshipId">The OpenXML relationship ID of the part.</param>
/// <param name="Blocks">The parsed content blocks within the header or footer.</param>
internal sealed record HeaderFooterContent(
	HeaderFooterKind Kind,
	string RelationshipId,
	IReadOnlyList<DocumentBlock> Blocks);
