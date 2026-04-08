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
}
