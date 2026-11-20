namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class TableBorderResolverTests
{
	private static readonly TableBorderDefinition Outer = new(BorderStyle.Thick, 10, "OUTER");
	private static readonly TableBorderDefinition Inner = new(BorderStyle.Single, 4, "INNER");
	private static readonly (bool, bool, bool, bool) InnerRow = (false, false, true, true);
	private static readonly (bool, bool, bool, bool) FirstRow = (true, false, true, true);
	private static readonly (bool, bool, bool, bool) LastRow = (false, true, true, true);
	private static readonly (bool, bool, bool, bool) InnerColumn = (true, true, false, false);
	private static readonly (bool, bool, bool, bool) FirstColumn = (true, true, true, false);

	[Fact]
	public void ResolveCellEdge_NullTable_ThrowsArgumentNullException()
	{
		var act = () => TableBorderResolver.ResolveCellEdge(
			null!,
			new TableRowElement { Cells = [] },
			new TableCellElement { Blocks = [] },
			BorderEdge.Top);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("table");
	}

	[Fact]
	public void ResolveCellEdge_PrefersCellOverRowAndTable()
	{
		var result = Resolve(
			BorderEdge.Top,
			new TableBorderSet(Top: new TableBorderDefinition(BorderStyle.Single, 4, "AAAAAA")),
			new TableBorderSet(Top: new TableBorderDefinition(BorderStyle.Double, 6, "BBBBBB")),
			new TableBorderSet(Top: new TableBorderDefinition(BorderStyle.Dotted, 8, "CCCCCC")));

		result.Should().Be(new TableBorderDefinition(BorderStyle.Dotted, 8, "CCCCCC"));
	}

	[Fact]
	public void ResolveCellEdge_PrefersRowOverTable_WhenCellMissing()
	{
		var result = Resolve(
			BorderEdge.Left,
			new TableBorderSet(Left: new TableBorderDefinition(BorderStyle.Single, 4, "AAAAAA")),
			new TableBorderSet(Left: new TableBorderDefinition(BorderStyle.Double, 6, "BBBBBB")));

		result.Should().Be(new TableBorderDefinition(BorderStyle.Double, 6, "BBBBBB"));
	}

	[Fact]
	public void ResolveCellEdge_UsesTable_WhenCellAndRowMissing()
	{
		var result = Resolve(
			BorderEdge.Right,
			new TableBorderSet(Right: new TableBorderDefinition(BorderStyle.Thick, 10, "DDDDDD")));

		result.Should().Be(new TableBorderDefinition(BorderStyle.Thick, 10, "DDDDDD"));
	}

	[Fact]
	public void ResolveCellEdge_NoBorderDefined_ReturnsNull()
		=> Resolve(BorderEdge.Bottom, null).Should().BeNull();

	[Fact]
	public void ResolveCellEdge_UnsupportedEdge_ReturnsNull()
		=> Resolve(BorderEdge.Between, new TableBorderSet(Top: new TableBorderDefinition(BorderStyle.Single, 4)))
			.Should().BeNull();

	[Fact]
	public void ResolveCellEdge_TopEdge_InnerRow_UsesInsideHorizontal()
	{
		var insideH = new TableBorderDefinition(BorderStyle.Single, 4, "111111");

		ResolveAt(BorderEdge.Top, InnerRow, new TableBorderSet(Top: Outer, InsideHorizontal: insideH))
			.Should().Be(insideH);
	}

	[Fact]
	public void ResolveCellEdge_BottomEdge_InnerRow_UsesInsideHorizontal()
	{
		var insideH = new TableBorderDefinition(BorderStyle.Dashed, 6, "222222");

		ResolveAt(BorderEdge.Bottom, InnerRow, new TableBorderSet(InsideHorizontal: insideH))
			.Should().Be(insideH);
	}

	[Fact]
	public void ResolveCellEdge_LeftEdge_InnerColumn_UsesInsideVertical()
	{
		var insideV = new TableBorderDefinition(BorderStyle.Dotted, 3, "333333");

		ResolveAt(BorderEdge.Left, InnerColumn, new TableBorderSet(Left: Outer, InsideVertical: insideV))
			.Should().Be(insideV);
	}

	[Fact]
	public void ResolveCellEdge_RightEdge_InnerColumn_UsesInsideVertical()
	{
		var insideV = new TableBorderDefinition(BorderStyle.Double, 8, "444444");

		ResolveAt(BorderEdge.Right, InnerColumn, new TableBorderSet(InsideVertical: insideV))
			.Should().Be(insideV);
	}

	[Fact]
	public void ResolveCellEdge_TopEdge_FirstRow_UsesOuterTopBorder()
		=> ResolveAt(BorderEdge.Top, FirstRow, new TableBorderSet(Top: Outer, InsideHorizontal: Inner))
			.Should().Be(Outer);

	[Fact]
	public void ResolveCellEdge_BottomEdge_LastRow_UsesOuterBottomBorder()
		=> ResolveAt(BorderEdge.Bottom, LastRow, new TableBorderSet(Bottom: Outer, InsideHorizontal: Inner))
			.Should().Be(Outer);

	[Fact]
	public void ResolveCellEdge_InnerEdge_NoInsideBorderDefined_ReturnsNull()
		=> ResolveAt(BorderEdge.Top, InnerRow, new TableBorderSet(Top: new TableBorderDefinition(BorderStyle.Thick, 10)))
			.Should().BeNull();

	[Fact]
	public void ResolveCellEdge_CellBorderTakesPrecedenceOverInsideH()
	{
		var cellBorder = new TableBorderDefinition(BorderStyle.Double, 6, "CELL");

		ResolveAt(
			BorderEdge.Top,
			InnerRow,
			new TableBorderSet(InsideHorizontal: new TableBorderDefinition(BorderStyle.Single, 4, "INSIDE")),
			cellBorders: new TableBorderSet(Top: cellBorder))
			.Should().Be(cellBorder);
	}

	[Fact]
	public void ResolveCellEdge_LeftEdge_OuterColumn_UsesOuterLeftBorder()
		=> ResolveAt(BorderEdge.Left, FirstColumn, new TableBorderSet(Left: Outer, InsideVertical: Inner))
			.Should().Be(Outer);

	private static TableBorderDefinition? Resolve(
		BorderEdge edge,
		TableBorderSet? tableBorders,
		TableBorderSet? rowBorders = null,
		TableBorderSet? cellBorders = null)
		=> TableBorderResolver.ResolveCellEdge(
			new TableElement { GridColumns = [], Rows = [], Borders = tableBorders ?? TableBorderSet.None },
			new TableRowElement { Cells = [], Borders = rowBorders ?? TableBorderSet.None },
			new TableCellElement { Blocks = [], Borders = cellBorders ?? TableBorderSet.None },
			edge);

	private static TableBorderDefinition? ResolveAt(
		BorderEdge edge,
		(bool FirstRow, bool LastRow, bool FirstColumn, bool LastColumn) position,
		TableBorderSet tableBorders,
		TableBorderSet? cellBorders = null)
		=> TableBorderResolver.ResolveCellEdge(
			new TableElement { GridColumns = [], Rows = [], Borders = tableBorders },
			new TableRowElement { Cells = [] },
			new TableCellElement { Blocks = [], Borders = cellBorders ?? TableBorderSet.None },
			edge,
			isFirstRow: position.FirstRow,
			isLastRow: position.LastRow,
			isFirstColumn: position.FirstColumn,
			isLastColumn: position.LastColumn);
}
