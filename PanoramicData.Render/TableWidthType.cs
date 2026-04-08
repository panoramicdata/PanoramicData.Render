namespace PanoramicData.Render;

/// <summary>
/// Specifies how a table width value should be interpreted.
/// </summary>
internal enum TableWidthUnit
{
	/// <summary>
	/// Width is automatically determined (no explicit value).
	/// </summary>
	Auto = 0,

	/// <summary>
	/// Width is specified in twips (absolute value).
	/// </summary>
	Dxa = 1,

	/// <summary>
	/// Width is specified as a percentage of the available space (in fiftieths of a percent).
	/// </summary>
	Pct = 2,

	/// <summary>
	/// No width specified (nil).
	/// </summary>
	Nil = 3,
}
