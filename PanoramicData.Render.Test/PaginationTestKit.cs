using System.Globalization;
using DocumentFormat.OpenXml.Wordprocessing;

namespace PanoramicData.Render.Test;

/// <summary>
/// Shared pagination test notation. A spec is a space separated list of blocks:
/// h&lt;height&gt; is an unsplittable block, l&lt;lines&gt; a splittable one where lines is a comma list of
/// V or NxV (N lines of height V). Optional /before/after set paragraph spacing and !flags may follow:
/// n keep with next, k keep lines together, b page break before, c column break before,
/// f widow/orphan control off.
/// A page shape lists blocks per page as "-" (unsplittable) or the line count, separated by commas;
/// pages are separated by "|". With columns, each block carries @columnIndex.
/// </summary>
internal static class PaginationTestKit
{
	public static ParagraphBlock Para() => new() { SourceElement = new Paragraph() };

	public static SectionInfo Section(int columns) => new() { ColumnCount = columns };

	private static float Num(string text) => float.Parse(text, CultureInfo.InvariantCulture);

	private static float[] ParseLines(string text) =>
		[.. text.Split(',').SelectMany(part =>
		{
			var pieces = part.Split('x');
			return pieces.Length == 1
				? new[] { Num(pieces[0]) }
				: Enumerable.Repeat(Num(pieces[1]), int.Parse(pieces[0], CultureInfo.InvariantCulture));
		})];

	public static LayoutBlock Block(string token)
	{
		var flagSplit = token.Split('!');
		var flags = flagSplit.Length > 1 ? flagSplit[1] : string.Empty;
		var parts = flagSplit[0][1..].Split('/');
		var before = parts.Length > 1 ? Num(parts[1]) : 0f;
		var after = parts.Length > 2 ? Num(parts[2]) : 0f;
		var lines = flagSplit[0][0] == 'l' ? ParseLines(parts[0]) : null;
		var height = lines is null ? Num(parts[0]) : before + lines.Sum() + after;

		return new LayoutBlock(
			Para(),
			height,
			before,
			after,
			lines,
			ForcePageBreakBefore: flags.Contains('b'),
			WidowOrphanControl: !flags.Contains('f'),
			KeepWithNext: flags.Contains('n'),
			KeepLinesTogether: flags.Contains('k'),
			ForceColumnBreakBefore: flags.Contains('c'));
	}

	public static LayoutBlock[] Blocks(string spec) =>
		[.. spec.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(Block)];

	public static IReadOnlyList<LayoutPage> Paginate(string spec, int columns = 1, float headerHeight = 0f) =>
		PageBuilder.Paginate(Blocks(spec), Section(columns), headerHeight);

	public static string Describe(LayoutBlock block) =>
		block.LineHeights is null ? "-" : block.LineHeights.Count.ToString(CultureInfo.InvariantCulture);

	public static string Shape(IReadOnlyList<LayoutPage> pages, bool withColumns = false) =>
		string.Join('|', pages.Select(page => string.Join(',', page.Blocks.Select((block, i) =>
			Describe(block) + (withColumns && i < page.BlockPlacements.Count ? "@" + page.BlockPlacements[i].ColumnIndex.ToString(CultureInfo.InvariantCulture) : string.Empty)))));

	public static string DescribeSplit((LayoutBlock First, LayoutBlock Second)? split) =>
		split is null ? "null" : Describe(split.Value.First) + "/" + Describe(split.Value.Second);

	public static string Metrics(LayoutBlock block) =>
		string.Create(CultureInfo.InvariantCulture, $"{Describe(block)}:{block.HeightTwips}/{block.SpaceBefore}/{block.SpaceAfter}");

	public static string MetricsOf((LayoutBlock First, LayoutBlock Second) split, string part) => part switch
	{
		"First" => Metrics(split.First),
		"Second" => Metrics(split.Second),
		_ => Metrics(split.First) + " " + Metrics(split.Second)
	};
}
