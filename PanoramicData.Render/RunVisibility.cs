namespace PanoramicData.Render;

/// <summary>
/// Determines whether a text run should be included in layout based on the
/// <c>w:vanish</c> toggle property and rendering options.
/// Hidden text (vanish = true) is excluded from layout by default unless
/// the rendering options specify that hidden text should be displayed.
/// </summary>
internal static class RunVisibility
{
	/// <summary>
	/// Determines whether a run should be included in the layout.
	/// </summary>
	/// <param name="vanish">Whether the vanish (hidden) toggle is active for this run.</param>
	/// <param name="showHiddenText">Whether hidden text should be displayed (from rendering options).</param>
	/// <returns><see langword="true"/> when the run should be included in layout; otherwise <see langword="false"/>.</returns>
	public static bool IsVisible(bool vanish, bool showHiddenText)
	{
		if (!vanish)
		{
			return true;
		}

		return showHiddenText;
	}
}
