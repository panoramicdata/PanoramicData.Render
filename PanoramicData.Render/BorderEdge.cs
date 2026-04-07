namespace PanoramicData.Render;

/// <summary>
/// Specifies an edge of a paragraph or table cell for border purposes.
/// </summary>
internal enum BorderEdge
{
	/// <summary>
	/// Top edge.
	/// </summary>
	Top,

	/// <summary>
	/// Bottom edge.
	/// </summary>
	Bottom,

	/// <summary>
	/// Left edge.
	/// </summary>
	Left,

	/// <summary>
	/// Right edge.
	/// </summary>
	Right,

	/// <summary>
	/// Between adjacent paragraphs (e.g., list items sharing borders).
	/// </summary>
	Between,

	/// <summary>
	/// Vertical bar drawn before the paragraph content.
	/// </summary>
	Bar
}
