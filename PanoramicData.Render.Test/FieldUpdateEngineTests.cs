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
		using var stream = CreateDocxWithPageFieldParagraphs();
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});

		result.HasChanges.Should().BeTrue();
		result.UpdatedFields.Should().Contain(["PAGE", "NUMPAGES"]);

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		GetParagraphResultText(paragraphs[0]).Should().Be("1");
		GetParagraphResultText(paragraphs[1]).Should().Be("2");
		GetParagraphResultText(paragraphs[2]).Should().Be("2");
	}

	[Fact]
	public void Apply_DocumentPropertyFields_UpdatesCachedResultText()
	{
		using var stream = CreateDocxWithDocumentPropertyFieldParagraphs();
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions(),
			SourceFilename = "uploaded.docx"
		});

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
		using var stream = CreateDocxWithTableOfContentsField();
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});

		result.HasChanges.Should().BeTrue();
		result.UpdatedFields.Should().Contain("TOC");

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		paragraphs.Should().HaveCount(5);
		GetParagraphStyleId(paragraphs[1]).Should().Be("TOC1");
		paragraphs[1].Descendants<TabChar>().Should().ContainSingle();
		GetParagraphTextNodes(paragraphs[1]).Should().Equal(["Chapter One", "2"]);
		GetParagraphStyleId(paragraphs[2]).Should().Be("TOC1");
		paragraphs[2].Descendants<TabChar>().Should().ContainSingle();
		GetParagraphTextNodes(paragraphs[2]).Should().Equal(["Chapter Two", "3"]);
		paragraphs.Select(GetParagraphResultText).Should().NotContain("Old Entry\t99");
	}

	[Fact]
	public void Apply_TableOfContents_WithCustomSeparator_OmitsDefaultTabLeader()
	{
		using var stream = CreateDocxWithTableOfContentsField("TOC \\o \"1-3\" \\p \" -- \"");
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});

		result.HasChanges.Should().BeTrue();
		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		GetParagraphResultText(paragraphs[1]).Should().Be("Chapter One -- 2");
		GetParagraphResultText(paragraphs[2]).Should().Be("Chapter Two -- 3");
	}

	[Fact]
	public void Apply_TableOfContents_WithNoPageNumbersSwitch_OmitsPageNumbers()
	{
		using var stream = CreateDocxWithTableOfContentsField("TOC \\o \"1-3\" \\n");
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});

		result.HasChanges.Should().BeTrue();
		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		GetParagraphResultText(paragraphs[1]).Should().Be("Chapter One");
		GetParagraphResultText(paragraphs[2]).Should().Be("Chapter Two");
	}

	[Fact]
	public void Apply_TableOfContents_WithCustomStyleSwitch_IncludesMappedParagraphs()
	{
		using var stream = CreateDocxWithCustomStyleTableOfContentsField();
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});

		result.HasChanges.Should().BeTrue();
		result.UpdatedFields.Should().Contain("TOC");

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		paragraphs.Should().HaveCount(3);
		GetParagraphStyleId(paragraphs[1]).Should().Be("TOC2");
		paragraphs[1].Descendants<TabChar>().Should().ContainSingle();
		GetParagraphTextNodes(paragraphs[1]).Should().Equal(["Appendix A", "2"]);
	}

	[Fact]
	public void Apply_TableOfContents_WithHyperlinkSwitch_WrapsEntriesInBookmarkHyperlinks()
	{
		using var stream = CreateDocxWithHyperlinkedTableOfContentsField();
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});

		result.HasChanges.Should().BeTrue();
		result.UpdatedFields.Should().Contain("TOC");

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
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
		using var stream = CreateDocxWithHyperlinkedTableOfContentsFieldWithoutHeadingBookmarks();
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});

		result.HasChanges.Should().BeTrue();
		result.UpdatedFields.Should().Contain("TOC");

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
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
		using var stream = CreateDocxWithTableOfContentsFieldAndExplicitTabLeaderTemplate();
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});

		result.HasChanges.Should().BeTrue();
		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
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
		using var stream = CreateDocxWithTableOfContentsFieldAndTemplateRunFormatting();
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});

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
		using var stream = CreateDocxWithTableOfFiguresField();
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});

		result.HasChanges.Should().BeTrue();
		result.UpdatedFields.Should().Contain("TOF");

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		paragraphs.Should().HaveCount(5);
		GetParagraphStyleId(paragraphs[1]).Should().Be("TOC1");
		paragraphs[1].Descendants<TabChar>().Should().ContainSingle();
		GetParagraphTextNodes(paragraphs[1]).Should().Equal(["Figure 1. Overview", "2"]);
		GetParagraphStyleId(paragraphs[2]).Should().Be("TOC1");
		paragraphs[2].Descendants<TabChar>().Should().ContainSingle();
		GetParagraphTextNodes(paragraphs[2]).Should().Equal(["Figure 2. Details", "3"]);
		paragraphs.Select(GetParagraphResultText).Should().NotContain("Old Figure\t99");
	}

	[Fact]
	public void Apply_TableOfFigures_FromSeqFigureParagraphs_RebuildsEntryParagraphs()
	{
		using var stream = CreateDocxWithTableOfFiguresFieldAndSeqFigureParagraphs();
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});

		result.HasChanges.Should().BeTrue();
		result.UpdatedFields.Should().Contain("TOF");

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		paragraphs.Should().HaveCount(5);
		GetParagraphTextNodes(paragraphs[1]).Should().Equal(["1. Overview", "2"]);
		GetParagraphTextNodes(paragraphs[2]).Should().Equal(["2. Details", "3"]);
	}

	[Fact]
	public void Apply_SequenceFields_AssignsSequentialNumbersPerIdentifier()
	{
		using var stream = CreateDocxWithSequenceFields();
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});

		result.HasChanges.Should().BeTrue();
		result.UpdatedFields.Should().Contain("SEQ");

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		GetParagraphResultText(paragraphs[0]).Should().Be("1 caption text");
		GetParagraphResultText(paragraphs[1]).Should().Be("2 more text");
		GetParagraphResultText(paragraphs[2]).Should().Be("1 first table");
	}

	[Fact]
	public void Apply_SequenceFields_WithResetSwitch_ResetsCounterToSpecifiedValue()
	{
		using var stream = CreateDocxWithSequenceFieldsAndResetSwitch();
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});

		result.HasChanges.Should().BeTrue();
		result.UpdatedFields.Should().Contain("SEQ");

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		GetParagraphResultText(paragraphs[0]).Should().Be("1 first");
		GetParagraphResultText(paragraphs[1]).Should().Be("2 second");
		GetParagraphResultText(paragraphs[2]).Should().Be("10 reset");
		GetParagraphResultText(paragraphs[3]).Should().Be("11 after reset");
	}

	[Fact]
	public void Apply_SequenceFields_WithHiddenSwitch_IncrementsButHidesResult()
	{
		using var stream = CreateDocxWithSequenceFieldsAndHiddenSwitch();
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions()
		});

		result.HasChanges.Should().BeTrue();
		result.UpdatedFields.Should().Contain("SEQ");

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		GetParagraphResultText(paragraphs[0]).Should().Be("1 visible");
		GetParagraphResultText(paragraphs[1]).Should().Be(" hidden counter");
		GetParagraphResultText(paragraphs[2]).Should().Be("3 skipped two");
	}

	[Fact]
	public void Apply_SequenceFields_WhenDisabled_DoesNotUpdateSeqValues()
	{
		using var stream = CreateDocxWithSequenceFields();
		using var doc = DocxDocument.Load(stream);
		var blocks = DocumentBlockParser.Parse(doc.DocumentBody);
		var layoutBlocks = DocumentLayoutEngine.MeasureBlocks(blocks);
		var pages = PageBuilder.PaginateDocument(layoutBlocks, new SectionInfo());

		var result = FieldUpdateEngine.Apply(doc, blocks, pages, new RenderOptions
		{
			FieldUpdate = new FieldUpdateOptions { UpdateSequenceFields = false }
		});

		result.UpdatedFields.Should().NotContain("SEQ");

		var paragraphs = doc.DocumentBody.Elements<Paragraph>().ToArray();
		GetParagraphResultText(paragraphs[0]).Should().Be("X caption text");
	}
}
