namespace PanoramicData.Render;

/// <summary>
/// Represents a resolved numbering level style after applying numbering instance overrides.
/// </summary>
internal sealed class NumberingLevelStyle
{
	/// <summary>
	/// Gets the numbering level index.
	/// </summary>
	public int LevelIndex { get; init; }

	/// <summary>
	/// Gets the starting number for this level.
	/// </summary>
	public int Start { get; init; }

	/// <summary>
	/// Gets the numbering format token (for example, <c>decimal</c>, <c>lowerLetter</c>).
	/// </summary>
	public string? NumberFormat { get; init; }

	/// <summary>
	/// Gets the level text pattern (for example, <c>%1.</c>).
	/// </summary>
	public string? LevelText { get; init; }

	/// <summary>
	/// Gets the 1-based higher level that restarts this level, or <see langword="null"/> when no restart rule is defined.
	/// A value of 1 means this level restarts whenever level 0 increments.
	/// </summary>
	public int? RestartAfterLevel { get; init; }
}
