namespace PanoramicData.Render;

/// <summary>
/// Represents how an OOXML toggle property should affect an inherited boolean value.
/// </summary>
internal enum ToggleInstruction
{
	/// <summary>
	/// No toggle property was specified.
	/// </summary>
	None,

	/// <summary>
	/// Toggle the inherited value.
	/// </summary>
	Toggle,

	/// <summary>
	/// Force the value to false.
	/// </summary>
	SetFalse
}
