namespace PanoramicData.Render;

/// <summary>
/// Represents an item in the Knuth-Plass line-breaking model.
/// Items are the building blocks that the algorithm uses to find optimal break points.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="KnuthPlassItem"/> class.
/// </remarks>
/// <param name="width">The natural width of this item in twips.</param>
internal abstract class KnuthPlassItem(float width)
{

	/// <summary>
	/// Gets the natural width of this item in twips.
	/// </summary>
	public float Width { get; } = width;
}

/// <summary>
/// A box represents content that must be typeset (e.g., a word or part of a word).
/// Boxes have a fixed width and cannot stretch or shrink.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="KnuthPlassBox"/> class.
/// </remarks>
/// <param name="width">The width of the box in twips.</param>
internal sealed class KnuthPlassBox(float width) : KnuthPlassItem(width)
{
}

/// <summary>
/// Glue represents stretchable/shrinkable space (e.g., inter-word space).
/// It has a natural width, plus stretch and shrink amounts.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="KnuthPlassGlue"/> class.
/// </remarks>
/// <param name="width">The natural width in twips.</param>
/// <param name="stretch">The maximum amount this glue can stretch, in twips.</param>
/// <param name="shrink">The maximum amount this glue can shrink, in twips.</param>
internal sealed class KnuthPlassGlue(float width, float stretch, float shrink) : KnuthPlassItem(width)
{

	/// <summary>
	/// Gets the maximum stretch amount in twips.
	/// </summary>
	public float Stretch { get; } = stretch;

	/// <summary>
	/// Gets the maximum shrink amount in twips.
	/// </summary>
	public float Shrink { get; } = shrink;
}

/// <summary>
/// A penalty represents a potential break point with an associated cost.
/// The algorithm will try to avoid breaks at high-penalty locations.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="KnuthPlassPenalty"/> class.
/// </remarks>
/// <param name="width">The width added if a break occurs here (e.g., hyphen width), in twips.</param>
/// <param name="penalty">The penalty cost for breaking here.</param>
/// <param name="isFlagged">Whether this penalty is flagged (e.g., a hyphen). Consecutive flagged breaks incur extra demerits.</param>
internal sealed class KnuthPlassPenalty(float width, float penalty, bool isFlagged = false) : KnuthPlassItem(width)
{
	/// <summary>
	/// Penalty value that indicates a forced break (must break here).
	/// </summary>
	public const float NegativeInfinity = float.NegativeInfinity;

	/// <summary>
	/// Penalty value that indicates a break is forbidden.
	/// </summary>
	public const float PositiveInfinity = float.PositiveInfinity;

	/// <summary>
	/// Gets the penalty cost for breaking at this point.
	/// Negative infinity forces a break; positive infinity forbids one.
	/// </summary>
	public float Penalty { get; } = penalty;

	/// <summary>
	/// Gets a value indicating whether this penalty is flagged (e.g., a hyphenation point).
	/// </summary>
	public bool IsFlagged { get; } = isFlagged;
}
