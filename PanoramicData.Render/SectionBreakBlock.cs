namespace PanoramicData.Render;

/// <summary>
/// Represents a section break between document sections.
/// </summary>
internal sealed class SectionBreakBlock : DocumentBlock
{
	/// <summary>
	/// Gets the parsed section properties for the section that precedes this break.
	/// </summary>
	public required SectionInfo SectionInfo { get; init; }
}
