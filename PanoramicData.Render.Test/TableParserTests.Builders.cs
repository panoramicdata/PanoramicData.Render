namespace PanoramicData.Render.Test;

using DocumentFormat.OpenXml.Wordprocessing;

public sealed partial class TableParserTests
{
	private static TableCell WideTextCell(string text) => new(
		new TableCellProperties(new TableCellWidth { Width = "2400", Type = TableWidthUnitValues.Dxa }),
		new Paragraph(new Run(new Text(text))));

	private static SdtRow SdtRowOf(string text) => new(new SdtContentRow(new TableRow(WideTextCell(text))));

	private static SdtCell SdtCellOf(string text) => new(new SdtContentCell(WideTextCell(text)));

	private static Table SingleCellTableWithMargins(params DocumentFormat.OpenXml.OpenXmlElement[] margins) => new(
		new TableRow(
			new TableCell(
				new TableCellProperties(new TableCellMargin(margins)),
				new Paragraph())));
}