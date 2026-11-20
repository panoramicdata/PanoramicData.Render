namespace PanoramicData.Render.Test;

using DocumentFormat.OpenXml.Wordprocessing;

internal static class TableParserBuilders
{
	internal static TableCell WideTextCell(string text) => new(
		new TableCellProperties(new TableCellWidth { Width = "2400", Type = TableWidthUnitValues.Dxa }),
		new Paragraph(new Run(new Text(text))));

	internal static SdtRow SdtRowOf(string text) => new(new SdtContentRow(new TableRow(WideTextCell(text))));

	internal static SdtCell SdtCellOf(string text) => new(new SdtContentCell(WideTextCell(text)));

	internal static Table SingleCellTableWithMargins(params DocumentFormat.OpenXml.OpenXmlElement[] margins) => new(
		new TableRow(
			new TableCell(
				new TableCellProperties(new TableCellMargin(margins)),
				new Paragraph())));
}