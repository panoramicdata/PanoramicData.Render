namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;
using static PanoramicData.Render.Test.EmitterBuilders;
using static PanoramicData.Render.Test.EmitterBuilders2;

public sealed partial class RenderCommandEmitterTests
{
	[Fact]
	public void EmitPage_ComplexPageField_RendersComputedCurrentPage()
	{
		var target = EmitField(ComplexFieldParagraph(" PAGE ", "999"), pageNumber: 7, totalPageCount: 13);

		AssertSingleText(target, "7");
	}

	[Fact]
	public void EmitPage_ComplexNumPagesField_RendersProvidedTotalPages()
	{
		var target = EmitField(ComplexFieldParagraph(" NUMPAGES ", "1"), totalPageCount: 42);

		AssertSingleText(target, "42");
	}

	[Fact]
	public void EmitPage_SimpleDateField_WithYearPictureSwitch_RendersYear()
	{
		// DATE \@ "yyyy" should render the 4-digit year from the live timestamp.
		var target = EmitField(SimpleFieldParagraph("stale", " DATE  \\@ \"yyyy\"  \\* MERGEFORMAT "), renderTimestampUtc: new DateTime(2026, 4, 16, 9, 0, 0, DateTimeKind.Utc));

		AssertSingleText(target, "2026");
	}

	[Fact]
	public void EmitPage_SimpleDateField_NoSwitch_RendersShortDateFallback()
	{
		// DATE with no \@ switch falls back to the "d" (short date) format.
		var timestamp = new DateTime(2026, 4, 16, 9, 0, 0, DateTimeKind.Utc);
		var target = EmitField(SimpleFieldParagraph("stale", " DATE "), renderTimestampUtc: timestamp);

		AssertSingleText(target, timestamp.ToString("d", System.Globalization.CultureInfo.InvariantCulture));
	}

	[Fact]
	public void EmitPage_SimpleTocField_RendersCachedResultText()
	{
		AssertCachedField("Heading 1........1", " TOC ");
	}

	[Fact]
	public void EmitPage_SimpleRefField_RendersCachedResultText()
	{
		AssertCachedField("Section 2", " REF myBookmark ");
	}

	[Fact]
	public void EmitPage_SimpleMergeField_RendersCachedResultText()
	{
		AssertCachedField("Jane Doe", " MERGEFIELD CustomerName ");
	}

	[Fact]
	public void EmitPage_ComplexHyperlinkField_EmitsHyperlinkRegion()
	{
		var target = EmitField(ComplexFieldParagraph(" HYPERLINK \"https://example.com\" ", "Click me"));

		AssertHyperlink(target, "Click me", "https://example.com");
		target.HyperlinkCalls[0].Rect.WidthTwips.Should().BeGreaterThan(0f);
	}

	[Fact]
	public void EmitPage_SimpleHyperlinkField_EmitsHyperlinkRegion()
	{
		AssertHyperlink(EmitField(SimpleFieldParagraph("Open", " HYPERLINK \"https://contoso.test\" ")), "Open", "https://contoso.test");
	}

	[Fact]
	public void EmitPage_HyperlinkElementWithAnchor_EmitsHyperlinkRegion()
	{
		var hyperlink = new Hyperlink(new Run(new Text("Go to section")))
		{
			Anchor = "myBookmark"
		};
		var target = EmitField(new Paragraph(hyperlink));

		AssertHyperlink(target, "Go to section", "#myBookmark");
		target.HyperlinkCalls[0].Rect.WidthTwips.Should().BeGreaterThan(0f);
	}

	[Fact]
	public void EmitPage_HyperlinkElementWithoutAnchorOrId_EmitsTextWithNoHyperlink()
	{
		var target = EmitField(new Paragraph(new Hyperlink(new Run(new Text("Orphaned link")))));

		AssertSingleText(target, "Orphaned link");
		target.HyperlinkCalls.Should().BeEmpty();
	}

	[Fact]
	public void EmitPage_ParagraphWithBookmarkStarts_EmitsNamedDestinations()
	{
		var target = Emit(BookmarkPage(new BookmarkStartInfo(1, "_Toc123456")));

		target.NamedDestinationCalls.Should().ContainSingle();
		target.NamedDestinationCalls[0].Name.Should().Be("_Toc123456");
		target.NamedDestinationCalls[0].XTwips.Should().Be(500f);
		target.NamedDestinationCalls[0].YTwips.Should().Be(1000f);
	}

	[Fact]
	public void EmitPage_ParagraphWithMultipleBookmarks_EmitsAllNamedDestinations()
	{
		var target = Emit(BookmarkPage(new BookmarkStartInfo(1, "first"), new BookmarkStartInfo(2, "second")));

		target.NamedDestinationCalls.Select(call => call.Name).Should().Equal("first", "second");
	}

	[Fact]
	public void EmitPage_ParagraphWithNoBookmarks_EmitsNoNamedDestinations()
	{
		var target = EmitField(new Paragraph(new Run(new Text("Plain text"))));

		target.NamedDestinationCalls.Should().BeEmpty();
	}

	[Fact]
	public void EmitDocument_NumberedParagraphs_EmitsIncrementingListLabels()
	{
		var target = new FakeRenderTarget();

		RenderCommandEmitter.EmitDocument(
			[
				StandardPage(1, NumberedBlock("First", 1, 0)),
				StandardPage(2, NumberedBlock("Second", 1, 0)),
				StandardPage(3, NumberedBlock("Third", 1, 0))
			],
			target);

		AssertTextsInOrder(target, "1. ", "First", "2. ", "Second", "3. ", "Third");
	}

	[Fact]
	public void EmitPage_MultiLevelNumbering_EmitsPatternedLabels()
	{
		var options = NumberingOptions(
			("1:0", NumberLevel(0, "%1.")),
			("1:1", NumberLevel(1, "%1.%2.")));
		var target = Emit(StandardPage(1, NumberedBlock("Top", 1, 0), NumberedBlock("Nested", 1, 1)), options: options);

		AssertTextsInOrder(target, "1. ", "Top", "1.1. ", "Nested");
	}
}
