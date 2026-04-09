using SkiaSharp;

namespace PanoramicData.Render;

/// <summary>
/// Computes line break positions for a paragraph by combining
/// <see cref="TextRunToItemMapper"/> and <see cref="KnuthPlassAlgorithm"/>.
/// </summary>
internal sealed class ParagraphLineBreaker
{
	private readonly TextRunToItemMapper _mapper;

	/// <summary>
	/// Initializes a new instance of the <see cref="ParagraphLineBreaker"/> class.
	/// </summary>
	/// <param name="engine">The measurement engine for computing glyph widths.</param>
	/// <param name="hyphenation">An optional hyphenation dictionary for automatic hyphenation.</param>
	public ParagraphLineBreaker(MeasurementEngine engine, HyphenationDictionary? hyphenation = null)
	{
		ArgumentNullException.ThrowIfNull(engine);
		_mapper = new TextRunToItemMapper(engine, hyphenation);
	}

	/// <summary>
	/// Computes line break positions for a paragraph.
	/// </summary>
	/// <param name="runs">The parsed runs in the paragraph.</param>
	/// <param name="typeface">The typeface for measuring widths.</param>
	/// <param name="fontSizePoints">The font size in typographic points.</param>
	/// <param name="lineWidthTwips">The target line width in twips.</param>
	/// <returns>A list of lines with start/end indices and adjustment ratios.</returns>
	public IReadOnlyList<KnuthPlassLine> ComputeLineBreaks(
		IReadOnlyList<ParsedRun> runs,
		SKTypeface typeface,
		float fontSizePoints,
		float lineWidthTwips)
	{
		var (lines, _) = ComputeLineBreaksWithItems(runs, typeface, fontSizePoints, lineWidthTwips);
		return lines;
	}

	/// <summary>
	/// Computes line break positions for a paragraph, applying wrap-region width reductions
	/// for floating objects registered in <paramref name="wrapRegistry"/>.
	/// </summary>
	/// <param name="runs">The parsed runs in the paragraph.</param>
	/// <param name="typeface">The typeface for measuring widths.</param>
	/// <param name="fontSizePoints">The font size in typographic points.</param>
	/// <param name="lineWidthTwips">The nominal line width in twips (used when no wrap regions affect a line).</param>
	/// <param name="wrapRegistry">The wrap region registry supplying per-line available widths.</param>
	/// <param name="contentLeftTwips">Absolute left edge of the paragraph content area in twips.</param>
	/// <param name="paragraphTopTwips">Absolute top of the paragraph in twips (page coordinates).</param>
	/// <param name="estimatedLineHeightTwips">Estimated height of each line in twips for y-position queries.</param>
	/// <returns>A list of lines with start/end indices and adjustment ratios.</returns>
	public IReadOnlyList<KnuthPlassLine> ComputeLineBreaks(
		IReadOnlyList<ParsedRun> runs,
		SKTypeface typeface,
		float fontSizePoints,
		float lineWidthTwips,
		WrapRegionRegistry wrapRegistry,
		float contentLeftTwips,
		float paragraphTopTwips,
		float estimatedLineHeightTwips)
	{
		var (lines, _) = ComputeLineBreaksWithItems(
			runs, typeface, fontSizePoints, lineWidthTwips,
			wrapRegistry, contentLeftTwips, paragraphTopTwips, estimatedLineHeightTwips);
		return lines;
	}

