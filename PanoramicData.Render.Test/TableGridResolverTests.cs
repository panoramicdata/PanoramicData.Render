namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class TableGridResolverTests
{
	private const VerticalMergeState Restart = VerticalMergeState.Restart;
	private const VerticalMergeState Continue = VerticalMergeState.Continue;

	[Fact]
	public void Resolve_NullTable_ThrowsArgumentNullException()
	{
		var act = () => TableGridResolver.Resolve(null!);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("table");
	}

	[Fact]
	public void Resolve_EmptyTable_ReturnsEmptyGrid() => AssertEmptyGrid(MakeTable(0));

	[Fact]
	public void Resolve_NoRows_ReturnsEmptyGrid() => AssertEmptyGrid(MakeTable(1));

	[Fact]
	public void Resolve_NoColumns_ReturnsEmptyGrid() => AssertEmptyGrid(MakeTable(0, Row(MakeCell())));

	[Fact]
	public void Resolve_SingleCell_OwnerIsItself()
	{
		var cell = MakeCell();

		var grid = TableGridResolver.Resolve(MakeTable(1, Row(cell)));

		AssertSize(grid, 1, 1);
		AssertOwner(grid, 0, 0, cell, 0, 0);
	}

	[Fact]
	public void Resolve_TwoCellsSingleRow_EachOwnsItsColumn()
	{
		var cell0 = MakeCell();
		var cell1 = MakeCell();

		var grid = TableGridResolver.Resolve(MakeTable(2, Row(cell0, cell1)));

		AssertOwner(grid, 0, 0, cell0);
		AssertOwner(grid, 0, 1, cell1);
	}

	[Fact]
	public void Resolve_GridSpan_CellOccupiesMultipleColumns()
	{
		var merged = MakeCell(gridSpan: 2);

		var grid = TableGridResolver.Resolve(MakeTable(3, Row(merged, MakeCell())));

		AssertOwner(grid, 0, 0, merged, ownerColumn: 0);
		AssertOwner(grid, 0, 1, merged, ownerColumn: 0);
		grid[0, 2]!.Value.Cell.Should().NotBeSameAs(merged);
	}

	[Fact]
	public void Resolve_VerticalMerge_ContinueCellReferencesRestartOwner()
	{
		var restart = MakeCell(verticalMerge: Restart);

		var grid = TableGridResolver.Resolve(MakeTable(1, Row(restart), Row(MakeCell(verticalMerge: Continue))));

		AssertOwner(grid, 0, 0, restart, 0);
		AssertOwner(grid, 1, 0, restart, 0);
	}

	[Fact]
	public void Resolve_VerticalMergeThreeRows_AllPointToRestart()
	{
		var restart = MakeCell(verticalMerge: Restart);

		var grid = TableGridResolver.Resolve(MakeTable(
			1,
			Row(restart),
			Row(MakeCell(verticalMerge: Continue)),
			Row(MakeCell(verticalMerge: Continue))));

		AssertOwner(grid, 0, 0, restart);
		AssertOwner(grid, 1, 0, restart, 0);
		AssertOwner(grid, 2, 0, restart, 0);
	}

	[Fact]
	public void Resolve_MixedMerge_GridSpanAndVerticalMerge()
	{
		var topMerged = MakeCell(gridSpan: 2, verticalMerge: Restart);
		var topRight = MakeCell();
		var botRight = MakeCell();

		var grid = TableGridResolver.Resolve(MakeTable(
			3,
			Row(topMerged, topRight),
			Row(MakeCell(gridSpan: 2, verticalMerge: Continue), botRight)));

		AssertOwner(grid, 0, 0, topMerged);
		AssertOwner(grid, 0, 1, topMerged);
		AssertOwner(grid, 0, 2, topRight);
		AssertOwner(grid, 1, 0, topMerged, 0, 0);
		AssertOwner(grid, 1, 1, topMerged);
		AssertOwner(grid, 1, 2, botRight);
	}

	[Fact]
	public void Resolve_VerticalMergeSkipsOccupiedColumns()
	{
		var restart = MakeCell(verticalMerge: Restart);
		var regular = MakeCell();

		var grid = TableGridResolver.Resolve(MakeTable(
			2,
			Row(restart, MakeCell()),
			Row(MakeCell(verticalMerge: Continue), MakeCell()),
			Row(regular, MakeCell())));

		AssertOwner(grid, 0, 0, restart);
		AssertOwner(grid, 1, 0, restart);
		AssertOwner(grid, 2, 0, regular);
	}

	[Fact]
	public void Resolve_VerticalMergePushesNextCellRight()
	{
		var restart = MakeCell(verticalMerge: Restart);
		var cellR1 = MakeCell();

		var grid = TableGridResolver.Resolve(MakeTable(
			2,
			Row(restart, MakeCell()),
			Row(MakeCell(verticalMerge: Continue), cellR1)));

		AssertOwner(grid, 1, 0, restart);
		AssertOwner(grid, 1, 1, cellR1);
	}

	[Fact]
	public void Resolve_VerticalMergeWithGridSpanSkipsOccupied()
	{
		var restart = MakeCell(gridSpan: 2, verticalMerge: Restart);
		var cellR1C2 = MakeCell();

		var grid = TableGridResolver.Resolve(MakeTable(
			3,
			Row(restart, MakeCell()),
			Row(MakeCell(gridSpan: 2, verticalMerge: Continue), cellR1C2)));

		AssertOwner(grid, 1, 0, restart);
		AssertOwner(grid, 1, 1, restart);
		AssertOwner(grid, 1, 2, cellR1C2);
	}

	[Fact]
	public void Resolve_ContinueWithNoRestart_UsesCellItself()
	{
		var cont = MakeCell(verticalMerge: Continue);

		var grid = TableGridResolver.Resolve(MakeTable(1, Row(cont)));

		AssertOwner(grid, 0, 0, cont, 0);
	}

	[Fact]
	public void Resolve_GridSpanExceedsColumns_ClipsToGrid()
	{
		var wide = MakeCell(gridSpan: 5);

		var grid = TableGridResolver.Resolve(MakeTable(2, Row(wide)));

		AssertOwner(grid, 0, 0, wide);
		AssertOwner(grid, 0, 1, wide);
	}

	[Fact]
	public void Resolve_MoreCellsThanColumns_ExtraCellsIgnored()
	{
		var cell0 = MakeCell();
		var cell1 = MakeCell();

		var grid = TableGridResolver.Resolve(MakeTable(2, Row(cell0, cell1, MakeCell())));

		AssertOwner(grid, 0, 0, cell0);
		AssertOwner(grid, 0, 1, cell1);
	}

	[Fact]
	public void Resolve_FewerCellsThanColumns_RemainingColumnsNull()
	{
		var cell = MakeCell();

		var grid = TableGridResolver.Resolve(MakeTable(3, Row(cell)));

		AssertOwner(grid, 0, 0, cell);
		grid[0, 1].Should().BeNull();
		grid[0, 2].Should().BeNull();
	}

	[Fact]
	public void Resolve_GridDimensions_MatchRowAndColumnCounts()
	{
		var grid = TableGridResolver.Resolve(MakeTable(
			3,
			Row(MakeCell(), MakeCell(), MakeCell()),
			Row(MakeCell(), MakeCell(), MakeCell())));

		AssertSize(grid, 2, 3);
	}

	private static void AssertEmptyGrid(TableElement table)
		=> AssertSize(TableGridResolver.Resolve(table), 0, 0);

	private static void AssertSize(ResolvedGridCell?[,] grid, int rows, int columns)
	{
		grid.GetLength(0).Should().Be(rows);
		grid.GetLength(1).Should().Be(columns);
	}

	private static void AssertOwner(
		ResolvedGridCell?[,] grid,
		int row,
		int column,
		TableCellElement cell,
		int? ownerRow = null,
		int? ownerColumn = null)
	{
		var resolved = grid[row, column];
		resolved.Should().NotBeNull();
		resolved!.Value.Cell.Should().BeSameAs(cell);
		if (ownerRow is { } r)
		{
			resolved.Value.OwnerRowIndex.Should().Be(r);
		}

		if (ownerColumn is { } c)
		{
			resolved.Value.OwnerColumnIndex.Should().Be(c);
		}
	}

	private static TableRowElement Row(params TableCellElement[] cells) => new() { Cells = cells };

	private static TableCellElement MakeCell(
		int gridSpan = 1,
		VerticalMergeState verticalMerge = VerticalMergeState.None)
		=> new()
		{
			Blocks = [],
			GridSpan = gridSpan,
			VerticalMerge = verticalMerge,
		};

	private static TableElement MakeTable(int columnCount, params TableRowElement[] rows)
		=> new()
		{
			GridColumns = [.. Enumerable.Range(0, columnCount).Select(_ => new TableGridColumn(1440f))],
			Rows = rows,
		};
}
