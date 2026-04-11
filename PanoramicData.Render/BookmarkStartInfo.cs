namespace PanoramicData.Render;

/// <summary>
/// Information captured from a <c>w:bookmarkStart</c> element.
/// </summary>
/// <param name="Id">The bookmark ID that pairs with a corresponding <see cref="BookmarkEndInfo"/>.</param>
/// <param name="Name">The bookmark name (e.g. "_Toc123456", "MyBookmark").</param>
internal readonly record struct BookmarkStartInfo(int Id, string Name);
