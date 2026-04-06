namespace PanoramicData.Render;

/// <summary>
/// Represents resolved boolean state for OOXML run-level toggle properties.
/// </summary>
internal sealed class ToggleState
{
	public bool Bold { get; init; }
	public bool Italic { get; init; }
	public bool Caps { get; init; }
	public bool SmallCaps { get; init; }
	public bool Strike { get; init; }
	public bool DoubleStrike { get; init; }
	public bool Vanish { get; init; }
	public bool Emboss { get; init; }
	public bool Imprint { get; init; }
	public bool Outline { get; init; }
	public bool Shadow { get; init; }
}
