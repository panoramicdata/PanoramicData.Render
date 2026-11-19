namespace PanoramicData.Render;

/// <summary>
/// Represents a floating (anchored) image within a run.
/// </summary>
internal sealed class AnchorImageRunElement : ImageRunElement
{
	/// <summary>
	/// Gets the anchor positioning metadata.
	/// </summary>
	public AnchorPlacementInfo Placement { get; init; } = AnchorPlacementInfo.None;
}
