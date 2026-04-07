namespace PanoramicData.Render;

/// <summary>
/// Specifies vertical text alignment for superscript and subscript.
/// Maps to OpenXML <c>w:vertAlign</c> element values on <c>w:rPr</c>.
/// </summary>
internal enum VerticalTextAlignment
{
	/// <summary>Normal baseline alignment (no superscript or subscript).</summary>
	Baseline,

	/// <summary>Superscript: raised above the baseline with reduced font size.</summary>
	Superscript,

	/// <summary>Subscript: lowered below the baseline with reduced font size.</summary>
	Subscript
}
