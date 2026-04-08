namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using Xunit;

public sealed class TableMergeLayoutIntegrationTests
{
	[Fact]
	public void HorizontalMerge_RegionAndContentGeometry_AreConsistent()
	{
		var mergedCell = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() }],
			GridSpan = 2,
			Margins = new CellMargins(10f, 20f, 10f, 30f),
		};
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1500f), new TableGridColumn(500f)],
			Rows = [new TableRowElement { Cells = [mergedCell, MakeCell()], HeightTwips = 300f }],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

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
		var restart = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() }],
			VerticalMerge = VerticalMergeState.Restart,
			VerticalAlignment = CellVerticalAlignment.Center,
		};
		var cont = new TableCellElement
		{
			Blocks = [],
			VerticalMerge = VerticalMergeState.Continue,
		};
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement { Cells = [restart, MakeCell()], HeightTwips = 400f },
				new TableRowElement { Cells = [cont, MakeCell()], HeightTwips = 600f },
			],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

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
		var restart = new TableCellElement
		{
			Blocks =
			[
				new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() },
				new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() },
			],
			GridSpan = 2,
			VerticalMerge = VerticalMergeState.Restart,
		};
		var cont = new TableCellElement
		{
			Blocks = [],
			GridSpan = 2,
			VerticalMerge = VerticalMergeState.Continue,
		};
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1000f), new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement { Cells = [restart, MakeCell()], HeightTwips = 300f },
				new TableRowElement { Cells = [cont, MakeCell()], HeightTwips = 500f },
			],
		};
		var layout = TableLayoutEngine.Layout(table, 9600f);

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

	private static TableCellElement MakeCell(int gridSpan = 1, VerticalMergeState verticalMerge = VerticalMergeState.None) => new()
	{
		Blocks = [],
		GridSpan = gridSpan,
		VerticalMerge = verticalMerge,
	};
}
