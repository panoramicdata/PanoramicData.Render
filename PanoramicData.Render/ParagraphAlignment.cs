namespace PanoramicData.Render;

/// <summary>
/// Represents the horizontal alignment mode for a paragraph.
/// </summary>
internal enum ParagraphAlignment
{
	/// <summary>
	/// Flush left (ragged right).
	/// </summary>
	Left,

	/// <summary>
	/// Centered.
	/// </summary>
	Center,

	/// <summary>
	/// Flush right (ragged left).
	/// </summary>
	Right,

	/// <summary>
	/// Justified — extra space distributed across glue items.
	/// </summary>
	Justified
}
