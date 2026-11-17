namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed partial class TableLayoutEngineTests
{
	[Fact]
	public void ComputeVerticalMergeRegions_MultipleRegions_ReturnsInOrder()
	{
		var r0c0 = MakeCell(verticalMerge: VerticalMergeState.Restart);
		var r1c0 = MakeCell(verticalMerge: VerticalMergeState.Continue);
		var r0c1 = MakeCell(verticalMerge: VerticalMergeState.Restart);
		var r1c1 = MakeCell(verticalMerge: VerticalMergeState.Continue);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement { Cells = [r0c0, r0c1], HeightTwips = 300f },
				new TableRowElement { Cells = [r1c0, r1c1], HeightTwips = 300f },
			],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var regions = TableLayoutEngine.ComputeVerticalMergeRegions(layout);

		regions.Should().HaveCount(2);
		regions[0].StartRowIndex.Should().Be(0);
		regions[0].ColumnIndex.Should().Be(0);
		regions[1].StartRowIndex.Should().Be(0);
		regions[1].ColumnIndex.Should().Be(1);
	}

	[Fact]
	public void ComputeVerticalMergeRegions_ContinueWithoutRestart_IsIgnored()
	{
		var orphanContinue = MakeCell(verticalMerge: VerticalMergeState.Continue);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f)],
			Rows = [new TableRowElement { Cells = [orphanContinue], HeightTwips = 300f }],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var regions = TableLayoutEngine.ComputeVerticalMergeRegions(layout);

		regions.Should().BeEmpty();
	}

	[Fact]
	public void ComputeVerticalMergeRegions_WithHorizontalAndVerticalMerge_UsesOwnerGeometry()
	{
		var restart = MakeCell(gridSpan: 2, verticalMerge: VerticalMergeState.Restart);
		var cont = MakeCell(gridSpan: 2, verticalMerge: VerticalMergeState.Continue);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1500f), new TableGridColumn(2000f)],
			Rows =
			[
				new TableRowElement { Cells = [restart, MakeCell()], HeightTwips = 400f },
				new TableRowElement { Cells = [cont, MakeCell()], HeightTwips = 600f },
			],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var regions = TableLayoutEngine.ComputeVerticalMergeRegions(layout);

		regions.Should().HaveCount(1);
		regions[0].ColumnIndex.Should().Be(0);
		regions[0].RowSpan.Should().Be(2);
		regions[0].Width.Should().Be(2500f);
		regions[0].Height.Should().Be(1000f);
		regions[0].Cell.Should().BeSameAs(restart);
	}

	// ---- Combined merge regions (4.4.3) ----

	[Fact]
	public void ComputeMergedCellRegions_NullLayout_ThrowsArgumentNullException()
	{
		var act = () => TableLayoutEngine.ComputeMergedCellRegions(null!);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("layout");
	}

	[Fact]
	public void ComputeMergedCellRegions_EmptyTable_ReturnsEmpty()
	{
		var table = new TableElement { GridColumns = [], Rows = [] };
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var regions = TableLayoutEngine.ComputeMergedCellRegions(layout);

		regions.Should().BeEmpty();
	}

	[Fact]
	public void ComputeMergedCellRegions_NoMerges_ReturnsEmpty()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement { Cells = [MakeCell(), MakeCell()] },
				new TableRowElement { Cells = [MakeCell(), MakeCell()] },
			],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var regions = TableLayoutEngine.ComputeMergedCellRegions(layout);

		regions.Should().BeEmpty();
	}

	[Fact]
	public void ComputeMergedCellRegions_HorizontalOnly_IncludesRegion()
	{
		var merged = MakeCell(gridSpan: 2);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1200f), new TableGridColumn(800f), new TableGridColumn(1000f)],
			Rows = [new TableRowElement { Cells = [merged, MakeCell()], HeightTwips = 300f }],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var regions = TableLayoutEngine.ComputeMergedCellRegions(layout);

		regions.Should().HaveCount(1);
		regions[0].RowSpan.Should().Be(1);
		regions[0].ColumnSpan.Should().Be(2);
		regions[0].Width.Should().Be(2000f);
		regions[0].Height.Should().Be(300f);
	}

	[Fact]
	public void ComputeMergedCellRegions_VerticalOnly_IncludesRegion()
	{
		var restart = MakeCell(verticalMerge: VerticalMergeState.Restart);
		var cont = MakeCell(verticalMerge: VerticalMergeState.Continue);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement { Cells = [restart], HeightTwips = 250f },
				new TableRowElement { Cells = [cont], HeightTwips = 350f },
			],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var regions = TableLayoutEngine.ComputeMergedCellRegions(layout);

		regions.Should().HaveCount(1);
		regions[0].RowSpan.Should().Be(2);
		regions[0].ColumnSpan.Should().Be(1);
		regions[0].Width.Should().Be(1000f);
		regions[0].Height.Should().Be(600f);
	}

	[Fact]
	public void ComputeMergedCellRegions_CombinedHorizontalAndVertical_IncludesRectangularRegion()
	{
		var restart = MakeCell(gridSpan: 2, verticalMerge: VerticalMergeState.Restart);
		var cont = MakeCell(gridSpan: 2, verticalMerge: VerticalMergeState.Continue);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1500f), new TableGridColumn(500f)],
			Rows =
			[
				new TableRowElement { Cells = [restart, MakeCell()], HeightTwips = 400f },
				new TableRowElement { Cells = [cont, MakeCell()], HeightTwips = 600f },
			],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var regions = TableLayoutEngine.ComputeMergedCellRegions(layout);

		regions.Should().HaveCount(1);
		regions[0].StartRowIndex.Should().Be(0);
		regions[0].StartColumnIndex.Should().Be(0);
		regions[0].RowSpan.Should().Be(2);
		regions[0].ColumnSpan.Should().Be(2);
		regions[0].Width.Should().Be(2500f);
		regions[0].Height.Should().Be(1000f);
		regions[0].Cell.Should().BeSameAs(restart);
	}

	[Fact]
	public void ComputeMergedCellRegions_MixedMergedCells_ReturnsInReadingOrder()
	{
		var r0c0 = MakeCell(gridSpan: 2);
		var r1c0 = MakeCell(verticalMerge: VerticalMergeState.Restart);
		var r2c0 = MakeCell(verticalMerge: VerticalMergeState.Continue);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1000f), new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement { Cells = [r0c0, MakeCell()], HeightTwips = 300f },
				new TableRowElement { Cells = [r1c0, MakeCell(), MakeCell()], HeightTwips = 300f },
				new TableRowElement { Cells = [r2c0, MakeCell(), MakeCell()], HeightTwips = 300f },
			],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var regions = TableLayoutEngine.ComputeMergedCellRegions(layout);

		regions.Should().HaveCount(2);
		regions[0].StartRowIndex.Should().Be(0);
		regions[0].StartColumnIndex.Should().Be(0);
		regions[1].StartRowIndex.Should().Be(1);
		regions[1].StartColumnIndex.Should().Be(0);
	}

	// ---- Merged content layout (4.4.4) ----

	[Fact]
	public void ComputeMergedCellContentLayouts_NullLayout_ThrowsArgumentNullException()
	{
		var act = () => TableLayoutEngine.ComputeMergedCellContentLayouts(null!);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("layout");
	}

	[Fact]
	public void ComputeMergedCellContentLayouts_NoMergedCells_ReturnsEmpty()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f)],
			Rows = [new TableRowElement { Cells = [MakeCell()] }],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var contentLayouts = TableLayoutEngine.ComputeMergedCellContentLayouts(layout);

		contentLayouts.Should().BeEmpty();
	}

	[Fact]
	public void ComputeMergedCellContentLayouts_HorizontalMerge_AdjustsContentAreaForMargins()
	{
		var mergedCell = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() }],
			GridSpan = 2,
			Margins = new CellMargins(20f, 50f, 30f, 40f),
		};
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1500f), new TableGridColumn(500f)],
			Rows = [new TableRowElement { Cells = [mergedCell, MakeCell()], HeightTwips = 400f }],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var contentLayouts = TableLayoutEngine.ComputeMergedCellContentLayouts(layout);

		contentLayouts.Should().HaveCount(1);
		contentLayouts[0].CellWidth.Should().Be(2500f);
		contentLayouts[0].ContentX.Should().Be(40f); // left margin
		contentLayouts[0].ContentWidth.Should().Be(2410f); // 2500 - 40 - 50
		contentLayouts[0].ContentHeight.Should().Be(240f); // one paragraph block
		contentLayouts[0].Blocks.Should().HaveCount(1);
	}

	[Fact]
	public void ComputeMergedCellContentLayouts_VerticalMerge_CenterAlignmentAdjustsY()
	{
		var restartCell = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() }],
			VerticalMerge = VerticalMergeState.Restart,
			VerticalAlignment = CellVerticalAlignment.Center,
		};
		var continueCell = new TableCellElement
		{
			Blocks = [],
			VerticalMerge = VerticalMergeState.Continue,
		};
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1200f)],
			Rows =
			[
				new TableRowElement { Cells = [restartCell], HeightTwips = 300f },
				new TableRowElement { Cells = [continueCell], HeightTwips = 500f },
			],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var contentLayouts = TableLayoutEngine.ComputeMergedCellContentLayouts(layout);

		contentLayouts.Should().HaveCount(1);
		contentLayouts[0].CellHeight.Should().Be(800f);
		contentLayouts[0].ContentY.Should().Be(280f); // (800 - 240) / 2
	}

	[Fact]
	public void ComputeMergedCellContentLayouts_CombinedMerge_UsesRectangularRegionGeometry()
	{
		var restart = new TableCellElement
		{
			Blocks =
			[
				new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() },
				new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() },
			],
			GridSpan = 2,
			VerticalMerge = VerticalMergeState.Restart,
			Margins = new CellMargins(10f, 20f, 10f, 20f),
		};
		var cont = new TableCellElement
		{
			Blocks = [],
			GridSpan = 2,
			VerticalMerge = VerticalMergeState.Continue,
		};
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1500f), new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement { Cells = [restart, MakeCell()], HeightTwips = 400f },
				new TableRowElement { Cells = [cont, MakeCell()], HeightTwips = 600f },
			],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var contentLayouts = TableLayoutEngine.ComputeMergedCellContentLayouts(layout);

		contentLayouts.Should().HaveCount(1);
		contentLayouts[0].RowSpan.Should().Be(2);
		contentLayouts[0].ColumnSpan.Should().Be(2);
		contentLayouts[0].CellWidth.Should().Be(2500f);
		contentLayouts[0].CellHeight.Should().Be(1100f);
		contentLayouts[0].ContentWidth.Should().Be(2460f);
		contentLayouts[0].ContentHeight.Should().Be(480f); // 2 paragraph blocks
		contentLayouts[0].Blocks.Should().HaveCount(2);
	}

	[Fact]
	public void ComputeMergedCellContentLayouts_MultipleMergedCells_ReturnsInReadingOrder()
	{
		var horizontal = MakeCell(gridSpan: 2);
		var verticalStart = MakeCell(verticalMerge: VerticalMergeState.Restart);
		var verticalCont = MakeCell(verticalMerge: VerticalMergeState.Continue);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1000f), new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement { Cells = [horizontal, MakeCell()] },
				new TableRowElement { Cells = [verticalStart, MakeCell(), MakeCell()] },
				new TableRowElement { Cells = [verticalCont, MakeCell(), MakeCell()] },
			],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

		var contentLayouts = TableLayoutEngine.ComputeMergedCellContentLayouts(layout);

		contentLayouts.Should().HaveCount(2);
		contentLayouts[0].StartRowIndex.Should().Be(0);
		contentLayouts[0].StartColumnIndex.Should().Be(0);
		contentLayouts[1].StartRowIndex.Should().Be(1);
		contentLayouts[1].StartColumnIndex.Should().Be(0);
	}

	// ---- Table cell backgrounds (4.8.2) ----

	[Fact]
	public void ComputeCellBackgrounds_NullLayout_ThrowsArgumentNullException()
	{
		var act = () => TableLayoutEngine.ComputeCellBackgrounds(null!);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("layout");
	}

	[Fact]
	public void ComputeCellBackgrounds_NoVisibleShading_ReturnsEmpty()
	{
		var layout = TableLayoutEngine.Layout(
			new TableElement
			{
				GridColumns = [new TableGridColumn(1200f)],
				Rows = [new TableRowElement { Cells = [MakeCell()] }],
			},
			1200f);

		var backgrounds = TableLayoutEngine.ComputeCellBackgrounds(layout);

		backgrounds.Should().BeEmpty();
	}

	[Fact]
	public void ComputeCellBackgrounds_ShadedCells_ReturnsReadingOrderRectangles()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1500f)],
			Rows =
			[
				new TableRowElement
				{
					Cells =
					[
						new TableCellElement
						{
							Blocks = [],
							Shading = new ParagraphShading(ShadingPattern.Clear, FillColor: "FFFF00"),
						},
						new TableCellElement
						{
							Blocks = [],
							Shading = new ParagraphShading(ShadingPattern.Solid, PatternColor: "00FF00"),
						},
					],
				},
			],
		};

		var layout = TableLayoutEngine.Layout(table, 2500f);

		var backgrounds = TableLayoutEngine.ComputeCellBackgrounds(layout);

		backgrounds.Should().HaveCount(2);

		backgrounds[0].RowIndex.Should().Be(0);
		backgrounds[0].ColumnIndex.Should().Be(0);
		backgrounds[0].X.Should().Be(0f);
		backgrounds[0].Y.Should().Be(0f);
		backgrounds[0].Width.Should().Be(1000f);
		backgrounds[0].Height.Should().Be(240f);
		backgrounds[0].Shading.FillColor.Should().Be("FFFF00");

		backgrounds[1].RowIndex.Should().Be(0);
		backgrounds[1].ColumnIndex.Should().Be(1);
		backgrounds[1].X.Should().Be(1000f);
		backgrounds[1].Width.Should().Be(1500f);
		backgrounds[1].Shading.Pattern.Should().Be(ShadingPattern.Solid);
		backgrounds[1].Shading.PatternColor.Should().Be("00FF00");
	}
}
