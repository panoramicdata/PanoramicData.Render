namespace PanoramicData.Render;

/// <summary>
/// Represents extracted image data from a DOCX package.
/// </summary>
/// <param name="Data">The raw image bytes.</param>
/// <param name="ContentType">The MIME content type (e.g. "image/png", "image/jpeg").</param>
internal sealed record ImageData(byte[] Data, string ContentType);
