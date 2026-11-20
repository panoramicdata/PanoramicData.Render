namespace PanoramicData.Render.Test;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

internal static class TableModelBuilders
{
	internal static TableGrid GridOf(params int[] widths)
	{
		var grid = new TableGrid();
		foreach (var width in widths)
		{
			grid.Append(new GridColumn { Width = width.ToString(System.Globalization.CultureInfo.InvariantCulture) });
		}

		return grid;
	}

	internal static TableCell CellWithText(string text, params OpenXmlElement[] cellProperties)
		=> CellWithParagraph(new Paragraph(new Run(new Text(text))), cellProperties);

	internal static TableCell BlankCell(params OpenXmlElement[] cellProperties)
		=> CellWithParagraph(new Paragraph(), cellProperties);

	internal static TableRow RowOfTexts(params string[] texts)
		=> new(texts.Select(text => (OpenXmlElement)CellWithText(text)));

	internal static TableRow BlankRow(int cellCount)
		=> new(Enumerable.Range(0, cellCount).Select(_ => (OpenXmlElement)BlankCell()));

	internal static TableElement ParseSingleBlankCell(params OpenXmlElement[] cellProperties)
		=> TableParser.Parse(new Table(GridOf(4800), new TableRow(BlankCell(cellProperties))));

	internal static TableElement ParseTableWith(params OpenXmlElement[] tableProperties)
		=> TableParser.Parse(new Table(new TableProperties(tableProperties), GridOf(4800), new TableRow(BlankCell())));

	internal static TableCellElement CellAt(ResolvedGridCell?[,] grid, int row, int column)
		=> grid[row, column]!.Value.Cell;

	private static TableCell CellWithParagraph(Paragraph paragraph, OpenXmlElement[] cellProperties)
	{
		var cell = new TableCell();
		if (cellProperties.Length > 0)
		{
			cell.Append(new TableCellProperties(cellProperties));
		}

		cell.Append(paragraph);
		return cell;
	}
}
