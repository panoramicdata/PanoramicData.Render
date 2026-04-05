namespace PanoramicData.Render;

/// <summary>
/// Specifies the type of break within a run.
/// </summary>
internal enum RunBreakType
{
	/// <summary>A line break (soft return).</summary>
	Line,

	/// <summary>A page break.</summary>
	Page,

	/// <summary>A column break.</summary>
	Column
}
