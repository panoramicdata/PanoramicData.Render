namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using static PanoramicData.Render.Test.EmitterBuilders;
using static PanoramicData.Render.Test.EmitterBuilders2;

public sealed partial class RenderCommandEmitterTests
{
	[Fact]
	public void EmitPage_ParagraphBlock_EmitsSingleDrawTextCommand()
	{
		var page = new LayoutPage
		{
			Section = SectionOf(1000, 1000, 12000),
			PageNumber = 1,
			ContentTopTwips = 1200,
			Blocks = [new LayoutBlock(ParagraphBlockOf("Hello world"), 400f)]
		};
		var target = Emit(page);

		AssertSingleText(target, "Hello world");
		target.DrawTextCalls[0].BaselineXTwips.Should().Be(1000f);
		target.DrawTextCalls[0].BaselineYTwips.Should().Be(1392f);
		target.DrawTextCalls[0].Font.Family.Should().Be("Times New Roman");
	}

	[Fact]
	public void EmitPage_WithFallbackFontFamily_UsesConfiguredFont()
	{
		var page = TestLayoutPageFactory.Create([new LayoutBlock(ParagraphBlockOf("Configured"), 300f)], 1, section: SectionOf(500, 500, 10000));
		var target = Emit(page, new RenderOptions { FallbackFontFamily = "Calibri" });

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Font.Family.Should().Be("Calibri");
	}

	[Fact]
	public void EmitPage_WrappedParagraphBlock_EmitsMultipleLines()
	{
		var target = Emit(AlphaBetaPage(new LayoutBlock(ParagraphBlockOf("Alpha Beta"), 480f, LineHeights: [240f, 240f]), 900));

		target.DrawTextCalls.Should().HaveCount(2);
		target.DrawTextCalls[0].Text.Should().Be("Alpha");
		target.DrawTextCalls[0].BaselineYTwips.Should().Be(1192f);
		target.DrawTextCalls[1].Text.Should().Be("Beta");
		target.DrawTextCalls[1].BaselineYTwips.Should().Be(1432f);
	}

	[Fact]
	public void EmitPage_WrappedParagraphContinuation_UsesLineStartIndex()
	{
		var target = Emit(AlphaBetaPage(new LayoutBlock(ParagraphBlockOf("Alpha Beta"), 240f, LineHeights: [240f], LineStartIndex: 1), 900));

		AssertSingleText(target, "Beta");
		target.DrawTextCalls[0].BaselineYTwips.Should().Be(1192f);
	}

	[Fact]
	public void EmitPage_WrappedHighlightedRun_IncludesWhitespaceHighlightSegment()
	{
		var block = new LayoutBlock(ParagraphBlockOf(HighlightedParagraph("Alpha Beta")), 480f, LineHeights: [240f, 240f]);
		var target = Emit(AlphaBetaPage(block, 1800));

		target.DrawTextCalls.Should().Contain(call => call.Text == " ");
		target.DrawRectCalls.Should().Contain(call => ((SolidRenderBrush)call.Fill!).Color == new RenderColor(255, 255, 0));
	}

	[Fact]
	public void EmitPage_AdjacentRunsWithSameFormatting_MergesIntoSingleDrawText()
	{
		var target = EmitBlock(ParagraphBlockOf(new Paragraph(new Run(new Text("Hel")), new Run(new Text("lo")))));

		AssertSingleText(target, "Hello");
	}

	[Fact]
	public void EmitPage_RunsWithDifferentFormatting_EmitSeparateDrawTextCommands()
	{
		var target = EmitBlock(ParagraphBlockOf(new Paragraph(
			new Run(new Text("A")),
			new Run(new RunProperties(new Bold()), new Text("B")))));

		target.DrawTextCalls.Select(call => call.Text).Should().Equal("A", "B");
		target.DrawTextCalls[0].Font.IsBold.Should().BeFalse();
		target.DrawTextCalls[1].Font.IsBold.Should().BeTrue();
		target.DrawTextCalls[1].BaselineXTwips.Should().BeGreaterThan(target.DrawTextCalls[0].BaselineXTwips);
	}

	[Fact]
	public void EmitPage_HighlightedRun_EmitsBackgroundRect()
	{
		var target = EmitBlock(ParagraphBlockOf(HighlightedParagraph("Highlighted")));

		target.DrawRectCalls.Should().ContainSingle();
		target.DrawTextCalls.Should().ContainSingle();
		((SolidRenderBrush)target.DrawRectCalls[0].Fill!).Color.Should().Be(new RenderColor(255, 255, 0));
		target.DrawRectCalls[0].Rect.XTwips.Should().Be(target.DrawTextCalls[0].BaselineXTwips);
		target.DrawRectCalls[0].Rect.YTwips.Should().Be(1000f);
		target.DrawRectCalls[0].Rect.HeightTwips.Should().Be(240f);
	}

	[Fact]
	public void EmitPage_NumberedParagraph_UsesParagraphFontAndIndentationForLabelAndText()
	{
		var paragraph = new ParagraphBlock
		{
			SourceElement = new Paragraph(
				new Run(
					new RunProperties(new RunFonts { Ascii = "Aptos Display" }, new FontSize { Val = "28" }, new Bold()),
					new Text("Company information"))),
			NumberingId = 1,
			NumberingLevel = 0,
			Indentation = new ParagraphIndentation(Left: 907f, Hanging: 907f)
		};
		var page = TestLayoutPageFactory.Create([new LayoutBlock(paragraph, 300f)], 1, section: SectionOf(1000, 1000, 10000));
		var level = new NumberingLevelStyle
		{
			LevelIndex = 0,
			Start = 1,
			NumberFormat = "decimal",
			LevelText = "%1",
			IndentLeftTwips = 576f,
			HangingTwips = 576f,
		};
		var target = Emit(page, NumberingOptions(("1:0", level)));

		AssertTextsInOrder(target, "1 ", "Company information");
		target.DrawTextCalls.Should().HaveCount(2);
		target.DrawTextCalls[0].Font.Family.Should().Be("Aptos Display");
		target.DrawTextCalls[0].Font.SizePoints.Should().Be(14f);
		target.DrawTextCalls[0].Font.IsBold.Should().BeTrue();
		target.DrawTextCalls[1].BaselineXTwips.Should().Be(1907f);
	}

