namespace PanoramicData.Render.Test;

using DocumentFormat.OpenXml.Wordprocessing;

public sealed partial class TableLayoutEngineTests
{
	private static TableCellElement TextCell(string text, int gridSpan = 1) => new()
	{
		Blocks =
		[
			new ParagraphBlock
			{
				SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
					new DocumentFormat.OpenXml.Wordprocessing.Run(
						new DocumentFormat.OpenXml.Wordprocessing.Text(text))),
			},
		],
		GridSpan = gridSpan,
	};

	private static ParagraphBlock EmptyBlock() => new() { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() };

	private static ParagraphBlock TextBlock(string text) => (ParagraphBlock)TextCell(text).Blocks[0];

	private static TablePlaceholderBlock NestedTableBlock(string first, string second) => new()
	{
		TableElement = new DocumentFormat.OpenXml.Wordprocessing.Table(
			new DocumentFormat.OpenXml.Wordprocessing.TableGrid(
				new DocumentFormat.OpenXml.Wordprocessing.GridColumn { Width = "0" },
				new DocumentFormat.OpenXml.Wordprocessing.GridColumn { Width = "0" }),
			new DocumentFormat.OpenXml.Wordprocessing.TableRow(
				NestedCell(first),
				NestedCell(second))),
	};

	private static DocumentFormat.OpenXml.Wordprocessing.TableCell NestedCell(string text) => new(
		new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
			new DocumentFormat.OpenXml.Wordprocessing.Run(
				new DocumentFormat.OpenXml.Wordprocessing.Text(text))));

	private static TablePlaceholderBlock EmptyNestedTableBlock(int rowCount, int columnCount, string gridWidth)
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
	private static TableLayoutResult LayoutGrid(float availableWidth, float[] columns, params TableRowElement[] rows) => TableLayoutEngine.Layout(
		new TableElement
		{
			GridColumns = [.. columns.Select(w => new TableGridColumn(w))],
			Rows = rows,
		},
		availableWidth);

	private static TableCellElement ShadedCell(ParagraphShading shading, int gridSpan = 1) => new() { Blocks = [], GridSpan = gridSpan, Shading = shading };

	private static TableCellElement BorderedCell(TableBorderSet borders, int gridSpan = 1) => new() { Blocks = [], GridSpan = gridSpan, Borders = borders };

	private static TableBorderDefinition Line(BorderStyle style, int size, string color) => new(style, size, color);

	private static TableBorderSet OuterBorders(TableBorderDefinition? insideHorizontal = null, TableBorderDefinition? insideVertical = null)
	{
		var line = Line(BorderStyle.Single, 4, "111111");
		return new TableBorderSet(
			Top: line,
			Bottom: line,
			Left: line,
			Right: line,
			InsideHorizontal: insideHorizontal,
			InsideVertical: insideVertical);
	}

	private static Styles HeaderShadeStyles()
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

	private static TableCellElement CellOf(DocumentBlock block) => new() { Blocks = [block] };

	private static TableCellElement WidthCell(float width, TableWidthUnit unit, int gridSpan = 1, VerticalMergeState verticalMerge = VerticalMergeState.None) => new()
	{
		Blocks = [],
		Width = new TableWidthValue(width, unit),
		GridSpan = gridSpan,
		VerticalMerge = verticalMerge,
	};

	private static TableRowElement RowOf(params TableCellElement[] cells) => new() { Cells = cells };

	private static TableRowElement RowOfText(params string[] texts) => RowOf([.. texts.Select(t => TextCell(t))]);

	private static TableElement AutoFitTable(int columnCount, params TableRowElement[] rows) => new()
	{
		GridColumns = [.. Enumerable.Repeat(new TableGridColumn(0f), columnCount)],
		Rows = rows,
	};

	private static TableElement SingleColumnTable(params TableRowElement[] rows) => new()
	{
		GridColumns = [new TableGridColumn(4800f)],
		Rows = rows,
	};

	private static TableRowElement HeightRow(float height) => new() { Cells = [MakeCell()], HeightTwips = height };

	private static TableElement EmptyTable(TableAlignment alignment = TableAlignment.Left, float indentation = 0f) => new()
	{
		GridColumns = [],
		Rows = [],
		Alignment = alignment,
		IndentationTwips = indentation,
	};
}
