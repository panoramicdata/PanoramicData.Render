namespace PanoramicData.Render;

/// <summary>
/// Represents line numbering properties for a document section.
/// Rendering of line numbers is best-effort; this type tracks the metadata.
/// </summary>
/// <param name="CountBy">Which lines to number (e.g. 1 = every line, 5 = every 5th). Default: 1.</param>
/// <param name="Start">The starting line number. Default: 1.</param>
/// <param name="Restart">When to restart numbering.</param>
/// <param name="DistanceTwips">Distance between the line number and the text, in twips. Default: 0.</param>
internal readonly record struct LineNumberingInfo(
	int CountBy = 1,
	int Start = 1,
	LineNumberRestart Restart = LineNumberRestart.NewPage,
	int DistanceTwips = 0);
