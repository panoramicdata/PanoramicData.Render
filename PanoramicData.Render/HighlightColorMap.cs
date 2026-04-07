namespace PanoramicData.Render;

/// <summary>
/// Maps <see cref="HighlightColor"/> values to their fixed hex RGB strings
/// as defined by the Word specification.
/// </summary>
internal static class HighlightColorMap
{
	/// <summary>
	/// Returns the 6-character hex RGB string for a <see cref="HighlightColor"/>,
	/// or <see langword="null"/> when the value is <see cref="HighlightColor.None"/>.
	/// </summary>
	/// <param name="color">The highlight color to look up.</param>
	/// <returns>The hex RGB string (e.g., <c>"FFFF00"</c>), or <see langword="null"/> for <see cref="HighlightColor.None"/>.</returns>
	public static string? ToHexRgb(HighlightColor color) => color switch
	{
		HighlightColor.None => null,
		HighlightColor.Black => "000000",
		HighlightColor.Blue => "0000FF",
		HighlightColor.Cyan => "00FFFF",
		HighlightColor.DarkBlue => "000080",
		HighlightColor.DarkCyan => "008080",
		HighlightColor.DarkGray => "808080",
		HighlightColor.DarkGreen => "008000",
		HighlightColor.DarkMagenta => "800080",
		HighlightColor.DarkRed => "800000",
		HighlightColor.DarkYellow => "808000",
		HighlightColor.Green => "00FF00",
		HighlightColor.LightGray => "C0C0C0",
		HighlightColor.Magenta => "FF00FF",
		HighlightColor.Red => "FF0000",
		HighlightColor.White => "FFFFFF",
		HighlightColor.Yellow => "FFFF00",
		_ => null
	};
}
