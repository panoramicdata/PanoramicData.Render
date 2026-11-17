namespace PanoramicData.Render;

internal static partial class KnuthPlassAlgorithm
{
	/// <summary>
	/// Returns true if the item represents a forced break (negative infinity penalty).
	/// </summary>
	private static bool IsForcedBreak(KnuthPlassItem item) =>
		item is KnuthPlassPenalty p && float.IsNegativeInfinity(p.Penalty);

	/// <summary>
	/// Represents a node in the Knuth-Plass active node list.
	/// </summary>
	private sealed class BreakNode(
		int position,
		int line,
		int fitnessClass,
		float totalWidth,
		float totalStretch,
		float totalShrink,
		float totalDemerits,
		KnuthPlassAlgorithm.BreakNode? previous)
	{
		/// <summary>
		/// Gets the position in the item list where this break occurs.
		/// </summary>
		public int Position { get; } = position;

		/// <summary>
		/// Gets the line number (starting from 1) after this break.
		/// </summary>
		public int Line { get; } = line;

		/// <summary>
		/// Gets the fitness class (0-3) of the line ending at this break.
		/// </summary>
		public int FitnessClass { get; } = fitnessClass;

		/// <summary>
		/// Gets the cumulative width up to the content following this break.
		/// </summary>
		public float TotalWidth { get; } = totalWidth;

		/// <summary>
		/// Gets the cumulative stretch up to the content following this break.
		/// </summary>
		public float TotalStretch { get; } = totalStretch;

		/// <summary>
		/// Gets the cumulative shrink up to the content following this break.
		/// </summary>
		public float TotalShrink { get; } = totalShrink;

		/// <summary>
		/// Gets the total demerits accumulated up to this break.
		/// </summary>
		public float TotalDemerits { get; } = totalDemerits;

		/// <summary>
		/// Gets the previous break node in the optimal path.
		/// </summary>
		public BreakNode? Previous { get; } = previous;
	}
}
