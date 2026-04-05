namespace PanoramicData.Render;

/// <summary>
/// Represents a reference from a section to a header or footer part.
/// </summary>
/// <param name="Type">The type of header/footer (default, first, even).</param>
/// <param name="RelationshipId">The OpenXML relationship ID referencing the header/footer part.</param>
internal sealed record HeaderFooterReference(HeaderFooterKind Type, string RelationshipId);
