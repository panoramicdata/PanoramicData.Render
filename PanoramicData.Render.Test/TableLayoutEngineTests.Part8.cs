namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

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
			Borders = new TableBorderSet(
				Top: new TableBorderDefinition(BorderStyle.Single, 4, "111111"),
				Bottom: new TableBorderDefinition(BorderStyle.Single, 4, "111111"),
				Left: new TableBorderDefinition(BorderStyle.Single, 4, "111111"),
				Right: new TableBorderDefinition(BorderStyle.Single, 4, "111111"),
				InsideHorizontal: new TableBorderDefinition(BorderStyle.Dotted, 8, "222222"),
				InsideVertical: new TableBorderDefinition(BorderStyle.Dashed, 6, "333333")),
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
						new TableCellElement
						{
							Blocks = [],
							Borders = new TableBorderSet(Right: new TableBorderDefinition(BorderStyle.Double, 10, "ABCDEF")),
						},
						MakeCell(),
					],
					Borders = new TableBorderSet(Bottom: new TableBorderDefinition(BorderStyle.Thick, 8, "123123")),
				},
			],
			Borders = new TableBorderSet(
				Top: new TableBorderDefinition(BorderStyle.Single, 4, "111111"),
				Bottom: new TableBorderDefinition(BorderStyle.Single, 4, "111111"),
				Left: new TableBorderDefinition(BorderStyle.Single, 4, "111111"),
				Right: new TableBorderDefinition(BorderStyle.Single, 4, "111111"),
				InsideVertical: new TableBorderDefinition(BorderStyle.Dashed, 6, "222222")),
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
			Borders = new TableBorderSet(
				Top: new TableBorderDefinition(BorderStyle.Single, 4, "111111"),
				Bottom: new TableBorderDefinition(BorderStyle.Single, 4, "111111"),
				Left: new TableBorderDefinition(BorderStyle.Single, 4, "111111"),
				Right: new TableBorderDefinition(BorderStyle.Single, 4, "111111"),
				InsideVertical: new TableBorderDefinition(BorderStyle.Dashed, 6, "333333")),
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

	// ---- Vertical alignment (4.2.5) ----

	[Fact]
	public void ComputeVerticalContentOffset_TopAlignment_ReturnsZero()
	{
		var offset = TableLayoutEngine.ComputeVerticalContentOffset(500f, 240f, CellVerticalAlignment.Top);

		offset.Should().Be(0f);
	}

	[Fact]
	public void ComputeVerticalContentOffset_CenterAlignment_CentersContent()
	{
		var offset = TableLayoutEngine.ComputeVerticalContentOffset(500f, 240f, CellVerticalAlignment.Center);

		// (500 - 240) / 2 = 130
		offset.Should().Be(130f);
	}

	[Fact]
	public void ComputeVerticalContentOffset_BottomAlignment_PushesContentDown()
	{
		var offset = TableLayoutEngine.ComputeVerticalContentOffset(500f, 240f, CellVerticalAlignment.Bottom);

		// 500 - 240 = 260
		offset.Should().Be(260f);
	}

	[Fact]
	public void ComputeVerticalContentOffset_ContentTallerThanCell_ReturnsZero()
	{
		// Center: (300 - 500) / 2 = -100 → clamped to 0
		var offset = TableLayoutEngine.ComputeVerticalContentOffset(300f, 500f, CellVerticalAlignment.Center);

		offset.Should().Be(0f);
	}

	[Fact]
	public void ComputeVerticalContentOffset_BottomWithContentTallerThanCell_ReturnsZero()
	{
		var offset = TableLayoutEngine.ComputeVerticalContentOffset(300f, 500f, CellVerticalAlignment.Bottom);

		offset.Should().Be(0f);
	}

	[Fact]
	public void ComputeVerticalContentOffset_CenterWithEqualHeights_ReturnsZero()
	{
		var offset = TableLayoutEngine.ComputeVerticalContentOffset(240f, 240f, CellVerticalAlignment.Center);

		offset.Should().Be(0f);
	}

	// ---- Fixed layout integration (4.2.6) ----

	[Fact]
	public void Layout_FullPipeline_CellPositionsWithMarginsAndContent()
	{
		var margins = new CellMargins(50f, 100f, 50f, 100f);
		var cell00 = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() }],
			Margins = margins,
		};
		var cell01 = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() },
					  new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() }],
			Margins = margins,
		};
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(3000f), new TableGridColumn(5000f)],
			Rows = [new TableRowElement { Cells = [cell00, cell01] }],
		};

		var layout = TableLayoutEngine.Layout(table, 9600f);
		var positions = TableLayoutEngine.ComputeCellPositions(layout);

		// cell00: 1 para + margins = 50 + 240 + 50 = 340
		// cell01: 2 para + margins = 50 + 480 + 50 = 580
		// Row height = max(340, 580) = 580
		positions.Should().HaveCount(2);
		positions[0].Height.Should().Be(580f);
		positions[1].Height.Should().Be(580f);

		// Content widths
		var cw0 = TableLayoutEngine.ComputeContentWidth(positions[0].Width, margins);
		cw0.Should().Be(2800f); // 3000 - 100 - 100

		var cw1 = TableLayoutEngine.ComputeContentWidth(positions[1].Width, margins);
		cw1.Should().Be(4800f); // 5000 - 100 - 100
	}

	[Fact]
	public void Layout_FullPipeline_VerticalAlignmentIntegration()
	{
		var cell = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() }],
			VerticalAlignment = CellVerticalAlignment.Center,
		};
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(4800f)],
			Rows = [new TableRowElement { Cells = [cell], HeightTwips = 600f }],
		};

		var layout = TableLayoutEngine.Layout(table, 9600f);
		var positions = TableLayoutEngine.ComputeCellPositions(layout);

		// Row height: max(600, 240) = 600. Content height: 240. Offset: (600-240)/2 = 180
		positions.Should().HaveCount(1);
		positions[0].Height.Should().Be(600f);

		var (_, contentHeight) = TableLayoutEngine.LayoutCellContent(cell);
		var vOffset = TableLayoutEngine.ComputeVerticalContentOffset(600f, contentHeight, cell.VerticalAlignment);
		vOffset.Should().Be(180f);
	}

	[Fact]
	public void Layout_FullPipeline_MultiRowWithMarginsAndAlignment()
	{
		var topMargins = new CellMargins(100f, 50f, 100f, 50f);
		var row0Cell = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() }],
			Margins = topMargins,
			VerticalAlignment = CellVerticalAlignment.Bottom,
		};
		var row1Cell = new TableCellElement
		{
			Blocks = [new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() },
					  new ParagraphBlock { SourceElement = new DocumentFormat.OpenXml.Wordprocessing.Paragraph() }],
		};
		var table = new TableElement
		{
			GridColumns = [new TableGridColumn(4800f)],
			Rows =
			[
				new TableRowElement { Cells = [row0Cell], HeightTwips = 600f },
				new TableRowElement { Cells = [row1Cell] },
			],
		};

		var layout = TableLayoutEngine.Layout(table, 9600f);
		var positions = TableLayoutEngine.ComputeCellPositions(layout);

		positions.Should().HaveCount(2);

		// Row 0: max(600, 100+240+100=440) = 600
		positions[0].Y.Should().Be(0f);
		positions[0].Height.Should().Be(600f);

		// Row 1: 2×240 = 480 (no margins), max(0, 480) = 480
		positions[1].Y.Should().Be(600f);
		positions[1].Height.Should().Be(480f);

		// Vertical offset for row0 cell (bottom alignment): 600 - 440 = 160
		var (_, ch) = TableLayoutEngine.LayoutCellContent(row0Cell);
		var vOff = TableLayoutEngine.ComputeVerticalContentOffset(600f, ch, row0Cell.VerticalAlignment);
		vOff.Should().Be(160f);
	}
	private static bool IsHorizontalAt(TableBorderSegment segment, BorderStyle style, float y)
		=> segment.Style == style && segment.Y1 == y && segment.Y2 == y;

	private static bool IsVerticalAt(TableBorderSegment segment, BorderStyle style, float x)
		=> segment.Style == style && segment.X1 == x && segment.X2 == x;

	private static bool IsSegment(TableBorderSegment segment, BorderStyle style, float x1, float y1, float x2, float y2)
		=> segment.Style == style && segment.X1 == x1 && segment.X2 == x2 && segment.Y1 == y1 && segment.Y2 == y2;

}
