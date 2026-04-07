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

		var lines = KnuthPlassAlgorithm.FindBreaks(items, lineWidthTwips);
		return (lines, items);
	}
}
