namespace PanoramicData.Render;

/// <summary>
/// Represents parsed outline information for a DrawingML shape.
/// </summary>
internal sealed record ShapeOutlineInfo
{
	/// <summary>
	/// Gets a value indicating whether an outline was defined.
	/// </summary>
	public bool HasOutline { get; init; }

	/// <summary>
	/// Gets outline width in EMUs.
	/// </summary>
	public long WidthEmu { get; init; }

	/// <summary>
	/// Gets outline color in RRGGBB form when available.
	/// </summary>
	public string? ColorHex { get; init; }

	/// <summary>
	/// Gets line dash style token (e.g. solid, dash, dot) when available.
	/// </summary>
	public string? DashStyle { get; init; }

	/// <summary>
	/// Gets line join style.
	/// </summary>
	public ShapeLineJoinKind JoinStyle { get; init; }

	/// <summary>
	/// Gets an empty outline descriptor.
	/// </summary>
	public static ShapeOutlineInfo None { get; } = new();
}
