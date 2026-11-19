namespace PanoramicData.Render.Test;

using A = DocumentFormat.OpenXml.Drawing;
using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using Xunit;
using RenderTabStop = PanoramicData.Render.TabStop;

public sealed partial class RenderCommandEmitterTests
{
	[Fact]
	public void EmitPage_ParagraphBlock_EmitsSingleDrawTextCommand()
	{
		var section = new SectionInfo
		{
			MarginLeft = 1000,
			MarginRight = 1000,
			PageWidth = 12000
		};
		var paragraph = ParagraphBlockOf("Hello world");
		var page = new LayoutPage
		{
			Section = section,
			PageNumber = 1,
			ContentTopTwips = 1200,
			Blocks =
			[
				new LayoutBlock(paragraph, 400f)
			]
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
		var paragraph = ParagraphBlockOf("Configured");
		var page = TestLayoutPageFactory.Create([new LayoutBlock(paragraph, 300f)], 1, section: new SectionInfo { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 });
		var target = new FakeRenderTarget();
		var options = new RenderOptions { FallbackFontFamily = "Calibri" };

		RenderCommandEmitter.EmitPage(page, target, options);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Font.Family.Should().Be("Calibri");
	}

	[Fact]
	public void EmitPage_WrappedParagraphBlock_EmitsMultipleLines()
	{
		var paragraph = ParagraphBlockOf("Alpha Beta");
		var page = TestLayoutPageFactory.Create([new LayoutBlock(paragraph, 480f, LineHeights: [240f, 240f])], 1, section: new SectionInfo { MarginLeft = 100, MarginRight = 100, PageWidth = 900 });
		var target = Emit(page);

		target.DrawTextCalls.Should().HaveCount(2);
		target.DrawTextCalls[0].Text.Should().Be("Alpha");
		target.DrawTextCalls[0].BaselineYTwips.Should().Be(1192f);
		target.DrawTextCalls[1].Text.Should().Be("Beta");
		target.DrawTextCalls[1].BaselineYTwips.Should().Be(1432f);
	}

	[Fact]
	public void EmitPage_WrappedParagraphContinuation_UsesLineStartIndex()
	{
		var paragraph = ParagraphBlockOf("Alpha Beta");
		var page = TestLayoutPageFactory.Create([new LayoutBlock(paragraph, 240f, LineHeights: [240f], LineStartIndex: 1)], 1, section: new SectionInfo { MarginLeft = 100, MarginRight = 100, PageWidth = 900 });
		var target = Emit(page);

		AssertSingleText(target, "Beta");
		target.DrawTextCalls[0].BaselineYTwips.Should().Be(1192f);
	}

	[Fact]
	public void EmitPage_WrappedHighlightedRun_IncludesWhitespaceHighlightSegment()
	{
		var paragraph = new ParagraphBlock
		{
			SourceElement = new Paragraph(
				new Run(
					new RunProperties(new Highlight { Val = HighlightColorValues.Yellow }),
					new Text("Alpha Beta")))
		};
		var page = TestLayoutPageFactory.Create([new LayoutBlock(paragraph, 480f, LineHeights: [240f, 240f])], 1, section: new SectionInfo { MarginLeft = 100, MarginRight = 100, PageWidth = 1800 });
		var target = Emit(page);

		target.DrawTextCalls.Should().Contain(call => call.Text == " ");
		target.DrawRectCalls.Should().Contain(call => ((SolidRenderBrush)call.Fill!).Color == new RenderColor(255, 255, 0));
	}

	[Fact]
	public void EmitPage_AdjacentRunsWithSameFormatting_MergesIntoSingleDrawText()
	{
		var paragraph = new ParagraphBlock
		{
			SourceElement = new Paragraph(
				new Run(new Text("Hel")),
				new Run(new Text("lo")))
		};
		var page = TestLayoutPageFactory.Create([new LayoutBlock(paragraph, 300f)], 1, section: new SectionInfo { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 });
		var target = Emit(page);

		AssertSingleText(target, "Hello");
	}

	[Fact]
	public void EmitPage_RunsWithDifferentFormatting_EmitSeparateDrawTextCommands()
	{
		var paragraph = new ParagraphBlock
		{
			SourceElement = new Paragraph(
				new Run(new Text("A")),
				new Run(new RunProperties(new Bold()), new Text("B")))
		};
		var page = TestLayoutPageFactory.Create([new LayoutBlock(paragraph, 300f)], 1, section: new SectionInfo { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 });
		var target = Emit(page);

		target.DrawTextCalls.Should().HaveCount(2);
		target.DrawTextCalls[0].Text.Should().Be("A");
		target.DrawTextCalls[1].Text.Should().Be("B");
		target.DrawTextCalls[0].Font.IsBold.Should().BeFalse();
		target.DrawTextCalls[1].Font.IsBold.Should().BeTrue();
		target.DrawTextCalls[1].BaselineXTwips.Should().BeGreaterThan(target.DrawTextCalls[0].BaselineXTwips);
	}

	[Fact]
	public void EmitPage_HighlightedRun_EmitsBackgroundRect()
	{
		var paragraph = new ParagraphBlock
		{
			SourceElement = new Paragraph(
				new Run(
					new RunProperties(new Highlight { Val = HighlightColorValues.Yellow }),
					new Text("Highlighted")))
		};
		var page = TestLayoutPageFactory.Create([new LayoutBlock(paragraph, 300f)], 1, section: new SectionInfo { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 });
		var target = Emit(page);

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
		var page = TestLayoutPageFactory.Create([new LayoutBlock(paragraph, 300f)], 1, section: new SectionInfo { MarginLeft = 1000, MarginRight = 1000, PageWidth = 10000 });
		var target = new FakeRenderTarget();
		var options = new RenderOptions
		{
			NumberingStyles = new Dictionary<string, NumberingLevelStyle>
			{
				["1:0"] = new NumberingLevelStyle
				{
					LevelIndex = 0,
					Start = 1,
					NumberFormat = "decimal",
					LevelText = "%1",
					IndentLeftTwips = 576f,
					HangingTwips = 576f,
				},
			},
		};

		RenderCommandEmitter.EmitPage(page, target, options);

		target.DrawTextCalls.Should().HaveCount(2);
		target.DrawTextCalls[0].Text.Should().Be("1 ");
		target.DrawTextCalls[1].Text.Should().Be("Company information");
		target.DrawTextCalls[0].Font.Family.Should().Be("Aptos Display");
		target.DrawTextCalls[0].Font.SizePoints.Should().Be(14f);
		target.DrawTextCalls[0].Font.IsBold.Should().BeTrue();
		target.DrawTextCalls[1].BaselineXTwips.Should().Be(1907f);
	}

	[Fact]
	public void EmitPage_TablePlaceholderBlock_EmitsDrawRectCommand()
	{
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 900, MarginRight = 1100, PageWidth = 15000 },
			PageNumber = 1,
			ContentTopTwips = 1400,
			Blocks =
			[
				new LayoutBlock(new TablePlaceholderBlock
				{
					TableElement = new Table()
				}, 800f)
			]
		};
		var target = Emit(page);

