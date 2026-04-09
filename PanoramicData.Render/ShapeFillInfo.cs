namespace PanoramicData.Render;

/// <summary>
/// Represents parsed fill information for a DrawingML shape.
/// </summary>
internal sealed record ShapeFillInfo
{
	/// <summary>
	/// Gets the fill kind.
	/// </summary>
	public ShapeFillKind Kind { get; init; }

	/// <summary>
	/// Gets the solid fill color in RRGGBB form when <see cref="Kind"/> is <see cref="ShapeFillKind.Solid"/>.
	/// </summary>
	public string? SolidColorHex { get; init; }

	/// <summary>
	/// Gets the gradient style descriptor (e.g. linear/radial) when <see cref="Kind"/> is <see cref="ShapeFillKind.Gradient"/>.
	/// </summary>
	public string? GradientStyle { get; init; }

	/// <summary>
	/// Gets gradient stops when <see cref="Kind"/> is <see cref="ShapeFillKind.Gradient"/>.
	/// </summary>
	public IReadOnlyList<GradientStopInfo> GradientStops { get; init; } = [];

	/// <summary>
	/// Gets the pattern preset name when <see cref="Kind"/> is <see cref="ShapeFillKind.Pattern"/>.
	/// </summary>
	public string? PatternPreset { get; init; }

	/// <summary>
	/// Gets the pattern foreground color in RRGGBB form.
	/// </summary>
	public string? PatternForegroundColorHex { get; init; }

	/// <summary>
	/// Gets the pattern background color in RRGGBB form.
	/// </summary>
	public string? PatternBackgroundColorHex { get; init; }

	/// <summary>
	/// Gets the related image id when <see cref="Kind"/> is <see cref="ShapeFillKind.Picture"/>.
	/// </summary>
	public string? PictureRelationshipId { get; init; }

	/// <summary>
	/// Gets an empty fill descriptor.
	/// </summary>
	public static ShapeFillInfo None { get; } = new();
}

/// <summary>
/// Represents a gradient stop in DrawingML.
/// </summary>
/// <param name="Position">Gradient stop position (0-100000).</param>
/// <param name="ColorHex">Stop color in RRGGBB form.</param>
internal readonly record struct GradientStopInfo(int Position, string ColorHex);
