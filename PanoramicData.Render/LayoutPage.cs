namespace PanoramicData.Render;

/// <summary>
/// Represents a single paginated page containing laid-out blocks.
/// </summary>
internal sealed class LayoutPage
{
	/// <summary>
	/// Gets the section properties that apply to this page.
	/// </summary>
	public required SectionInfo Section { get; init; }

	/// <summary>
	/// Gets the 1-based page number.
	/// </summary>
	public required int PageNumber { get; init; }

	/// <summary>
	/// Gets the blocks assigned to this page.
	/// </summary>
	public required IReadOnlyList<LayoutBlock> Blocks { get; init; }
}
