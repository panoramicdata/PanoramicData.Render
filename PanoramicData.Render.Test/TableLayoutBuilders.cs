namespace PanoramicData.Render.Test;

using DocumentFormat.OpenXml.Wordprocessing;

internal static class TableLayoutBuilders
{
	internal static TableCellElement TextCell(string text, int gridSpan = 1) => new()
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

	internal static TableRowElement RowOf(params TableCellElement[] cells) => new() { Cells = cells };

	internal static TableRowElement RowOfText(params string[] texts) => RowOf([.. texts.Select(t => TextCell(t))]);

	internal static TableElement AutoFitTable(int columnCount, params TableRowElement[] rows) => new()
	{
		GridColumns = [.. Enumerable.Repeat(new TableGridColumn(0f), columnCount)],
		Rows = rows,
	};

	internal static TableElement SingleColumnTable(params TableRowElement[] rows) => new()
	{
		GridColumns = [new TableGridColumn(4800f)],
		Rows = rows,
	};

	internal static TableRowElement HeightRow(float height) => new() { Cells = [new TableCellElement { Blocks = [] }], HeightTwips = height };

	internal static TableElement EmptyTable(TableAlignment alignment = TableAlignment.Left, float indentation = 0f) => new()
	{
		GridColumns = [],
		Rows = [],
		Alignment = alignment,
		IndentationTwips = indentation,
	};

	internal static TableLayoutResult LayoutGrid(float availableWidth, float[] columns, params TableRowElement[] rows) => TableLayoutEngine.Layout(
		new TableElement
		{
			GridColumns = [.. columns.Select(w => new TableGridColumn(w))],
			Rows = rows,
		},
		availableWidth);

	internal static TableCellElement ShadedCell(ParagraphShading shading, int gridSpan = 1) => new() { Blocks = [], GridSpan = gridSpan, Shading = shading };

	internal static TableCellElement BorderedCell(TableBorderSet borders, int gridSpan = 1) => new() { Blocks = [], GridSpan = gridSpan, Borders = borders };

	internal static TableBorderDefinition Line(BorderStyle style, int size, string color) => new(style, size, color);

	internal static TableBorderSet OuterBorders(TableBorderDefinition? insideHorizontal = null, TableBorderDefinition? insideVertical = null)
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

	internal static TableCellElement CellOf(DocumentBlock block) => new() { Blocks = [block] };

	internal static TableCellElement WidthCell(float width, TableWidthUnit unit, int gridSpan = 1, VerticalMergeState verticalMerge = VerticalMergeState.None) => new()
	{
		Blocks = [],
		Width = new TableWidthValue(width, unit),
		GridSpan = gridSpan,
		VerticalMerge = verticalMerge,
	};
}
