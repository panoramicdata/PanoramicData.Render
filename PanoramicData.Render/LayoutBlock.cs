namespace PanoramicData.Render;

/// <summary>
/// Associates a <see cref="DocumentBlock"/> with its computed height for pagination.
/// </summary>
/// <param name="Block">The document block.</param>
/// <param name="HeightTwips">The computed height of the block in twips.</param>
internal readonly record struct LayoutBlock(DocumentBlock Block, float HeightTwips);
