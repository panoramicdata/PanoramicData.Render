namespace PanoramicData.Render.Test;

using A = DocumentFormat.OpenXml.Drawing;
using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using Xunit;
using RenderTabStop = PanoramicData.Render.TabStop;

public sealed class RenderCommandEmitterTests
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
		var paragraph = new ParagraphBlock
		{
			SourceElement = new Paragraph(new Run(new Text("Hello world")))
		};
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
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be("Hello world");
		target.DrawTextCalls[0].BaselineXTwips.Should().Be(1000f);
		target.DrawTextCalls[0].BaselineYTwips.Should().Be(1440f);
		target.DrawTextCalls[0].Font.Family.Should().Be("Times New Roman");
	}

	[Fact]
	public void EmitPage_WithFallbackFontFamily_UsesConfiguredFont()
	{
		var paragraph = new ParagraphBlock
		{
			SourceElement = new Paragraph(new Run(new Text("Configured")))
		};
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(paragraph, 300f)]
		};
		var target = new FakeRenderTarget();
		var options = new RenderOptions { FallbackFontFamily = "Calibri" };

		RenderCommandEmitter.EmitPage(page, target, options);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Font.Family.Should().Be("Calibri");
	}

	[Fact]
	public void EmitPage_WrappedParagraphBlock_EmitsMultipleLines()
	{
		var paragraph = new ParagraphBlock
		{
			SourceElement = new Paragraph(new Run(new Text("Alpha Beta")))
		};
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 100, MarginRight = 100, PageWidth = 900 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(paragraph, 480f, LineHeights: [240f, 240f])]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().HaveCount(2);
		target.DrawTextCalls[0].Text.Should().Be("Alpha");
		target.DrawTextCalls[0].BaselineYTwips.Should().Be(1240f);
		target.DrawTextCalls[1].Text.Should().Be("Beta");
		target.DrawTextCalls[1].BaselineYTwips.Should().Be(1480f);
	}

	[Fact]
	public void EmitPage_WrappedParagraphContinuation_UsesLineStartIndex()
	{
		var paragraph = new ParagraphBlock
		{
			SourceElement = new Paragraph(new Run(new Text("Alpha Beta")))
		};
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 100, MarginRight = 100, PageWidth = 900 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(paragraph, 240f, LineHeights: [240f], LineStartIndex: 1)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be("Beta");
		target.DrawTextCalls[0].BaselineYTwips.Should().Be(1240f);
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
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(paragraph, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be("Hello");
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
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(paragraph, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().HaveCount(2);
		target.DrawTextCalls[0].Text.Should().Be("A");
		target.DrawTextCalls[1].Text.Should().Be("B");
		target.DrawTextCalls[0].Font.IsBold.Should().BeFalse();
		target.DrawTextCalls[1].Font.IsBold.Should().BeTrue();
		target.DrawTextCalls[1].BaselineXTwips.Should().BeGreaterThan(target.DrawTextCalls[0].BaselineXTwips);
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
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

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
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawRectCalls.Should().ContainSingle();
		target.DrawRectCalls[0].Rect.Should().Be(new RenderRect(900f, 1400f, 2400f, 240f));
		target.DrawRectCalls[0].Fill.Should().BeOfType<SolidRenderBrush>();
		((SolidRenderBrush)target.DrawRectCalls[0].Fill!).Color.Should().Be(new RenderColor(255, 255, 0));
		target.DrawLineCalls.Should().HaveCount(4);
		target.DrawLineCalls.Should().Contain(call => call.From == new RenderPoint(900f, 1400f) && call.To == new RenderPoint(3300f, 1400f));
		target.DrawLineCalls.Should().Contain(call => call.From == new RenderPoint(900f, 1640f) && call.To == new RenderPoint(3300f, 1640f));
		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be("Cell 1");
		target.DrawTextCalls[0].BaselineXTwips.Should().Be(900f);
		target.DrawTextCalls[0].BaselineYTwips.Should().Be(1640f);
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
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().HaveCount(2);
		target.DrawTextCalls[0].Text.Should().Be("First");
		target.DrawTextCalls[0].BaselineXTwips.Should().Be(1440f);
		target.DrawTextCalls[1].Text.Should().Be("Second");
		target.DrawTextCalls[1].BaselineXTwips.Should().Be(6480f);
	}

	[Fact]
	public void EmitDocument_MultiplePages_EmitsCommandsAcrossPages()
	{
		var p1 = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 720, MarginRight = 720, PageWidth = 12240 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks =
			[
				new LayoutBlock(new ParagraphBlock
				{
					SourceElement = new Paragraph(new Run(new Text("First")))
				}, 300f)
			]
		};
		var p2 = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 720, MarginRight = 720, PageWidth = 12240 },
			PageNumber = 2,
			ContentTopTwips = 1000,
			Blocks =
			[
				new LayoutBlock(new ParagraphBlock
				{
					SourceElement = new Paragraph(new Run(new Text("Second")))
				}, 300f)
			]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitDocument([p1, p2], target);

		target.DrawTextCalls.Should().HaveCount(2);
		target.DrawTextCalls[0].Text.Should().Be("First");
		target.DrawTextCalls[1].Text.Should().Be("Second");
	}

	[Fact]
	public void EmitPage_ComplexPageField_RendersComputedCurrentPage()
	{
		var paragraph = new Paragraph(
			new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
			new Run(new FieldCode(" PAGE ")),
			new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
			new Run(new Text("999")),
			new Run(new FieldChar { FieldCharType = FieldCharValues.End }));
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 },
			PageNumber = 7,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target, totalPageCount: 13, renderTimestampUtc: new DateTime(2026, 4, 10, 11, 30, 0, DateTimeKind.Utc));

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be("7");
	}

	[Fact]
	public void EmitPage_ComplexNumPagesField_RendersProvidedTotalPages()
	{
		var paragraph = new Paragraph(
			new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
			new Run(new FieldCode(" NUMPAGES ")),
			new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
			new Run(new Text("1")),
			new Run(new FieldChar { FieldCharType = FieldCharValues.End }));
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target, totalPageCount: 42, renderTimestampUtc: new DateTime(2026, 4, 10, 11, 30, 0, DateTimeKind.Utc));

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be("42");
	}

	[Fact]
	public void EmitPage_SimpleDateField_RendersUsingTimestamp()
	{
		var paragraph = new Paragraph(
			new SimpleField(new Run(new Text("stale")))
			{
				Instruction = " DATE "
			});
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();
		var timestamp = new DateTime(2026, 4, 10, 11, 30, 0, DateTimeKind.Utc);

		RenderCommandEmitter.EmitPage(page, target, renderTimestampUtc: timestamp);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be(timestamp.ToString("d", System.Globalization.CultureInfo.InvariantCulture));
	}

	[Fact]
	public void EmitPage_SimpleTocField_RendersCachedResultText()
	{
		var paragraph = new Paragraph(
			new SimpleField(new Run(new Text("Heading 1........1")))
			{
				Instruction = " TOC "
			});
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be("Heading 1........1");
	}

	[Fact]
	public void EmitPage_ComplexHyperlinkField_EmitsHyperlinkRegion()
	{
		var paragraph = new Paragraph(
			new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
			new Run(new FieldCode(" HYPERLINK \"https://example.com\" ")),
			new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
			new Run(new Text("Click me")),
			new Run(new FieldChar { FieldCharType = FieldCharValues.End }));
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be("Click me");
		target.HyperlinkCalls.Should().ContainSingle();
		target.HyperlinkCalls[0].Uri.Should().Be("https://example.com");
		target.HyperlinkCalls[0].Rect.WidthTwips.Should().BeGreaterThan(0f);
	}

	[Fact]
	public void EmitPage_SimpleHyperlinkField_EmitsHyperlinkRegion()
	{
		var paragraph = new Paragraph(
			new SimpleField(new Run(new Text("Open")))
			{
				Instruction = " HYPERLINK \"https://contoso.test\" "
			});
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be("Open");
		target.HyperlinkCalls.Should().ContainSingle();
		target.HyperlinkCalls[0].Uri.Should().Be("https://contoso.test");
	}

	[Fact]
	public void EmitPage_HyperlinkElementWithAnchor_EmitsHyperlinkRegion()
	{
		var hyperlink = new Hyperlink(new Run(new Text("Go to section")))
		{
			Anchor = "myBookmark"
		};
		var paragraph = new Paragraph(hyperlink);
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be("Go to section");
		target.HyperlinkCalls.Should().ContainSingle();
		target.HyperlinkCalls[0].Uri.Should().Be("#myBookmark");
		target.HyperlinkCalls[0].Rect.WidthTwips.Should().BeGreaterThan(0f);
	}

	[Fact]
	public void EmitPage_HyperlinkElementWithoutAnchorOrId_EmitsTextWithNoHyperlink()
	{
		var hyperlink = new Hyperlink(new Run(new Text("Orphaned link")));
		var paragraph = new Paragraph(hyperlink);
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be("Orphaned link");
		target.HyperlinkCalls.Should().BeEmpty();
	}

	[Fact]
	public void EmitPage_ParagraphWithBookmarkStarts_EmitsNamedDestinations()
	{
		var paragraph = new Paragraph(new Run(new Text("Chapter 1")));
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock
			{
				SourceElement = paragraph,
				BookmarkStarts = [new BookmarkStartInfo(1, "_Toc123456")]
			}, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.NamedDestinationCalls.Should().ContainSingle();
		target.NamedDestinationCalls[0].Name.Should().Be("_Toc123456");
		target.NamedDestinationCalls[0].XTwips.Should().Be(500f);
		target.NamedDestinationCalls[0].YTwips.Should().Be(1000f);
	}

	[Fact]
	public void EmitPage_ParagraphWithMultipleBookmarks_EmitsAllNamedDestinations()
	{
		var paragraph = new Paragraph(new Run(new Text("Section")));
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock
			{
				SourceElement = paragraph,
				BookmarkStarts =
				[
					new BookmarkStartInfo(1, "first"),
					new BookmarkStartInfo(2, "second")
				]
			}, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.NamedDestinationCalls.Should().HaveCount(2);
		target.NamedDestinationCalls[0].Name.Should().Be("first");
		target.NamedDestinationCalls[1].Name.Should().Be("second");
	}

	[Fact]
	public void EmitPage_ParagraphWithNoBookmarks_EmitsNoNamedDestinations()
	{
		var paragraph = new Paragraph(new Run(new Text("Plain text")));
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.NamedDestinationCalls.Should().BeEmpty();
	}

	[Fact]
	public void EmitPage_SimpleRefField_RendersCachedResultText()
	{
		var paragraph = new Paragraph(
			new SimpleField(new Run(new Text("Section 2")))
			{
				Instruction = " REF myBookmark "
			});
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be("Section 2");
	}

	[Fact]
	public void EmitPage_SimpleMergeField_RendersCachedResultText()
	{
		var paragraph = new Paragraph(
			new SimpleField(new Run(new Text("Jane Doe")))
			{
				Instruction = " MERGEFIELD CustomerName "
			});
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be("Jane Doe");
	}

	[Fact]
	public void EmitDocument_NumberedParagraphs_EmitsIncrementingListLabels()
	{
		LayoutPage CreatePage(int pageNumber, string text)
		{
			return new LayoutPage
			{
				Section = new SectionInfo { MarginLeft = 720, MarginRight = 720, PageWidth = 12240 },
				PageNumber = pageNumber,
				ContentTopTwips = 1000,
				Blocks =
				[
					new LayoutBlock(new ParagraphBlock
					{
						SourceElement = new Paragraph(new Run(new Text(text))),
						NumberingId = 1,
						NumberingLevel = 0
					}, 300f)
				]
			};
		}

		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitDocument([CreatePage(1, "First"), CreatePage(2, "Second"), CreatePage(3, "Third")], target);

		target.DrawTextCalls.Select(call => call.Text).Should().ContainInOrder("1. ", "First", "2. ", "Second", "3. ", "Third");
	}

	[Fact]
	public void EmitPage_MultiLevelNumbering_EmitsPatternedLabels()
	{
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 720, MarginRight = 720, PageWidth = 12240 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks =
			[
				new LayoutBlock(new ParagraphBlock
				{
					SourceElement = new Paragraph(new Run(new Text("Top"))),
					NumberingId = 1,
					NumberingLevel = 0
				}, 300f),
				new LayoutBlock(new ParagraphBlock
				{
					SourceElement = new Paragraph(new Run(new Text("Nested"))),
					NumberingId = 1,
					NumberingLevel = 1
				}, 300f)
			]
		};
		var options = new RenderOptions
		{
			NumberingStyles =
			{
				["1:0"] = new NumberingLevelStyle { LevelIndex = 0, Start = 1, NumberFormat = "decimal", LevelText = "%1." },
				["1:1"] = new NumberingLevelStyle { LevelIndex = 1, Start = 1, NumberFormat = "decimal", LevelText = "%1.%2." }
			}
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target, options);

		target.DrawTextCalls.Select(call => call.Text).Should().ContainInOrder("1. ", "Top", "1.1. ", "Nested");
	}

	[Fact]
	public void EmitPage_NestedListLabel_IsPositionedLeftOfBodyText()
	{
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 720, MarginRight = 720, PageWidth = 12240 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks =
			[
				new LayoutBlock(new ParagraphBlock
				{
					SourceElement = new Paragraph(new Run(new Text("Nested text"))),
					NumberingId = 1,
					NumberingLevel = 1
				}, 300f)
			]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().HaveCount(2);
		var labelCall = target.DrawTextCalls[0];
		var bodyCall = target.DrawTextCalls[1];
		labelCall.Text.Should().EndWith(" ");
		labelCall.BaselineXTwips.Should().BeLessThan(bodyCall.BaselineXTwips);
	}

	[Fact]
	public void EmitDocument_RestartRule_RestartsNestedLevelAfterParentIncrement()
	{
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 720, MarginRight = 720, PageWidth = 12240 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks =
			[
				new LayoutBlock(new ParagraphBlock { SourceElement = new Paragraph(new Run(new Text("P1"))), NumberingId = 1, NumberingLevel = 0 }, 300f),
				new LayoutBlock(new ParagraphBlock { SourceElement = new Paragraph(new Run(new Text("C1"))), NumberingId = 1, NumberingLevel = 1 }, 300f),
				new LayoutBlock(new ParagraphBlock { SourceElement = new Paragraph(new Run(new Text("C2"))), NumberingId = 1, NumberingLevel = 1 }, 300f),
				new LayoutBlock(new ParagraphBlock { SourceElement = new Paragraph(new Run(new Text("P2"))), NumberingId = 1, NumberingLevel = 0 }, 300f),
				new LayoutBlock(new ParagraphBlock { SourceElement = new Paragraph(new Run(new Text("C3"))), NumberingId = 1, NumberingLevel = 1 }, 300f)
			]
		};
		var options = new RenderOptions
		{
			NumberingStyles =
			{
				["1:0"] = new NumberingLevelStyle { LevelIndex = 0, Start = 1, NumberFormat = "decimal", LevelText = "%1." },
				["1:1"] = new NumberingLevelStyle { LevelIndex = 1, Start = 1, NumberFormat = "decimal", LevelText = "%1.%2.", RestartAfterLevel = 1 }
			}
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitDocument([page], target, options);

		target.DrawTextCalls.Select(call => call.Text).Should().ContainInOrder(
			"1. ", "P1",
			"1.1. ", "C1",
			"1.2. ", "C2",
			"2. ", "P2",
			"2.1. ", "C3");
	}

	[Fact]
	public void EmitPage_BulletStyle_UsesConfiguredBulletFontFamily()
	{
		var page = new LayoutPage
		{
			Section = new SectionInfo { MarginLeft = 720, MarginRight = 720, PageWidth = 12240 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks =
			[
				new LayoutBlock(new ParagraphBlock
				{
					SourceElement = new Paragraph(new Run(new Text("Bullet item"))),
					NumberingId = 5,
					NumberingLevel = 0
				}, 300f)
			]
		};
		var options = new RenderOptions
		{
			NumberingStyles =
			{
				["5:0"] = new NumberingLevelStyle { LevelIndex = 0, Start = 1, NumberFormat = "bullet", LevelText = "%1", FontFamily = "Symbol" }
			}
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target, options);

		target.DrawTextCalls.Should().HaveCount(2);
		target.DrawTextCalls[0].Text.Should().Be("• ");
		target.DrawTextCalls[0].Font.Family.Should().Be("Symbol");
	}

	[Fact]
	public void EmitPage_WithTextWatermark_DrawsRotatedTextAtCenter()
	{
		var paragraph = new Paragraph(new Run(new Text("Body text")));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)],
			Watermark = new WatermarkInfo
			{
				Kind = WatermarkKind.Text,
				Text = "DRAFT",
				FontFamily = "Calibri",
				FillColor = "silver",
				Opacity = 0.5f,
				RotationDegrees = 315f,
				WidthTwips = 10557f,
				HeightTwips = 2639f,
				IsHorizontallyCentered = true,
				IsVerticallyCentered = true
			}
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.RotatedTextCalls.Should().ContainSingle();
		target.RotatedTextCalls[0].Text.Should().Be("DRAFT");
		target.RotatedTextCalls[0].CenterXTwips.Should().Be(12240f / 2f);
		target.RotatedTextCalls[0].CenterYTwips.Should().Be(15840f / 2f);
		target.RotatedTextCalls[0].RotationDegrees.Should().Be(315f);
		target.RotatedTextCalls[0].Font.Family.Should().Be("Calibri");
		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be("Body text");
	}

	[Fact]
	public void EmitPage_NoWatermark_DoesNotDrawRotatedText()
	{
		var paragraph = new Paragraph(new Run(new Text("Body text")));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.RotatedTextCalls.Should().BeEmpty();
	}

	[Fact]
	public void EmitPage_WithImageWatermark_DrawsRotatedImageAtCenter()
	{
		var paragraph = new Paragraph(new Run(new Text("Body text")));
		var imageData = new ImageData([0x89, 0x50, 0x4E, 0x47], "image/png");
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)],
			Watermark = new WatermarkInfo
			{
				Kind = WatermarkKind.Image,
				ResolvedImageData = imageData,
				Opacity = 0.3f,
				RotationDegrees = 0f,
				WidthTwips = 7200f,
				HeightTwips = 5400f,
				IsHorizontallyCentered = true,
				IsVerticallyCentered = true
			}
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.RotatedImageCalls.Should().ContainSingle();
		target.RotatedImageCalls[0].Image.Should().BeSameAs(imageData);
		target.RotatedImageCalls[0].CenterXTwips.Should().Be(12240f / 2f);
		target.RotatedImageCalls[0].CenterYTwips.Should().Be(15840f / 2f);
		target.RotatedImageCalls[0].WidthTwips.Should().Be(7200f);
		target.RotatedImageCalls[0].HeightTwips.Should().Be(5400f);
		target.RotatedImageCalls[0].Opacity.Should().Be(0.3f);
		target.RotatedTextCalls.Should().BeEmpty();
	}

	[Fact]
	public void EmitPage_ImageWatermarkWithNoResolvedData_DoesNotDraw()
	{
		var paragraph = new Paragraph(new Run(new Text("Body text")));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)],
			Watermark = new WatermarkInfo
			{
				Kind = WatermarkKind.Image,
				ImageRelationshipId = "rId1",
				WidthTwips = 7200f,
				HeightTwips = 5400f
			}
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.RotatedImageCalls.Should().BeEmpty();
		target.RotatedTextCalls.Should().BeEmpty();
	}

	[Fact]
	public void EmitPage_TextWatermark_RenderedBeforeBodyContent()
	{
		var paragraph = new Paragraph(new Run(new Text("Body")));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)],
			Watermark = new WatermarkInfo
			{
				Kind = WatermarkKind.Text,
				Text = "DRAFT",
				WidthTwips = 5000f
			}
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.CallOrder.Count.Should().BeGreaterThanOrEqualTo(2);
		target.CallOrder[0].Should().StartWith("DrawRotatedText:");
		target.CallOrder[1].Should().StartWith("DrawText:");
	}

	[Fact]
	public void EmitPage_ImageWatermark_RenderedBeforeBodyContent()
	{
		var paragraph = new Paragraph(new Run(new Text("Body")));
		var imageData = new ImageData([0x89, 0x50, 0x4E, 0x47], "image/png");
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)],
			Watermark = new WatermarkInfo
			{
				Kind = WatermarkKind.Image,
				ResolvedImageData = imageData,
				WidthTwips = 7200f,
				HeightTwips = 5400f
			}
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.CallOrder.Count.Should().BeGreaterThanOrEqualTo(2);
		target.CallOrder[0].Should().Be("DrawRotatedImage");
		target.CallOrder[1].Should().StartWith("DrawText:");
	}

	[Fact]
	public void EmitPage_ParagraphWithBarTab_DrawsVerticalLine()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(
				new Tabs(
					new DocumentFormat.OpenXml.Wordprocessing.TabStop { Val = TabStopValues.Bar, Position = 2880 }
				)),
			new Run(new Text("Body text")));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawLineCalls.Should().ContainSingle();
		target.DrawLineCalls[0].From.XTwips.Should().Be(720f + 2880f);
		target.DrawLineCalls[0].From.YTwips.Should().Be(1000f);
		target.DrawLineCalls[0].To.XTwips.Should().Be(720f + 2880f);
		target.DrawLineCalls[0].To.YTwips.Should().Be(1300f);
	}

	[Fact]
	public void EmitPage_ParagraphWithoutBarTab_DrawsNoLine()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(
				new Tabs(
					new DocumentFormat.OpenXml.Wordprocessing.TabStop { Val = TabStopValues.Left, Position = 2880 }
				)),
			new Run(new Text("Body text")));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawLineCalls.Should().BeEmpty();
	}

	[Fact]
	public void EmitPage_DecimalTabStop_AlignsOnDecimalPoint()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(
				new Tabs(
					new DocumentFormat.OpenXml.Wordprocessing.TabStop { Val = TabStopValues.Decimal, Position = 4320 }
				)),
			new Run(new Text("Label") { Space = SpaceProcessingModeValues.Preserve }),
			new Run(new TabChar()),
			new Run(new Text("12.50") { Space = SpaceProcessingModeValues.Preserve }));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		// Should have two text draws: "Label" and "12.50"
		target.DrawTextCalls.Should().HaveCount(2);
		target.DrawTextCalls[0].Text.Should().Be("Label");
		target.DrawTextCalls[1].Text.Should().Be("12.50");

		// The "12.50" text should be positioned so that the "." aligns at the tab stop (4320 twips from margin)
		// "12" before decimal = 2 chars * 12 * 10 = 240 twips
		// Position = 720 (margin) + 4320 (tab) - 240 (before decimal) = 4800
		var expectedX = 720f + TabStopResolver.ComputeContentStart(
			new RenderTabStop(4320f, TabStopType.Decimal),
			0f,
			EstimateWidth("12", 12f));
		target.DrawTextCalls[1].BaselineXTwips.Should().Be(expectedX);
	}

	[Fact]
	public void EmitPage_DecimalTabStop_NoDecimalInText_AlignsFullWidth()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(
				new Tabs(
					new DocumentFormat.OpenXml.Wordprocessing.TabStop { Val = TabStopValues.Decimal, Position = 4320 }
				)),
			new Run(new TabChar()),
			new Run(new Text("1234") { Space = SpaceProcessingModeValues.Preserve }));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be("1234");

		// No decimal point — entire text width treated as widthBeforeDecimal
		var expectedX = 720f + TabStopResolver.ComputeContentStart(
			new RenderTabStop(4320f, TabStopType.Decimal),
			0f,
			EstimateWidth("1234", 12f));
		target.DrawTextCalls[0].BaselineXTwips.Should().Be(expectedX);
	}

	[Fact]
	public void EmitPage_RightTabStop_AlignsTextRight()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(
				new Tabs(
					new DocumentFormat.OpenXml.Wordprocessing.TabStop { Val = TabStopValues.Right, Position = 9360 }
				)),
			new Run(new TabChar()),
			new Run(new Text("Right aligned") { Space = SpaceProcessingModeValues.Preserve }));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		var contentWidth = EstimateWidth("Right aligned", 12f);
		var expectedX = 720f + TabStopResolver.ComputeContentStart(
			new RenderTabStop(9360f, TabStopType.Right),
			contentWidth);
		target.DrawTextCalls[0].BaselineXTwips.Should().Be(expectedX);
	}

	[Fact]
	public void EmitPage_CenterTabStop_CentersText()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(
				new Tabs(
					new DocumentFormat.OpenXml.Wordprocessing.TabStop { Val = TabStopValues.Center, Position = 4320 }
				)),
			new Run(new TabChar()),
			new Run(new Text("Centered") { Space = SpaceProcessingModeValues.Preserve }));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		var contentWidth = EstimateWidth("Centered", 12f);
		var expectedX = 720f + TabStopResolver.ComputeContentStart(
			new RenderTabStop(4320f, TabStopType.Center),
			contentWidth);
		target.DrawTextCalls[0].BaselineXTwips.Should().Be(expectedX);
	}

	[Fact]
	public void EmitPage_LeftTabStop_PositionsAtTabStop()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(
				new Tabs(
					new DocumentFormat.OpenXml.Wordprocessing.TabStop { Val = TabStopValues.Left, Position = 2880 }
				)),
			new Run(new Text("Before") { Space = SpaceProcessingModeValues.Preserve }),
			new Run(new TabChar()),
			new Run(new Text("After") { Space = SpaceProcessingModeValues.Preserve }));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().HaveCount(2);
		target.DrawTextCalls[0].Text.Should().Be("Before");
		target.DrawTextCalls[1].Text.Should().Be("After");
		target.DrawTextCalls[1].BaselineXTwips.Should().Be(720f + 2880f);
	}

	[Fact]
	public void EmitPage_DotLeader_DrawsDotsBeforeTabStop()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(
				new Tabs(
					new DocumentFormat.OpenXml.Wordprocessing.TabStop
					{
						Val = TabStopValues.Right,
						Position = 9360,
						Leader = TabStopLeaderCharValues.Dot
					}
				)),
			new Run(new Text("Item") { Space = SpaceProcessingModeValues.Preserve }),
			new Run(new TabChar()),
			new Run(new Text("100") { Space = SpaceProcessingModeValues.Preserve }));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		// Should have "Item", multiple "." leader characters, and "100"
		target.DrawTextCalls.Count.Should().BeGreaterThan(3);
		target.DrawTextCalls[0].Text.Should().Be("Item");
		target.DrawTextCalls[^1].Text.Should().Be("100");
		target.DrawTextCalls.Where(c => c.Text == ".").Should().NotBeEmpty();
	}

	[Fact]
	public void EmitPage_HyphenLeader_DrawsHyphensBeforeTabStop()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(
				new Tabs(
					new DocumentFormat.OpenXml.Wordprocessing.TabStop
					{
						Val = TabStopValues.Right,
						Position = 9360,
						Leader = TabStopLeaderCharValues.Hyphen
					}
				)),
			new Run(new TabChar()),
			new Run(new Text("End") { Space = SpaceProcessingModeValues.Preserve }));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Where(c => c.Text == "-").Should().NotBeEmpty();
	}

	[Fact]
	public void EmitPage_UnderscoreLeader_DrawsUnderscoresBeforeTabStop()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(
				new Tabs(
					new DocumentFormat.OpenXml.Wordprocessing.TabStop
					{
						Val = TabStopValues.Left,
						Position = 4320,
						Leader = TabStopLeaderCharValues.Underscore
					}
				)),
			new Run(new TabChar()),
			new Run(new Text("Value") { Space = SpaceProcessingModeValues.Preserve }));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Where(c => c.Text == "_").Should().NotBeEmpty();
	}

	[Fact]
	public void EmitPage_MiddleDotLeader_DrawsMiddleDotsBeforeTabStop()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(
				new Tabs(
					new DocumentFormat.OpenXml.Wordprocessing.TabStop
					{
						Val = TabStopValues.Left,
						Position = 4320,
						Leader = TabStopLeaderCharValues.MiddleDot
					}
				)),
			new Run(new TabChar()),
			new Run(new Text("Price") { Space = SpaceProcessingModeValues.Preserve }));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Where(c => c.Text == "\u00B7").Should().NotBeEmpty();
	}

	[Fact]
	public void EmitPage_NoLeader_DoesNotDrawLeaderCharacters()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(
				new Tabs(
					new DocumentFormat.OpenXml.Wordprocessing.TabStop
					{
						Val = TabStopValues.Left,
						Position = 4320
						// No Leader specified — defaults to None
					}
				)),
			new Run(new Text("A") { Space = SpaceProcessingModeValues.Preserve }),
			new Run(new TabChar()),
			new Run(new Text("B") { Space = SpaceProcessingModeValues.Preserve }));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		// Only "A" and "B" should be drawn — no leader characters
		target.DrawTextCalls.Should().HaveCount(2);
		target.DrawTextCalls[0].Text.Should().Be("A");
		target.DrawTextCalls[1].Text.Should().Be("B");
	}

	[Fact]
	public void EmitPage_HeaderWithRightTab_DrawsHeaderTextAligned()
	{
		// Common header pattern: "Title" <tab> "Page 1" right-aligned
		var headerParagraph = new Paragraph(
			new ParagraphProperties(
				new Tabs(
					new DocumentFormat.OpenXml.Wordprocessing.TabStop
					{
						Val = TabStopValues.Right,
						Position = 9360
					}
				)),
			new Run(new Text("Title") { Space = SpaceProcessingModeValues.Preserve }),
			new Run(new TabChar()),
			new Run(new Text("Page 1") { Space = SpaceProcessingModeValues.Preserve }));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1440,
			HeaderTopTwips = 720,
			HeaderBlocks = [new LayoutBlock(new ParagraphBlock { SourceElement = headerParagraph }, 240f)],
			Blocks = []
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		// Should have "Title" and "Page 1"
		target.DrawTextCalls.Should().HaveCount(2);
		target.DrawTextCalls[0].Text.Should().Be("Title");
		target.DrawTextCalls[1].Text.Should().Be("Page 1");

		// "Page 1" should be right-aligned at tab stop 9360
		var contentWidth = EstimateWidth("Page 1", 12f);
		var expectedX = 720f + TabStopResolver.ComputeContentStart(
			new RenderTabStop(9360f, TabStopType.Right),
			contentWidth);
		target.DrawTextCalls[1].BaselineXTwips.Should().Be(expectedX);
	}

	[Fact]
	public void EmitPage_FooterWithRightTab_DrawsFooterTextAligned()
	{
		var footerParagraph = new Paragraph(
			new ParagraphProperties(
				new Tabs(
					new DocumentFormat.OpenXml.Wordprocessing.TabStop
					{
						Val = TabStopValues.Right,
						Position = 9360
					}
				)),
			new Run(new TabChar()),
			new Run(new Text("Page 2") { Space = SpaceProcessingModeValues.Preserve }));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 2,
			ContentTopTwips = 1440,
			FooterTopTwips = 14400,
			FooterBlocks = [new LayoutBlock(new ParagraphBlock { SourceElement = footerParagraph }, 240f)],
			Blocks = []
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be("Page 2");

		var contentWidth = EstimateWidth("Page 2", 12f);
		var expectedX = 720f + TabStopResolver.ComputeContentStart(
			new RenderTabStop(9360f, TabStopType.Right),
			contentWidth);
		target.DrawTextCalls[0].BaselineXTwips.Should().Be(expectedX);
	}

	[Fact]
	public void EmitPage_HeaderInlineImageTallerThanLine_PushesTextBaselineDown()
	{
		var headerParagraph = new Paragraph(
			new Run(new Text("Title") { Space = SpaceProcessingModeValues.Preserve }),
			new Run(new TabChar()),
			new Run(CreateInlineDrawing("rId-logo", widthTwips: 720, heightTwips: 900)));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 2,
			ContentTopTwips = 1440,
			HeaderTopTwips = 720,
			HeaderBlocks = [new LayoutBlock(new ParagraphBlock { SourceElement = headerParagraph }, 240f)],
			Blocks = []
		};
		var target = new FakeRenderTarget();
		var images = new Dictionary<string, ImageData>
		{
			["rId-logo"] = new ImageData([1, 2, 3], "image/png")
		};

		RenderCommandEmitter.EmitPage(page, target, images: images);

		target.DrawTextCalls.Should().NotBeEmpty();
		target.DrawTextCalls[0].Text.Should().Be("Title");
		target.DrawTextCalls[0].BaselineYTwips.Should().BeGreaterThan(960f);
	}

	[Fact]
	public void EmitPage_RtlRunInParagraph_DrawsText()
	{
		// An RTL run should still produce DrawText output (detection only for now)
		var paragraph = new Paragraph(
			new Run(
				new RunProperties(new RightToLeftText()),
				new Text("مرحبا")));
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be("مرحبا");
	}

	[Fact]
	public void EmitPage_BiDiParagraphNoAlignment_DefaultsToRightAligned()
	{
		// A BiDi paragraph with no explicit alignment should default to right-aligned
		var paragraph = new Paragraph(
			new ParagraphProperties(new BiDi()),
			new Run(new Text("Test")));
		var section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 };
		var contentWidth = section.PageWidth - section.MarginLeft - section.MarginRight;
		var page = new LayoutPage
		{
			Section = section,
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph, IsBiDi = true }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		// Text should start at right margin minus text width
		var textWidth = EstimateWidth("Test", 12f);
		var expectedX = section.MarginLeft + contentWidth - textWidth;
		target.DrawTextCalls[0].BaselineXTwips.Should().Be(expectedX);
	}

	[Fact]
	public void EmitPage_BiDiParagraphExplicitLeftAlignment_LeftAligned()
	{
		// A BiDi paragraph with explicit left alignment should be left-aligned
		var paragraph = new Paragraph(
			new ParagraphProperties(new BiDi(), new Justification { Val = JustificationValues.Left }),
			new Run(new Text("Test")));
		var section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 };
		var contentWidth = section.PageWidth - section.MarginLeft - section.MarginRight;
		var page = new LayoutPage
		{
			Section = section,
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph, IsBiDi = true, Alignment = ParagraphAlignment.Left }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		// Explicit left alignment: text starts at left margin
		target.DrawTextCalls[0].BaselineXTwips.Should().Be((float)section.MarginLeft);
	}

	[Fact]
	public void EmitPage_CenterAlignedParagraph_TextCentered()
	{
		var paragraph = new Paragraph(
			new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
			new Run(new Text("Hi")));
		var section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 };
		var contentWidth = section.PageWidth - section.MarginLeft - section.MarginRight;
		var page = new LayoutPage
		{
			Section = section,
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph, Alignment = ParagraphAlignment.Center }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		var textWidth = EstimateWidth("Hi", 12f);
		var expectedX = section.MarginLeft + (contentWidth - textWidth) / 2f;
		target.DrawTextCalls[0].BaselineXTwips.Should().Be(expectedX);
	}

	[Fact]
	public void EmitPage_InlineSdtRun_RendersInnerContent()
	{
		// An inline content control (SdtRun) wrapping a run should render the inner text
		var sdt = new SdtRun(
			new SdtProperties(),
			new SdtContentRun(
				new Run(new Text("Controlled text"))));
		var paragraph = new Paragraph(sdt);
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be("Controlled text");
	}

	[Fact]
	public void EmitPage_SdtRunMixedWithNormalRun_RendersBoth()
	{
		var sdt = new SdtRun(
			new SdtProperties(),
			new SdtContentRun(
				new Run(new Text("SDT") { Space = SpaceProcessingModeValues.Preserve })));
		var paragraph = new Paragraph(
			new Run(new Text("Normal ") { Space = SpaceProcessingModeValues.Preserve }),
			sdt);
		var page = new LayoutPage
		{
			Section = new SectionInfo { PageWidth = 12240, PageHeight = 15840, MarginLeft = 720, MarginRight = 720 },
			PageNumber = 1,
			ContentTopTwips = 1000,
			Blocks = [new LayoutBlock(new ParagraphBlock { SourceElement = paragraph }, 300f)]
		};
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitPage(page, target);

		// Both text segments should be rendered (may merge into one if same font)
		var allText = string.Concat(target.DrawTextCalls.Select(c => c.Text));
		allText.Should().Contain("Normal ");
		allText.Should().Contain("SDT");
	}

	private static float EstimateWidth(string text, float sizePoints)
	{
		// Must match RenderCommandEmitter.EstimateTextWidthTwips: text.Length * sizePoints * AverageGlyphWidthFactor (10)
		return text.Length * sizePoints * 10f;
	}

	private static Drawing CreateInlineDrawing(string relationshipId, int widthTwips, int heightTwips)
	{
		var blip = new A.Blip { Embed = relationshipId };
		var graphicData = new A.GraphicData(blip)
		{
			Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture"
		};
		var graphic = new A.Graphic(graphicData);
		const float emusPerTwip = 635f;
		var inline = new DW.Inline(
			new DW.Extent
			{
				Cx = (long)(widthTwips * emusPerTwip),
				Cy = (long)(heightTwips * emusPerTwip)
			},
			graphic);
		return new Drawing(inline);
	}

	private sealed class FakeRenderTarget : IRenderTarget
	{
		public List<DrawTextCall> DrawTextCalls { get; } = [];
		public List<DrawRectCall> DrawRectCalls { get; } = [];
		public List<HyperlinkCall> HyperlinkCalls { get; } = [];
		public List<NamedDestinationCall> NamedDestinationCalls { get; } = [];
		public List<RotatedTextCall> RotatedTextCalls { get; } = [];
		public List<RotatedImageCall> RotatedImageCalls { get; } = [];
		public List<DrawLineCall> DrawLineCalls { get; } = [];
		public List<string> CallOrder { get; } = [];

		public void DrawText(string text, float baselineXTwips, float baselineYTwips, RenderFont font, RenderBrush brush)
		{
			DrawTextCalls.Add(new DrawTextCall(text, baselineXTwips, baselineYTwips, font, brush));
			CallOrder.Add($"DrawText:{text}");
		}

		public void DrawLine(RenderPoint from, RenderPoint to, RenderStroke stroke)
		{
			DrawLineCalls.Add(new DrawLineCall(from, to, stroke));
		}

		public void DrawRect(RenderRect rect, RenderBrush? fill, RenderStroke? stroke)
		{
			DrawRectCalls.Add(new DrawRectCall(rect, fill, stroke));
		}

		public void DrawImage(ImageData image, RenderRect rect)
		{
		}

		public void DrawPath(string pathData, RenderBrush? fill, RenderStroke? stroke)
		{
		}

		public void PushClip(RenderRect clipRect)
		{
		}

		public void PopClip()
		{
		}

		public void SetHyperlink(RenderRect rect, string uri)
		{
			HyperlinkCalls.Add(new HyperlinkCall(rect, uri));
		}

		public void SetNamedDestination(string name, float xTwips, float yTwips)
		{
			NamedDestinationCalls.Add(new NamedDestinationCall(name, xTwips, yTwips));
		}

		public void DrawRotatedText(string text, float centerXTwips, float centerYTwips, float rotationDegrees, RenderFont font, RenderBrush brush)
		{
			RotatedTextCalls.Add(new RotatedTextCall(text, centerXTwips, centerYTwips, rotationDegrees, font, brush));
			CallOrder.Add($"DrawRotatedText:{text}");
		}

		public void DrawRotatedImage(ImageData image, float centerXTwips, float centerYTwips, float widthTwips, float heightTwips, float rotationDegrees, float opacity)
		{
			RotatedImageCalls.Add(new RotatedImageCall(image, centerXTwips, centerYTwips, widthTwips, heightTwips, rotationDegrees, opacity));
			CallOrder.Add("DrawRotatedImage");
		}
	}

	private readonly record struct DrawTextCall(
		string Text,
		float BaselineXTwips,
		float BaselineYTwips,
		RenderFont Font,
		RenderBrush Brush);

	private readonly record struct DrawRectCall(
		RenderRect Rect,
		RenderBrush? Fill,
		RenderStroke? Stroke);

	private readonly record struct HyperlinkCall(RenderRect Rect, string Uri);

	private readonly record struct NamedDestinationCall(string Name, float XTwips, float YTwips);

	private readonly record struct RotatedTextCall(string Text, float CenterXTwips, float CenterYTwips, float RotationDegrees, RenderFont Font, RenderBrush Brush);

	private readonly record struct RotatedImageCall(ImageData Image, float CenterXTwips, float CenterYTwips, float WidthTwips, float HeightTwips, float RotationDegrees, float Opacity);

	private readonly record struct DrawLineCall(RenderPoint From, RenderPoint To, RenderStroke Stroke);
}
