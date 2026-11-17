namespace PanoramicData.Render;

/// <summary>
/// Base class for run elements that occupy a rectangular extent.
/// </summary>
internal abstract class ExtentRunElement : RunElement
{
	/// <summary>
	/// Gets the width in English Metric Units (EMU). 1 inch = 914400 EMU.
	/// </summary>
	public long WidthEmu { get; init; }

	/// <summary>
	/// Gets the height in English Metric Units (EMU). 1 inch = 914400 EMU.
	/// </summary>
	public long HeightEmu { get; init; }
}