	[Fact]
	public void EmitPage_TablePlaceholderBlock_EmitsDrawRectCommand()
	{
		var target = Emit(TablePage(new Table(), 800f));

		target.DrawRectCalls.Should().ContainSingle();
		target.DrawRectCalls[0].Rect.XTwips.Should().Be(900f);
		target.DrawRectCalls[0].Rect.YTwips.Should().Be(1400f);
		target.DrawRectCalls[0].Rect.WidthTwips.Should().Be(13000f);
		target.DrawRectCalls[0].Rect.HeightTwips.Should().Be(800f);
	}

	[Fact]
	public void EmitPage_TablePlaceholderBlock_RendersTableCellBackgroundBordersAndText()
	{
		var table = new Table(
			new TableProperties(
				new TableBorders(
					new TopBorder { Val = BorderValues.Single, Size = 8, Color = "000000" },
					new BottomBorder { Val = BorderValues.Single, Size = 8, Color = "000000" },
					new LeftBorder { Val = BorderValues.Single, Size = 8, Color = "000000" },
					new RightBorder { Val = BorderValues.Single, Size = 8, Color = "000000" })),
			new TableGrid(new GridColumn { Width = "2400" }),
			new TableRow(
				new TableCell(
					new TableCellProperties(
						new Shading { Val = ShadingPatternValues.Clear, Fill = "FFFF00" }),
					new Paragraph(new Run(new Text("Cell 1"))))));
		var target = Emit(TablePage(table, 240f));

		target.DrawRectCalls.Should().ContainSingle();
		target.DrawRectCalls[0].Rect.Should().Be(new RenderRect(900f, 1400f, 2400f, 240f));
		target.DrawRectCalls[0].Fill.Should().BeOfType<SolidRenderBrush>();
		((SolidRenderBrush)target.DrawRectCalls[0].Fill!).Color.Should().Be(new RenderColor(255, 255, 0));
		target.DrawLineCalls.Should().HaveCount(4);
		target.DrawLineCalls.Should().Contain(call => call.From == new RenderPoint(900f, 1400f) && call.To == new RenderPoint(3300f, 1400f));
		target.DrawLineCalls.Should().Contain(call => call.From == new RenderPoint(900f, 1640f) && call.To == new RenderPoint(3300f, 1640f));
		AssertSingleText(target, "Cell 1");
		// Left margin is 108 twips (Word's built-in default). Cell starts at left margin 900,
		// so text baseline X = 900 + 108 = 1008.
		target.DrawTextCalls[0].BaselineXTwips.Should().Be(1008f);
		target.DrawTextCalls[0].BaselineYTwips.Should().Be(1592f);
	}

	[Fact]
	public void EmitPage_WithBlockPlacements_UsesPlacementCoordinates()
	{
		var firstBlock = new LayoutBlock(ParagraphBlockOf("First"), 300f);
		var secondBlock = new LayoutBlock(ParagraphBlockOf("Second"), 300f);
		var page = new LayoutPage
		{
			Section = SectionOf(1440, 1440, 12240),
			PageNumber = 1,
			ContentTopTwips = 1440,
			Blocks = [firstBlock, secondBlock],
			BlockPlacements =
			[
				new LayoutBlockPlacement(firstBlock, 1440f, 1440f, 4320f, 0),
				new LayoutBlockPlacement(secondBlock, 6480f, 1440f, 4320f, 1)
			]
		};
		var target = Emit(page);

		target.DrawTextCalls.Select(call => call.Text).Should().Equal("First", "Second");
		target.DrawTextCalls[0].BaselineXTwips.Should().Be(1440f);
		target.DrawTextCalls[1].BaselineXTwips.Should().Be(6480f);
	}

	[Fact]
	public void EmitDocument_MultiplePages_EmitsCommandsAcrossPages()
	{
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitDocument(
			[
				TestLayoutPageFactory.Create([new LayoutBlock(ParagraphBlockOf("First"), 300f)], 1, section: SectionOf(720, 720, 12240)),
				TestLayoutPageFactory.Create([new LayoutBlock(ParagraphBlockOf("Second"), 300f)], 2, section: SectionOf(720, 720, 12240))
			],
			target);

		target.DrawTextCalls.Select(call => call.Text).Should().Equal("First", "Second");
	}

	private static SectionInfo SectionOf(int marginLeft, int marginRight, int pageWidth)
		=> new() { MarginLeft = marginLeft, MarginRight = marginRight, PageWidth = pageWidth };

	private static LayoutPage AlphaBetaPage(LayoutBlock block, int pageWidth)
		=> TestLayoutPageFactory.Create([block], 1, section: SectionOf(100, 100, pageWidth));

	private static Paragraph HighlightedParagraph(string text)
		=> new(new Run(new RunProperties(new Highlight { Val = HighlightColorValues.Yellow }), new Text(text)));

	private static LayoutPage TablePage(Table table, float height)
		=> new()
		{
			Section = SectionOf(900, 1100, 15000),
			PageNumber = 1,
			ContentTopTwips = 1400,
			Blocks = [new LayoutBlock(new TablePlaceholderBlock { TableElement = table }, height)]
		};
}
