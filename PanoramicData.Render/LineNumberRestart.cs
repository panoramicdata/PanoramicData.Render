namespace PanoramicData.Render;

/// <summary>
/// Specifies when line numbering restarts.
/// </summary>
internal enum LineNumberRestart
{
	/// <summary>Line numbering restarts on each new page.</summary>
	NewPage,

	/// <summary>Line numbering restarts at each new section.</summary>
	NewSection,

	/// <summary>Line numbering is continuous across pages and sections.</summary>
	Continuous
}
