namespace PanoramicData.Render;

/// <summary>
/// Specifies the type of a header or footer reference (default, first page, or even pages).
/// </summary>
internal enum HeaderFooterKind
{
	/// <summary>The default header or footer for odd pages.</summary>
	Default,

	/// <summary>The header or footer used on the first page of the section.</summary>
	First,

	/// <summary>The header or footer used on even-numbered pages.</summary>
	Even
}
