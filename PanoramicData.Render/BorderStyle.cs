namespace PanoramicData.Render;

/// <summary>
/// Specifies the visual style of a paragraph border line.
/// Corresponds to the OOXML w:val attribute on border elements (BorderValues).
/// </summary>
internal enum BorderStyle
{
	/// <summary>
	/// No border.
	/// </summary>
	None,

	/// <summary>
	/// Single solid line.
	/// </summary>
	Single,

	/// <summary>
	/// Double line.
	/// </summary>
	Double,

	/// <summary>
	/// Dotted line.
	/// </summary>
	Dotted,

	/// <summary>
	/// Dashed line.
	/// </summary>
	Dashed,

	/// <summary>
	/// Dot-dash pattern.
	/// </summary>
	DotDash,

	/// <summary>
	/// Dot-dot-dash pattern.
	/// </summary>
	DotDotDash,

	/// <summary>
	/// Triple line.
	/// </summary>
	Triple,

	/// <summary>
	/// Thick solid line.
	/// </summary>
	Thick,

	/// <summary>
	/// Thin-thick small gap.
	/// </summary>
	ThinThickSmallGap,

	/// <summary>
	/// Thick-thin small gap.
	/// </summary>
	ThickThinSmallGap,

	/// <summary>
	/// Thin-thick-thin small gap.
	/// </summary>
	ThinThickThinSmallGap,

	/// <summary>
	/// Wavy line.
	/// </summary>
	Wave,

	/// <summary>
	/// Double wavy line.
	/// </summary>
	DoubleWave,

	/// <summary>
	/// 3D embossed border.
	/// </summary>
	ThreeDEmboss,

	/// <summary>
	/// 3D engraved border.
	/// </summary>
	ThreeDEngrave,

	/// <summary>
	/// Border with shadow effect.
	/// </summary>
	Shadow
}
