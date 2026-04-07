namespace PanoramicData.Render;

/// <summary>
/// A positioned content box on a laid-out line, with its computed X offset.
/// </summary>
/// <param name="ItemIndex">The index of this box in the items array.</param>
/// <param name="XOffset">The X offset (in twips) where this box starts on the line.</param>
/// <param name="Width">The width of this box in twips.</param>
internal readonly record struct PositionedBox(int ItemIndex, float XOffset, float Width);

/// <summary>
/// Computes X offsets for content boxes on a line based on paragraph alignment.
/// </summary>
internal static class ParagraphAligner
{
	/// <summary>
	/// Computes the X position of each content box on a single line.
	/// </summary>
	/// <param name="items">The full list of Knuth-Plass items.</param>
	/// <param name="line">The line break result specifying item range and adjustment ratio.</param>
	/// <param name="lineWidth">The target line width in twips.</param>
	/// <param name="alignment">The paragraph alignment mode.</param>
	/// <param name="isLastLine">
	/// Whether this is the last line of the paragraph. When <c>true</c> and alignment is
	/// <see cref="ParagraphAlignment.Justified"/>, the line is rendered left-aligned instead
	/// (the last line of a justified paragraph is not stretched).
	/// </param>
	/// <returns>A list of positioned boxes with their X offsets.</returns>
	public static IReadOnlyList<PositionedBox> ComputeBoxPositions(
		IReadOnlyList<KnuthPlassItem> items,
		KnuthPlassLine line,
		float lineWidth,
		ParagraphAlignment alignment,
		bool isLastLine = false)
	{
		ArgumentNullException.ThrowIfNull(items);

		if (lineWidth <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(lineWidth), "Line width must be positive.");
		}

		var boxes = new List<PositionedBox>();
		var ratio = line.AdjustmentRatio;

		// The last line of a justified paragraph is left-aligned (not stretched)
		var effectiveAlignment = alignment == ParagraphAlignment.Justified && isLastLine
			? ParagraphAlignment.Left
			: alignment;
		var isJustified = effectiveAlignment == ParagraphAlignment.Justified;

		// Compute the natural content width (for center/right offset calculation)
		var contentWidth = ComputeNaturalContentWidth(items, line);

		// Compute the starting X offset based on alignment
		var startOffset = effectiveAlignment switch
		{
			ParagraphAlignment.Center => Math.Max(0f, (lineWidth - contentWidth) / 2f),
			ParagraphAlignment.Right => Math.Max(0f, lineWidth - contentWidth),
			_ => 0f // Left and Justified start at 0
		};

		// Walk through items on this line, positioning boxes
		var x = startOffset;
		for (var i = line.StartIndex; i < line.EndIndex && i < items.Count; i++)
		{
			switch (items[i])
			{
				case KnuthPlassBox box:
					boxes.Add(new PositionedBox(i, x, box.Width));
					x += box.Width;
					break;

				case KnuthPlassGlue glue:
					x += isJustified
						? ComputeAdjustedGlueWidth(glue, ratio)
						: glue.Width;
					break;

					// Penalties within the line contribute 0 width
			}
		}

		// If the break is at a flagged penalty with width (hyphen), add it
		if (line.EndIndex >= 0 && line.EndIndex < items.Count
			&& items[line.EndIndex] is KnuthPlassPenalty { IsFlagged: true, Width: > 0 } penalty)
		{
			boxes.Add(new PositionedBox(line.EndIndex, x, penalty.Width));
		}

		return boxes;
	}

	/// <summary>
	/// Computes the natural (unstretched) content width for a line.
	/// </summary>
	private static float ComputeNaturalContentWidth(
		IReadOnlyList<KnuthPlassItem> items,
		KnuthPlassLine line)
	{
		var width = 0f;
		for (var i = line.StartIndex; i < line.EndIndex && i < items.Count; i++)
		{
			width += items[i].Width;
		}

		// Add penalty width at break point if applicable
		if (line.EndIndex >= 0 && line.EndIndex < items.Count
			&& items[line.EndIndex] is KnuthPlassPenalty p)
		{
			width += p.Width;
		}

		return width;
	}

	/// <summary>
	/// Computes the adjusted width of a glue item using the line's adjustment ratio.
	/// </summary>
	private static float ComputeAdjustedGlueWidth(KnuthPlassGlue glue, float ratio)
	{
		if (ratio >= 0)
		{
			return glue.Width + ratio * glue.Stretch;
		}

		return glue.Width + ratio * glue.Shrink;
	}

	/// <summary>
	/// Computes box positions for all lines in a paragraph, automatically detecting the last line.
	/// The last line of a justified paragraph is rendered left-aligned.
	/// </summary>
	/// <param name="items">The full list of Knuth-Plass items.</param>
	/// <param name="lines">All line break results for the paragraph.</param>
	/// <param name="lineWidth">The target line width in twips.</param>
	/// <param name="alignment">The paragraph alignment mode.</param>
	/// <returns>A list of positioned-box lists, one per line.</returns>
	public static IReadOnlyList<IReadOnlyList<PositionedBox>> ComputeParagraphBoxPositions(
		IReadOnlyList<KnuthPlassItem> items,
		IReadOnlyList<KnuthPlassLine> lines,
		float lineWidth,
		ParagraphAlignment alignment)
	{
		ArgumentNullException.ThrowIfNull(items);
		ArgumentNullException.ThrowIfNull(lines);

		if (lineWidth <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(lineWidth), "Line width must be positive.");
		}

		var result = new List<IReadOnlyList<PositionedBox>>(lines.Count);
		for (var i = 0; i < lines.Count; i++)
		{
			var isLastLine = i == lines.Count - 1;
			result.Add(ComputeBoxPositions(items, lines[i], lineWidth, alignment, isLastLine));
		}

		return result;
	}
}
