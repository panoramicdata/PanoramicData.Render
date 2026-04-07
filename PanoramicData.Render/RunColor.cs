namespace PanoramicData.Render;

/// <summary>
/// Represents a resolved foreground color for a text run after applying the style cascade.
/// Colors are stored as 6-character uppercase hex RGB strings (e.g., <c>"FF0000"</c> for red).
/// </summary>
/// <param name="HexRgb">The resolved color as a 6-character hex RGB string (uppercase).</param>
/// <param name="IsAuto">
/// Whether the color was resolved from OpenXML's <c>auto</c> value, which
/// defaults to black (<c>"000000"</c>) for text foreground.
/// </param>
internal readonly record struct RunColor(string HexRgb, bool IsAuto = false)
{
	/// <summary>
	/// The hex RGB value used when the color is <c>auto</c> (Word defaults to black text).
	/// </summary>
	public const string AutoHexValue = "000000";

	/// <summary>
	/// The hex RGB value used when no color is specified (defaults to black).
	/// </summary>
	public const string DefaultHexValue = "000000";

	/// <summary>
	/// Automatic color (black foreground, matching Word's default behaviour).
	/// </summary>
	public static readonly RunColor Auto = new(AutoHexValue, IsAuto: true);

	/// <summary>
	/// Default color (black, non-auto).
	/// </summary>
	public static readonly RunColor Default = new(DefaultHexValue);

	/// <summary>
	/// Creates a <see cref="RunColor"/> from the resolved color string produced by
	/// <see cref="EffectiveFormatting.ResolvedRunColor"/>.
	/// Returns <see cref="Auto"/> when the input is <see langword="null"/>,
	/// empty, or the literal value <c>"auto"</c>.
	/// </summary>
	/// <param name="resolvedColor">
	/// A hex RGB string (e.g., <c>"FF0000"</c>) or <c>"auto"</c>, or <see langword="null"/>.
	/// </param>
	/// <returns>A <see cref="RunColor"/> wrapping the resolved value.</returns>
	public static RunColor FromResolvedColor(string? resolvedColor)
	{
		if (string.IsNullOrWhiteSpace(resolvedColor))
		{
			return Auto;
		}

		if (resolvedColor.Equals("auto", StringComparison.OrdinalIgnoreCase))
		{
			return Auto;
		}

		return new RunColor(resolvedColor.ToUpperInvariant());
	}

	/// <summary>
	/// Gets the red channel value (0–255).
	/// </summary>
	public byte Red => ParseChannel(0);

	/// <summary>
	/// Gets the green channel value (0–255).
	/// </summary>
	public byte Green => ParseChannel(2);

	/// <summary>
	/// Gets the blue channel value (0–255).
	/// </summary>
	public byte Blue => ParseChannel(4);

	private byte ParseChannel(int offset)
	{
		if (HexRgb is null || HexRgb.Length < offset + 2)
		{
			return 0;
		}

		return byte.TryParse(
			HexRgb.AsSpan(offset, 2),
			System.Globalization.NumberStyles.HexNumber,
			null,
			out var value)
			? value
			: (byte)0;
	}
}
