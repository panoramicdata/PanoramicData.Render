namespace PanoramicData.Render.Test;

using AwesomeAssertions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

public sealed partial class FieldUpdateEngineTests
{
	[Fact]
	public void Apply_PageAndNumPagesFields_UpdatesCachedResultText()
	{
		using var run = ApplyChanged(CreateDocxWithPageFieldParagraphs(), ["PAGE", "NUMPAGES"]);
		var paragraphs = run.Paragraphs;
		GetParagraphResultText(paragraphs[0]).Should().Be("1");
		GetParagraphResultText(paragraphs[1]).Should().Be("2");
		GetParagraphResultText(paragraphs[2]).Should().Be("2");
	}

	[Fact]
	public void Apply_DocumentPropertyFields_UpdatesCachedResultText()
	{
		using var run = ApplyFieldUpdate(CreateDocxWithDocumentPropertyFieldParagraphs(), new FieldUpdateOptions(), "uploaded.docx");
		var doc = run.Doc;
		var result = run.Result;

		result.HasChanges.Should().BeTrue();
		result.UpdatedFields.Should().Contain(["AUTHOR", "DESCRIPTION", "FILENAME", "KEYWORDS", "SUBJECT", "TITLE"]);

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		GetParagraphResultText(paragraphs[0]).Should().Be("Quarterly Report");
		GetParagraphResultText(paragraphs[1]).Should().Be("Alice Example");
		GetParagraphResultText(paragraphs[2]).Should().Be("Master Services Agreement");
		GetParagraphResultText(paragraphs[3]).Should().Be("finance; forecast");
		GetParagraphResultText(paragraphs[4]).Should().Be("Uploaded by browser client");
		GetParagraphResultText(paragraphs[5]).Should().Be("uploaded.docx");
	}

	[Fact]
	public void Apply_TableOfContents_RebuildsEntryParagraphs()
	{
		using var run = ApplyChanged(CreateDocxWithTableOfContentsField(), "TOC");
		var paragraphs = run.Paragraphs;
		paragraphs.Should().HaveCount(5);
		AssertTocEntry(paragraphs[1], "TOC1", "Chapter One", "2");
		AssertTocEntry(paragraphs[2], "TOC1", "Chapter Two", "3");
		paragraphs.Select(GetParagraphResultText).Should().NotContain("Old Entry\t99");
	}

	[Fact]
	public void Apply_TableOfContents_WithCustomSeparator_OmitsDefaultTabLeader()
	{
		using var run = ApplyChanged(CreateDocxWithTableOfContentsField("TOC \\o \"1-3\" \\p \" -- \""));
		var paragraphs = run.Paragraphs;
		GetParagraphResultText(paragraphs[1]).Should().Be("Chapter One -- 2");
		GetParagraphResultText(paragraphs[2]).Should().Be("Chapter Two -- 3");
	}

	[Fact]
	public void Apply_TableOfContents_WithNoPageNumbersSwitch_OmitsPageNumbers()
	{
		using var run = ApplyChanged(CreateDocxWithTableOfContentsField("TOC \\o \"1-3\" \\n"));
		var paragraphs = run.Paragraphs;
		GetParagraphResultText(paragraphs[1]).Should().Be("Chapter One");
		GetParagraphResultText(paragraphs[2]).Should().Be("Chapter Two");
	}

	[Fact]
	public void Apply_TableOfContents_WithCustomStyleSwitch_IncludesMappedParagraphs()
	{
		using var run = ApplyChanged(CreateDocxWithCustomStyleTableOfContentsField(), "TOC");
		var paragraphs = run.Paragraphs;
		paragraphs.Should().HaveCount(3);
		AssertTocEntry(paragraphs[1], "TOC2", "Appendix A", "2");
	}

	[Fact]
	public void Apply_TableOfContents_WithHyperlinkSwitch_WrapsEntriesInBookmarkHyperlinks()
	{
		using var run = ApplyChanged(CreateDocxWithHyperlinkedTableOfContentsField(), "TOC");
		var paragraphs = run.Paragraphs;
		var firstEntryLink = paragraphs[1].Descendants<Hyperlink>().Should().ContainSingle().Subject;
		firstEntryLink.Anchor?.Value.Should().Be("_TocChapterOne");
		paragraphs[1].Descendants<TabChar>().Should().ContainSingle();
		GetParagraphTextNodes(paragraphs[1]).Should().Equal(["Chapter One", "2"]);

		var secondEntryLink = paragraphs[2].Descendants<Hyperlink>().Should().ContainSingle().Subject;
		secondEntryLink.Anchor?.Value.Should().Be("_TocChapterTwo");
		paragraphs[2].Descendants<TabChar>().Should().ContainSingle();
		GetParagraphTextNodes(paragraphs[2]).Should().Equal(["Chapter Two", "3"]);
	}

