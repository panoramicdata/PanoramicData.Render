namespace PanoramicData.Render;

/// <summary>
/// Specifies the leader character that fills the space before a tab stop.
/// Corresponds to the OOXML w:tab/@w:leader attribute (TabStopLeaderCharValues).
/// </summary>
internal enum TabStopLeader
{
	/// <summary>
	/// No leader fill character.
	/// </summary>
	None,

	/// <summary>
	/// Period (.) leader characters.
	/// </summary>
	Dot,

	/// <summary>
	/// Hyphen (-) leader characters.
	/// </summary>
	Hyphen,

	/// <summary>
	/// Heavy (thick) line leader.
	/// </summary>
	Heavy,

	/// <summary>
	/// Middle dot (·) leader characters.
	/// </summary>
	MiddleDot,

	/// <summary>
	/// Underscore (_) leader characters.
	/// </summary>
	Underscore
}
