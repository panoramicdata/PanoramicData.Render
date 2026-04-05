namespace PanoramicData.Render;

/// <summary>
/// Represents a break element within a run (line, page, or column break).
/// </summary>
internal sealed class BreakRunElement : RunElement
{
	/// <summary>
	/// Gets the type of break.
	/// </summary>
	public required RunBreakType BreakType { get; init; }
}