	[Fact]
	public void Apply_TableOfContents_WithHyperlinkSwitch_GeneratesSyntheticBookmarksForHeadingsWithoutAnchors()
	{
		using var run = ApplyChanged(CreateDocxWithHyperlinkedTableOfContentsFieldWithoutHeadingBookmarks(), "TOC");
		var paragraphs = run.Paragraphs;
		var firstSyntheticAnchor = GetParagraphHyperlinkAnchor(paragraphs[1]);
		firstSyntheticAnchor.Should().Be("_TocGenerated1");
		GetHeadingBookmarkName(paragraphs[3]).Should().Be(firstSyntheticAnchor);

		var secondSyntheticAnchor = GetParagraphHyperlinkAnchor(paragraphs[2]);
		secondSyntheticAnchor.Should().Be("_TocGenerated2");
		GetHeadingBookmarkName(paragraphs[4]).Should().Be(secondSyntheticAnchor);
	}

	[Fact]
	public void Apply_TableOfContents_WithExplicitTabLeaderTemplate_PreservesParagraphTabsAndUsesTabRuns()
	{
		using var run = ApplyChanged(CreateDocxWithTableOfContentsFieldAndExplicitTabLeaderTemplate());
		var paragraphs = run.Paragraphs;
		var firstEntry = paragraphs[1];

		firstEntry.Descendants<TabChar>().Should().ContainSingle();
		var paragraphProperties = firstEntry.ParagraphProperties;
		paragraphProperties.Should().NotBeNull();
		var tabs = paragraphProperties!.Tabs;
		tabs.Should().NotBeNull();
		var tabStop = tabs!.Elements<DocumentFormat.OpenXml.Wordprocessing.TabStop>().Should().ContainSingle().Subject;
		tabStop.Val?.Value.Should().Be(TabStopValues.Right);
		tabStop.Leader?.Value.Should().Be(TabStopLeaderCharValues.Dot);

		var textNodes = firstEntry.Descendants<Text>().Select(text => text.Text).ToArray();
		textNodes.Should().HaveCount(2);
		textNodes[0].Should().Be("Chapter One");
		textNodes[1].Should().Be("2");
	}

	[Fact]
	public void Apply_TableOfContents_WithTemplateRunFormatting_PreservesRunProperties()
	{
		using var run = ApplyFieldUpdate(CreateDocxWithTableOfContentsFieldAndTemplateRunFormatting());
		var doc = run.Doc;
		var result = run.Result;

		result.HasChanges.Should().BeTrue();
		var firstEntry = doc.DocumentBody.Elements<Paragraph>().ElementAt(1);
		var runs = firstEntry.Descendants<Run>().ToArray();

		runs.Should().HaveCount(3);
		var firstRunProperties = runs[0].RunProperties;
		firstRunProperties.Should().NotBeNull();
		firstRunProperties!.Bold.Should().NotBeNull();
		firstRunProperties.FontSize?.Val?.Value.Should().Be("28");

		runs[1].Elements<TabChar>().Should().ContainSingle();
		var tabRunProperties = runs[1].RunProperties;
		tabRunProperties.Should().NotBeNull();
		tabRunProperties!.Italic.Should().NotBeNull();
		tabRunProperties.FontSize?.Val?.Value.Should().Be("24");

		var pageRunProperties = runs[2].RunProperties;
		pageRunProperties.Should().NotBeNull();
		pageRunProperties!.Italic.Should().NotBeNull();
		pageRunProperties.FontSize?.Val?.Value.Should().Be("20");
	}

	[Fact]
	public void Apply_TableOfFigures_FromCaptionParagraphs_RebuildsEntryParagraphs()
	{
		using var run = ApplyChanged(CreateDocxWithTableOfFiguresField(), "TOF");
		var paragraphs = run.Paragraphs;
		paragraphs.Should().HaveCount(5);
		AssertTocEntry(paragraphs[1], "TOC1", "Figure 1. Overview", "2");
		AssertTocEntry(paragraphs[2], "TOC1", "Figure 2. Details", "3");
		paragraphs.Select(GetParagraphResultText).Should().NotContain("Old Figure\t99");
	}

