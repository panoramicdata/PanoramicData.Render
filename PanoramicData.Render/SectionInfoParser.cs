namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using OoxmlSectionProperties = DocumentFormat.OpenXml.Wordprocessing.SectionProperties;

/// <summary>
/// Parses OpenXML section properties into <see cref="SectionInfo"/> instances.
/// </summary>
internal static partial class SectionInfoParser
{
	/// <summary>
	/// Parses a single OpenXML <see cref="OoxmlSectionProperties"/> element into a <see cref="SectionInfo"/>.
	/// </summary>
	/// <param name="sectPr">The OpenXML section properties element.</param>
	/// <returns>A parsed <see cref="SectionInfo"/>.</returns>
	public static SectionInfo Parse(OoxmlSectionProperties sectPr)
	{
		ArgumentNullException.ThrowIfNull(sectPr);

		var pageSize = sectPr.GetFirstChild<PageSize>();
		var margins = ParseMargins(sectPr.GetFirstChild<PageMargin>());
		var columns = sectPr.GetFirstChild<Columns>();
		var columnLayout = ParseColumnLayout(columns);
		var titlePage = sectPr.GetFirstChild<TitlePage>();

		return new SectionInfo
		{
			PageWidth = ToTwips(pageSize?.Width, 12240),
			PageHeight = ToTwips(pageSize?.Height, 15840),
			Orientation = ParseOrientation(pageSize),
			MarginTop = margins.Top,
			MarginRight = margins.Right,
			MarginBottom = margins.Bottom,
			MarginLeft = margins.Left,
			MarginHeader = margins.Header,
			MarginFooter = margins.Footer,
			MarginGutter = margins.Gutter,
			BreakType = ParseBreakType(sectPr.GetFirstChild<SectionType>()),
			ColumnCount = columnLayout.Count,
			ColumnsEqualWidth = columnLayout.EqualWidth,
			ColumnSpacingTwips = ParseColumnSpacing(columns),
			ColumnSeparator = columnLayout.Separator,
			Columns = ParseColumnDefinitions(columns),
			LineNumbering = ParseLineNumbering(sectPr.GetFirstChild<LineNumberType>()),
			TitlePage = titlePage is not null && (titlePage.Val is null || titlePage.Val.Value),
			HeaderReferences = ParseHeaderReferences(sectPr),
			FooterReferences = ParseFooterReferences(sectPr)
		};
	}

	private readonly record struct ColumnLayout(int Count, bool EqualWidth, bool Separator);

	private static ColumnLayout ParseColumnLayout(Columns? columns)
		=> new(ParseColumnCount(columns), BoolOr(columns?.EqualWidth, true), BoolOr(columns?.Separator, false));

	private static int ParseColumnCount(Columns? columns) => (int?)columns?.ColumnCount?.Value ?? 1;

	private static bool BoolOr(OnOffValue? value, bool fallback) => value?.Value ?? fallback;

	private readonly record struct PageMargins(int Top, int Right, int Bottom, int Left, int Header, int Footer, int Gutter);
}
