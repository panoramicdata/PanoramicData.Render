namespace PanoramicData.Render;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

/// <summary>
/// Parses OpenXML table elements into the internal <see cref="TableElement"/> model.
/// </summary>
internal static partial class TableParser
{
	private const string WordprocessingNamespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

	/// <summary>
	/// Parses a <c>w:tbl</c> element into a <see cref="TableElement"/>.
	/// </summary>
	/// <param name="table">The OpenXML table element.</param>
	/// <param name="styles">Optional styles document used to resolve default cell margins from the table style chain.</param>
	/// <returns>A parsed <see cref="TableElement"/>.</returns>
	public static TableElement Parse(Table table, Styles? styles = null)
	{
		ArgumentNullException.ThrowIfNull(table);

		var tblPr = table.GetFirstChild<TableProperties>();
		var styleId = tblPr?.TableStyle?.Val?.Value;
		var defaultCellMargins = ResolveTableDefaultCellMargins(tblPr, styles, styleId);

		return new TableElement
		{
			GridColumns = ParseGrid(table),
			Rows = ParseRows(table, defaultCellMargins),
			StyleId = styleId,
			Width = ParseTableWidthOf(tblPr),
			Alignment = ParseAlignmentOf(tblPr),
			IndentationTwips = ParseIndentationOf(tblPr),
			Borders = ParseTableBordersOf(tblPr),
			BorderSpacingTwips = ParseTableCellSpacing(tblPr?.TableCellSpacing),
			DefaultCellMargins = defaultCellMargins,
			Look = ParseTableLookOf(tblPr),
			IsBiDi = IsBiDiTable(tblPr),
		};
	}

	private static TableWidthValue ParseTableWidthOf(TableProperties? tblPr) => ParseTableWidth(tblPr?.TableWidth);

	private static TableAlignment ParseAlignmentOf(TableProperties? tblPr) => ParseAlignment(tblPr?.TableJustification);

	private static float ParseIndentationOf(TableProperties? tblPr) => ParseIndentation(tblPr?.TableIndentation);

	private static TableBorderSet ParseTableBordersOf(TableProperties? tblPr) => ParseTableBorders(tblPr?.TableBorders);

	private static TableLookOptions ParseTableLookOf(TableProperties? tblPr) => ParseTableLook(tblPr?.TableLook);

	// When the table element itself defines no margins, inherit them from the table style chain
	// (for example TableNormal defines 108-twip left and right margins by default).
	private static CellMargins ResolveTableDefaultCellMargins(TableProperties? tblPr, Styles? styles, string? styleId)
	{
		var defaultCellMargins = ParseDefaultCellMargins(tblPr?.TableCellMarginDefault);
		return defaultCellMargins == CellMargins.None
			? TableStyleResolver.ResolveDefaultCellMargins(styles, styleId)
			: defaultCellMargins;
	}

	private static bool IsBiDiTable(TableProperties? tblPr)
		=> tblPr?.BiDiVisual is { } bidi && (bidi.Val is null || bidi.Val.Value == OnOffOnlyValues.On);

	internal static float ParseTableCellSpacing(TableCellSpacing? spacing)
	{
		if (spacing?.Width?.Value is null || IsNonAbsoluteWidthType(spacing.Type?.Value))
		{
			return 0f;
		}

		return float.TryParse(spacing.Width.Value, out var parsed) ? parsed : 0f;
	}
}
