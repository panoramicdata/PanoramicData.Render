namespace PanoramicData.Render;

/// <summary>
/// Represents resolved font properties for a text run after applying the full style cascade.
/// OpenXML stores font size in half-points (e.g., <c>sz=24</c> → 12 pt); use <see cref="FromHalfPoints"/>
/// to construct from raw OOXML values.
/// </summary>
/// <param name="FamilyName">The resolved font family name (e.g., <c>"Calibri"</c>).</param>
/// <param name="SizePoints">The font size in typographic points.</param>
/// <param name="Bold">Whether bold styling is active after toggle resolution.</param>
/// <param name="Italic">Whether italic styling is active after toggle resolution.</param>
internal readonly record struct FontProperties(
	string FamilyName,
	float SizePoints,
	bool Bold,
	bool Italic)
{
	/// <summary>
	/// Default font family used when no family is specified in the document.
	/// </summary>
	public const string DefaultFamilyName = "Calibri";

	/// <summary>
	/// Default font size in points used when no size is specified in the document.
	/// </summary>
	public const float DefaultSizePoints = 11f;

	/// <summary>
	/// Default font properties matching Word's out-of-box defaults (Calibri 11pt, normal weight and style).
	/// </summary>
	public static readonly FontProperties Default = new(DefaultFamilyName, DefaultSizePoints, Bold: false, Italic: false);

	/// <summary>
	/// Gets the font size in twips (1 point = 20 twips).
	/// </summary>
	public float SizeTwips => TwipConverter.PointsToTwips(SizePoints);

	/// <summary>
	/// Gets the font size in half-points, the native unit used by OpenXML (<c>w:sz</c>).
	/// </summary>
	public float SizeHalfPoints => SizePoints * 2f;

	/// <summary>
	/// Creates a <see cref="FontProperties"/> from an OpenXML half-point size value.
	/// </summary>
	/// <param name="familyName">The resolved font family name.</param>
	/// <param name="sizeHalfPoints">The font size in half-points (e.g., <c>24</c> for 12 pt).</param>
	/// <param name="bold">Whether bold styling is active.</param>
	/// <param name="italic">Whether italic styling is active.</param>
	/// <returns>A new <see cref="FontProperties"/> with the size converted to points.</returns>
	public static FontProperties FromHalfPoints(
		string familyName,
		int sizeHalfPoints,
		bool bold = false,
		bool italic = false) =>
		new(familyName, sizeHalfPoints / 2f, bold, italic);

	/// <summary>
	/// Attempts to resolve an <see cref="SkiaSharp.SKTypeface"/> for these font properties
	/// using the provided <paramref name="fontResolver"/>.
	/// </summary>
	/// <param name="fontResolver">The font resolver to look up typefaces.</param>
	/// <param name="typeface">When successful, the resolved typeface; otherwise <see langword="null"/>.</param>
	/// <returns><see langword="true"/> when a typeface was resolved; otherwise <see langword="false"/>.</returns>
	public bool TryResolveTypeface(
		FontResolver fontResolver,
		out SkiaSharp.SKTypeface? typeface)
	{
		ArgumentNullException.ThrowIfNull(fontResolver);
		return fontResolver.TryGetTypeface(FamilyName, Bold, Italic, out typeface);
	}
}
