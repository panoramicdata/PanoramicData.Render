namespace PanoramicData.Render;

/// <summary>
/// Information captured from a <c>w:bookmarkEnd</c> element.
/// </summary>
/// <param name="Id">The bookmark ID that pairs with a corresponding <see cref="BookmarkStartInfo"/>.</param>
internal readonly record struct BookmarkEndInfo(int Id);
