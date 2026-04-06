namespace PanoramicData.Render;

/// <summary>
/// Holds parsed toggle instructions for run-level OOXML toggle properties.
/// </summary>
internal sealed class ToggleProperties
{
	public ToggleInstruction Bold { get; init; }
	public ToggleInstruction Italic { get; init; }
	public ToggleInstruction Caps { get; init; }
	public ToggleInstruction SmallCaps { get; init; }
	public ToggleInstruction Strike { get; init; }
	public ToggleInstruction DoubleStrike { get; init; }
	public ToggleInstruction Vanish { get; init; }
	public ToggleInstruction Emboss { get; init; }
	public ToggleInstruction Imprint { get; init; }
	public ToggleInstruction Outline { get; init; }
	public ToggleInstruction Shadow { get; init; }
}
