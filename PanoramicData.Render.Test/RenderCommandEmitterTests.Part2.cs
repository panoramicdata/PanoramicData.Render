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
	public void EmitPage_ComplexPageField_RendersComputedCurrentPage()
	{
		var paragraph = ComplexFieldParagraph(" PAGE ", "999");
		var target = EmitParagraph(paragraph, NarrowSection(), pageNumber: 7, totalPageCount: 13, renderTimestampUtc: new DateTime(2026, 4, 10, 11, 30, 0, DateTimeKind.Utc));

		AssertSingleText(target, "7");
	}

	[Fact]
	public void EmitPage_ComplexNumPagesField_RendersProvidedTotalPages()
	{
		var paragraph = ComplexFieldParagraph(" NUMPAGES ", "1");
		var target = EmitParagraph(paragraph, NarrowSection(), totalPageCount: 42, renderTimestampUtc: new DateTime(2026, 4, 10, 11, 30, 0, DateTimeKind.Utc));

		AssertSingleText(target, "42");
	}

	[Fact]
	public void EmitPage_SimpleDateField_WithYearPictureSwitch_RendersYear()
	{
		// DATE \@ "yyyy" should render the 4-digit year from the live timestamp.
		var paragraph = SimpleFieldParagraph("stale", " DATE  \\@ \"yyyy\"  \\* MERGEFORMAT ");
		var target = EmitParagraph(paragraph, NarrowSection(), renderTimestampUtc: new DateTime(2026, 4, 16, 9, 0, 0, DateTimeKind.Utc));

		AssertSingleText(target, "2026");
	}

	[Fact]
	public void EmitPage_SimpleDateField_NoSwitch_RendersShortDateFallback()
	{
		// DATE with no \@ switch falls back to the "d" (short date) format.
		var paragraph = SimpleFieldParagraph("stale", " DATE ");
		var timestamp = new DateTime(2026, 4, 16, 9, 0, 0, DateTimeKind.Utc);
		var target = EmitParagraph(paragraph, NarrowSection(), renderTimestampUtc: timestamp);

		target.DrawTextCalls.Should().ContainSingle();
		target.DrawTextCalls[0].Text.Should().Be(timestamp.ToString("d", System.Globalization.CultureInfo.InvariantCulture));
	}

	[Fact]
	public void EmitPage_SimpleTocField_RendersCachedResultText()
	{
		var paragraph = SimpleFieldParagraph("Heading 1........1", " TOC ");
		var target = EmitParagraph(paragraph, NarrowSection());

		AssertSingleText(target, "Heading 1........1");
	}

	[Fact]
	public void EmitPage_ComplexHyperlinkField_EmitsHyperlinkRegion()
	{
		var paragraph = ComplexFieldParagraph(" HYPERLINK \"https://example.com\" ", "Click me");
		var target = EmitParagraph(paragraph, NarrowSection());

		AssertSingleText(target, "Click me");
		target.HyperlinkCalls.Should().ContainSingle();
		target.HyperlinkCalls[0].Uri.Should().Be("https://example.com");
		target.HyperlinkCalls[0].Rect.WidthTwips.Should().BeGreaterThan(0f);
	}

	[Fact]
	public void EmitPage_SimpleHyperlinkField_EmitsHyperlinkRegion()
	{
		var paragraph = SimpleFieldParagraph("Open", " HYPERLINK \"https://contoso.test\" ");
		var target = EmitParagraph(paragraph, NarrowSection());

		AssertSingleText(target, "Open");
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
		var target = EmitParagraph(paragraph, NarrowSection());

		AssertSingleText(target, "Go to section");
		target.HyperlinkCalls.Should().ContainSingle();
		target.HyperlinkCalls[0].Uri.Should().Be("#myBookmark");
		target.HyperlinkCalls[0].Rect.WidthTwips.Should().BeGreaterThan(0f);
	}

	[Fact]
	public void EmitPage_HyperlinkElementWithoutAnchorOrId_EmitsTextWithNoHyperlink()
	{
		var hyperlink = new Hyperlink(new Run(new Text("Orphaned link")));
		var paragraph = new Paragraph(hyperlink);
		var target = EmitParagraph(paragraph, NarrowSection());

		AssertSingleText(target, "Orphaned link");
		target.HyperlinkCalls.Should().BeEmpty();
	}

	[Fact]
	public void EmitPage_ParagraphWithBookmarkStarts_EmitsNamedDestinations()
	{
		var paragraph = new Paragraph(new Run(new Text("Chapter 1")));
		var page = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock
			{
				SourceElement = paragraph,
				BookmarkStarts = [new BookmarkStartInfo(1, "_Toc123456")]
			}, 300f)], 1, section: new SectionInfo { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 });
		var target = Emit(page);

		target.NamedDestinationCalls.Should().ContainSingle();
		target.NamedDestinationCalls[0].Name.Should().Be("_Toc123456");
		target.NamedDestinationCalls[0].XTwips.Should().Be(500f);
		target.NamedDestinationCalls[0].YTwips.Should().Be(1000f);
	}

	[Fact]
	public void EmitPage_ParagraphWithMultipleBookmarks_EmitsAllNamedDestinations()
	{
		var paragraph = new Paragraph(new Run(new Text("Section")));
		var page = TestLayoutPageFactory.Create([new LayoutBlock(new ParagraphBlock
			{
				SourceElement = paragraph,
				BookmarkStarts =
				[
					new BookmarkStartInfo(1, "first"),
					new BookmarkStartInfo(2, "second")
				]
			}, 300f)], 1, section: new SectionInfo { MarginLeft = 500, MarginRight = 500, PageWidth = 10000 });
		var target = Emit(page);

		target.NamedDestinationCalls.Should().HaveCount(2);
		target.NamedDestinationCalls[0].Name.Should().Be("first");
		target.NamedDestinationCalls[1].Name.Should().Be("second");
	}

	[Fact]
	public void EmitPage_ParagraphWithNoBookmarks_EmitsNoNamedDestinations()
	{
		var paragraph = new Paragraph(new Run(new Text("Plain text")));
		var target = EmitParagraph(paragraph, NarrowSection());

		target.NamedDestinationCalls.Should().BeEmpty();
	}

	[Fact]
	public void EmitPage_SimpleRefField_RendersCachedResultText()
	{
		var paragraph = SimpleFieldParagraph("Section 2", " REF myBookmark ");
		var target = EmitParagraph(paragraph, NarrowSection());

		AssertSingleText(target, "Section 2");
	}

	[Fact]
	public void EmitPage_SimpleMergeField_RendersCachedResultText()
	{
		var paragraph = SimpleFieldParagraph("Jane Doe", " MERGEFIELD CustomerName ");
		var target = EmitParagraph(paragraph, NarrowSection());

		AssertSingleText(target, "Jane Doe");
	}

	[Fact]
	public void EmitDocument_NumberedParagraphs_EmitsIncrementingListLabels()
	{
		LayoutPage CreatePage(int pageNumber, string text)
		{
			return TestLayoutPageFactory.Create([
					new LayoutBlock(new ParagraphBlock
					{
						SourceElement = new Paragraph(new Run(new Text(text))),
						NumberingId = 1,
						NumberingLevel = 0
					}, 300f)
				], pageNumber, section: new SectionInfo { MarginLeft = 720, MarginRight = 720, PageWidth = 12240 });
		}

		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitDocument([CreatePage(1, "First"), CreatePage(2, "Second"), CreatePage(3, "Third")], target);

		target.DrawTextCalls.Select(call => call.Text).Should().ContainInOrder("1. ", "First", "2. ", "Second", "3. ", "Third");
	}

	[Fact]
	public void EmitPage_MultiLevelNumbering_EmitsPatternedLabels()
	{
		var page = TestLayoutPageFactory.Create([
				NumberedBlock("Top", 1, 0),
				NumberedBlock("Nested", 1, 1)
			], 1, section: new SectionInfo { MarginLeft = 720, MarginRight = 720, PageWidth = 12240 });
		var options = new RenderOptions
		{
			NumberingStyles =
			{
				["1:0"] = new NumberingLevelStyle { LevelIndex = 0, Start = 1, NumberFormat = "decimal", LevelText = "%1." },
				["1:1"] = new NumberingLevelStyle { LevelIndex = 1, Start = 1, NumberFormat = "decimal", LevelText = "%1.%2." }
			}
		};
		var target = Emit(page, options: options);

		target.DrawTextCalls.Select(call => call.Text).Should().ContainInOrder("1. ", "Top", "1.1. ", "Nested");
	}
}
