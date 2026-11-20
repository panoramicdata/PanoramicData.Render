namespace PanoramicData.Render.Test;

using DocumentFormat.OpenXml.Wordprocessing;
using static PanoramicData.Render.Test.TableLayoutBuilders;

internal static class NestedTableBuilders
{
	internal static ParagraphBlock EmptyBlock() => new() { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() };

	internal static ParagraphBlock TextBlock(string text) => (ParagraphBlock)TextCell(text).Blocks[0];

	internal static TablePlaceholderBlock NestedTableBlock(string first, string second) => new()
	{
		TableElement = new DocumentFormat.OpenXml.Wordprocessing.Table(
			new DocumentFormat.OpenXml.Wordprocessing.TableGrid(
				new DocumentFormat.OpenXml.Wordprocessing.GridColumn { Width = "0" },
				new DocumentFormat.OpenXml.Wordprocessing.GridColumn { Width = "0" }),
			new DocumentFormat.OpenXml.Wordprocessing.TableRow(
				NestedCell(first),
				NestedCell(second))),
	};

	internal static DocumentFormat.OpenXml.Wordprocessing.TableCell NestedCell(string text) => new(
		new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
			new DocumentFormat.OpenXml.Wordprocessing.Run(
				new DocumentFormat.OpenXml.Wordprocessing.Text(text))));

	internal static TablePlaceholderBlock EmptyNestedTableBlock(int rowCount, int columnCount, string gridWidth)
	{
		var table = new DocumentFormat.OpenXml.Wordprocessing.Table();
		var grid = new DocumentFormat.OpenXml.Wordprocessing.TableGrid();
		for (var c = 0; c < columnCount; c++)
		{
			grid.Append(new DocumentFormat.OpenXml.Wordprocessing.GridColumn { Width = gridWidth });
		}

		table.Append(grid);
		for (var r = 0; r < rowCount; r++)
		{
			var row = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
			for (var c = 0; c < columnCount; c++)
			{
				row.Append(new DocumentFormat.OpenXml.Wordprocessing.TableCell(new DocumentFormat.OpenXml.Wordprocessing.Paragraph()));
			}

			table.Append(row);
		}

		return new TablePlaceholderBlock { TableElement = table };
	}

	internal static Styles HeaderShadeStyles()
	{
		var firstRowShading = new DocumentFormat.OpenXml.Wordprocessing.Shading { Fill = "ddeeaa" };
		firstRowShading.SetAttribute(new DocumentFormat.OpenXml.OpenXmlAttribute("w", "val", "http://schemas.openxmlformats.org/wordprocessingml/2006/main", "clear"));

		var style = new Style(
			new TableStyleProperties(new TableCellProperties(firstRowShading))
			{ Type = TableStyleOverrideValues.FirstRow })
		{
			Type = StyleValues.Table,
			StyleId = "HeaderShade",
		};

		return new Styles(style);
	}
}
