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
}
