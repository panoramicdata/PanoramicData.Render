namespace PanoramicData.Render;

/// <summary>
/// Specifies the style of underline applied to a text run.
/// Maps to OpenXML <c>w:u</c> element <c>w:val</c> attribute values.
/// </summary>
internal enum UnderlineStyle
{
	/// <summary>No underline.</summary>
	None,

	/// <summary>Single solid underline.</summary>
	Single,

	/// <summary>Double solid underline.</summary>
	Double,

	/// <summary>Thick solid underline.</summary>
	Thick,

	/// <summary>Single dotted underline.</summary>
	Dotted,

	/// <summary>Thick dotted underline.</summary>
	DottedHeavy,

	/// <summary>Single dashed underline.</summary>
	Dash,

	/// <summary>Thick dashed underline.</summary>
	DashedHeavy,

	/// <summary>Single long-dashed underline.</summary>
	DashLong,

	/// <summary>Thick long-dashed underline.</summary>
	DashLongHeavy,

	/// <summary>Single dash-dot underline.</summary>
	DotDash,

	/// <summary>Thick dash-dot underline.</summary>
	DashDotHeavy,

	/// <summary>Single dash-dot-dot underline.</summary>
	DotDotDash,

	/// <summary>Thick dash-dot-dot underline.</summary>
	DashDotDotHeavy,

	/// <summary>Single wavy underline.</summary>
	Wave,

	/// <summary>Double wavy underline.</summary>
	WavyDouble,

	/// <summary>Heavy wavy underline.</summary>
	WavyHeavy,

	/// <summary>Underline words only (spaces are not underlined).</summary>
	Words
}
