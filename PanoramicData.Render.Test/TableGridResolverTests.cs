namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class TableGridResolverTests
{
	[Fact]
	public void Resolve_NullTable_ThrowsArgumentNullException()
	{
		var act = () => TableGridResolver.Resolve(null!);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("table");
	}

	[Fact]
	public void Resolve_EmptyTable_ReturnsEmptyGrid()
	{
		var table = new TableElement
		{
			GridColumns = [],
			Rows = [],
		};

		var grid = TableGridResolver.Resolve(table);

		grid.GetLength(0).Should().Be(0);
		grid.GetLength(1).Should().Be(0);
	}

	[Fact]
	public void Resolve_NoRows_ReturnsEmptyGrid()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1440f)],
			Rows = [],
		};

		var grid = TableGridResolver.Resolve(table);

		grid.GetLength(0).Should().Be(0);
		grid.GetLength(1).Should().Be(0);
	}

	[Fact]
	public void Resolve_NoColumns_ReturnsEmptyGrid()
	{
		var table = new TableElement
		{
			GridColumns = [],
			Rows = [new TableRowElement { Cells = [MakeCell()] }],
		};

		var grid = TableGridResolver.Resolve(table);

		grid.GetLength(0).Should().Be(0);
		grid.GetLength(1).Should().Be(0);
	}

	[Fact]
	public void Resolve_SingleCell_OwnerIsItself()
	{
		var cell = MakeCell();
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1440f)],
			Rows = [new TableRowElement { Cells = [cell] }],
		};

		var grid = TableGridResolver.Resolve(table);

		grid.GetLength(0).Should().Be(1);
		grid.GetLength(1).Should().Be(1);
		grid[0, 0].Should().NotBeNull();
		grid[0, 0]!.Value.OwnerRowIndex.Should().Be(0);
		grid[0, 0]!.Value.OwnerColumnIndex.Should().Be(0);
		grid[0, 0]!.Value.Cell.Should().BeSameAs(cell);
	}

	[Fact]
	public void Resolve_TwoCellsSingleRow_EachOwnsItsColumn()
	{
		var cell0 = MakeCell();
		var cell1 = MakeCell();
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1440f), new TableGridColumn(1440f)],
			Rows = [new TableRowElement { Cells = [cell0, cell1] }],
		};

		var grid = TableGridResolver.Resolve(table);

		grid[0, 0]!.Value.Cell.Should().BeSameAs(cell0);
		grid[0, 1]!.Value.Cell.Should().BeSameAs(cell1);
	}

	[Fact]
	public void Resolve_GridSpan_CellOccupiesMultipleColumns()
	{
		var merged = MakeCell(gridSpan: 2);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(720f), new TableGridColumn(720f), new TableGridColumn(720f)],
			Rows = [new TableRowElement { Cells = [merged, MakeCell()] }],
		};

		var grid = TableGridResolver.Resolve(table);

		grid[0, 0]!.Value.Cell.Should().BeSameAs(merged);
		grid[0, 0]!.Value.OwnerColumnIndex.Should().Be(0);
		grid[0, 1]!.Value.Cell.Should().BeSameAs(merged);
		grid[0, 1]!.Value.OwnerColumnIndex.Should().Be(0);
		grid[0, 2]!.Value.Cell.Should().NotBeSameAs(merged);
	}

	[Fact]
	public void Resolve_VerticalMerge_ContinueCellReferencesRestartOwner()
	{
		var restart = MakeCell(verticalMerge: VerticalMergeState.Restart);
		var cont = MakeCell(verticalMerge: VerticalMergeState.Continue);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1440f)],
			Rows =
			[
				new TableRowElement { Cells = [restart] },
				new TableRowElement { Cells = [cont] },
			],
		};

		var grid = TableGridResolver.Resolve(table);

		grid[0, 0]!.Value.Cell.Should().BeSameAs(restart);
		grid[0, 0]!.Value.OwnerRowIndex.Should().Be(0);
		grid[1, 0]!.Value.Cell.Should().BeSameAs(restart);
		grid[1, 0]!.Value.OwnerRowIndex.Should().Be(0);
	}

	[Fact]
	public void Resolve_VerticalMergeThreeRows_AllPointToRestart()
	{
		var restart = MakeCell(verticalMerge: VerticalMergeState.Restart);
		var cont1 = MakeCell(verticalMerge: VerticalMergeState.Continue);
		var cont2 = MakeCell(verticalMerge: VerticalMergeState.Continue);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1440f)],
			Rows =
			[
				new TableRowElement { Cells = [restart] },
				new TableRowElement { Cells = [cont1] },
				new TableRowElement { Cells = [cont2] },
			],
		};

		var grid = TableGridResolver.Resolve(table);

		grid[0, 0]!.Value.Cell.Should().BeSameAs(restart);
		grid[1, 0]!.Value.Cell.Should().BeSameAs(restart);
		grid[1, 0]!.Value.OwnerRowIndex.Should().Be(0);
		grid[2, 0]!.Value.Cell.Should().BeSameAs(restart);
		grid[2, 0]!.Value.OwnerRowIndex.Should().Be(0);
	}

	[Fact]
	public void Resolve_MixedMerge_GridSpanAndVerticalMerge()
	{
		// 3 columns, 2 rows
		// Row 0: cell spanning 2 cols (restart vmerge), then 1 regular cell
		// Row 1: cell spanning 2 cols (continue vmerge), then 1 regular cell
		var topMerged = MakeCell(gridSpan: 2, verticalMerge: VerticalMergeState.Restart);
		var topRight = MakeCell();
		var botMerged = MakeCell(gridSpan: 2, verticalMerge: VerticalMergeState.Continue);
		var botRight = MakeCell();

		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(720f), new TableGridColumn(720f), new TableGridColumn(720f)],
			Rows =
			[
				new TableRowElement { Cells = [topMerged, topRight] },
				new TableRowElement { Cells = [botMerged, botRight] },
			],
		};

		var grid = TableGridResolver.Resolve(table);

		// Top row: merged cell occupies [0,0] and [0,1]
		grid[0, 0]!.Value.Cell.Should().BeSameAs(topMerged);
		grid[0, 1]!.Value.Cell.Should().BeSameAs(topMerged);
		grid[0, 2]!.Value.Cell.Should().BeSameAs(topRight);

		// Bottom row: continue cell references the restart cell
		grid[1, 0]!.Value.Cell.Should().BeSameAs(topMerged);
		grid[1, 0]!.Value.OwnerRowIndex.Should().Be(0);
		grid[1, 0]!.Value.OwnerColumnIndex.Should().Be(0);
		grid[1, 1]!.Value.Cell.Should().BeSameAs(topMerged);
		grid[1, 2]!.Value.Cell.Should().BeSameAs(botRight);
	}

	[Fact]
	public void Resolve_VerticalMergeSkipsOccupiedColumns()
	{
		// 2 columns, 3 rows
		// Row 0: restart in col 0, regular in col 1
		// Row 1: continue in col 0, regular in col 1
		// Row 2: regular in col 0, regular in col 1
		var restart = MakeCell(verticalMerge: VerticalMergeState.Restart);
		var cont = MakeCell(verticalMerge: VerticalMergeState.Continue);
		var regular = MakeCell();

		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1440f), new TableGridColumn(1440f)],
			Rows =
			[
				new TableRowElement { Cells = [restart, MakeCell()] },
				new TableRowElement { Cells = [cont, MakeCell()] },
				new TableRowElement { Cells = [regular, MakeCell()] },
			],
		};

		var grid = TableGridResolver.Resolve(table);

		grid[0, 0]!.Value.Cell.Should().BeSameAs(restart);
		grid[1, 0]!.Value.Cell.Should().BeSameAs(restart);
		grid[2, 0]!.Value.Cell.Should().BeSameAs(regular);
	}

	[Fact]
	public void Resolve_VerticalMergePushesNextCellRight()
	{
		// 2 columns, 2 rows
		// Row 0: col 0 = restart vmerge (span 1), col 1 = regular
		// Row 1: col 0 = continue vmerge, next cell should go to col 1
		// The continue cell fills col 0, then the regular cell at row 1 gets col 1.
		// But we need a 3-col scenario where the continue cell occupies col 0
		// and the next cell in row 1 has to skip over it.
		var restart = MakeCell(verticalMerge: VerticalMergeState.Restart);
		var cellR1 = MakeCell();

		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(720f), new TableGridColumn(720f)],
			Rows =
			[
				new TableRowElement { Cells = [restart, MakeCell()] },
				new TableRowElement { Cells = [MakeCell(verticalMerge: VerticalMergeState.Continue), cellR1] },
			],
		};

		var grid = TableGridResolver.Resolve(table);

		// Row 1, col 0 is occupied by vmerge continue → points to restart
		grid[1, 0]!.Value.Cell.Should().BeSameAs(restart);
		// Row 1, col 1 is the second cell from row 1
		grid[1, 1]!.Value.Cell.Should().BeSameAs(cellR1);
	}

	[Fact]
	public void Resolve_VerticalMergeWithGridSpanSkipsOccupied()
	{
		// 3 columns, 2 rows
		// Row 0: col 0-1 = restart vmerge (gridSpan=2), col 2 = regular
		// Row 1: col 0-1 = continue vmerge (gridSpan=2), col 2 = regular cell
		// Then ensure row 1 col 2 cell doesn't collide
		var restart = MakeCell(gridSpan: 2, verticalMerge: VerticalMergeState.Restart);
		var cellR1C2 = MakeCell();

		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(720f), new TableGridColumn(720f), new TableGridColumn(720f)],
			Rows =
			[
				new TableRowElement { Cells = [restart, MakeCell()] },
				new TableRowElement { Cells = [MakeCell(gridSpan: 2, verticalMerge: VerticalMergeState.Continue), cellR1C2] },
			],
		};

		var grid = TableGridResolver.Resolve(table);

		grid[1, 0]!.Value.Cell.Should().BeSameAs(restart);
		grid[1, 1]!.Value.Cell.Should().BeSameAs(restart);
		grid[1, 2]!.Value.Cell.Should().BeSameAs(cellR1C2);
	}

	[Fact]
	public void Resolve_ContinueWithNoRestart_UsesCellItself()
	{
		// Edge case: a continue cell with no restart above
		var cont = MakeCell(verticalMerge: VerticalMergeState.Continue);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1440f)],
			Rows = [new TableRowElement { Cells = [cont] }],
		};

		var grid = TableGridResolver.Resolve(table);

		// Should use the continue cell itself as fallback
		grid[0, 0]!.Value.Cell.Should().BeSameAs(cont);
		grid[0, 0]!.Value.OwnerRowIndex.Should().Be(0);
	}

	[Fact]
	public void Resolve_GridSpanExceedsColumns_ClipsToGrid()
	{
		var wide = MakeCell(gridSpan: 5);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(720f), new TableGridColumn(720f)],
			Rows = [new TableRowElement { Cells = [wide] }],
		};

		var grid = TableGridResolver.Resolve(table);

		grid[0, 0]!.Value.Cell.Should().BeSameAs(wide);
		grid[0, 1]!.Value.Cell.Should().BeSameAs(wide);
	}

	[Fact]
	public void Resolve_MoreCellsThanColumns_ExtraCellsIgnored()
	{
		var cell0 = MakeCell();
		var cell1 = MakeCell();
		var extra = MakeCell();
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1440f), new TableGridColumn(1440f)],
			Rows = [new TableRowElement { Cells = [cell0, cell1, extra] }],
		};

		var grid = TableGridResolver.Resolve(table);

		grid[0, 0]!.Value.Cell.Should().BeSameAs(cell0);
		grid[0, 1]!.Value.Cell.Should().BeSameAs(cell1);
	}

	[Fact]
	public void Resolve_FewerCellsThanColumns_RemainingColumnsNull()
	{
		var cell = MakeCell();
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(720f), new TableGridColumn(720f), new TableGridColumn(720f)],
			Rows = [new TableRowElement { Cells = [cell] }],
		};

		var grid = TableGridResolver.Resolve(table);

		grid[0, 0]!.Value.Cell.Should().BeSameAs(cell);
		grid[0, 1].Should().BeNull();
		grid[0, 2].Should().BeNull();
	}

	[Fact]
	public void Resolve_GridDimensions_MatchRowAndColumnCounts()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(720f), new TableGridColumn(720f), new TableGridColumn(720f)],
			Rows =
			[
				new TableRowElement { Cells = [MakeCell(), MakeCell(), MakeCell()] },
				new TableRowElement { Cells = [MakeCell(), MakeCell(), MakeCell()] },
			],
		};

		var grid = TableGridResolver.Resolve(table);

		grid.GetLength(0).Should().Be(2);
		grid.GetLength(1).Should().Be(3);
	}

	private static TableCellElement MakeCell(
		int gridSpan = 1,
		VerticalMergeState verticalMerge = VerticalMergeState.None)
		=> new()
		{
			Blocks = [],
			GridSpan = gridSpan,
			VerticalMerge = verticalMerge,
		};
}
