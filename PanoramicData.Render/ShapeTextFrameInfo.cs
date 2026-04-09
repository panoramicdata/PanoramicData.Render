namespace PanoramicData.Render;

/// <summary>
/// Represents parsed text-frame metadata for a DrawingML shape.
/// </summary>
internal sealed record ShapeTextFrameInfo
{
	/// <summary>
	/// Gets a value indicating whether a text frame exists.
	/// </summary>
	public bool HasTextFrame { get; init; }

	/// <summary>
	/// Gets the extracted plain text content.
	/// </summary>
	public string Text { get; init; } = string.Empty;

	/// <summary>
	/// Gets left internal margin in EMUs.
	/// </summary>
	public long LeftInsetEmu { get; init; }

	/// <summary>
	/// Gets top internal margin in EMUs.
	/// </summary>
	public long TopInsetEmu { get; init; }

	/// <summary>
	/// Gets right internal margin in EMUs.
	/// </summary>
	public long RightInsetEmu { get; init; }

	/// <summary>
	/// Gets bottom internal margin in EMUs.
	/// </summary>
	public long BottomInsetEmu { get; init; }

	/// <summary>
	/// Gets text auto-fit mode.
	/// </summary>
	public ShapeTextAutoFitMode AutoFitMode { get; init; }

	/// <summary>
	/// Gets an empty text-frame descriptor.
	/// </summary>
	public static ShapeTextFrameInfo None { get; } = new();
}
