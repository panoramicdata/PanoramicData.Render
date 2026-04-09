namespace PanoramicData.Render;

/// <summary>
/// Represents parsed transform metadata for a DrawingML shape.
/// </summary>
internal sealed record ShapeTransformInfo
{
	/// <summary>
	/// Gets a value indicating whether transform data exists.
	/// </summary>
	public bool HasTransform { get; init; }

	/// <summary>
	/// Gets rotation angle in OOXML units (1/60000 degree).
	/// </summary>
	public int RotationAngle60000 { get; init; }

	/// <summary>
	/// Gets a value indicating whether horizontal flip is enabled.
	/// </summary>
	public bool FlipHorizontal { get; init; }

	/// <summary>
	/// Gets a value indicating whether vertical flip is enabled.
	/// </summary>
	public bool FlipVertical { get; init; }

	/// <summary>
	/// Gets an empty transform descriptor.
	/// </summary>
	public static ShapeTransformInfo None { get; } = new();
}
