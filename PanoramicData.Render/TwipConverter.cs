namespace PanoramicData.Render;

/// <summary>
/// Converts between twips and other units.
/// One twip = 1/1440 inch. One point = 1/72 inch = 20 twips.
/// </summary>
internal static class TwipConverter
{
	/// <summary>
	/// The number of twips per typographic point (1/72 inch).
	/// </summary>
	internal const float TwipsPerPoint = 20f;

	/// <summary>
	/// The number of twips per inch.
	/// </summary>
	internal const int TwipsPerInch = 1440;

	/// <summary>
	/// Converts a value in typographic points to twips.
	/// </summary>
	/// <param name="points">The value in points.</param>
	/// <returns>The value in twips.</returns>
	public static float PointsToTwips(float points) => points * TwipsPerPoint;

	/// <summary>
	/// Converts a value in twips to typographic points.
	/// </summary>
	/// <param name="twips">The value in twips.</param>
	/// <returns>The value in points.</returns>
	public static float TwipsToPoints(float twips) => twips / TwipsPerPoint;

	/// <summary>
	/// Converts a value in inches to twips.
	/// </summary>
	/// <param name="inches">The value in inches.</param>
	/// <returns>The value in twips.</returns>
	public static int InchesToTwips(double inches) => (int)(inches * TwipsPerInch);

	/// <summary>
	/// Converts a value in twips to inches.
	/// </summary>
	/// <param name="twips">The value in twips.</param>
	/// <returns>The value in inches.</returns>
	public static double TwipsToInches(int twips) => (double)twips / TwipsPerInch;

	/// <summary>
	/// Converts a value in twips to pixels at the specified DPI.
	/// </summary>
	/// <param name="twips">The value in twips.</param>
	/// <param name="dpi">The target dots per inch.</param>
	/// <returns>The value in pixels.</returns>
	public static double TwipsToPixels(int twips, double dpi) => (double)twips / TwipsPerInch * dpi;
}
