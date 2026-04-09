namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class TableBorderResolverTests
{
	[Fact]
	public void ResolveCellEdge_NullTable_ThrowsArgumentNullException()
	{
		var row = new TableRowElement { Cells = [] };
		var cell = new TableCellElement { Blocks = [] };
		var act = () => TableBorderResolver.ResolveCellEdge(null!, row, cell, BorderEdge.Top);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("table");
	}

	[Fact]
	public void ResolveCellEdge_PrefersCellOverRowAndTable()
	{
		var table = new TableElement
		{
			GridColumns = [],
			Rows = [],
			Borders = new TableBorderSet(Top: new TableBorderDefinition(BorderStyle.Single, 4, "AAAAAA")),
		};
		var row = new TableRowElement
		{
			Cells = [],
			Borders = new TableBorderSet(Top: new TableBorderDefinition(BorderStyle.Double, 6, "BBBBBB")),
		};
		var cell = new TableCellElement
		{
			Blocks = [],
			Borders = new TableBorderSet(Top: new TableBorderDefinition(BorderStyle.Dotted, 8, "CCCCCC")),
		};

		var result = TableBorderResolver.ResolveCellEdge(table, row, cell, BorderEdge.Top);

		result.Should().Be(new TableBorderDefinition(BorderStyle.Dotted, 8, "CCCCCC"));
	}

	[Fact]
	public void ResolveCellEdge_PrefersRowOverTable_WhenCellMissing()
	{
		var table = new TableElement
		{
			GridColumns = [],
			Rows = [],
			Borders = new TableBorderSet(Left: new TableBorderDefinition(BorderStyle.Single, 4, "AAAAAA")),
		};
		var row = new TableRowElement
		{
			Cells = [],
			Borders = new TableBorderSet(Left: new TableBorderDefinition(BorderStyle.Double, 6, "BBBBBB")),
		};
		var cell = new TableCellElement { Blocks = [] };

		var result = TableBorderResolver.ResolveCellEdge(table, row, cell, BorderEdge.Left);

		result.Should().Be(new TableBorderDefinition(BorderStyle.Double, 6, "BBBBBB"));
	}

	[Fact]
	public void ResolveCellEdge_UsesTable_WhenCellAndRowMissing()
	{
		var table = new TableElement
		{
			GridColumns = [],
			Rows = [],
			Borders = new TableBorderSet(Right: new TableBorderDefinition(BorderStyle.Thick, 10, "DDDDDD")),
		};
		var row = new TableRowElement { Cells = [] };
		var cell = new TableCellElement { Blocks = [] };

		var result = TableBorderResolver.ResolveCellEdge(table, row, cell, BorderEdge.Right);

		result.Should().Be(new TableBorderDefinition(BorderStyle.Thick, 10, "DDDDDD"));
	}

	[Fact]
	public void ResolveCellEdge_NoBorderDefined_ReturnsNull()
	{
		var table = new TableElement { GridColumns = [], Rows = [] };
		var row = new TableRowElement { Cells = [] };
		var cell = new TableCellElement { Blocks = [] };

		var result = TableBorderResolver.ResolveCellEdge(table, row, cell, BorderEdge.Bottom);

		result.Should().BeNull();
	}

	[Fact]
	public void ResolveCellEdge_UnsupportedEdge_ReturnsNull()
	{
		var table = new TableElement
		{
			GridColumns = [],
			Rows = [],
			Borders = new TableBorderSet(Top: new TableBorderDefinition(BorderStyle.Single, 4)),
		};
		var row = new TableRowElement { Cells = [] };
		var cell = new TableCellElement { Blocks = [] };

		var result = TableBorderResolver.ResolveCellEdge(table, row, cell, BorderEdge.Between);

		result.Should().BeNull();
	}

	// ---- 4.5.3: insideH / insideV inner grid line tests ----

	[Fact]
	public void ResolveCellEdge_TopEdge_InnerRow_UsesInsideHorizontal()
	{
		var insideH = new TableBorderDefinition(BorderStyle.Single, 4, "111111");
		var table = new TableElement
		{
			GridColumns = [],
			Rows = [],
			Borders = new TableBorderSet(
				Top: new TableBorderDefinition(BorderStyle.Thick, 10, "OUTER"),
				InsideHorizontal: insideH),
		};
		var row = new TableRowElement { Cells = [] };
		var cell = new TableCellElement { Blocks = [] };

		var result = TableBorderResolver.ResolveCellEdge(table, row, cell, BorderEdge.Top,
			isFirstRow: false, isLastRow: false, isFirstColumn: true, isLastColumn: true);

		result.Should().Be(insideH);
	}

	[Fact]
	public void ResolveCellEdge_BottomEdge_InnerRow_UsesInsideHorizontal()
	{
		var insideH = new TableBorderDefinition(BorderStyle.Dashed, 6, "222222");
		var table = new TableElement
		{
			GridColumns = [],
			Rows = [],
			Borders = new TableBorderSet(InsideHorizontal: insideH),
		};
		var row = new TableRowElement { Cells = [] };
		var cell = new TableCellElement { Blocks = [] };

		var result = TableBorderResolver.ResolveCellEdge(table, row, cell, BorderEdge.Bottom,
			isFirstRow: false, isLastRow: false, isFirstColumn: true, isLastColumn: true);

		result.Should().Be(insideH);
	}

	[Fact]
	public void ResolveCellEdge_LeftEdge_InnerColumn_UsesInsideVertical()
	{
		var insideV = new TableBorderDefinition(BorderStyle.Dotted, 3, "333333");
		var table = new TableElement
		{
			GridColumns = [],
			Rows = [],
			Borders = new TableBorderSet(
				Left: new TableBorderDefinition(BorderStyle.Thick, 10, "OUTER"),
				InsideVertical: insideV),
		};
		var row = new TableRowElement { Cells = [] };
		var cell = new TableCellElement { Blocks = [] };

		var result = TableBorderResolver.ResolveCellEdge(table, row, cell, BorderEdge.Left,
			isFirstRow: true, isLastRow: true, isFirstColumn: false, isLastColumn: false);

		result.Should().Be(insideV);
	}

	[Fact]
	public void ResolveCellEdge_RightEdge_InnerColumn_UsesInsideVertical()
	{
		var insideV = new TableBorderDefinition(BorderStyle.Double, 8, "444444");
		var table = new TableElement
		{
			GridColumns = [],
			Rows = [],
			Borders = new TableBorderSet(InsideVertical: insideV),
		};
		var row = new TableRowElement { Cells = [] };
		var cell = new TableCellElement { Blocks = [] };

		var result = TableBorderResolver.ResolveCellEdge(table, row, cell, BorderEdge.Right,
			isFirstRow: true, isLastRow: true, isFirstColumn: false, isLastColumn: false);

		result.Should().Be(insideV);
	}

	[Fact]
	public void ResolveCellEdge_TopEdge_FirstRow_UsesOuterTopBorder()
	{
		var outerTop = new TableBorderDefinition(BorderStyle.Thick, 10, "OUTER");
		var table = new TableElement
		{
			GridColumns = [],
			Rows = [],
			Borders = new TableBorderSet(
				Top: outerTop,
				InsideHorizontal: new TableBorderDefinition(BorderStyle.Single, 4, "INNER")),
		};
		var row = new TableRowElement { Cells = [] };
		var cell = new TableCellElement { Blocks = [] };

		var result = TableBorderResolver.ResolveCellEdge(table, row, cell, BorderEdge.Top,
			isFirstRow: true, isLastRow: false, isFirstColumn: true, isLastColumn: true);

		result.Should().Be(outerTop);
	}

	[Fact]
	public void ResolveCellEdge_BottomEdge_LastRow_UsesOuterBottomBorder()
	{
		var outerBottom = new TableBorderDefinition(BorderStyle.Thick, 10, "OUTER");
		var table = new TableElement
		{
			GridColumns = [],
			Rows = [],
			Borders = new TableBorderSet(
				Bottom: outerBottom,
				InsideHorizontal: new TableBorderDefinition(BorderStyle.Single, 4, "INNER")),
		};
		var row = new TableRowElement { Cells = [] };
		var cell = new TableCellElement { Blocks = [] };

		var result = TableBorderResolver.ResolveCellEdge(table, row, cell, BorderEdge.Bottom,
			isFirstRow: false, isLastRow: true, isFirstColumn: true, isLastColumn: true);

		result.Should().Be(outerBottom);
	}

	[Fact]
	public void ResolveCellEdge_InnerEdge_NoInsideBorderDefined_ReturnsNull()
	{
		var table = new TableElement
		{
			GridColumns = [],
			Rows = [],
			Borders = new TableBorderSet(Top: new TableBorderDefinition(BorderStyle.Thick, 10)),
		};
		var row = new TableRowElement { Cells = [] };
		var cell = new TableCellElement { Blocks = [] };

		var result = TableBorderResolver.ResolveCellEdge(table, row, cell, BorderEdge.Top,
			isFirstRow: false, isLastRow: false, isFirstColumn: true, isLastColumn: true);

		result.Should().BeNull();
	}

	[Fact]
	public void ResolveCellEdge_CellBorderTakesPrecedenceOverInsideH()
	{
		var cellBorder = new TableBorderDefinition(BorderStyle.Double, 6, "CELL");
		var table = new TableElement
		{
			GridColumns = [],
			Rows = [],
			Borders = new TableBorderSet(InsideHorizontal: new TableBorderDefinition(BorderStyle.Single, 4, "INSIDE")),
		};
		var row = new TableRowElement { Cells = [] };
		var cell = new TableCellElement
		{
			Blocks = [],
			Borders = new TableBorderSet(Top: cellBorder),
		};

		var result = TableBorderResolver.ResolveCellEdge(table, row, cell, BorderEdge.Top,
			isFirstRow: false, isLastRow: false, isFirstColumn: true, isLastColumn: true);

		result.Should().Be(cellBorder);
	}

	[Fact]
	public void ResolveCellEdge_LeftEdge_OuterColumn_UsesOuterLeftBorder()
	{
		var outerLeft = new TableBorderDefinition(BorderStyle.Thick, 10, "OUTER");
		var table = new TableElement
		{
			GridColumns = [],
			Rows = [],
			Borders = new TableBorderSet(
				Left: outerLeft,
				InsideVertical: new TableBorderDefinition(BorderStyle.Single, 4, "INNER")),
		};
		var row = new TableRowElement { Cells = [] };
		var cell = new TableCellElement { Blocks = [] };

		var result = TableBorderResolver.ResolveCellEdge(table, row, cell, BorderEdge.Left,
			isFirstRow: true, isLastRow: true, isFirstColumn: true, isLastColumn: false);

		result.Should().Be(outerLeft);
	}
}
