namespace PanoramicData.Render;

/// <summary>
/// The 16 named highlight colors available in Microsoft Word, plus <see cref="None"/>.
/// Maps to OpenXML <c>w:highlight</c> element <c>w:val</c> attribute values.
/// Each value has a fixed RGB colour defined by the Word specification.
/// </summary>
internal enum HighlightColor
{
	/// <summary>No highlight.</summary>
	None,

	/// <summary>Black highlight (#000000).</summary>
	Black,

	/// <summary>Blue highlight (#0000FF).</summary>
	Blue,

	/// <summary>Cyan highlight (#00FFFF).</summary>
	Cyan,

	/// <summary>Dark blue highlight (#000080).</summary>
	DarkBlue,

	/// <summary>Dark cyan highlight (#008080).</summary>
	DarkCyan,

	/// <summary>Dark gray highlight (#808080).</summary>
	DarkGray,

	/// <summary>Dark green highlight (#008000).</summary>
	DarkGreen,

	/// <summary>Dark magenta highlight (#800080).</summary>
	DarkMagenta,

	/// <summary>Dark red highlight (#800000).</summary>
	DarkRed,

	/// <summary>Dark yellow highlight (#808000).</summary>
	DarkYellow,

	/// <summary>Green highlight (#00FF00).</summary>
	Green,

	/// <summary>Light gray highlight (#C0C0C0).</summary>
	LightGray,

	/// <summary>Magenta highlight (#FF00FF).</summary>
	Magenta,

	/// <summary>Red highlight (#FF0000).</summary>
	Red,

	/// <summary>White highlight (#FFFFFF).</summary>
	White,

	/// <summary>Yellow highlight (#FFFF00).</summary>
	Yellow
}
