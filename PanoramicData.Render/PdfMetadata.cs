namespace PanoramicData.Render;

/// <summary>
/// Metadata values written to generated PDF documents.
/// </summary>
/// <param name="Title">The document title.</param>
/// <param name="Author">The document author.</param>
/// <param name="CreationDateUtc">The UTC creation timestamp.</param>
internal readonly record struct PdfMetadata(string? Title, string? Author, DateTime? CreationDateUtc);
