namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class TableMergeLayoutIntegrationTests
{
	[Fact]
	public void HorizontalMerge_RegionAndContentGeometry_AreConsistent()
	{
		var mergedCell = Cell(1, gridSpan: 2, margins: new CellMargins(10f, 20f, 10f, 30f));
		var layout = Layout([1000f, 1500f, 500f], (300f, [mergedCell, MakeCell()]));

		var regions = TableLayoutEngine.ComputeMergedCellRegions(layout);
		var content = TableLayoutEngine.ComputeMergedCellContentLayouts(layout);

		regions.Should().HaveCount(1);
		content.Should().HaveCount(1);
		regions[0].ColumnSpan.Should().Be(2);
		regions[0].Width.Should().Be(2500f);
		content[0].ContentX.Should().Be(30f);
		content[0].ContentWidth.Should().Be(2450f);
	}

	[Fact]
	public void VerticalMerge_CenterAlignment_PositionsContentInsideMergedHeight()
	{
		var restart = Cell(1, verticalMerge: VerticalMergeState.Restart, alignment: CellVerticalAlignment.Center);
		var layout = Layout(
			[1000f, 1000f],
			(400f, [restart, MakeCell()]),
			(600f, [Cell(0, verticalMerge: VerticalMergeState.Continue), MakeCell()]));

		var vRegions = TableLayoutEngine.ComputeVerticalMergeRegions(layout);
		var content = TableLayoutEngine.ComputeMergedCellContentLayouts(layout);

		vRegions.Should().HaveCount(1);
		vRegions[0].RowSpan.Should().Be(2);
		vRegions[0].Height.Should().Be(1000f);
		content.Should().HaveCount(1);
		content[0].ContentY.Should().Be(380f); // (1000 - 240) / 2
	}

	[Fact]
	public void CombinedMerge_ProducesSingleRectangularRegion_AndContentLayout()
	{
		var layout = Layout(
			[1000f, 1000f, 1000f],
			(300f, [Cell(2, 2, VerticalMergeState.Restart), MakeCell()]),
			(500f, [Cell(0, 2, VerticalMergeState.Continue), MakeCell()]));

		var merged = TableLayoutEngine.ComputeMergedCellRegions(layout);
		var content = TableLayoutEngine.ComputeMergedCellContentLayouts(layout);

		merged.Should().HaveCount(1);
		merged[0].RowSpan.Should().Be(2);
		merged[0].ColumnSpan.Should().Be(2);
		merged[0].Width.Should().Be(2000f);
		merged[0].Height.Should().Be(980f);

		content.Should().HaveCount(1);
		content[0].RowSpan.Should().Be(2);
		content[0].ColumnSpan.Should().Be(2);
		content[0].ContentHeight.Should().Be(480f);
	}

	private static TableLayoutResult Layout(float[] columns, params (float Height, TableCellElement[] Cells)[] rows)
		=> TableLayoutEngine.Layout(
			new TableElement
			{
				GridColumns = [.. columns.Select(width => new TableGridColumn(width))],
				Rows = [.. rows.Select(row => new TableRowElement { Cells = row.Cells, HeightTwips = row.Height })],
			},
			9600f);

	private static TableCellElement Cell(
		int paragraphs,
		int gridSpan = 1,
		VerticalMergeState verticalMerge = VerticalMergeState.None,
		CellMargins? margins = null,
		CellVerticalAlignment alignment = CellVerticalAlignment.Top)
		=> new()
		{
			Blocks = [.. Enumerable.Range(0, paragraphs).Select(_ => new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() })],
			GridSpan = gridSpan,
			VerticalMerge = verticalMerge,
			Margins = margins ?? CellMargins.None,
			VerticalAlignment = alignment,
		};

	private static TableCellElement MakeCell() => Cell(0);
}
