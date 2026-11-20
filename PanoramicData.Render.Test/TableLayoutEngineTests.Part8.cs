namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using static PanoramicData.Render.Test.NestedTableBuilders;
using static PanoramicData.Render.Test.TableLayoutBuilders;

public sealed partial class TableLayoutEngineTests
{
	[Fact]
	public void ComputeRowHeights_CellMarginsAffectRowHeight()
	{
		var margins = new CellMargins(100f, 0f, 100f, 0f);
		var cell = MakeCellWithParagraphs(1, margins);
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(4800f)],
			Rows = [new TableRowElement { Cells = [cell] }],
		};

		var heights = TableLayoutEngine.ComputeRowHeights(table);

		// 100 (top) + 240 (content) + 100 (bottom) = 440
		heights[0].Should().Be(440f);
	}

	[Fact]
	public void ComputeRowHeights_BorderSpacingAffectsRowHeight()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(4800f)],
			Rows = [new TableRowElement { Cells = [MakeCellWithParagraphs(1)] }],
			BorderSpacingTwips = 10f,
		};

		var heights = TableLayoutEngine.ComputeRowHeights(table);

		heights.Should().ContainSingle();
		heights[0].Should().Be(260f);
	}

	// ---- Table border rendering segments (4.5.5) ----

	[Fact]
	public void ComputeBorderSegments_NullLayout_ThrowsArgumentNullException()
	{
		var act = () => TableLayoutEngine.ComputeBorderSegments(null!);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("layout");
	}

	[Fact]
	public void ComputeBorderSegments_EmptyTable_ReturnsEmpty()
	{
		var layout = TableLayoutEngine.Layout(new TableElement { GridColumns = [], Rows = [] }, 9600f);

		var segments = TableLayoutEngine.ComputeBorderSegments(layout);

		segments.Should().BeEmpty();
	}

	[Fact]
	public void ComputeBorderSegments_SingleCell_ProducesOuterSegmentsWithStyleWidthColor()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f)],
			Rows = [new TableRowElement { Cells = [MakeCell()] }],
			Borders = new TableBorderSet(
				Top: new TableBorderDefinition(BorderStyle.Single, 8, "ff0000"),
				Bottom: new TableBorderDefinition(BorderStyle.Dashed, 6, "00ff00"),
				Left: new TableBorderDefinition(BorderStyle.Dotted, 4, "0000ff"),
				Right: new TableBorderDefinition(BorderStyle.Double, 10, "auto")),
		};

		var layout = TableLayoutEngine.Layout(table, 1000f);

		var segments = TableLayoutEngine.ComputeBorderSegments(layout);

		segments.Should().HaveCount(4);

		segments.Should().ContainSingle(s => IsHorizontalAt(s, BorderStyle.Single, 0f));
		segments.Should().ContainSingle(s => IsHorizontalAt(s, BorderStyle.Dashed, 240f));
		segments.Should().ContainSingle(s => IsVerticalAt(s, BorderStyle.Dotted, 0f));
		segments.Should().ContainSingle(s => IsVerticalAt(s, BorderStyle.Double, 1000f));

		var top = segments.Single(s => s.Style == BorderStyle.Single);
		top.WidthTwips.Should().Be(20f);
		top.ColorHex.Should().Be("FF0000");
		top.DashPatternTwips.Should().BeNull();

		var bottom = segments.Single(s => s.Style == BorderStyle.Dashed);
		bottom.WidthTwips.Should().Be(15f);
		bottom.ColorHex.Should().Be("00FF00");
		bottom.DashPatternTwips.Should().NotBeNull();

		var right = segments.Single(s => s.Style == BorderStyle.Double);
		right.ColorHex.Should().Be("000000");
	}

	[Fact]
	public void ComputeBorderSegments_InnerEdges_UseInsideBorders()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement { Cells = [MakeCell(), MakeCell()] },
				new TableRowElement { Cells = [MakeCell(), MakeCell()] },
			],
			Borders = OuterBorders(insideHorizontal: Line(BorderStyle.Dotted, 8, "222222"), insideVertical: Line(BorderStyle.Dashed, 6, "333333")),
		};

		var layout = TableLayoutEngine.Layout(table, 2000f);

		var segments = TableLayoutEngine.ComputeBorderSegments(layout);

		segments.Should().NotBeEmpty();

		segments.Should().ContainSingle(s => IsSegment(s, BorderStyle.Dashed, 1000f, 0f, 1000f, 240f));

		segments.Should().ContainSingle(s => IsSegment(s, BorderStyle.Dotted, 0f, 240f, 1000f, 240f));
	}

	[Fact]
	public void ComputeBorderSegments_RowAndCellPrecedence_OverridesInsideAndTableBorders()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement
				{
					Cells =
					[
						BorderedCell(new TableBorderSet(Right: new TableBorderDefinition(BorderStyle.Double, 10, "ABCDEF"))),
						MakeCell(),
					],
					Borders = new TableBorderSet(Bottom: new TableBorderDefinition(BorderStyle.Thick, 8, "123123")),
				},
			],
			Borders = OuterBorders(insideVertical: Line(BorderStyle.Dashed, 6, "222222")),
		};

		var layout = TableLayoutEngine.Layout(table, 2000f);

		var segments = TableLayoutEngine.ComputeBorderSegments(layout);

		segments.Should().ContainSingle(s =>
			s.Style == BorderStyle.Double
			&& s.ColorHex == "ABCDEF"
			&& s.X1 == 1000f
			&& s.X2 == 1000f);

		segments.Should().Contain(s =>
			s.Style == BorderStyle.Thick
			&& s.ColorHex == "123123"
			&& s.Y1 == 240f
			&& s.Y2 == 240f);
	}

	[Fact]
	public void ComputeBorderSegments_MergedCell_UsesMergedGeometryForSegments()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f), new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement
				{
					Cells =
					[
						new TableCellElement
						{
							Blocks = [],
							GridSpan = 2,
						},
					],
				},
				new TableRowElement { Cells = [MakeCell(), MakeCell()] },
			],
			Borders = OuterBorders(insideVertical: Line(BorderStyle.Dashed, 6, "333333")),
		};

		var layout = TableLayoutEngine.Layout(table, 2000f);

		var segments = TableLayoutEngine.ComputeBorderSegments(layout);

		segments.Should().ContainSingle(s =>
			s.Style == BorderStyle.Single
			&& s.Y1 == 0f
			&& s.Y2 == 0f
			&& s.X1 == 0f
			&& s.X2 == 2000f);

		segments.Should().NotContain(s =>
			s.X1 == 1000f
			&& s.X2 == 1000f
			&& s.Y1 == 0f
			&& s.Y2 == 240f);
	}

	[Fact]
	public void ComputeBorderSegments_DotDashAndDotDotDash_EmitExpectedDashPatterns()
	{
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(1000f)],
			Rows =
			[
				new TableRowElement
				{
					Cells =
					[
						new TableCellElement
						{
							Blocks = [],
							Borders = new TableBorderSet(
								Top: new TableBorderDefinition(BorderStyle.DotDash, 4, "AAAAAA"),
								Bottom: new TableBorderDefinition(BorderStyle.DotDotDash, 4, "BBBBBB")),
						},
					],
				},
			],
		};

		var layout = TableLayoutEngine.Layout(table, 1000f);

		var segments = TableLayoutEngine.ComputeBorderSegments(layout);

		var dotDash = segments.Single(s => s.Style == BorderStyle.DotDash);
		dotDash.DashPatternTwips.Should().Equal(7.5f, 5f, 2.5f, 5f);

		var dotDotDash = segments.Single(s => s.Style == BorderStyle.DotDotDash);
		dotDotDash.DashPatternTwips.Should().Equal(7.5f, 5f, 2.5f, 5f, 2.5f, 5f);
	}

	private static bool IsHorizontalAt(TableBorderSegment segment, BorderStyle style, float y)
		=> segment.Style == style && segment.Y1 == y && segment.Y2 == y;

	private static bool IsVerticalAt(TableBorderSegment segment, BorderStyle style, float x)
		=> segment.Style == style && segment.X1 == x && segment.X2 == x;

	private static bool IsSegment(TableBorderSegment segment, BorderStyle style, float x1, float y1, float x2, float y2)
		=> segment.Style == style && segment.X1 == x1 && segment.X2 == x2 && segment.Y1 == y1 && segment.Y2 == y2;

}