		target.DrawRectCalls.Should().ContainSingle();
		target.DrawRectCalls[0].Rect.XTwips.Should().Be(900f);
		target.DrawRectCalls[0].Rect.YTwips.Should().Be(1400f);
		target.DrawRectCalls[0].Rect.WidthTwips.Should().Be(13000f);
		target.DrawRectCalls[0].Rect.HeightTwips.Should().Be(800f);
	}

	[Fact]
	public void EmitPage_TablePlaceholderBlock_RendersTableCellBackgroundBordersAndText()
	{
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 900, MarginRight = 1100, PageWidth = 15000 },
			PageNumber = 1,
			ContentTopTwips = 1400,
			Blocks =
			[
				new LayoutBlock(new TablePlaceholderBlock
				{
					TableElement = new Table(
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
								new Paragraph(new Run(new Text("Cell 1"))))))
				}, 240f)
			]
		};
		var target = Emit(page);

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
		var firstBlock = new LayoutBlock(new ParagraphBlock
		{
			SourceElement = new Paragraph(new Run(new Text("First")))
		}, 300f);
		var secondBlock = new LayoutBlock(new ParagraphBlock
		{
			SourceElement = new Paragraph(new Run(new Text("Second")))
		}, 300f);
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 1440, MarginRight = 1440, PageWidth = 12240 },
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

		target.DrawTextCalls.Should().HaveCount(2);
		target.DrawTextCalls[0].Text.Should().Be("First");
		target.DrawTextCalls[0].BaselineXTwips.Should().Be(1440f);
		target.DrawTextCalls[1].Text.Should().Be("Second");
		target.DrawTextCalls[1].BaselineXTwips.Should().Be(6480f);
	}

	[Fact]
	public void EmitDocument_MultiplePages_EmitsCommandsAcrossPages()
	{
		var p1 = TestLayoutPageFactory.Create([
				new LayoutBlock(new ParagraphBlock
				{
					SourceElement = new Paragraph(new Run(new Text("First")))
				}, 300f)
			], 1, section: new SectionInfo { MarginLeft = 720, MarginRight = 720, PageWidth = 12240 });
		var p2 = TestLayoutPageFactory.Create([
				new LayoutBlock(new ParagraphBlock
				{
					SourceElement = new Paragraph(new Run(new Text("Second")))
				}, 300f)
			], 2, section: new SectionInfo { MarginLeft = 720, MarginRight = 720, PageWidth = 12240 });
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitDocument([p1, p2], target);

		target.DrawTextCalls.Should().HaveCount(2);
		target.DrawTextCalls[0].Text.Should().Be("First");
		target.DrawTextCalls[1].Text.Should().Be("Second");
	}
}
