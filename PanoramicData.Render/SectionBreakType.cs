namespace PanoramicData.Render;

/// <summary>
/// Represents the type of section break.
/// </summary>
internal enum SectionBreakType
{
	/// <summary>Section begins on the next page.</summary>
	NextPage,

	/// <summary>Section begins on the same page (continuous).</summary>
	Continuous,

	/// <summary>Section begins on the next even-numbered page.</summary>
	EvenPage,

	/// <summary>Section begins on the next odd-numbered page.</summary>
	OddPage,

	/// <summary>Section begins in the next column.</summary>
	NextColumn
}