	/// <summary>
	/// Computes line break positions for a paragraph, also returning the Knuth-Plass items
	/// so callers can inspect or render the content referenced by the line indices.
	/// </summary>
	/// <param name="runs">The parsed runs in the paragraph.</param>
	/// <param name="typeface">The typeface for measuring widths.</param>
	/// <param name="fontSizePoints">The font size in typographic points.</param>
	/// <param name="lineWidthTwips">The target line width in twips.</param>
	/// <returns>A tuple of (lines, items).</returns>
	public (IReadOnlyList<KnuthPlassLine> Lines, IReadOnlyList<KnuthPlassItem> Items) ComputeLineBreaksWithItems(
		IReadOnlyList<ParsedRun> runs,
		SKTypeface typeface,
		float fontSizePoints,
		float lineWidthTwips)
	{
		return ComputeLineBreaksWithItems(
			runs, typeface, fontSizePoints, lineWidthTwips,
			wrapRegistry: null, contentLeftTwips: 0f, paragraphTopTwips: 0f, estimatedLineHeightTwips: 240f);
	}

	/// <summary>
	/// Computes line break positions for a paragraph, also returning the Knuth-Plass items,
	/// applying wrap-region width reductions for floating objects.
	/// </summary>
	/// <param name="runs">The parsed runs in the paragraph.</param>
	/// <param name="typeface">The typeface for measuring widths.</param>
	/// <param name="fontSizePoints">The font size in typographic points.</param>
	/// <param name="lineWidthTwips">The nominal line width in twips.</param>
	/// <param name="wrapRegistry">The optional wrap region registry; pass <c>null</c> for no wrapping.</param>
	/// <param name="contentLeftTwips">Absolute left edge of the paragraph content area in twips.</param>
	/// <param name="paragraphTopTwips">Absolute top of the paragraph in twips.</param>
	/// <param name="estimatedLineHeightTwips">Estimated height of each line in twips for y-position queries.</param>
	/// <returns>A tuple of (lines, items).</returns>
	public (IReadOnlyList<KnuthPlassLine> Lines, IReadOnlyList<KnuthPlassItem> Items) ComputeLineBreaksWithItems(
		IReadOnlyList<ParsedRun> runs,
		SKTypeface typeface,
		float fontSizePoints,
		float lineWidthTwips,
		WrapRegionRegistry? wrapRegistry,
		float contentLeftTwips,
		float paragraphTopTwips,
		float estimatedLineHeightTwips)
	{
		ArgumentNullException.ThrowIfNull(runs);
		ArgumentNullException.ThrowIfNull(typeface);

		if (fontSizePoints <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(fontSizePoints));
		}

		if (lineWidthTwips <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(lineWidthTwips));
		}

		// Build the Knuth-Plass item list from all runs
		var items = new List<KnuthPlassItem>();

		foreach (var run in runs)
		{
			var runItems = _mapper.MapRunElements(run.Elements, typeface, fontSizePoints);
			items.AddRange(runItems);
		}

		if (items.Count == 0)
		{
			return ([], []);
		}

		// Append the standard paragraph-finishing sequence:
		// 1. Finishing glue with infinite stretch (absorbs remaining space on last line)
		// 2. Forced break penalty (forces the algorithm to break at end of paragraph)
		items.Add(new KnuthPlassGlue(0f, float.PositiveInfinity, 0f));
		items.Add(new KnuthPlassPenalty(0f, KnuthPlassPenalty.NegativeInfinity));

		IReadOnlyList<KnuthPlassLine> lines;

		if (wrapRegistry is null || wrapRegistry.IsEmpty)
		{
			lines = KnuthPlassAlgorithm.FindBreaks(items, lineWidthTwips);
		}
		else
		{
			// Build a per-line width selector based on estimated y-positions.
			// Line index 0 = first line of the paragraph, starting at paragraphTopTwips.
			lines = KnuthPlassAlgorithm.FindBreaks(items, lineIndex =>
			{
				var lineTop = paragraphTopTwips + lineIndex * estimatedLineHeightTwips;
				var width = wrapRegistry.GetPrimaryLineWidth(
					contentLeftTwips, lineWidthTwips, lineTop, estimatedLineHeightTwips);
				// Never return a non-positive width — fall back to nominal width.
				return width > 0f ? width : lineWidthTwips;
			});
		}

		return (lines, items);
	}
}
