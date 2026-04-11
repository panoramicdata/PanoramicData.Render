namespace PanoramicData.Render;

/// <summary>
/// Represents a parsed run with its character style and content elements.
/// </summary>
internal sealed class ParsedRun
{
	/// <summary>
	/// Gets the character style ID, or <see langword="null"/> if none is set.
	/// </summary>
	public string? StyleId { get; init; }

	/// <summary>
	/// Gets the content elements within this run.
	/// </summary>
	public required IReadOnlyList<RunElement> Elements { get; init; }

	/// <summary>
	/// Gets the hyperlink URI associated with this run, or <see langword="null"/> if the run is not inside a hyperlink.
	/// For external links this is the resolved URL; for internal bookmark links this is <c>#bookmarkName</c>.
	/// </summary>
	public string? HyperlinkUri { get; init; }
}
