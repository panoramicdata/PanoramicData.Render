namespace PanoramicData.Render;

/// <summary>
/// Provides simplified Unicode Bidirectional (BiDi) reordering for text segments.
/// Implements a basic reordering that reverses consecutive RTL runs in visual order,
/// based on a per-element RTL flag derived from <c>w:rtl</c> run properties.
/// </summary>
internal static class BiDiReorderer
{
	/// <summary>
	/// Reorders elements for visual display in a BiDi context.
	/// Consecutive RTL elements are reversed in visual order while LTR elements
	/// maintain their logical order. For RTL paragraphs, the entire result is
	/// reversed and non-RTL runs within are treated as embedded LTR.
	/// </summary>
	/// <typeparam name="T">The element type.</typeparam>
	/// <param name="elements">The logical-order elements.</param>
	/// <param name="isRtl">Predicate that returns <see langword="true"/> if an element is RTL.</param>
	/// <param name="paragraphIsRtl">Whether the paragraph base direction is RTL (<c>w:bidi</c>).</param>
	/// <returns>A new list of elements in visual order.</returns>
	public static IReadOnlyList<T> Reorder<T>(IReadOnlyList<T> elements, Func<T, bool> isRtl, bool paragraphIsRtl)
	{
		if (elements.Count <= 1)
		{
			return elements;
		}

		// If no RTL elements and paragraph is LTR, nothing to do
		if (!paragraphIsRtl && !elements.Any(isRtl))
		{
			return elements;
		}

		var result = new List<T>(elements.Count);
		var oppositeGroup = new List<T>();

		foreach (var element in elements)
		{
			// In an RTL paragraph, LTR runs are the "opposite" direction; in LTR paragraph, RTL runs are
			var isOpposite = paragraphIsRtl ? !isRtl(element) : isRtl(element);

			if (isOpposite)
			{
				oppositeGroup.Add(element);
				continue;
			}

			FlushReversed(oppositeGroup, result);
			result.Add(element);
		}

		FlushReversed(oppositeGroup, result);

		// For RTL paragraphs, the entire visual order is reversed
		if (paragraphIsRtl)
		{
			result.Reverse();
		}

		return result;
	}

	// Flush an accumulated opposite-direction group in reversed order.
	private static void FlushReversed<T>(List<T> group, List<T> result)
	{
		if (group.Count == 0)
		{
			return;
		}

		group.Reverse();
		result.AddRange(group);
		group.Clear();
	}
}