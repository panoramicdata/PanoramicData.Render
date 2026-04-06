namespace PanoramicData.Render;

using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Represents effective paragraph/run formatting after applying the full style cascade.
/// </summary>
internal sealed class EffectiveFormatting
{
	/// <summary>
	/// Gets the merged paragraph properties.
	/// </summary>
	public required ParagraphProperties ParagraphProperties { get; init; }

	/// <summary>
	/// Gets the merged run properties.
	/// </summary>
	public required RunProperties RunProperties { get; init; }

	/// <summary>
	/// Gets the resolved toggle state after all style and direct formatting stages.
	/// </summary>
	public required ToggleState ToggleState { get; init; }

	/// <summary>
	/// Gets the resolved run color (hex RGB) when available.
	/// </summary>
	public string? ResolvedRunColor { get; init; }

	/// <summary>
	/// Gets the numbering level style that participated in resolution.
	/// </summary>
	public NumberingLevelStyle? NumberingLevel { get; init; }
}
