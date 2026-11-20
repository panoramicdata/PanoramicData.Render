namespace PanoramicData.Render.Test;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static class TableParserBuilders
{
	internal static TableCell WideTextCell(string text) => new(
		new TableCellProperties(new TableCellWidth { Width = "2400", Type = TableWidthUnitValues.Dxa }),
		new Paragraph(new Run(new Text(text))));

	internal static SdtRow SdtRowOf(string text) => new(new SdtContentRow(new TableRow(WideTextCell(text))));

	internal static SdtCell SdtCellOf(string text) => new(new SdtContentCell(WideTextCell(text)));

	internal static Table SingleCellTableWithMargins(params OpenXmlElement[] margins) => new(
		new TableRow(
			new TableCell(
				new TableCellProperties(new TableCellMargin(margins)),
				new Paragraph())));

	internal static TableRowElement ParsedRow(params OpenXmlElement[] rowProperties)
		=> TableParser.Parse(new Table(new TableRow(new TableRowProperties(rowProperties), new TableCell(new Paragraph())))).Rows[0];

	internal static TableCellElement ParsedCell(params OpenXmlElement[] cellProperties)
		=> TableParser.Parse(new Table(new TableRow(new TableCell(new TableCellProperties(cellProperties), new Paragraph())))).Rows[0].Cells[0];

	internal static TableCellElement ParsedPlainCell()
		=> TableParser.Parse(new Table(new TableRow(new TableCell(new Paragraph())))).Rows[0].Cells[0];

	internal static CellMargins ParsedMargins(params OpenXmlElement[] margins)
		=> TableParser.Parse(SingleCellTableWithMargins(margins)).Rows[0].Cells[0].Margins;

	internal static TableElement ParsedTableWith(params OpenXmlElement[] tableProperties)
		=> TableParser.Parse(new Table(
			new TableProperties(tableProperties),
			new TableGrid(new GridColumn { Width = "2000" }),
			new TableRow(new TableCell(new Paragraph(new Run(new Text("A")))))));
}