	[Fact]
	public void Apply_TableOfFigures_FromSeqFigureParagraphs_RebuildsEntryParagraphs()
	{
		using var run = ApplyChanged(CreateDocxWithTableOfFiguresFieldAndSeqFigureParagraphs(), "TOF");
		var paragraphs = run.Paragraphs;
		paragraphs.Should().HaveCount(5);
		GetParagraphTextNodes(paragraphs[1]).Should().Equal(["1. Overview", "2"]);
		GetParagraphTextNodes(paragraphs[2]).Should().Equal(["2. Details", "3"]);
	}

	[Fact]
	public void Apply_SequenceFields_AssignsSequentialNumbersPerIdentifier()
	{
		using var run = ApplyChanged(CreateDocxWithSequenceFields(), "SEQ");
		var paragraphs = run.Paragraphs;
		GetParagraphResultText(paragraphs[0]).Should().Be("1 caption text");
		GetParagraphResultText(paragraphs[1]).Should().Be("2 more text");
		GetParagraphResultText(paragraphs[2]).Should().Be("1 first table");
	}

	[Fact]
	public void Apply_SequenceFields_WithResetSwitch_ResetsCounterToSpecifiedValue()
	{
		using var run = ApplyChanged(CreateDocxWithSequenceFieldsAndResetSwitch(), "SEQ");
		var paragraphs = run.Paragraphs;
		GetParagraphResultText(paragraphs[0]).Should().Be("1 first");
		GetParagraphResultText(paragraphs[1]).Should().Be("2 second");
		GetParagraphResultText(paragraphs[2]).Should().Be("10 reset");
		GetParagraphResultText(paragraphs[3]).Should().Be("11 after reset");
	}

	[Fact]
	public void Apply_SequenceFields_WithHiddenSwitch_IncrementsButHidesResult()
	{
		using var run = ApplyChanged(CreateDocxWithSequenceFieldsAndHiddenSwitch(), "SEQ");
		var paragraphs = run.Paragraphs;
		GetParagraphResultText(paragraphs[0]).Should().Be("1 visible");
		GetParagraphResultText(paragraphs[1]).Should().Be(" hidden counter");
		GetParagraphResultText(paragraphs[2]).Should().Be("3 skipped two");
	}

	[Fact]
	public void Apply_SequenceFields_WhenDisabled_DoesNotUpdateSeqValues()
	{
		using var run = ApplyFieldUpdate(CreateDocxWithSequenceFields(), new FieldUpdateOptions { UpdateSequenceFields = false });
		var doc = run.Doc;
		var result = run.Result;

		result.UpdatedFields.Should().NotContain("SEQ");

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		GetParagraphResultText(paragraphs[0]).Should().Be("X caption text");
	}
	private static FieldUpdateRun ApplyFieldUpdate(Stream docx, FieldUpdateOptions? options = null, string? sourceFilename = null)
	{
		var doc = DocxDocument.Load(docx);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());
		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = options ?? new FieldUpdateOptions(),
			SourceFilename = sourceFilename
		});
		return new FieldUpdateRun(doc, result, docx);
	}

	private static void AssertTocEntry(Paragraph entry, string styleId, string text, string page)
	{
		GetParagraphStyleId(entry).Should().Be(styleId);
		entry.Descendants<TabChar>().Should().ContainSingle();
		GetParagraphTextNodes(entry).Should().Equal([text, page]);
	}

	private static FieldUpdateRun ApplyChanged(Stream docx, params string[] updatedFields)
	{
		var run = ApplyFieldUpdate(docx);
		run.Result.HasChanges.Should().BeTrue();
		foreach (var field in updatedFields)
		{
			run.Result.UpdatedFields.Should().Contain(field);
		}

		return run;
	}

	private sealed class FieldUpdateRun(DocxDocument doc, FieldUpdatePassResult result, Stream stream) : IDisposable
	{
		public DocxDocument Doc { get; } = doc;

		public Paragraph[] Paragraphs => Doc.DocumentBody.Elements<Paragraph>().ToArray();

		public FieldUpdatePassResult Result { get; } = result;

		public void Dispose()
		{
			Doc.Dispose();
			stream.Dispose();
		}
	}

}
